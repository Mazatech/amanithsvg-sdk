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

#ifndef CONFIG_H
#define CONFIG_H

/*!
    \file config.h
    \brief Configuration thresholds and default argument values.
    \author Matteo Muratori
    \author Michele Fabbri
*/

#if (defined(WIN32) || defined(_WIN32) || defined(__WIN32__)) || (defined(WIN64) || defined(_WIN64) || defined(__WIN64__))
    // Windows
    #include <windows.h>
    #if defined(_DEBUG)
        // Visual Leak Detector
        #include <vld.h>
    #endif
#endif

#if defined(_MSC_VER)
    // take care of Visual Studio .Net 2005+ C deprecations
    #if _MSC_VER >= 1400
        #if !defined(_CRT_SECURE_NO_DEPRECATE)
            #define _CRT_SECURE_NO_DEPRECATE 1
        #endif
        #if !defined(_CRT_NONSTDC_NO_DEPRECATE)
            #define _CRT_NONSTDC_NO_DEPRECATE 1
        #endif
        #pragma warning (disable:4996)
    #endif
#endif

// stadard C library
#include <stdio.h>
#include <string.h>
// atlas definition
#include "atlas.h"

// MIN / MAX macros
#ifndef MIN
    #define MIN(a, b) ((a) < (b) ? (a) : (b))
#endif
#ifndef MAX
    #define MAX(a, b) ((a) > (b) ? (a) : (b))
#endif

// clamp macro
#define CLAMP(_v, _lo, _hi) (((_v) > (_hi)) ? (_hi) : ((((_v) < (_lo)) ? (_lo) : (_v))))

#if (defined(WIN32) || defined(_WIN32) || defined(__WIN32__)) || (defined(WIN64) || defined(_WIN64) || defined(__WIN64__))
    #define TRAILER_PATH_DELIMITER     '\\'
    #define TRAILER_PATH_DELIMITER_STR "\\"
#else
    #define TRAILER_PATH_DELIMITER     '/'
    #define TRAILER_PATH_DELIMITER_STR "/"
#endif

// pixel format for output PNG
typedef enum {
    FORMAT_RGBA = 0,
    FORMAT_BGRA = 1
} PixelFormat;

typedef enum {
    // no filter
    FILTER_NONE = 0,
    // 1 pixel dilate filter
    FILTER_DILATE = 1
} PostRenderingFilter;

/************************************************************
                           Logger
************************************************************/
typedef enum {
    // error conditions
    LOG_LEVEL_ERROR,
    // warning conditions
    LOG_LEVEL_WARNING,
    // informational message
    LOG_LEVEL_INFO
} LogLevel;

// print an error message
#define LOG_ERROR(_msg) logMessage(_msg, LOG_LEVEL_ERROR, args)
#define LOG_ERROR_EXT(format, ...) { \
    char msg[1024] = { '\0' }; \
    snprintf(msg, 1023, format, __VA_ARGS__); \
    LOG_ERROR(msg); \
}

// print a warning message
#define LOG_WARNING(_msg) logMessage(_msg, LOG_LEVEL_WARNING, args)
#define LOG_WARNING_EXT(format, ...) { \
    char msg[1024] = { '\0' }; \
    snprintf(msg, 1023, format, __VA_ARGS__); \
    LOG_WARNING(msg); \
}

// print an informational message
#define LOG_INFO(_msg) logMessage(_msg, LOG_LEVEL_INFO, args)
#define LOG_INFO_EXT(format, ...) { \
    char msg[1024] = { '\0' }; \
    snprintf(msg, 1023, format, __VA_ARGS__); \
    LOG_INFO(msg); \
}

/************************************************************
            Default values for program arguments
************************************************************/

// the default log buffer capacity
#define DEFAULT_LOG_BUFFER_CAPACITY         16384U
 
// the deafult output directory
#define DEFAULT_OUTPUT_DIR                  "."TRAILER_PATH_DELIMITER_STR

// the deafult screen/display width, in pixels
#define DEFAULT_SCREEN_WIDTH                1024U

// the deafult screen/display height, in pixels
#define DEFAULT_SCREEN_HEIGHT               768U

// default dpi resolution
#define DEFAULT_DPI                         72.0f

// default user-agent language
#define DEFAULT_USER_AGENT_LANGUAGE         "en"

// default clear (background) color
#define DEFAULT_CLEAR_COLOR_R               1.0f
#define DEFAULT_CLEAR_COLOR_G               1.0f
#define DEFAULT_CLEAR_COLOR_B               1.0f
#define DEFAULT_CLEAR_COLOR_A               0.0f

// the default output width, in pixel; a negative number will cause the value to be taken directly from the SVG file(s)
#define DEFAULT_OUTPUT_WIDTH                -1

// the default output height, in pixel; a negative number will cause the value to be taken directly from the SVG file(s)
#define DEFAULT_OUTPUT_HEIGHT               -1

// the default scale to be applied to all SVG files
#define DEFAULT_OUTPUT_SCALE                1.0f

// default compression level for the deflate algorithm
#define DEFAULT_COMPRESSION_LEVEL           6U

// the default rendering quality
#define DEFAULT_RENDERING_QUALITY           60

// default post-rendering filter
#define DEFAULT_POST_RENDERING_FILTER       FILTER_NONE

// default pixel format
#define DEFAULT_PIXEL_FORMAT                FORMAT_RGBA

// default value for quiet mode
#define DEFAULT_QUIET_MODE                  SVGT_FALSE

// default maximum dimension of generated atlas, in pixels; a negative (or zero) value will cause the value to be taken directly from AmanithSVG
#define DEFAULT_ATLAS_MAX_DIMENSION         -1

// default pad between each sprite (within atlas), in pixels
#define DEFAULT_ATLAS_BORDER                1

// default atlas power-of-two dimensions constraint
#define DEFAULT_ATLAS_POW2_CONSTRAINT       SVGT_FALSE

// default output prefix for atlas bitmap and data files
#define DEFAULT_ATLAS_OUTPUT_PREFIX         "atlas"

// default output format for atlas data files
#define DEFAULT_ATLAS_OUTPUT_FORMAT         ATLAS_XML_GENERIC_FORMAT

// default output format for atlas map file
#define DEFAULT_ATLAS_MAP_FORMAT            ATLAS_MAP_ARRAY_FORMAT

// the name that will be assigned to sprites/groups that do not have the 'id' attribute assigned within the svg files
// NB: this name will also be used as a search key when assigning unique names for sprites (see rendering.c function 'atlasPageElementName')
#define DEFAULT_ATLAS_EMPTY_ELEMENTS_ID     "__empty__"

// output stream used for error, warning and informational messages
#define OUTPUT_STREAM                       stdout

/************************************************************
            External resources (fonts and images)
************************************************************/

// an external in-memory resource
typedef struct {
    // full filename
    FileName fileName;
    // the file loaded into memory
    const SVGTubyte* buffer;
    size_t bufferSize;
    // bitwise OR of values from the SVGTResourceHint enumeration
    SVGTbitfield hints;
} ExternalResource;

// an array of external resources
DYNARRAY_DECLARE(ExternalResourceDynArray, ExternalResource)

/************************************************************
                      Program arguments
************************************************************/

typedef struct {

    /************************************************************
                         AmanithSVG settings
    ************************************************************/
    // screen/display dpi resolution
    SVGTfloat dpi;
    // screen/display width, in pixels
    SVGTuint screenWidth;
    // screen/display height, in pixels
    SVGTuint screenHeight;
    // user-agent language (used during the 'systemLanguage' attribute resolving)
    const char* language;
    // log buffer
    SVGTuint logBufferCapacity;
    char* logBuffer;

    /************************************************************
                          external resources
    ************************************************************/
    // fonts path, the location where font resources are searched for
    Directory fontsDir;
    // images path, the location where bitmap resources(pngand jpg) are searched for
    Directory imagesDir;

    /************************************************************
                         PNG output settings
    ************************************************************/
    // compression level used to generate output PNG files, a number between 0 and 9
    SVGTuint compressionLevel;
    // quiet mode
    SVGTboolean quiet;

    /************************************************************
                             rendering
    ************************************************************/
    // output path, the location where output files will be written to
    Directory outputDir;
    // clear (background) color; r, g, b, a, each value in the [0; 1] range
    SVGTfloat clearColor[4];
    // rendering quality
    SVGTuint renderingQuality;
    // post-rendering filter
    PostRenderingFilter filter;
    // pixel format
    PixelFormat pixelFormat;

    /************************************************************
                  rendering (single or multiple files)
    ************************************************************/
    // a single SVG file or an input path that will be scanned
    FileName inputDir;
    // the output width, in pixel; a negative number will cause the value to be taken directly from the SVG file(s)
    SVGTint width;
    // the output height, in pixel; a negative number will cause the value to be taken directly from the SVG file(s)
    SVGTint height;
    // additional scale to be applied to all SVG files; it is a positive number
    SVGTfloat scale;

    /************************************************************
                       rendering (atlas mode)
    ************************************************************/
    // maximum dimension of generated PNG bitmap/textures, in pixels
    // NB: a negative value will cause the value to be taken directly from AmanithSVG
    SVGTint atlasMaxDimension;
    // pad between each sprite, in pixels
    SVGTuint atlasBorder;
    // force PNG bitmap/textures to have power-of-two dimensions
    SVGTboolean atlasPow2;
    // output prefix for atlas bitmap and data files
    const char* atlasPrefix;
    // output format for atlas data
    AtlasFormat atlasFormat;
    // output format for atlas map
    AtlasMapFormat atlasMapFormat;
    // input files to be packed
    AtlasInputDynArray atlasInputs;

} CommandArguments;

// set default values for the given arguments
void argumentsInit(CommandArguments* args);
// release arguments
void argumentsDestroy(CommandArguments* args);
// log the given message
void logMessage(const char* msg,
                const LogLevel msgLevel,
                const CommandArguments* args);


#endif /* CONFIG_H */
