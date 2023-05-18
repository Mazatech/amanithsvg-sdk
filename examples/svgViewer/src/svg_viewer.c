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

#include "svg_viewer.h"
#include <stdint.h>
#include <stdio.h>
#include <string.h>
#include <math.h>

#if (defined(WIN32) || defined(_WIN32) || defined(__WIN32__)) || (defined(WIN64) || defined(_WIN64) || defined(__WIN64__))
    #define TRAILER_PATH_DELIMITER     '\\'
#else
    #define TRAILER_PATH_DELIMITER     '/'
#endif

// load a font file in memory, returning the allocated buffer and its size in bytes
static SVGTubyte* loadFile(const char* fileName,
                           const SVGTuint padAmount,
                           size_t* fileSize) {

    SVGTubyte* buffer;
    size_t size, read;
    FILE* fp = NULL;

#if defined(_MSC_VER) && (_MSC_VER >= 1400)
    // make Microsoft Visual Studio happy
    errno_t err = fopen_s(&fp, fileName, "rb");
    if ((fp == NULL) || (err != 0)) {
#else
    fp = fopen(fileName, "rb");
    if (fp == NULL) {
#endif
        return NULL;
    }
    
    (void)fseek(fp, 0, SEEK_SET);
    (void)fgetc(fp);
    if (ferror(fp) != 0) {
        (void)fclose(fp);
        return NULL;
    }

    // get the file size
    (void)fseek(fp, 0, SEEK_END);
    size = ftell(fp);
    (void)fseek(fp, 0, SEEK_SET);

    if (size == 0U) {
        (void)fclose(fp);
        return NULL;
    }

    if ((buffer = calloc(size + padAmount, sizeof(SVGTubyte))) == NULL) {
        (void)fclose(fp);
        return NULL;
    }

    // read the file content and store it within the memory buffer
    if ((read = fread(buffer, sizeof(SVGTubyte), size, fp)) != size) {
        free(buffer);
        (void)fclose(fp);
        return NULL;
    }

    // close the file and return the pointer to the memory buffer
    (void)fclose(fp);
    *fileSize = size;
    return buffer;
}

static char* loadXml(const char* fileName) {

    size_t fileSize;
    // add a trailing '\0'
    SVGTubyte* buffer = loadFile(fileName, 1U, &fileSize);
    return (char*)buffer;
}

// load a binary resource file in memory, returning the allocated buffer and its size in bytes
SVGTubyte* loadResourceFile(const char* fileName,
                            size_t* fileSize) {

    return loadFile(fileName, 0U, fileSize);
}

// load the given SVG file in memory and create a relative SVG document
SVGTHandle loadSvgFile(const char* fileName) {

    SVGTHandle svgHandle = SVGT_INVALID_HANDLE;

    if (fileName != NULL) {
        // allocate buffer and load xml file
        char* buffer = loadXml(fileName);
        if (buffer != NULL) {
            // create the SVG document
            svgHandle = svgtDocCreate(buffer);
            // free xml buffer
            free(buffer);
        }
    }

    return svgHandle;
}

// extract the file name part from a given full path name (e.g. extractFileName("./subdir/myfile.txt") returns "myfile")
void extractFileName(char* fileName,
                     const char* fullFileName,
                     const SVGTboolean includeExtension) {

    const char* lastDelimiter = strrchr(fullFileName, TRAILER_PATH_DELIMITER);

    if (!lastDelimiter) {
        // delimiter has not been found, so the last occurence of '.' will be the file extension
        const char* ext = strrchr(fullFileName, '.');
        if (!ext) {
            // no delimiter, no extension (e.g. "myfile" --> "myfile")
            (void)strcpy(fileName, fullFileName);
        }
        else {
            // no delimiter, extension dot found (e.g. "myfile.txt" --> "myfile")
            size_t len = (size_t)((intptr_t)ext - (intptr_t)fullFileName);
            (void)memcpy(fileName, fullFileName, len * sizeof(char));
            fileName[len] = '\0';
            // append extension, if requested
            if (includeExtension) {
                (void)strcat(fileName, ext);
            }
        }
    }
    else {
        // delimiter has been found, so the last occurence of '.' after delimiter will be the file extension
        const char* ext = strrchr(lastDelimiter, '.');
        if (!ext) {
            // delimiter found, extension dot not found (e.g. "./subdir/myfile" --> "myfile")
            size_t len = (strlen(fullFileName) -  (size_t)((intptr_t)lastDelimiter - (intptr_t)fullFileName)) - 1;
            (void)memcpy(fileName, lastDelimiter + 1, len * sizeof(char));
            fileName[len] = '\0';
        }
        else {
            // delimiter found, extension dot found (e.g. "./subdir/myfile.txt" --> "myfile")
            size_t len = (size_t)((intptr_t)ext - (intptr_t)lastDelimiter) - 1;
            (void)memcpy(fileName, lastDelimiter + 1, len * sizeof(char));
            fileName[len] = '\0';
            // append extension, if requested
            if (includeExtension) {
                (void)strcat(fileName, ext);
            }
        }
    }
}

// extract the extension part from a given full path name (e.g. extractFileExt("./subdir/myfile.txt") returns "txt")
void extractFileExt(char* fileExt,
                    const char* fullFileName) {

    const char* ext = strrchr(fullFileName, '.');

    if (!ext) {
        // no extension
        (void)strcpy(fileExt, "");
    }
    else {
        (void)strcpy(fileExt, &ext[1]);
    }
}

void boxFit(SVGTuint* srcWidth,
            SVGTuint* srcHeight,
            SVGTuint dstWidth,
            SVGTuint dstHeight) {

    SVGTfloat finalWidth = (SVGTfloat)(*srcWidth);
    SVGTfloat finalHeight = (SVGTfloat)(*srcHeight);
    // adapt dimensions
    SVGTfloat widthScale = (SVGTfloat)dstWidth / finalWidth;
    SVGTfloat heightScale = (SVGTfloat)dstHeight / finalHeight;
    SVGTfloat scale = (widthScale < heightScale) ? widthScale : heightScale;
    // scale desired dimensions
    finalWidth *= scale;
    finalHeight *= scale;
    *srcWidth = (SVGTuint)(finalWidth + 0.5f);
    *srcHeight = (SVGTuint)(finalHeight + 0.5f);
}

SimpleRect surfaceDimensionsCalc(const SVGTHandle doc,
                                 const SVGTuint maxWidth,
                                 const SVGTuint maxHeight) {

    SimpleRect result = { 0, 0 };

    if (doc != SVGT_INVALID_HANDLE) {
        SVGTuint srfWidth, srfHeight;
        SVGTuint maxAllowedDimension = svgtSurfaceMaxDimension();
        // round document dimensions
        SVGTuint svgWidth = (SVGTuint)(svgtDocWidth(doc) + 0.5f);
        SVGTuint svgHeight = (SVGTuint)(svgtDocHeight(doc) + 0.5f);
        // if the SVG document (i.e. the outermost <svg> element) does not specify 'width' and 'height' attributes, we start with default
        // surface dimensions, keeping the same aspect ratio of the 'viewBox' attribute (present in the outermost <svg> element)
        if ((svgWidth < 1) || (svgHeight < 1)) {
            SVGTfloat docViewport[4];
            // get document viewport (as it appears in the 'viewBox' attribute)
            if (svgtDocViewportGet(doc, docViewport) == SVGT_NO_ERROR) {
                // start with desired dimensions
                srfWidth = (SVGTuint)(docViewport[2] + 0.5f);
                srfHeight = (SVGTuint)(docViewport[3] + 0.5f);
                if ((srfWidth > maxWidth) || (srfHeight > maxHeight)) {
                    // adapt desired dimensions to screen bounds
                    boxFit(&srfWidth, &srfHeight, maxWidth, maxHeight);
                }
            }
            else {
                // just start with something valid
                srfWidth = (maxWidth / 3) + 1;
                srfHeight = (maxHeight / 3) + 1;
            }
        }
        else {
            // start with desired dimensions
            srfWidth = svgWidth;
            srfHeight = svgHeight;
            if ((srfWidth > maxWidth) || (srfHeight > maxHeight)) {
                // adapt desired dimensions to screen bounds
                boxFit(&srfWidth, &srfHeight, maxWidth, maxHeight);
            }
        }

        if ((srfWidth < 1) || (srfHeight < 1)) {
            // just start with something valid
            srfWidth = (maxWidth / 3) + 1;
            srfHeight = (maxHeight / 3) + 1;
        }

        result.width = srfWidth;
        result.height = srfHeight;
        // take care of the maximum allowed dimension for drawing surfaces
        if ((result.width > maxAllowedDimension) || (result.height > maxAllowedDimension)) {
            boxFit(&result.width, &result.height, maxAllowedDimension, maxAllowedDimension);
        }
    }

    return result;
}
