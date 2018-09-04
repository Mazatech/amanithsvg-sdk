/****************************************************************************
** Copyright (C) 2013-2018 Mazatech S.r.l. All rights reserved.
**
** This file is part of AmanithSVG software, an SVG rendering engine.
**
** W3C (World Wide Web Consortium) and SVG are trademarks of the W3C.
** OpenGL is a registered trademark and OpenGL ES is a trademark of
** Silicon Graphics, Inc.
**
** This file is provided AS IS with NO WARRANTY OF ANY KIND, INCLUDING THE
** WARRANTY OF DESIGN, MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE.
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

    if (fileName) {
        // allocate buffer and load xml file
        char* buffer = loadXml(fileName);
        if (buffer != NULL) {
            // create the SVG document
            SVGTHandle svgHandle = svgtDocCreate(buffer);
            // free xml buffer
            free(buffer);
            return svgHandle;
        }
    }
    return SVGT_INVALID_HANDLE;
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
