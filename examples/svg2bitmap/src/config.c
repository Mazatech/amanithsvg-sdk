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
    \file config.c
    \brief Configuration thresholds and default argument values.
    \author Matteo Muratori
    \author Michele Fabbri
*/

#include "config.h"

// default user-agent language
static const char* defaultLanguage = DEFAULT_USER_AGENT_LANGUAGE;
// default output prefix for atlas bitmap and data files
static const char* defaultAtlasPrefix = DEFAULT_ATLAS_OUTPUT_PREFIX;

// set default values for the given arguments
void argumentsInit(CommandArguments* args) {

    /************************************************************
                         AmanithSVG settings
    ************************************************************/
    // screen/display dpi resolution
    args->dpi = DEFAULT_DPI;
    // screen/display width, in pixels
    args->screenWidth = DEFAULT_SCREEN_WIDTH;
    // screen/display height, in pixels
    args->screenHeight = DEFAULT_SCREEN_HEIGHT;
    // user-agent language (used during the 'systemLanguage' attribute resolving)
    args->language = defaultLanguage;
    // log buffer
    args->logBuffer = calloc(DEFAULT_LOG_BUFFER_CAPACITY, sizeof(char));
    args->logBufferCapacity = (args->logBuffer != NULL) ? DEFAULT_LOG_BUFFER_CAPACITY : 0U;

    /************************************************************
                          external resources
    ************************************************************/
    // fonts path, the location where font resources are searched for
    (void)memset(args->fontsDir.path, 0, sizeof(Directory));
    // images path, the location where bitmap resources(pngand jpg) are searched for
    (void)memset(args->imagesDir.path, 0, sizeof(Directory));

    /************************************************************
                         PNG output settings
    ************************************************************/
    // compression level used to generate output PNG files, a number between 0 and 9
    args->compressionLevel = DEFAULT_COMPRESSION_LEVEL;
    // quiet mode
    args->quiet = DEFAULT_QUIET_MODE;

    /************************************************************
                             rendering
    ************************************************************/
    // output path, the location where output files will be written to
    (void)strcpy(args->outputDir.path, DEFAULT_OUTPUT_DIR);
    // clear (background) color
    args->clearColor[0] = DEFAULT_CLEAR_COLOR_R;
    args->clearColor[1] = DEFAULT_CLEAR_COLOR_G;
    args->clearColor[2] = DEFAULT_CLEAR_COLOR_B;
    args->clearColor[3] = DEFAULT_CLEAR_COLOR_A;
    // rendering quality
    args->renderingQuality = DEFAULT_RENDERING_QUALITY;
    // post-rendering filter
    args->filter = DEFAULT_POST_RENDERING_FILTER;
    // pixel format
    args->pixelFormat = DEFAULT_PIXEL_FORMAT;

    /************************************************************
                  rendering (single or multiple files)
    ************************************************************/
    // a single SVG file or an input path that will be scanned
    (void)memset(args->inputDir.buffer, 0, sizeof(FileName));
    // the output width, in pixel; a negative number will cause the value to be taken directly from the SVG file(s)
    args->width = DEFAULT_OUTPUT_WIDTH;
    // the output height, in pixel; a negative number will cause the value to be taken directly from the SVG file(s)
    args->height = DEFAULT_OUTPUT_HEIGHT;
    // additional scale to be applied to all SVG files
    args->scale = DEFAULT_OUTPUT_SCALE;
    
    /************************************************************
                       rendering (atlas mode)
    ************************************************************/
    // maximum dimension of generated atlas, in pixels
    args->atlasMaxDimension = DEFAULT_ATLAS_MAX_DIMENSION;
    // pad between each sprite, in pixels
    args->atlasBorder = DEFAULT_ATLAS_BORDER;
    // atlas power-of-two dimensions constraint
    args->atlasPow2 = DEFAULT_ATLAS_POW2_CONSTRAINT;
    // output prefix for atlas bitmap and data files
    args->atlasPrefix = defaultAtlasPrefix;
    // output format for atlas data files
    args->atlasFormat = DEFAULT_ATLAS_OUTPUT_FORMAT;
    // output format for atlas map file
    args->atlasMapFormat = DEFAULT_ATLAS_MAP_FORMAT;
    // input files to be packed
    DYNARRAY_INIT(args->atlasInputs)
}

// release arguments
void argumentsDestroy(CommandArguments* args) {

    if (args->logBuffer != NULL) {
        free(args->logBuffer);
    }
    DYNARRAY_DESTROY(args->atlasInputs)
}

// log the given message
void logMessage(const char* msg,
                const LogLevel msgLevel,
                const CommandArguments* args) {

    if ((msgLevel == LOG_LEVEL_ERROR) || (!args->quiet)) {
        (void)fprintf(stderr, "%s", msg);
    }
}