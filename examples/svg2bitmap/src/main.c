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
    \file main.c
    \brief An utility to convert SVG files to bitmaps.
    \author Matteo Muratori
    \author Michele Fabbri
*/

#include "arg_parser.h"
#include "file_utils.h"
#include "str_utils.h"
#include "rendering.h"
#include <limits.h>
#if defined(_DEBUG)
    #include <assert.h>
#endif

/************************************************************
                      AmanithSVG setup
************************************************************/

// load a font file in memory, returning the allocated buffer and its size in bytes
static SVGTubyte* loadFont(const char* fileName,
                           size_t* fileSize) {

    // no additional pad
    return loadFile(fileName, 0U, fileSize);
}

// load an image file in memory, returning the allocated buffer and its size in bytes
static SVGTubyte* loadImage(const char* fileName,
                            size_t* fileSize) {

    // no additional pad
    return loadFile(fileName, 0U, fileSize);
}

// check if the given file extension corresponds to a supported font format
static SVGTboolean fontSupported(const FileName* f) {

    FileName ext = { { '\0' } };

    // extract file extension (e.g. /data/fonts/arial.ttf --> ttf)
    extractFileExt(ext.name, f->name);

    // check supported vector fonts formats
    return ((strCaseCmp(ext.name, "otf") == 0) ||
            (strCaseCmp(ext.name, "ttf") == 0) ||
            (strCaseCmp(ext.name, "woff") == 0) ||
            (strCaseCmp(ext.name, "woff2") == 0)) ? SVGT_TRUE : SVGT_FALSE;
}

// check if the given file extension corresponds to a supported image format
static SVGTboolean imageSupported(const FileName* f) {

    FileName ext = { { '\0'} };

    // extract file extension (e.g. /data/images/pattern.png --> png)
    extractFileExt(ext.name, f->name);

    // check supported image formats
    return ((strCaseCmp(ext.name, "jpg") == 0) ||
            (strCaseCmp(ext.name, "jpeg") == 0) ||
            (strCaseCmp(ext.name, "png") == 0)) ? SVGT_TRUE : SVGT_FALSE;
}

// load font resources
static SVGTboolean resourcesLoad(ExternalResourceDynArray* resources,
                                 const CommandArguments* args,
                                 const SVGTResourceType type) {

    SVGTboolean ok = SVGT_TRUE;
    const char* directory = (type == SVGT_RESOURCE_TYPE_FONT) ? args->fontsDir.path : args->imagesDir.path;

    // skip the process if an empty directory has been provided
    if (*directory != '\0') {

        FileSearchResult searchResult;
        // setup search settings (no recursive scan, add path to returned file names, no filter on extension)
        const FileSearchSettings searchSettings = { SVGT_FALSE, SVGT_TRUE, NULL };

        // initialize temporary array
        DYNARRAY_INIT(searchResult.fileNames)

        // scan the path
        if ((ok = scanPath(&searchResult, &searchSettings, directory))) {

            // initialize output array
            DYNARRAY_INIT(*resources)

            // loop over font files
            for (size_t i = 0U; i < searchResult.fileNames.size; ++i) {

                const FileName* f = &searchResult.fileNames.data[i];

                // check for supported font / image formats
                if (((type == SVGT_RESOURCE_TYPE_FONT) && fontSupported(f)) || ((type == SVGT_RESOURCE_TYPE_IMAGE) && imageSupported(f))) {
                    // load the font file into memory
                    size_t bufferSize = 0U;
                    const SVGTubyte* buffer = (type == SVGT_RESOURCE_TYPE_FONT) ? loadFont(f->name, &bufferSize) : loadImage(f->name, &bufferSize);
                    // if file was loaded...
                    if ((buffer != NULL) && (bufferSize > 0U)) {
                        // ...keep track of it
                        const ExternalResource resource = { *f, buffer, bufferSize, 0 };
                        DYNARRAY_PUSH_BACK(*resources, ExternalResource, resource)
                    }
                    else {
                        LOG_WARNING_EXT("\nunable to load %s file\n", f->name);
                    }
                }
            }

            for (size_t i = 0U; i < resources->size; ++i) {

                SVGTErrorCode err;
                FileName id = { { '\0'} };
                SVGTbitfield hints = 0U;
                const ExternalResource* r = &resources->data[i];

                // extract filename (e.g. /data/fonts/arial.ttf --> arial.ttf)
                extractFileName(id.name, r->fileName.name, SVGT_TRUE);

                if (type == SVGT_RESOURCE_TYPE_FONT) {
                    // TO DO derive hints according to arguments
                    (void)args;
                }
            
                // provide the resource to AmanithSVG (id, in-memory file, resource type, hints)
                if ((err = svgtResourceSet(id.name, r->buffer, (SVGTuint)r->bufferSize, type, hints)) != SVGT_NO_ERROR) {
                    LOG_WARNING_EXT("\nfailed to set resource %s (AmanithSVG error code = %d), still go ahead\n", id.name, err);
                }
            }
        }

        // destroy temporary array
        DYNARRAY_DESTROY(searchResult.fileNames)
    }

    return ok;
}

// initialize AmanithSVG and load external resources (fonts and images)
static SVGTboolean amanithsvgInit(const CommandArguments* args,
                                  ExternalResourceDynArray* fontResources,
                                  ExternalResourceDynArray* imageResources) {

    SVGTErrorCode err;
    // initialize AmanithSVG library
    SVGTboolean ok = svgtInit(args->screenWidth, args->screenHeight, args->dpi) == SVGT_NO_ERROR;

    if (ok) {

        // set user-agent language
        if ((args->language != NULL) && (*args->language != '\0')) {
            if ((err = svgtLanguageSet(args->language)) != SVGT_NO_ERROR) {
                LOG_WARNING_EXT("\nfailed to set language (AmanithSVG error code = %d), still go ahead\n", err);
            }
        }

        // set rendering quality
        if ((err = svgtConfigSet(SVGT_CONFIG_CURVES_QUALITY, (SVGTfloat)args->renderingQuality)) != SVGT_NO_ERROR) {
            LOG_WARNING_EXT("\nfailed to set rendering quality (AmanithSVG error code = %d), still go ahead\n", err);
        }

        // load external fonts
        if ((ok = resourcesLoad(fontResources, args, SVGT_RESOURCE_TYPE_FONT))) {
            if (fontResources->size > 0U) {
                LOG_INFO_EXT("- %d fonts were found and loaded successfully\n", (SVGTuint)fontResources->size);
            }
            // load external images
            if ((ok = resourcesLoad(imageResources, args, SVGT_RESOURCE_TYPE_IMAGE))) {
                if (imageResources->size > 0U) {
                    LOG_INFO_EXT("- %d images were found and loaded successfully\n", (SVGTuint)imageResources->size);
                }
                // enable log buffer to keep rack of errors
                if (args->logBuffer != NULL) {
                    (void)svgtLogBufferSet(args->logBuffer, args->logBufferCapacity, SVGT_LOG_LEVEL_ERROR);
                }

                // we have finished
                LOG_INFO("- AmanithSVG library and external resources have been properly loaded and initialized\n");
            }
            else {
                LOG_ERROR("\nunable to scan images path (maybe it is not readable or you don't have the necessary permissions)\n");
            }
        }
        else {
            LOG_ERROR("\nunable to scan fonts path (maybe it is not readable or you don't have the necessary permissions)\n");
        }
    }
    else {
        LOG_ERROR("\nunable to initialize AmanithSVG library\n");
    }

    return ok;
}

// release AmanithSVG and external resources (fonts and images)
static void amanithsvgDestroy(const CommandArguments* args,
                              ExternalResourceDynArray* fontResources,
                              ExternalResourceDynArray* imageResources) {

    // release AmantihSVG library
    svgtDone();

    // release in-memory fonts
    for (size_t i = 0U; i < fontResources->size; ++i) {
        free((void*)fontResources->data[i].buffer);
    }
    DYNARRAY_DESTROY(*fontResources)

    // release in-memory images
    for (size_t i = 0U; i < imageResources->size; ++i) {
        free((void*)imageResources->data[i].buffer);
    }
    DYNARRAY_DESTROY(*imageResources)

    LOG_INFO("- Release AmanithSVG library and external resources\n");
}

/************************************************************
                      arguments parser
************************************************************/

// parse the given color string, in the format #RRGGBBAA
static SVGTboolean parseColor(const char* str,
                              SVGTfloat rgba[4]) {

    SVGTboolean ok = SVGT_FALSE;
    // copy string into local variables
    const char* s = str;
    size_t sLen = strlen(str);

    // skip initial spaces
    s = skipSpaces(s, &sLen);
    if (s[0] == '#') {
        // skip '#'
        s++;
        sLen--;
        // now parse RRGGBBAA
        if (sLen == 8) {
            const char hex[9] = { s[0], s[1], s[2], s[3], s[4], s[5], s[6], s[7], '\0' };
            if (isHexDigit(hex[0]) && isHexDigit(hex[1]) &&
                isHexDigit(hex[2]) && isHexDigit(hex[3]) &&
                isHexDigit(hex[4]) && isHexDigit(hex[5]) &&
                isHexDigit(hex[6]) && isHexDigit(hex[7])) {
                // parse hex value
                const SVGTuint col = axtoi(hex);
                // map each component from [0; 255] --> [0; 1]
                rgba[0] = (SVGTfloat)(col >> 24) / 255.0f;
                rgba[1] = (SVGTfloat)((col >> 16) & 0xFFU) / 255.0f;
                rgba[2] = (SVGTfloat)((col >> 8) & 0xFFU) / 255.0f;
                rgba[3] = (SVGTfloat)(col & 0xFFU) / 255.0f;
                ok = SVGT_TRUE;
            }
        }
    }

    return ok;
}

// check the validity (i.e. the existence) of the given path
static argparse_error checkParsedPath(const char* path,
                                      char* fixedPath) {

    // copy path
    strcpy(fixedPath, path);
    // ensure that every slash present within the path, will be set according to the current operating system type
    fixPath(fixedPath, SVGT_TRUE);
    // path must exist
    return directoryExists(fixedPath) ? ARG_PARSE_NO_ERROR : ARG_PARSE_CALLBACK_ERROR;
}

// check the validity of a parsed integer number
static argparse_error checkParsedInteger(const SVGTint v,
                                         const SVGTboolean allowNegative,
                                         const SVGTint low,
                                         const SVGTint high) {

    const SVGTboolean signOk = ((!allowNegative) && (v < 0)) ? SVGT_FALSE : SVGT_TRUE;
    const SVGTboolean rangeOk = ((v < low) || (v > high)) ? SVGT_FALSE : SVGT_TRUE;
    return (signOk && rangeOk) ? ARG_PARSE_NO_ERROR : ARG_PARSE_CALLBACK_ERROR;
}

// callback for showing help
static argparse_error cbHelp(argparse* self,
                             const argparse_option* option) {

    (void)option;

    // print usage
    argparse_usage(self);

    // exit program
    exit(EXIT_SUCCESS);

    return ARG_PARSE_NO_ERROR;
}

// callback for setting input path/file
static argparse_error cbInput(argparse* self,
                              const argparse_option* option) {

    argparse_error err = ARG_PARSE_NO_ERROR;
    CommandArguments* args = (CommandArguments*)option->data;
    const char* input = option->value.string;
#if defined(_DEBUG)
    assert((input != NULL) && (*input != '\0'));
#endif

    (void)self;

    // copy input path
    strcpy(args->inputDir.name, input);
    // ensure that every slash present within the path, will be set according to the current operating system type
    fixPath(args->inputDir.name, SVGT_FALSE);
    // input path/file must exist
    if ((!directoryExists(args->inputDir.name)) && (!fileExists(args->inputDir.name))) {
        LOG_ERROR_EXT("\ninput path/file '%s' does not exist or it is not readable\n\n", input);
        err = ARG_PARSE_CALLBACK_ERROR;
    }
    else {
        // if the given 'inputPath' is a regular file, check its extension (it must be "svg")
        if (fileExists(args->inputDir.name)) {
            char fileExt[_MAX_EXT];
            // extract file extension
            extractFileExt(fileExt, args->inputDir.name);
            // it must be "svg"
            if (strCaseCmp(fileExt, "svg") != 0) {
                LOG_ERROR_EXT("\ninput file '%s' must have a .svg extension\n\n", input);
                err = ARG_PARSE_CALLBACK_ERROR;
            }
        }
        else {
            // if the given 'inputPath' is a directory, ensure a final delimiter
            fixPath(args->inputDir.name, SVGT_TRUE);
        }
    }

    return err;
}

// callback for setting output path
static argparse_error cbOutputPath(argparse* self,
                                   const argparse_option* option) {

    argparse_error err;
    CommandArguments* args = (CommandArguments*)option->data;
    const char* outputDir = option->value.string;
#if defined(_DEBUG)
    assert((outputDir != NULL) && (*outputDir != '\0'));
#endif

    (void)self;

    // check path existence
    if ((err = checkParsedPath(outputDir, args->outputDir.path)) != ARG_PARSE_NO_ERROR) {
        LOG_ERROR_EXT("\noutput path '%s' does not exist\n\n", outputDir);
    }

    return err;
}

// callback for setting fonts path
static argparse_error cbFontsPath(argparse* self,
                                  const argparse_option* option) {

    argparse_error err;
    CommandArguments* args = (CommandArguments*)option->data;
    const char* fontsDir = option->value.string;
#if defined(_DEBUG)
    assert((fontsDir != NULL) && (*fontsDir != '\0'));
#endif

    (void)self;

    // check path existence
    if ((err = checkParsedPath(fontsDir, args->fontsDir.path)) != ARG_PARSE_NO_ERROR) {
        LOG_ERROR_EXT("\nfonts path '%s' does not exist\n\n", fontsDir);
    }

    return err;
}

// callback for setting images path
static argparse_error cbImagesPath(argparse* self,
                                   const argparse_option* option) {

    argparse_error err;
    CommandArguments* args = (CommandArguments*)option->data;
    const char* imagesDir = option->value.string;
#if defined(_DEBUG)
    assert((imagesDir != NULL) && (*imagesDir != '\0'));
#endif

    (void)self;

    // check path existence
    if ((err = checkParsedPath(imagesDir, args->imagesDir.path)) != ARG_PARSE_NO_ERROR) {
        LOG_ERROR_EXT("\nimages path '%s' does not exist\n\n", imagesDir);
    }

    return err;
}

// callback for setting dpi resolution
static argparse_error cbDpi(argparse* self,
                            const argparse_option* option) {

    CommandArguments* args = (CommandArguments*)option->data;
    argparse_error err = (option->value.float_number > 0.0f) ? ARG_PARSE_NO_ERROR : ARG_PARSE_CALLBACK_ERROR;

    (void)self;

    if (err != ARG_PARSE_NO_ERROR) {
        LOG_ERROR("\ndpi must be a positive number, greater than 0\n\n");
    }
    else {
        // copy the value
        args->dpi = option->value.float_number;
    }

    return err;
}

// callback for setting user-agent language
static argparse_error cbLanguage(argparse* self,
                                 const argparse_option* option) {

    CommandArguments* args = (CommandArguments*)option->data;
    const char* lang = option->value.string;
#if defined(_DEBUG)
    assert((lang != NULL) && (*lang != '\0'));
#endif

    (void)self;

    // copy language
    args->language = lang;

    return ARG_PARSE_NO_ERROR;
}

// callback for setting clear/background color
static argparse_error cbClearColor(argparse* self,
                                   const argparse_option* option) {

    argparse_error err = ARG_PARSE_NO_ERROR;
    CommandArguments* args = (CommandArguments*)option->data;
    const char* color = option->value.string;
#if defined(_DEBUG)
    assert((color != NULL) && (*color != '\0'));
#endif

    (void)self;

    // parse color string
    if (!parseColor(color, args->clearColor)) {
        LOG_ERROR("\ninvalid color syntax, it must be expressed as #RRGGBBAA (e.g. #A08355FF)\n\n");
        err = ARG_PARSE_CALLBACK_ERROR;
    }

    return err;
}

// callback for setting output width, in pixels
static argparse_error cbOutputWidth(argparse* self,
                                    const argparse_option* option) {

    argparse_error err;
    CommandArguments* args = (CommandArguments*)option->data;

    (void)self;

    if (option->value.int_number == 0) {
        LOG_ERROR("\noutput width cannot be zero\n\n");
        err = ARG_PARSE_CALLBACK_ERROR;
    }
    else {
        args->width = option->value.int_number;
        err = ARG_PARSE_NO_ERROR;
    }

    return err;
}

// callback for setting output height, in pixels
static argparse_error cbOutputHeight(argparse* self,
                                     const argparse_option* option) {

    argparse_error err;
    CommandArguments* args = (CommandArguments*)option->data;

    (void)self;

    if (option->value.int_number == 0) {
        LOG_ERROR("\noutput height cannot be zero\n\n");
        err = ARG_PARSE_CALLBACK_ERROR;
    }
    else {
        args->height = option->value.int_number;
        err = ARG_PARSE_NO_ERROR;
    }

    return err;
}

// check the validity of a parsed screen dimension
static argparse_error checkScreenDimension(argparse* self,
                                           const argparse_option* option,
                                           SVGTuint* output) {

    CommandArguments* args = (CommandArguments*)option->data;
    argparse_error err = checkParsedInteger(option->value.int_number, SVGT_FALSE, 0, INT_MAX);

    (void)self;

    if (err != ARG_PARSE_NO_ERROR) {
        LOG_ERROR("\nscreen dimensions must be positive numbers\n\n");
    }
    else {
        // copy the value
        *output = (SVGTuint)option->value.int_number;
    }

    return err;
}

// callback for setting screen width
static argparse_error cbScreenWidth(argparse* self,
                                    const argparse_option* option) {

    return checkScreenDimension(self, option, &(((CommandArguments*)option->data)->screenWidth));
}

// callback for setting screen height
static argparse_error cbScreenHeight(argparse* self,
                                     const argparse_option* option) {

    return checkScreenDimension(self, option, &(((CommandArguments*)option->data)->screenHeight));
}

// callback for setting additional scale
static argparse_error cbScale(argparse* self,
                              const argparse_option* option) {

    CommandArguments* args = (CommandArguments*)option->data;
    argparse_error err = (option->value.float_number > 0.0f) ? ARG_PARSE_NO_ERROR : ARG_PARSE_CALLBACK_ERROR;

    (void)self;

    if (err != ARG_PARSE_NO_ERROR) {
        LOG_ERROR("\nscale must be a positive number greater than 0\n\n");
    }
    else {
        // copy the value
        args->scale = option->value.float_number;
    }

    return err;
}

// callback for setting rendering quality
static argparse_error cbRenderingQuality(argparse* self,
                                         const argparse_option* option) {

    CommandArguments* args = (CommandArguments*)option->data;
    argparse_error err = checkParsedInteger(option->value.int_number, SVGT_FALSE, 1, 100);

    (void)self;

    if (err != ARG_PARSE_NO_ERROR) {
        LOG_ERROR("\nrendering quality must be a positive number between 1 and 100\n\n");
    }
    else {
        // copy the value
        args->renderingQuality = (SVGTuint)option->value.int_number;
    }

    return err;
}

// callback for setting the post-rendering filter
static argparse_error cbFilter(argparse* self,
                               const argparse_option* option) {

    argparse_error err = ARG_PARSE_NO_ERROR;
    CommandArguments* args = (CommandArguments*)option->data;
    const char* flt = option->value.string;
#if defined(_DEBUG)
    assert((flt != NULL) && (*flt != '\0'));
#endif

    (void)self;

    if (strCaseCmp(flt, "none") == 0) {
        args->filter = FILTER_NONE;
    }
    else
    if (strCaseCmp(flt, "dilate") == 0) {
        args->filter = FILTER_DILATE;
    }
    else {
        LOG_ERROR_EXT("\ninvalid value for post-rendering filter: %s (valid values are 'none' and 'dilate')\n\n", flt);
        err = ARG_PARSE_CALLBACK_ERROR;
    }

    return err;
}

// callback for setting the pixel format
static argparse_error cbPixelFormat(argparse* self,
                                    const argparse_option* option) {

    argparse_error err = ARG_PARSE_NO_ERROR;
    CommandArguments* args = (CommandArguments*)option->data;
    const char* fmt = option->value.string;
#if defined(_DEBUG)
    assert((fmt != NULL) && (*fmt != '\0'));
#endif

    (void)self;

    if (strCaseCmp(fmt, "rgba") == 0) {
        args->pixelFormat = FORMAT_RGBA;
    }
    else
    if (strCaseCmp(fmt, "bgra") == 0) {
        args->pixelFormat = FORMAT_BGRA;
    }
    else {
        LOG_ERROR_EXT("\ninvalid value for pixel format: %s (valid values are 'rgba' and 'bgra')\n\n", fmt);
        err = ARG_PARSE_CALLBACK_ERROR;
    }

    return err;
}

// callback for setting compression level
static argparse_error cbCompressionLevel(argparse* self,
                                         const argparse_option* option) {

    CommandArguments* args = (CommandArguments*)option->data;
    argparse_error err = checkParsedInteger(option->value.int_number, SVGT_FALSE, 0, 9);

    (void)self;

    if (err != ARG_PARSE_NO_ERROR) {
        LOG_ERROR("\ncompression level must be a number between 0 and 9\n\n");
    }
    else {
        // copy the value
        args->compressionLevel = (SVGTuint)option->value.int_number;
    }

    return err;
}

// callback for setting quiet mode
static argparse_error cbQuiet(argparse* self,
                              const argparse_option* option) {

    ((CommandArguments*)option->data)->quiet = option->value.bool_value;

    (void)self;

    return ARG_PARSE_NO_ERROR;
}

// callback for setting atlas mode and the relative parameters
static argparse_error cbAtlasMode(argparse* self,
                                  const argparse_option* option) {

    SVGTuint i = 0U;
    argparse_error err = ARG_PARSE_NO_ERROR;
    const char* params = option->value.string;
    size_t len = strlen(option->value.string);
    // arguments structure, whose fields will be written
    CommandArguments* args = (CommandArguments*)option->data;
    // start as "unassigned" values
    SVGTint atlasBorder = -1;
    SVGTint atlasPow2 = -1;

    (void)self;

    // we must read three parameters: <max dimensions>, <pad between each sprite>, <pow2 / npot>
    while ((*params != '\0') && (i < 3U) && (err == ARG_PARSE_NO_ERROR)) {

        const char* valueStart = skipSpaces(params, &len);
        char* valueEnd = (char*)skipUntilChar(valueStart, &len, ',');

        if (*valueEnd == ',') {
            // overwrite comma with a '\0', so that 'valueStart' now points to a null-termined string
            *valueEnd = '\0';
            // skip comma
            valueEnd++;
        }

        switch (i) {
            case 0:
                // maximum dimension of generated PNG bitmap/textures, in pixels
                // NB: a negative (or zero) value will cause the value to be taken directly from AmanithSVG
                if ((err = argparse_int(valueStart, &args->atlasMaxDimension)) != ARG_PARSE_NO_ERROR) {
                    LOG_ERROR_EXT("\nmalformed value for atlas maximum dimension: %s", valueStart);
                }
                else {
                    // get maximum allowed value from AmanithSVG
                    const SVGTuint maxDimension = svgtSurfaceMaxDimension();
                    // check parsed value
                    if ((args->atlasMaxDimension > 0) && ((SVGTuint)args->atlasMaxDimension > maxDimension)) {
                        LOG_ERROR_EXT("\nthe maximum value allowed for the size of atlas is %d\n\n", maxDimension);
                        err = ARG_PARSE_CALLBACK_ERROR;
                    }
                }
                break;
            case 1:
                // pad between each sprite, in pixels
                if ((err = argparse_int(valueStart, &atlasBorder)) != ARG_PARSE_NO_ERROR) {
                    LOG_ERROR_EXT("\nmalformed value for atlas border: %s", valueStart);
                }
                else {
                    // check parsed value
                    if (atlasBorder < 0) {
                        LOG_ERROR("\natlas border must be a non-negative number");
                        err = ARG_PARSE_CALLBACK_ERROR;
                    }
                }
                break;
            case 2:
            default:
                // force PNG bitmap/textures to have power-of-two dimensions
                if (strCaseCmp(valueStart, "pow2") == 0) {
                    atlasPow2 = (SVGTint)SVGT_TRUE;
                }
                else
                if (strCaseCmp(valueStart, "npot") == 0) {
                    atlasPow2 = (SVGTint)SVGT_FALSE;
                }
                else {
                    LOG_ERROR_EXT("\ninvalid value for atlas constraint: %s (valid values are 'pow2' and 'npot')\n\n", valueStart);
                    err = ARG_PARSE_CALLBACK_ERROR;
                }
                break;
        }

        // next parameter
        i++;
        params = valueEnd;
    }

    // check that all parameters have been assigned
    if (err == ARG_PARSE_NO_ERROR) {
        if (atlasBorder < 0) {
            LOG_ERROR("\nplease provide a value for atlas border\n\n");
            err = ARG_PARSE_CALLBACK_ERROR;
        }
        else
        if (atlasPow2 < 0) {
            LOG_ERROR("\nplease provide a value for atlas constraint\n\n");
            err = ARG_PARSE_CALLBACK_ERROR;
        }
    }

    // write verified fields to the arguments structure
    if (err == ARG_PARSE_NO_ERROR) {
        args->atlasBorder = (SVGTuint)atlasBorder;
        args->atlasPow2 = (SVGTboolean)atlasPow2;
    }
    
    return err;
}

// callback for setting atlas output prefix and format
static argparse_error cbAtlasOutput(argparse* self,
                                   const argparse_option* option) {

    SVGTuint i = 0U;
    argparse_error err = ARG_PARSE_NO_ERROR;
    const char* params = option->value.string;
    size_t len = strlen(option->value.string);
    // arguments structure, whose fields will be written
    CommandArguments* args = (CommandArguments*)option->data;
    // start as "unassigned" values
    SVGTint atlasFormat = -1;
    SVGTint atlasMapFormat = -1;

    (void)self;

    // we must read two parameters: <prefix>, <data format>, <map format>
    while ((*params != '\0') && (i < 3U) && (err == ARG_PARSE_NO_ERROR)) {

        const char* valueStart = skipSpaces(params, &len);
        char* valueEnd = (char*)skipUntilChar(valueStart, &len, ',');

        if (*valueEnd == ',') {
            // overwrite comma with a '\0', so that 'valueStart' now points to a null-termined string
            *valueEnd = '\0';
            // skip comma
            valueEnd++;
        }

        switch (i) {
            case 0:
                // parse prefix
                args->atlasPrefix = valueStart;
                break;
            case 1:
                // parse data format
                if (strCaseCmp(valueStart, "xml") == 0) {
                    atlasFormat = ATLAS_XML_GENERIC_FORMAT;
                }
                else
                if (strCaseCmp(valueStart, "cocos2d") == 0) {
                    atlasFormat = ATLAS_COCOS2D_FORMAT;
                }
                else
                if (strCaseCmp(valueStart, "json-array") == 0) {
                    atlasFormat = ATLAS_JSON_ARRAY_FORMAT;
                }
                else
                if (strCaseCmp(valueStart, "json-hash") == 0) {
                    atlasFormat = ATLAS_JSON_HASH_FORMAT;
                }
                else
                if (strCaseCmp(valueStart, "phaser2") == 0) {
                    atlasFormat = ATLAS_PHASER2_FORMAT;
                }
                else
                if (strCaseCmp(valueStart, "phaser3") == 0) {
                    atlasFormat = ATLAS_PHASER3_FORMAT;
                }
                else
                if (strCaseCmp(valueStart, "pixijs") == 0) {
                    atlasFormat = ATLAS_PIXIJS_FORMAT;
                }
                else
                if (strCaseCmp(valueStart, "godot3-sheet") == 0) {
                    atlasFormat = ATLAS_GODOT3_SPRITE_SHEET_FORMAT;
                }
                else
                if (strCaseCmp(valueStart, "godot3-tset") == 0) {
                    atlasFormat = ATLAS_GODOT3_TILE_SET_FORMAT;
                }
                else
                if (strCaseCmp(valueStart, "libgdx") == 0) {
                    atlasFormat = ATLAS_LIBGDX_FORMAT;
                }
                else
                if (strCaseCmp(valueStart, "spine") == 0) {
                    atlasFormat = ATLAS_SPINE_FORMAT;
                }
                else
                if (strCaseCmp(valueStart, "code-c") == 0) {
                    atlasFormat = ATLAS_CODE_C_FORMAT;
                }
                else
                if (strCaseCmp(valueStart, "code-libgdx") == 0) {
                    atlasFormat = ATLAS_CODE_LIBGDX_FORMAT;
                }
                else {
                    LOG_ERROR_EXT("\nunrecognized atlas output format %s (valid values are: 'xml', 'cocos2d', 'json-array', 'json-hash', 'phaser2', 'phaser3', 'pixijs', 'godot3-sheet', 'godot3-tset', 'libgdx', 'spine', 'code-c', 'code-libgdx')\n\n", valueStart);
                    err = ARG_PARSE_CALLBACK_ERROR;
                }
                break;
            case 2:
            default:
                // parse map format
                if (strCaseCmp(valueStart, "array") == 0) {
                    atlasMapFormat = ATLAS_MAP_ARRAY_FORMAT;
                }
                else
                if (strCaseCmp(valueStart, "hash") == 0) {
                    atlasMapFormat = ATLAS_MAP_HASH_FORMAT;
                }
                else {
                    LOG_ERROR_EXT("\nunrecognized atlas map format %s (valid values are: 'array', 'hash')\n\n", valueStart);
                    err = ARG_PARSE_CALLBACK_ERROR;
                }
                break;
        }

        // next parameter
        i++;
        params = valueEnd;
    }

    // check that all parameters have been assigned
    if (err == ARG_PARSE_NO_ERROR) {
        if (atlasFormat < 0) {
            LOG_ERROR("\nplease provide the output format for the atlas data file\n\n");
            err = ARG_PARSE_CALLBACK_ERROR;
        }
        else
        if (atlasMapFormat < 0) {
            LOG_ERROR("\nplease provide the output format for the atlas map file\n\n");
            err = ARG_PARSE_CALLBACK_ERROR;
        }
        else {
            args->atlasFormat = (AtlasFormat)atlasFormat;
            args->atlasMapFormat = (AtlasMapFormat)atlasMapFormat;
        }
    }

    return err;
}

// callback for adding an SVG to the atlas generation process
static argparse_error cbAtlasInput(argparse* self,
                                   const argparse_option* option) {

    SVGTuint i = 0U;
    argparse_error err = ARG_PARSE_NO_ERROR;
    const char* params = option->value.string;
    size_t len = strlen(option->value.string);
    // arguments structure, whose fields will be written
    CommandArguments* args = (CommandArguments*)option->data;
    // start as "unassigned" values
    const char* fileName = NULL;
    SVGTfloat scale = -1.0f;
    SVGTint exploreGroups = -1;
    AtlasInput atlasInput = { 0 };
    
    (void)self;

    // we must read three parameters: <filename>, <scale>, <explode groups>
    while ((*params != '\0') && (i < 3U) && (err == ARG_PARSE_NO_ERROR)) {

        const char* valueStart = skipSpaces(params, &len);
        char* valueEnd = (char*)skipUntilChar(valueStart, &len, ',');

        if (*valueEnd == ',') {
            // overwrite comma with a '\0', so that 'valueStart' now points to a null-termined string
            *valueEnd = '\0';
            // skip comma
            valueEnd++;
        }

        switch (i) {
            case 0:
                // copy input filename
                strcpy(atlasInput.fullFileName.name, valueStart);
                // ensure that every slash present within the path, will be set according to the current operating system type
                fixPath(atlasInput.fullFileName.name, SVGT_FALSE);
                // input file
                if (fileExists(atlasInput.fullFileName.name)) {
                    fileName = atlasInput.fullFileName.name;
                }
                else {
                    LOG_ERROR_EXT("\nfile '%s' does not exist or it is not readable\n\n", valueStart);
                    err = ARG_PARSE_CALLBACK_ERROR;
                }
                break;
            case 1:
                // additional scale
                if ((err = argparse_float(valueStart, &scale)) != ARG_PARSE_NO_ERROR) {
                    LOG_ERROR_EXT("\nmalformed scale value for atlas input file %s: %s", fileName, valueStart);
                }
                else {
                    if (scale <= 0.0f) {
                        LOG_ERROR_EXT("\nscale value for atlas input file %s must be a positive number greater than 0\n\n", fileName);
                        err = ARG_PARSE_CALLBACK_ERROR;
                    }
                }
                break;
            case 2:
            default:
                // "explode groups" flag
                if (strCaseCmp(valueStart, "true") == 0) {
                    exploreGroups = (SVGTint)SVGT_TRUE;
                }
                else
                if (strCaseCmp(valueStart, "false") == 0) {
                    exploreGroups = (SVGTint)SVGT_FALSE;
                }
                else {
                    LOG_ERROR_EXT("\ninvalid value for 'explode groups' flag: %s (valid values are 'true' and 'false')", valueStart);
                    err = ARG_PARSE_CALLBACK_ERROR;
                }
                break;
        }

        // next parameter
        i++;
        params = valueEnd;
    }

    // check that all parameters have been assigned
    if (err == ARG_PARSE_NO_ERROR) {
        if ((fileName == NULL) || (*fileName == '\0')) {
            LOG_ERROR("\nplease provide an input filename the atlas generation process");
            err = ARG_PARSE_CALLBACK_ERROR;
        }
        else
        if (scale < 0.0f) {
            LOG_ERROR_EXT("\nplease provide a scale value for the input file %s", fileName);
            err = ARG_PARSE_CALLBACK_ERROR;
        }
        else
        if (exploreGroups < 0) {
            LOG_ERROR_EXT("\nplease provide the 'explode groups' flag for the input file %s", fileName);
            err = ARG_PARSE_CALLBACK_ERROR;
        }
    }

    // write verified fields to the arguments structure
    if (err == ARG_PARSE_NO_ERROR) {
        atlasInput.scale = scale;
        atlasInput.explodeGroups = (SVGTboolean)exploreGroups;
        atlasInput.docHandle = SVGT_INVALID_HANDLE;
        // extract SVG base file name (e.g. /home/data/icon.svg --> icon)
        extractFileName(atlasInput.baseFileName.name, fileName, SVGT_FALSE);
        // initiliaze hashmap as empty
        (void)strHashMapInit(&atlasInput.elemHashmap, 0U);
        // push the new entry
        DYNARRAY_PUSH_BACK(args->atlasInputs, AtlasInput, atlasInput)
    }

    return err;
}

/************************************************************
                       main program
************************************************************/
static SVGTboolean svg2Bitmap(CommandArguments* args) {

    SVGTboolean ok;
    ExternalResourceDynArray fontResources = { 0 };
    ExternalResourceDynArray imageResources = { 0 };

    // initialize AmanithSVG library and load resources
    if ((ok = amanithsvgInit(args, &fontResources, &imageResources))) {

        if (args->atlasInputs.size > 0U) {
            // atlas generation
            ok = svg2BitmapAtlas(args, &fontResources, &imageResources);
        }
        else {
            // rendering of single or multiple SVG files (i.e. one PNG image will be produced for each rendered file)
            ok = svg2BitmapFile(args);
        }

        // release AmanithSVG library and resources
        amanithsvgDestroy(args, &fontResources, &imageResources);

        LOG_INFO("- Done\n");
    }

    return ok;
}

int main(int argc,
         char *argv[]) {

    // program name
    FileName prgName;
    // arguments parser
    argparse argsParser;
    argparse_error argsErr;
    CommandArguments prgArgs;
    char usage0[1024];
    char usage1[1024];
    const char* const usages[] = {
        usage0, usage1, NULL
    };
    // short name, long name, help-string, callback, data
    argparse_option options[] = {
        OPT_GROUP("OPTIONS:\n\n"),
        // optional arguments
        OPT_BOOLEAN( 'h', "help", "show this help message and exit", cbHelp, NULL),
        OPT_FLOAT  ('\0', "dpi", "dpi resolution, must be a positive number; default is 72", cbDpi, &prgArgs),
        OPT_INTEGER('\0', "screen-width", "screen/display width, in pixels; default is 1024", cbScreenWidth, &prgArgs),
        OPT_INTEGER('\0', "screen-height", "screen/display width, in pixels; default is 768", cbScreenHeight, &prgArgs),
        OPT_STRING ('\0', "language", "user-agent language (used during the 'systemLanguage' attribute resolving); a list of languages separated by semicolon (e.g.en-US;en-GB;it;fr), default is 'en'", cbLanguage, &prgArgs),
        OPT_STRING ('\0', "fonts-path", "optional fonts path, the location where font resources are searched for", cbFontsPath, &prgArgs),
        OPT_STRING ('\0', "images-path", "optional images path, the location where bitmap resources (png and jpg) are searched for", cbImagesPath, &prgArgs),
        OPT_STRING ('\0', "clear-color", "clear (background) color, expressed as #RRGGBBAA; default is transparent white #FFFFFF00", cbClearColor, &prgArgs),
        OPT_INTEGER('\0', "output-width", "set the output width, in pixel; a negative number will cause the value to be taken directly from the SVG file(s)", cbOutputWidth, &prgArgs),
        OPT_INTEGER('\0', "output-height", "set the output height, in pixel; a negative number will cause the value to be taken directly from the SVG file(s)", cbOutputHeight, &prgArgs),
        OPT_FLOAT  ('\0', "scale", "additional scale to be applied to all SVG files (also in atlas mode), must be a positive number; default is 1.0", cbScale, &prgArgs),
        OPT_INTEGER('\0', "rendering-quality", "rendering quality, must be a number between 1 and 100 (where 100 represents the best quality)", cbRenderingQuality, &prgArgs),
        OPT_STRING ('\0', "filter", "optional post-rendering filter, valid values are: 'none' (default), 'dilate'", cbFilter, &prgArgs),
        OPT_STRING ('\0', "pixel-format", "pixel format of produced PNG, valid values are: 'rgba' (default), 'bgra'", cbPixelFormat, &prgArgs),
        OPT_INTEGER('\0', "compression-level", "compression level used to generate output PNG files, must be a number between 0 and 9 (default is 6)", cbCompressionLevel, &prgArgs),
        OPT_STRING ('\0', "atlas-mode", "atlas mode parameters: <max bitmap dimensions>, <pad between each sprite in pixels>, <constraint: pow2 / npot (default)>; e.g. '2048,1,npot'", cbAtlasMode, &prgArgs),
        OPT_STRING ('\0', "atlas-output", "atlas output format: <prefix>,<data format: xml (default), cocos2d, json-array, json-hash, phaser2, phaser3, pixijs, godot3-sheet, godot3-tset, libgdx, spine, code-c, code-libgdx>,<map format: array (default), hash>; e.g. 'myatlas,xml,array'", cbAtlasOutput, &prgArgs),
        OPT_STRING ('\0', "atlas-input", "in atlas mode, add the given SVG to the packing; format is: <filename>,<scale>,<explode groups> (e.g. icons.svg,0.8,true)", cbAtlasInput, &prgArgs),
        OPT_STRING ( 'o', "output-path", "optional output path, the location where output files will be written to (default is current directory)", cbOutputPath, &prgArgs),
        OPT_BOOLEAN( 'q', "quiet", "quiet mode, it disables information messages and warnings", cbQuiet, &prgArgs),
        // mandatory arguments
        OPT_GROUP("ARGS:\n\n"),
        OPT_STRING ( 'i', "input", "a list of SVG files (separated by comma), or an input path that will be scanned, looking for SVG files", cbInput, &prgArgs),
        OPT_END()
    };
    SVGTint exitCode = EXIT_FAILURE;

    // save the program name
    extractFileName(prgName.name, argv[0], SVGT_TRUE);

    // create usage strings
    (void)sprintf(usage0, "%s [OPTIONS] --input=<input path/file> --output-path=<output_path>", prgName.name);
    (void)sprintf(usage1, "%s [OPTIONS] --atlas-input=icons.svg,1,true --atlas-output=icons_atlas,xml --output-path=<output_path>", prgName.name);

    // initialize arguments parser
    argparse_init(&argsParser, options, usages);
    argparse_describe(&argsParser, "\nAn utility to convert SVG files to bitmaps, by Mazatech S.r.l. - www.mazatech.com", "");

    // set default values for arguments
    argumentsInit(&prgArgs);

    // parse arguments
    if ((argsErr = argparse_parse(&argsParser, argc, (const char**)argv)) == ARG_PARSE_NO_ERROR) {

        if ((strlen(prgArgs.inputDir.name) > 0) || (prgArgs.atlasInputs.size > 0U)) {
            // do the real conversion
            exitCode = svg2Bitmap(&prgArgs) ? EXIT_SUCCESS : EXIT_FAILURE;
        }
        else {
            // the mandatory --input argument has not been provided, print usage
            argparse_usage(&argsParser);
        }
    }
    else {
        CommandArguments* args = &prgArgs;
        if (argsErr == ARG_PARSE_UNKNOWN_OPTION_ERROR) {
            LOG_ERROR_EXT("\nUnknown option %s\n\n", argsParser.current_option);
        }
    }

    // release arguments
    argumentsDestroy(&prgArgs);

    return exitCode;
}
