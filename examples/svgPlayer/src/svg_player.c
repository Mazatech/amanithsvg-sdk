/****************************************************************************
** Copyright (c) 2013-2018 Mazatech S.r.l.
** All rights reserved.
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

#include "svg_player.h"
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <math.h>

static char* loadXml(const char* fileName) {

    char* buffer;
    size_t size, read;
    FILE* fp = 0;

#if defined(_MSC_VER) && (_MSC_VER >= 1400)
    errno_t err = fopen_s(&fp, fileName, "rb");
    if (!fp || err) {
#else
    fp = fopen(fileName, "rb");
    if (!fp) {
#endif
        return NULL;
    }
    
    fseek(fp, 0, SEEK_SET);
    fgetc(fp);
    if (ferror(fp) != 0) {
        fclose(fp);
        return NULL;
    }

    fseek(fp, 0, SEEK_END);
    size = ftell(fp);
    fseek(fp, 0, SEEK_SET);

    if (size == 0) {
        fclose(fp);
        return NULL;
    }

    buffer = (char *)malloc((size + 1) * sizeof(char));
    if (!buffer) {
        fclose(fp);
        return NULL;
    }

    read = fread(buffer, sizeof(char), size, fp);
    if (read != size) {
        free(buffer);
        fclose(fp);
        return NULL;
    }

    buffer[size] = '\0';
    fclose(fp);
    return buffer;
}

SVGTHandle loadSvg(const char* fileName) {

    SVGTHandle svgHandle = SVGT_INVALID_HANDLE;

    if (fileName != NULL) {
        // allocate buffer and load xml file
        char* buffer = loadXml(fileName);
        if (buffer != NULL) {
            // create the SVG document
            SVGTHandle svgHandle = svgtDocCreate(buffer);
            // free xml buffer
            free(buffer);
        }
    }

    return svgHandle;
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
