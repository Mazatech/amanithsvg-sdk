/****************************************************************************
** Copyright (c) 2013-2023 Mazatech S.r.l.
** All rights reserved.
**
** This file is part of AmanithSVG software, an SVG rendering library.
**
** Redistribution and use in source and binary forms, with or without
** modification, are permitted (subject to the limitations in the disclaimer
** below) provided that the following conditions are met:
**
** - Redistributions of source code must retain the above copyright notice,
**   this list of conditions and the following disclaimer.
**
** - Redistributions in binary form must reproduce the above copyright notice,
**   this list of conditions and the following disclaimer in the documentation
**   and/or other materials provided with the distribution.
**
** - Neither the name of Mazatech S.r.l. nor the names of its contributors
**   may be used to endorse or promote products derived from this software
**   without specific prior written permission.
**
** NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED
** BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
** CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT
** NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
** A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER
** OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
** EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
** PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
** OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
** WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
** OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
** ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
**
** For any information, please contact info@mazatech.com
**
****************************************************************************/

/*!
    \file arg_parser.c
    \brief Command line arguments parser, implementation.
    \author Matteo Muratori
    \author Michele Fabbri
*/

#include "arg_parser.h"
#include "str_utils.h"

static const char* prefix_skip(const char* str,
                               const char* prefix) {

    const size_t len = strlen(prefix);
    return strncmp(str, prefix, len) ? NULL : (str + len);
}

// parse a float number; return ARG_PARSE_NO_ERROR if a valid conversion was possible, else ARG_PARSE_NUMBER_SYNTAX_ERROR
argparse_error argparse_float(const char* str,
                              SVGTfloat* v) {

    argparse_error err = ARG_PARSE_REQUIRES_VALUE_ERROR;

    if ((str != NULL) && (*str != '\0')) {
        char* endPtr;
        *v = (SVGTfloat)strtod(str, &endPtr);
        err = ((*endPtr == '\0') || (isSpace(*endPtr))) ? ARG_PARSE_NO_ERROR : ARG_PARSE_NUMBER_SYNTAX_ERROR;
    }

    return err;
}

// parse an integer number; return ARG_PARSE_NO_ERROR if a valid conversion was possible, else ARG_PARSE_NUMBER_SYNTAX_ERROR
argparse_error argparse_int(const char* str,
                            SVGTint* v) {

    SVGTfloat f = 0.0f;
    argparse_error err = argparse_float(str, &f);

    *v = (SVGTint)f;
    return err;
}

static argparse_error argparse_value_get(argparse* self,
                                         argparse_option* opt,
                                         const SVGTboolean isShortOpt) {

    argparse_error err = ARG_PARSE_NO_ERROR;

    switch (opt->type) {

        case ARGPARSE_OPT_BOOLEAN:
            opt->value.bool_value = SVGT_TRUE;
            break;

        case ARGPARSE_OPT_STRING:
            if ((self->optvalue != NULL) && (*self->optvalue != '\0')) {
                //*(const char **)opt->value = self->optvalue;
                opt->value.string = self->optvalue;
            }
            else
            if ((self->argc > 1) && isShortOpt) {
                // NB: if a short option requires an option argument, the option is separated by a space from the option argument, e.g. -n 10
                self->argc--;
                self->argv++;
                if ((*self->argv != NULL) && (*(*self->argv) != '\0')) {
                    opt->value.string = *self->argv;
                }
                else {
                    err = ARG_PARSE_REQUIRES_VALUE_ERROR;
                }
            }
            else {
                err = ARG_PARSE_REQUIRES_VALUE_ERROR;
            }
            break;

        case ARGPARSE_OPT_INTEGER:
            if ((self->optvalue != NULL) && (*self->optvalue != '\0')) {
                // do the real parsing
                SVGTint v = 0;
                if ((err = argparse_int(self->optvalue, &v)) == ARG_PARSE_NO_ERROR) {
                    opt->value.int_number = v;
                }
            }
            else
            if ((self->argc > 1) && isShortOpt) {
                // do the real parsing
                SVGTint v = 0;
                // NB: if a short option requires an option argument, the option is separated by a space from the option argument, e.g. -n 10
                self->argc--;
                self->argv++;
                if ((err = argparse_int(*self->argv, &v)) == ARG_PARSE_NO_ERROR) {
                    //*(SVGTint*)opt->value = v;
                    opt->value.int_number = v;
                }
            }
            else {
                err = ARG_PARSE_REQUIRES_VALUE_ERROR;
            }
            break;

        case ARGPARSE_OPT_FLOAT:
            if ((self->optvalue != NULL) && (*self->optvalue != '\0')) {
                // do the real parsing
                SVGTfloat v = 0.0f;
                if ((err = argparse_float(self->optvalue, &v)) == ARG_PARSE_NO_ERROR) {
                    opt->value.float_number = v;
                }
            }
            else
            if ((self->argc > 1) && isShortOpt) {
                // do the real parsing
                SVGTfloat v = 0.0f;
                // NB: if a short option requires an option argument, the option is separated by a space from the option argument, e.g. -n 10
                self->argc--;
                self->argv++;
                if ((err = argparse_float(*self->argv, &v)) == ARG_PARSE_NO_ERROR) {
                    opt->value.float_number = v;
                }
            }
            else {
                err = ARG_PARSE_REQUIRES_VALUE_ERROR;
            }
            break;

        default:
            break;
    }

    // invoke the callback, if any
    if ((err == ARG_PARSE_NO_ERROR) && (opt->callback != NULL)) {
        err = opt->callback(self, opt);
    }

    return err;
}

static argparse_error argparse_short_opt(argparse* self,
                                         argparse_option* options) {

    argparse_error err = ARG_PARSE_UNKNOWN_OPTION_ERROR;

    for (; options->type != ARGPARSE_OPT_END; options++) {
        if (options->short_name == *self->optvalue) {
            // NB: if a short option requires an option argument, the option is separated by a space from the option argument, e.g. -n 10
            self->optvalue = self->optvalue[1] ? (self->optvalue + 1) : NULL;
            // argparse_getvalue will inspect the next argument in order to get the value
            err = argparse_value_get(self, options, SVGT_TRUE);
            break;
        }
    }

    return err;
}

static argparse_error argparse_long_opt(argparse* self,
                                        argparse_option* options) {

    argparse_error err = ARG_PARSE_UNKNOWN_OPTION_ERROR;

    for (; options->type != ARGPARSE_OPT_END; options++) {

        if (options->long_name != NULL) {

            // return NULL if option prefix does not match
            const char* rest = prefix_skip(self->argv[0] + 2, options->long_name);

            // option prefix matches
            if (rest != NULL) {
                // boolean options do not require values
                if ((options->type == ARGPARSE_OPT_BOOLEAN) && (*rest == '\0')) {
                    err = argparse_value_get(self, options, SVGT_FALSE);
                }
                else {
                    // option prefix matches and next char is '='
                    // NB: if a long option requires an option argument, the option is separated by a = from the option argument, e.g. --lines=10
                    if (*rest == '=') {
                        self->optvalue = rest + 1;
                        err = argparse_value_get(self, options, SVGT_FALSE);
                        break;
                    }
                }
            }
            // - if 'rest' is NULL, it means that option prefix does not match
            // - if 'rest' is not NULL and the next char is not '=', it means that a valid option prefix has been
            //   provided, but the actual whole option is not valid (e.g. --width vs. --width-squared)
        }
    }

    return err;
}

// initialize the parser
void argparse_init(argparse* self,
                   argparse_option* options,
                   const char* const* usages) {

    (void)memset(self, 0, sizeof(*self));
    self->options = options;
    self->usages = usages;
    self->description = NULL;
    self->epilog = NULL;
    self->optvalue = NULL;
    self->current_option = NULL;
}

void argparse_describe(argparse* self,
                       const char* description,
                       const char* epilog) {

    self->description = description;
    self->epilog = epilog;
}

// perform the real arguments parsing
argparse_error argparse_parse(argparse* self,
                              int argc,
                              const char** argv) {

    argparse_error err = ARG_PARSE_NO_ERROR;

    self->argc = argc - 1;
    self->argv = argv + 1;

    for (; self->argc && (err == ARG_PARSE_NO_ERROR); self->argc--, self->argv++) {

        const char* arg = self->argv[0];

        // keep track of current option
        self->current_option = arg;

        // skip arguments that does not begin with '-', or consisting of a single '-'
        if ((arg[0] == '-') && (arg[1] != '\0')) {
            // short option
            if (arg[1] != '-') {
                self->optvalue = arg + 1;
                err = argparse_short_opt(self, self->options);
            }
            else {
                // long option
                if (arg[2] != '\0') {
                    err = argparse_long_opt(self, self->options);
                }
                else {
                    // skip '--'
                    self->argc--;
                    self->argv++;
                }
            }
        }
    }

    return err;
}

void argparse_usage(argparse *self) {

    size_t len;
    size_t usage_opts_width = 0;
    const argparse_option* options;

    // print description
    if (self->description) {
        fprintf(OUTPUT_STREAM, "%s\n", self->description);
    }

    if (self->usages) {
        fprintf(OUTPUT_STREAM, "\nUsage: %s\n", *self->usages++);
        while (*self->usages && **self->usages) {
            fprintf(OUTPUT_STREAM, "   or: %s\n", *self->usages++);
        }
    }

    // figure out best width
    usage_opts_width = 0U;
    options = self->options;
    for (; options->type != ARGPARSE_OPT_END; options++) {
        len = 0U;
        if ((options)->short_name) {
            len += 2;
        }
        if ((options)->short_name && (options)->long_name) {
            len += 2;           // separator ", "
        }
        if ((options)->long_name) {
            len += strlen((options)->long_name) + 2;
        }
        if (options->type == ARGPARSE_OPT_INTEGER) {
            len += strlen("=<int>");
        }
        if (options->type == ARGPARSE_OPT_FLOAT) {
            len += strlen("=<flt>");
        }
        else
        if (options->type == ARGPARSE_OPT_STRING) {
            len += strlen("=<str>");
        }
        len = (len + 3) - ((len + 3) & 3);
        if (usage_opts_width < len) {
            usage_opts_width = len;
        }
    }
    usage_opts_width += 4;      // 4 spaces prefix

    options = self->options;
    for (; options->type != ARGPARSE_OPT_END; options++) {
        size_t pos = 0U;
        size_t pad = 0U;
        if (options->type == ARGPARSE_OPT_GROUP) {
            fputc('\n', OUTPUT_STREAM);
            fprintf(OUTPUT_STREAM, "%s", options->help);
            fputc('\n', OUTPUT_STREAM);
            continue;
        }
        pos = fprintf(OUTPUT_STREAM, "    ");
        if (options->short_name) {
            pos += fprintf(OUTPUT_STREAM, "-%c", options->short_name);
        }
        if (options->long_name && options->short_name) {
            pos += fprintf(OUTPUT_STREAM, ", ");
        }
        if (options->long_name) {
            pos += fprintf(OUTPUT_STREAM, "--%s", options->long_name);
        }
        if (options->type == ARGPARSE_OPT_INTEGER) {
            pos += fprintf(OUTPUT_STREAM, "=<int>");
        }
        else
        if (options->type == ARGPARSE_OPT_FLOAT) {
            pos += fprintf(OUTPUT_STREAM, "=<flt>");
        }
        else
        if (options->type == ARGPARSE_OPT_STRING) {
            pos += fprintf(OUTPUT_STREAM, "=<str>");
        }
        if (pos <= usage_opts_width) {
            pad = usage_opts_width - pos;
        }
        else {
            fputc('\n', OUTPUT_STREAM);
            pad = usage_opts_width;
        }
        fprintf(OUTPUT_STREAM, "%*s%s\n", (int)pad + 2, "", options->help);
    }

    // print epilog
    if (self->epilog) {
        fprintf(OUTPUT_STREAM, "%s\n", self->epilog);
    }
}
