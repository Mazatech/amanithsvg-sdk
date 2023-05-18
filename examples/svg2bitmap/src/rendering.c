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
    \file rendering.c
    \brief SVG files and atlas rendering functions, implementation.
    \author Matteo Muratori
    \author Michele Fabbri
*/

#include "atlas_export.h"
#include "png_utils.h"
#include "str_utils.h"
#include "rendering.h"
#include <math.h>

static const char* defaultUnassignedIdName = DEFAULT_ATLAS_EMPTY_ELEMENTS_ID;

// a simple rectangle structure
typedef struct {
    SVGTuint width;
    SVGTuint height;
} SimpleRect;

// round the given (positive) number
static SVGTint roundInteger(const SVGTfloat v) {

    return (v < 0.0f) ? (SVGTint)(v - 0.5f) : (SVGTint)(v + 0.5f);
}

/************************************************************
                  SVG rendering (utilities)
************************************************************/
static char* loadXml(const char* fileName) {

    size_t fileSize;
    // add a trailing '\0'
    return (char*)loadFile(fileName, 1U, &fileSize);
}

static void boxFit(SVGTuint* srcWidth,
                   SVGTuint* srcHeight,
                   SVGTuint dstWidth,
                   SVGTuint dstHeight) {

    SVGTfloat finalWidth = (SVGTfloat)(*srcWidth);
    SVGTfloat finalHeight = (SVGTfloat)(*srcHeight);
    // adapt dimensions
    const SVGTfloat widthScale = (SVGTfloat)dstWidth / finalWidth;
    const SVGTfloat heightScale = (SVGTfloat)dstHeight / finalHeight;
    const SVGTfloat scale = (widthScale < heightScale) ? widthScale : heightScale;
    // scale desired dimensions
    finalWidth *= scale;
    finalHeight *= scale;
    *srcWidth = (SVGTuint)roundInteger(finalWidth);
    *srcHeight = (SVGTuint)roundInteger(finalHeight);
}

static SimpleRect surfaceDimensionsCalc(const SVGTHandle doc,
                                        const CommandArguments* args) {

    SimpleRect result = { 0U, 0U };

    // a negative number will cause the value to be taken directly from the SVG file
    if ((args->width < 0) || (args->height < 0)) {

        // round document dimensions
        const SVGTint docWidth = roundInteger(svgtDocWidth(doc));
        const SVGTint docHeight = roundInteger(svgtDocHeight(doc));

        // if the SVG document (i.e. the outermost <svg> element) does not specify 'width' and 'height'
        // attributes, we start with the 'viewBox' attribute (present in the outermost <svg> element)
        if ((docWidth < 1) || (docHeight < 1)) {

            SVGTfloat docViewport[4] = { 0.0f, 0.0f, 0.0f, 0.0f };

            // get document viewport (as it appears in the 'viewBox' attribute)
            if (svgtDocViewportGet(doc, docViewport) == SVGT_NO_ERROR) {
                // round viewport dimensions
                const SVGTint width = roundInteger(docViewport[2]);
                const SVGTint height = roundInteger(docViewport[3]);
                // if positive, start with these values
                if ((width > 0) && (height > 0)) {
                    result.width = (SVGTuint)width;
                    result.height = (SVGTuint)height;
                }
            }
            else {
                LOG_WARNING("unable to get the document viewport");
            }
        }
        else {
            // start with SVG's document dimensions
            result.width = (SVGTuint)docWidth;
            result.height = (SVGTuint)docHeight;
        }
    }
    else {
        // start with provided arguments
        result.width = (SVGTuint)args->width;
        result.height = (SVGTuint)args->height;
    }

    if ((result.width > 0U) && (result.height > 0U)) {

        // now we have to apply the scale argument
        SVGTuint width = (SVGTuint)roundInteger(result.width * args->scale);
        SVGTuint height = (SVGTuint)roundInteger(result.height * args->scale);
        const SVGTuint maxAllowedDimension = svgtSurfaceMaxDimension();

        if ((width > maxAllowedDimension) || (height > maxAllowedDimension)) {
            // take care of the maximum allowed dimension for drawing surfaces
            boxFit(&width, &height, maxAllowedDimension, maxAllowedDimension);
        }
        result.width = width;
        result.height = height;
    }

    return result;
}

static SVGTboolean pngWrite(SVGTHandle svgSurface,
                            const char* outFileName,
                            const CommandArguments* args) {

    SVGTboolean ok = SVGT_TRUE;
    const SVGTuint width = svgtSurfaceWidth(svgSurface);
    const SVGTuint height = svgtSurfaceHeight(svgSurface);
    const SVGTubyte* pixels = (const SVGTubyte*)svgtSurfacePixels(svgSurface);
    SVGTubyte* actualPixels = (SVGTubyte*)pixels;

    // apply dilate filter, if requested
    if (args->filter == FILTER_DILATE) {

        const SVGTuint n = width * height;
        // each pixel is a 4-bytes RGBA
        actualPixels = calloc(n, sizeof(SVGTuint));
        if ((ok = (actualPixels != NULL))) {
            // copy surface pixels into temporary buffer, applying a dilate filter
            (void)svgtSurfaceCopy(svgSurface, actualPixels, SVGT_FALSE, SVGT_TRUE);
        }
        else {
            LOG_ERROR("failed to allocate temporary buffer in order to perform dilate filter");
        }
    }

    if (ok) {
        size_t pngDataSize;
        // encode the drawing surface to PNG
        void* pngData = pngEncode(&pngDataSize, actualPixels, width, height, args->compressionLevel, args->pixelFormat, "", "");

        if ((ok = (pngData != NULL))) {
            // open the output file
            FILE* pngFile = fopen(outFileName, "wb");
            if ((ok = (pngFile != NULL))) {
                // write data
                if (fwrite(pngData, 1, pngDataSize, pngFile) < pngDataSize) {
                    LOG_ERROR_EXT("failed to write output file (i.e. file not fully written) %s", outFileName);
                    ok = SVGT_FALSE;
                }
                // close file
                (void)fclose(pngFile);
            }
            else {
                LOG_ERROR_EXT("failed to open output file %s", outFileName);
            }
            // free allocated memory
            free(pngData);
        }
        else {
            LOG_ERROR_EXT("failed to encode PNG data for %s", outFileName);
        }

        // release temporary buffer
        if (args->filter == FILTER_DILATE) {
            free(actualPixels);
        }
    }

    return ok;
}

/************************************************************
                SVG rendering, single file(s)
************************************************************/
static char* logErrorDesc(const char* logBuffer) {

    // find the first logged error
    char* s = strstr(logBuffer, "[E]");

    if (s != NULL) {
        char* end;
        // skip "[E]"
        s += 3;
        // skip all spaces
        while ((*s != '\0') && isSpace(*s)) {
            s++;
        }
        // skip all until we find a '\n'
        end = s;
        while ((*end != '\0') && (*end != '\n')) {
            end++;
        }
        // put a closing terminator
        *end = '\0';
    }

    return s;
}

static SVGTboolean svgRender(const char* inFileName,
                             const char* outFileName,
                             const CommandArguments* args) {

    char* xmlBuffer;
    FileName baseFileName;
    SVGTboolean ok = SVGT_TRUE;

    // extract SVG base file name (e.g. /home/data/icon.svg --> icon.svg)
    extractFileName(baseFileName.name, inFileName, SVGT_TRUE);

    // allocate buffer and load SVG file
    if ((xmlBuffer = loadXml(inFileName)) != NULL) {

        // create (and parse) the SVG document
        SVGTHandle svgDoc = svgtDocCreate(xmlBuffer);
        if (svgDoc != SVGT_INVALID_HANDLE) {

            // calculate drawing surface dimension
            const SimpleRect r = surfaceDimensionsCalc(svgDoc, args);
            if ((r.width > 0U) && (r.height > 0U)) {

                // create the drawing surface
                SVGTHandle svgSurface = svgtSurfaceCreate(r.width, r.height);
                if (svgSurface != SVGT_INVALID_HANDLE) {

                    SVGTErrorCode err;

                    // clear the drawing surface
                    if ((err = svgtSurfaceClear(svgSurface, args->clearColor[0], args->clearColor[1], args->clearColor[2], args->clearColor[3])) == SVGT_NO_ERROR) {
                        // clear / rewind log buffer
                        if (args->logBuffer != NULL) {
                            (void)svgtLogBufferSet(args->logBuffer, args->logBufferCapacity, SVGT_LOG_LEVEL_ERROR);
                        }
                        // draw the document
                        if ((err = svgtDocDraw(svgDoc, svgSurface, SVGT_RENDERING_QUALITY_BETTER)) == SVGT_NO_ERROR) {
                            // write drawing surface content to PNG file
                            if ((ok = pngWrite(svgSurface, outFileName, args))) {
                                LOG_INFO_EXT("- %s rendered successfully\n", baseFileName.name);
                            }
                        }
                        else {
                            if (err == SVGT_INVALID_SVG_ERROR) {
                                // get error description (just the first one)
                                const char* desc = (args->logBuffer != NULL) ? logErrorDesc(args->logBuffer) : NULL;
                                if (desc != NULL) {
                                    LOG_ERROR_EXT("\nerrors in document (%s)\n", desc);
                                }
                                else {
                                    LOG_ERROR("\nerrors in document (e.g. incomplete paint servers, negative values where not allowed)\n");
                                }
                                // the document was in error, write drawing surface content to PNG file anyway
                                // according to SVG specifications, the document shall be rendered up to, but not
                                // including, the first element which has an error
                                ok = pngWrite(svgSurface, outFileName, args);
                            }
                            else {
                                // other AmanithSVG errors (e.g. out of memory, parser errors due to malformed XML)
                                // in this case we don't write the PNG file
                                LOG_ERROR_EXT("\nerror drawing the document (AmanithSVG error code = %d)\n", err);
                            }
                            ok = SVGT_FALSE;
                        }
                    }
                    else {
                        LOG_ERROR_EXT("\nerror clearing the drawing surface (AmanithSVG error code = %d)\n", err);
                        ok = SVGT_FALSE;
                    }

                    // destroy the drawing surface
                    (void)svgtSurfaceDestroy(svgSurface);
                }
                else {
                    LOG_ERROR_EXT("\nfailed to create the drawing surface for %s (AmanithSVG error code = %d)\n", baseFileName.name, svgtGetLastError());
                    ok = SVGT_FALSE;
                }

                // destroy SVG document
                (void)svgtDocDestroy(svgDoc);
            }
            else {
                LOG_INFO_EXT("- %s skipped, <svg> root element does not contain valid width or height attributes\n", baseFileName.name);
                ok = SVGT_FALSE;
            }
        }
        else {
            LOG_ERROR_EXT("\nfailed to create SVG document for %s (AmanithSVG error code = %d)\n", baseFileName.name, svgtGetLastError());
            ok = SVGT_FALSE;
        }

        // free xml buffer
        free(xmlBuffer);
    }
    else {
        LOG_ERROR_EXT("\nfailed to load file %s\n", baseFileName.name);
        ok = SVGT_FALSE;
    }

    return ok;
}

SVGTboolean svg2BitmapFile(const CommandArguments* args) {

    SVGTboolean ok = SVGT_TRUE;
    FileSearchResult searchResult = { 0 };

    // initialize temporary array
    DYNARRAY_INIT(searchResult.fileNames)

    // if 'inputDir' is a directory, perform a scan; else 'inputDir' is a regular file
    if (directoryExists(args->inputDir.name)) {
        static const char svgFilter[] = "svg";
        // setup search settings (no recursive scan, add path to returned file names, no filter on extension)
        const FileSearchSettings searchSettings = { SVGT_FALSE, SVGT_TRUE, svgFilter };
        // scan the path
        ok = scanPath(&searchResult, &searchSettings, args->inputDir.name);
    }
    else {
        // just a single regular file
        DYNARRAY_PUSH_BACK(searchResult.fileNames, FileName, args->inputDir)
    }

    if (ok) {

        // loop over the input SVG files
        for (size_t i = 0U; (i < searchResult.fileNames.size); ++i) {

            char outFileName[2048];
            FileName baseName = { 0 };
            const FileName* in = &searchResult.fileNames.data[i];

            // extract SVG base file name (e.g. /home/data/icon.svg --> icon)
            extractFileName(baseName.name, in->name, SVGT_FALSE);

            // build output bitmap file name (<output path> + <base filename> + ".png")
            (void)sprintf(outFileName, "%s%s.png", args->outputDir.path, baseName.name);

            // render the SVG
            ok = svgRender(in->name, outFileName, args);
        }
        // NB: if there were multiple files to render (i.e. the tool was invoked with --input=<directory>), we
        // report the last exit code (the 'ok' variable) to the outside; if there was only one file to
        // render, such last exit code corresponds to the file in question only
    }

    // destroy temporary array
    DYNARRAY_DESTROY(searchResult.fileNames)
    // we have finished
    return ok;
}

/************************************************************
                 SVG rendering, atlas mode
************************************************************/

// load all SVG documents that will be used for atlas generation
static SVGTboolean atlasDocumentsLoad(const CommandArguments* args) {

    SVGTboolean ok = SVGT_TRUE;

    // load SVG document
    for (size_t i = 0U; (i < args->atlasInputs.size) && ok; ++i) {

        AtlasInput* input = &args->atlasInputs.data[i];
        // allocate buffer and load SVG file
        char* xmlBuffer = loadXml(input->fullFileName.name);

        // allocate buffer and load SVG file
        if (xmlBuffer != NULL) {
            // create (and parse) the SVG document
            if ((input->docHandle = svgtDocCreate(xmlBuffer)) != SVGT_INVALID_HANDLE) {
                LOG_INFO_EXT("- %s loaded successfully\n", input->fullFileName.name);
            }
            else {
                LOG_ERROR_EXT("\nfailed to create SVG document for %s (AmanithSVG error code = %d)\n", input->fullFileName.name, svgtGetLastError());
                ok = SVGT_FALSE;
            }
            // free xml buffer
            free(xmlBuffer);
        }
        else {
            LOG_ERROR_EXT("\nfailed to load file %s\n", input->fullFileName.name);
            ok = SVGT_FALSE;
        }
    }

    return ok;
}

// release SVG documents used for atlas generation
static void atlasDocumentsDestroy(CommandArguments* args) {

    for (size_t i = 0U; i < args->atlasInputs.size; ++i) {
        AtlasInput* input = &args->atlasInputs.data[i];
        if (input->docHandle != SVGT_INVALID_HANDLE) {
            // destroy SVG document
            (void)svgtDocDestroy(input->docHandle);
        }
        strHashMapDestroy(&input->elemHashmap);
    }
}

// add the given document to the collection process
static SVGTboolean atlasDocumentAdd(const SVGTuint index,
                                    CommandArguments* args) {

    SVGTboolean ok = SVGT_TRUE;
    SVGTuint info[2] = { 0, 0 };
    AtlasInput* input = &args->atlasInputs.data[index];
    // add the loaded document to the collection process
    SVGTErrorCode err = svgtPackingAdd(input->docHandle, input->explodeGroups, input->scale, info);

    if (err == SVGT_NO_ERROR) {
        // info[0] = number of collected elements
        // info[1] = the actual number of elements that could be packed (less than or equal to info[0])
        if (info[0] == info[1]) {
            // initialize the hashmap used to generate unique element names
            (void)strHashMapInit(&input->elemHashmap, 32U);
            LOG_INFO_EXT("- %s added to the packing process successfully\n", input->fullFileName.name);
        }
        else {
            if (input->explodeGroups) {
                LOG_ERROR_EXT("\nunable to pack some elements of %s, their size exceeds maximum atlas dimension; increase this value or specify a smaller scale on the document\n", input->fullFileName.name);
            }
            else {
                LOG_ERROR_EXT("\nunable to pack %s, its size exceeds maximum atlas dimension; increase this value or specify a smaller scale on the document\n", input->fullFileName.name);
            }
            ok = SVGT_FALSE;
        }
    }
    else {
        LOG_ERROR_EXT("\nfailed to add document %s to the packing process (AmanithSVG error code = %d)\n", input->fullFileName.name, err);
        ok = SVGT_FALSE;
    }

    return ok;
}

// get SVG filename corresponding to the given document handle (NULL if not found)
static AtlasInput* atlasInputFromDoc(const SVGTHandle docHandle,
                                     const CommandArguments* args) {

    size_t i;
    AtlasInput* result = NULL;

    for (i = 0U; (i < args->atlasInputs.size) && (result == NULL); ++i) {
        if (args->atlasInputs.data[i].docHandle == docHandle) {
            result = &args->atlasInputs.data[i];
        }
    }

    return result;
}

static void atlasPageElementName(char* uniqueName,
                                 AtlasInput* input,
                                 const char* elemId) {

    if (!input->explodeGroups) {
        // a whole SVG has been packed, in this case the uniquename is the SVG base filename
        (void)strcpy(uniqueName, input->baseFileName.name);
    }
    else {
        HashedElementData* value = NULL;
        const char* actualId = ((elemId == NULL) || (*elemId == '\0')) ? defaultUnassignedIdName : elemId;

        // if the 'actualId' already exists, it means that within the SVG file there are elements with
        // the same 'id' attribute, or there multiple elements with no 'id' attribute assigned
        if (strHashMapGet(&input->elemHashmap, actualId, &value)) {
            // advance counter
            value->u++;
            // generate unique name: <svg file basename> + '#' + <element 'id' attribute> (e.g. orc#head(1))
            sprintf(uniqueName, "%s#%s(%d)", input->baseFileName.name, actualId, value->u);
        }
        else {
            // start counter
            const HashedElementData v = { 0 };
            // put 'actualId' within the hashmap
            strHashMapPut(&input->elemHashmap, actualId, v);
            // generate unique name: <svg file basename> + '#' + <element 'id' attribute> (e.g. orc#head)
            sprintf(uniqueName, "%s#%s", input->baseFileName.name, actualId);
        }
    }
}

static SVGTboolean atlasPageDataCollect(const char* baseName,
                                        const SVGTuint pageWith,
                                        const SVGTuint pageHeight,
                                        const SVGTPackedRect* rects,
                                        const SVGTuint rectsCount,
                                        const CommandArguments* args,
                                        AtlasPage* page) {

    SVGTboolean ok;

    // initialize sprites array; because we know the size in advance, we can also allocate the whole neededmemory
    DYNARRAY_INIT_ALLOCATE(page->sprites, AtlasSprite, rectsCount)
    if ((ok = (page->sprites.data != NULL))) {

        // fix sprites array size
        page->sprites.size = rectsCount;
        // set basic fields
        (void)strcpy(page->baseName.name, baseName);
        page->width = pageWith;
        page->height = pageHeight;
      
        // generate sprites data
        for (SVGTuint i = 0U; i < rectsCount; ++i) {

            AtlasSprite* sprite = &page->sprites.data[i];
            // find the original input SVG file to which this sprite belongs
            AtlasInput* input = atlasInputFromDoc(rects[i].docHandle, args);

            // link the sprite to its page
            sprite->page = page;
            // generate a unique id/name for the sprite
            atlasPageElementName(sprite->id.buffer, input, rects[i].elemName);
            // original corner
            sprite->originalX = rects[i].originalX;
            sprite->originalY = rects[i].originalY;
            // corner position, within its atlas page
            sprite->x = rects[i].x;
            sprite->y = rects[i].y;
            // dimensions
            sprite->width = rects[i].width;
            sprite->height = rects[i].height;
            // SVG document handle
            sprite->docHandle = rects[i].docHandle;
            /* 0 for the whole SVG, else the element (tree) index. */
            //SVGTuint elemIdx;
            // z-order
            sprite->zOrder = rects[i].zOrder;
            // the used destination viewport width
            sprite->dstViewportWidth = rects[i].dstViewportWidth;
            // the used destination viewport height
            sprite->dstViewportHeight = rects[i].dstViewportHeight;
        }
    }
    
    return ok;
}

static SVGTboolean atlasPageRender(const SVGTuint index,
                                   const CommandArguments* args,
                                   AtlasExporter* exporter) {

    SVGTboolean ok;
    SVGTErrorCode err;
    FileName atlasBaseName, atlasFileName;
    SVGTuint pageInfo[3] = { 0 };

    // atlas-prefix + number
    (void)sprintf(atlasBaseName.name, "%s-%d", args->atlasPrefix, index);
    // output path + atlas-prefix + number + ".png"
    (void)sprintf(atlasFileName.name, "%s%s-%d.png", args->outputDir.path, args->atlasPrefix, index);

    // pageInfo[0] = page width, in pixels
    // pageInfo[1] = page height, in pixels
    // pageInfo[2] = number of packed elements inside the page
    err = svgtPackingBinInfo(index, pageInfo);
    if ((ok = (err == SVGT_NO_ERROR))) {

        // get packed elements
        const SVGTPackedRect* rects = svgtPackingBinRects(index);

        // draw collected groups/rectangles
        if ((rects != NULL) && (pageInfo[2] > 0)) {

            // create the drawing surface
            SVGTHandle svgSurface = svgtSurfaceCreate(pageInfo[0], pageInfo[1]);
            if ((ok = (svgSurface != SVGT_INVALID_HANDLE))) {

                // clear the drawing surface
                err = svgtSurfaceClear(svgSurface, args->clearColor[0], args->clearColor[1], args->clearColor[2], args->clearColor[3]);
                if ((ok = (err == SVGT_NO_ERROR))) {

                    // draw packed elements
                    err = svgtPackingRectsDraw(rects, pageInfo[2], svgSurface, SVGT_RENDERING_QUALITY_BETTER);
                    if ((ok = (err == SVGT_NO_ERROR))) {

                        // write atlas page PNG
                        if ((ok = pngWrite(svgSurface, atlasFileName.name, args))) {

                            // collect sprites
                            if ((ok = atlasPageDataCollect(atlasBaseName.name, pageInfo[0], pageInfo[1], rects, pageInfo[2], args, &exporter->pages.data[index]))) {
                                exporter->pages.size++;
                                LOG_INFO_EXT("- %s (%dx%d) written successfully\n", atlasFileName.name, pageInfo[0], pageInfo[1]);
                            }
                            else {
                                LOG_ERROR_EXT("\nfailed to collect sprites for atlas page %d\n", index);
                            }
                        }
                    }
                    else {
                        LOG_ERROR_EXT("\nerror drawing packed elements for atlas page %d (AmanithSVG error code = %d)\n", index, err);
                    }
                }
                else {
                    LOG_ERROR_EXT("\nerror clearing the drawing surface for atlas page %d (AmanithSVG error code = %d)\n", index, err);
                }

                // destroy the drawing surface
                (void)svgtSurfaceDestroy(svgSurface);
            }
            else {
                LOG_ERROR_EXT("\nfailed to create the drawing surface for atlas page %d (AmanithSVG error code = %d)\n", index, svgtGetLastError());
            }
        }
    }
    else {
        LOG_ERROR_EXT("\nfailed to get information about atlas page %d (AmanithSVG error code = %d)\n", index, err);
    }

    return ok;
}

SVGTboolean svg2BitmapAtlas(CommandArguments* args,
                            const ExternalResourceDynArray* fontResources,
                            const ExternalResourceDynArray* imageResources) {

    // load all SVG documents that will be used for atlas generation
    SVGTboolean ok = atlasDocumentsLoad(args);

    if (ok) {

        const SVGTuint maxDimension = (args->atlasMaxDimension <= 0) ? svgtSurfaceMaxDimension() : (SVGTuint)args->atlasMaxDimension;
        // start collecting the elements that will later be packed
        SVGTErrorCode err = svgtPackingBegin(maxDimension, args->atlasBorder, args->atlasPow2, args->scale);

        if ((ok = (err == SVGT_NO_ERROR))) {

            // loop over SVG documents
            for (size_t i = 0U; (i < args->atlasInputs.size) && ok; ++i) {
                ok = atlasDocumentAdd((SVGTuint)i, args);
            }

            if (ok) {
                // close the current packing task and perform the real packing algorithm
                err = svgtPackingEnd(SVGT_TRUE);
                if ((ok = (err == SVGT_NO_ERROR))) {

                    AtlasExporter exporter;
                    // get the number of atlas pages
                    const SVGTint pagesCount = svgtPackingBinsCount();

                    if (pagesCount > 0) {
                        // initialize the given atlas exporter
                        if ((ok = atlasExporterInit(&exporter, pagesCount))) {
                            // loop over atlas pages
                            for (SVGTint i = 0; (i < pagesCount) && ok; ++i) {
                                ok = atlasPageRender((SVGTuint)i, args, &exporter);
                            }
                            if (ok) {
                                // export atlas pages to file
                                if ((ok = atlasExport(&exporter, fontResources, imageResources, args))) {
                                    LOG_INFO("- Atlas data files written successfully\n");
                                }
                                else {
                                    LOG_ERROR("\nfailed to export atlas data files\n");
                                }
                            }
                            // release collected atlas pages and sprite sheets
                            atlasExporterRelease(&exporter);
                        }
                        else {
                            LOG_ERROR("\nfailed to initialize atlas exporter\n");
                        }
                    }
                    else {
                        LOG_INFO("- No altas page to write (perhaps empty SVG or ones with zero/negative dimensions were used?)\n");
                    }
                }
                else {
                    LOG_ERROR_EXT("\nfailed to perform the packing algorithm (AmanithSVG error code = %d)\n", err);
                }
            }
            else {
                // close the current packing task silently
                (void)svgtPackingEnd(SVGT_FALSE);
            }
        }
        else {
            LOG_ERROR_EXT("\nfailed to start the packing process (AmanithSVG error code = %d)\n", err);
        }
    }

    // release SVG documents used for atlas generation
    atlasDocumentsDestroy(args);

    return ok;
}
