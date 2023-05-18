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

#ifndef SVG_VIEWER_H
#define SVG_VIEWER_H

// AmanithSVG API
#include <SVGT/svgt.h>
#include <stdlib.h>

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

// MIN / MAX macros
#ifndef MIN
    #define MIN(a, b) ((a) < (b) ? (a) : (b))
#endif
#ifndef MAX
    #define MAX(a, b) ((a) > (b) ? (a) : (b))
#endif

// AmanithSVG log buffer size
#define AMANITHSVG_LOG_BUFFER_SIZE 32768

// AmanithSVG curves quality (used by AmanithSVG geometric kernel to
// approximate curves with straight line segments (flattening); valid
// range is [1; 100], where 100 represents the best quality
#define AMANITHSVG_CURVES_QUALITY 75.0f

// AmanithSVG system/user-agent language; this setting will affect
// the conditional rendering of <switch> elements and elements
// with 'systemLanguage' attribute specified
#define AMANITHSVG_USER_AGENT_LANGUAGE "en"

// background pattern (dimensions and ARGB colors)
#define BACKGROUND_PATTERN_WIDTH 32
#define BACKGROUND_PATTERN_HEIGHT 32
#define BACKGROUND_PATTERN_COL0 0xFF808080
#define BACKGROUND_PATTERN_COL1 0xFFC0C0C0

// a simple rectangle structure
typedef struct {
    SVGTuint width;
    SVGTuint height;
} SimpleRect;

// an external in-memory resource
typedef struct {
    // filename without path (e.g. myfont.ttf)
    char fileName[128];
    // resource type (font or image)
    SVGTResourceType type;
    // the file loaded into memory
    SVGTubyte* buffer;
    size_t bufferSize;
    // bitwise OR of values from the SVGTResourceHint enumeration
    SVGTbitfield hints;
} SVGExternalResource;

// load a binary resource file in memory, returning the allocated buffer and its size in bytes
SVGTubyte* loadResourceFile(const char* fileName,
                            size_t* fileSize);

// load the given SVG file in memory and create a relative SVG document
SVGTHandle loadSvgFile(const char* fileName);

// extract the file name part from a given full path name (e.g. extractFileName("./subdir/myfile.txt") returns "myfile")
void extractFileName(char* fileName,
                     const char* fullFileName,
                     const SVGTboolean includeExtension);

// extract the extension part from a given full path name (e.g. extractFileExt("./subdir/myfile.txt") returns "txt")
void extractFileExt(char* fileExt,
                    const char* fullFileName);

void boxFit(SVGTuint* srcWidth,
            SVGTuint* srcHeight,
            SVGTuint dstWidth,
            SVGTuint dstHeight);

SimpleRect surfaceDimensionsCalc(const SVGTHandle doc,
                                 const SVGTuint maxWidth,
                                 const SVGTuint maxHeight);

#endif
