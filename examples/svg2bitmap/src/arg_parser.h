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

#ifndef ARG_PARSE_H
#define ARG_PARSE_H

/*!
    \file arg_parser.h
    \brief Command line arguments parser, header.
    \author Matteo Muratori
    \author Michele Fabbri
*/

#include "config.h"

// forward reference
struct argparse;
struct argparse_option;

// error codes
typedef enum {
    ARG_PARSE_NO_ERROR = 0,
    ARG_PARSE_REQUIRES_VALUE_ERROR = 1,
    ARG_PARSE_NUMBER_SYNTAX_ERROR = 2,
    ARG_PARSE_NUMBER_INVALID_VALUE_ERROR = 3,
    ARG_PARSE_UNKNOWN_OPTION_ERROR = 4,
    ARG_PARSE_CALLBACK_ERROR = 5
} argparse_error;

typedef enum {
    // special
    ARGPARSE_OPT_END,
    ARGPARSE_OPT_GROUP,
    // options with no arguments
    ARGPARSE_OPT_BOOLEAN,
    // options with arguments (optional or required)
    ARGPARSE_OPT_INTEGER,
    ARGPARSE_OPT_FLOAT,
    ARGPARSE_OPT_STRING,
} argparse_option_type;

// function type that is called when corresponding argument is parsed
typedef argparse_error argparse_callback(struct argparse* self,
                                         const struct argparse_option* option);

// an option value
typedef union {
    const char* string;
    SVGTint int_number;
    SVGTfloat float_number;
    SVGTboolean bool_value;
} argparse_option_value;

typedef struct argparse_option {
    // holds the type of the option, you must have an ARGPARSE_OPT_END last in your array
    argparse_option_type type;
    // stores the value to be filled
    argparse_option_value value;
    // the character to use as a short option name, '\0' if none
    const char short_name;
    // the long option name, without the leading dash, NULL if none
    const char* long_name;
    // the short help message associated to what the option does; it must never be NULL (except for ARGPARSE_OPT_END)
    const char* help;
    // function is called when corresponding argument is parsed
    argparse_callback *callback;
    // associated data, callbacks can use it like they want
    void* data;
} argparse_option;

// the parser
typedef struct argparse {
    // user supplied
    argparse_option* options;
    const char* const* usages;
    // a description after usage
    const char* description;
    // a description at the end
    const char* epilog;
    // internal context
    int argc;
    const char** argv;
    // current option value
    const char* optvalue;
    // current option
    const char* current_option;
} argparse;

// built-in option macros
#define OPT_END()        { ARGPARSE_OPT_END,     { 0 }, '\0', NULL, "", NULL, NULL }
#define OPT_BOOLEAN(...) { ARGPARSE_OPT_BOOLEAN, { 0 }, __VA_ARGS__ }
#define OPT_INTEGER(...) { ARGPARSE_OPT_INTEGER, { 0 }, __VA_ARGS__ }
#define OPT_FLOAT(...)   { ARGPARSE_OPT_FLOAT,   { 0 }, __VA_ARGS__ }
#define OPT_STRING(...)  { ARGPARSE_OPT_STRING,  { 0 }, __VA_ARGS__ }
#define OPT_GROUP(h)     { ARGPARSE_OPT_GROUP,   { 0 }, '\0', NULL, "", NULL, NULL }

// parse a float number; return ARG_PARSE_NO_ERROR if a valid conversion was possible, else ARG_PARSE_NUMBER_SYNTAX_ERROR
argparse_error argparse_float(const char* str,
                              SVGTfloat* v);

// parse an integer number; return ARG_PARSE_NO_ERROR if a valid conversion was possible, else ARG_PARSE_NUMBER_SYNTAX_ERROR
argparse_error argparse_int(const char* str,
                            SVGTint* v);

// initialize the parser
void argparse_init(argparse* self,
                   argparse_option* options,
                   const char* const* usages);

void argparse_describe(argparse* self,
                       const char* description,
                       const char* epilog);

// perform the real arguments parsing
argparse_error argparse_parse(argparse* self,
                              int argc,
                              const char** argv);

void argparse_usage(argparse* self);

#endif  /* ARG_PARSE_H */
