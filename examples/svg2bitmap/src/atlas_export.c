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
    \file atlas_export.c
    \brief Atlas exporter, implementation.
    \author Matteo Muratori
    \author Michele Fabbri
*/

#include "json_utils.h"
#include "xml_utils.h"
#include "str_utils.h"
#include "atlas_export.h"

#define XML_HEADER_ROOT         "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
#define XML_HEADER_ROOT_PLIST   "<!DOCTYPE plist PUBLIC \"-//Apple Computer//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n"
#define XML_HEADER_COPYRIGHT    "<!-- Created with svg2bitmap, a tool for AmanithSVG (https://www.amanithsvg.com), by Mazatech (https://www.mazatech.com) -->\n"

// default pivot point
#define SPRITES_PIVOT_X                            0.5
#define SPRITES_PIVOT_Y                            0.5

#define MAPPED_SPRITE_NAME_FIELD                   "name"
// field/property name that identifies a mapped sprites's page/texture
#define MAPPED_SPRITE_PAGE_FIELD                   "texture"
// field/property name that identifies a mapped sprites's index/z-order
#define MAPPED_SPRITE_Z_ORDER_FIELD                "zOrder"
// field/property name that identifies a mapped sprites's instantiation offset
#define MAPPED_SPRITE_INSTANTIATION_OFFSET_PREFIX  "instantiation"
#define MAPPED_SPRITE_INSTANTIATION_OFFSET_FIELD   MAPPED_SPRITE_INSTANTIATION_OFFSET_PREFIX"Offset"

#define METADATA_APP_FIELD                         "app"
#define METADATA_APP_VALUE                         "https://www.amanithsvg.com"

// for each output format, the relatve file extension
static const char* atlasExtensions[ATLAS_CODE_LIBGDX_FORMAT - ATLAS_XML_GENERIC_FORMAT + 1] = {

    // ATLAS_XML_FORMAT
    ".xml",
    // ATLAS_COCOS2D_FORMAT
    ".plist",
    // ATLAS_JSON_ARRAY_FORMAT
    ".json",
    // ATLAS_JSON_HASH_FORMAT
    ".json",
    // ATLAS_PHASER2_FORMAT
    ".json",
    // ATLAS_PHASER3_FORMAT
    ".json",
    // ATLAS_PIXIJS_FORMAT
    ".json",
    // ATLAS_GODOT3_SPRITE_SHEET_FORMAT
    ".tpsheet",
    // ATLAS_GODOT3_TILE_SET_FORMAT
    ".tpset",
    // ATLAS_LIBGDX_FORMAT
    ".txt",
    // ATLAS_SPINE_FORMAT
    ".atlas",
    // ATLAS_CODE_C_FORMAT
    ".c",
    // ATLAS_CODE_LIBGDX_FORMAT
    ".java"
};

// get AmanithSVG version
const char* metadataAppVersion(void) {

    return svgtGetString(SVGT_VERSION);
}

SVGTboolean atlasExporterInit(AtlasExporter* exporter,
                              const SVGTuint pagesCount) {

    // initialize array of atlas pages
    //DYNARRAY_INIT(exporter->pages)
    // initialize hashmap used to generate unique element names
    //return strHashMapInit(&exporter->namesHashmap, 64);

    // initialize array of atlas pages
    DYNARRAY_INIT_ALLOCATE(exporter->pages, AtlasPage, pagesCount)
    return (exporter->pages.data != NULL) ? SVGT_TRUE : SVGT_FALSE;
}

void atlasExporterRelease(AtlasExporter* exporter) {

    for (size_t i = 0U; i < exporter->pages.size; ++i) {
        // destroy array of sprites
        AtlasPage* page = &exporter->pages.data[i];
        DYNARRAY_DESTROY(page->sprites)
    }
    // destroy array of pages
    DYNARRAY_DESTROY(exporter->pages)
}

static size_t atlasGuessFileSize(const AtlasExporter* exporter,
                                 const ExternalResourceDynArray* fontResources,
                                 const ExternalResourceDynArray* imageResources,
                                 const CommandArguments* args) {

    size_t size = 0U;

    switch (args->atlasFormat) {
        // XML based
        case ATLAS_XML_GENERIC_FORMAT:
        case ATLAS_COCOS2D_FORMAT:
            break;
        // JSON based
        case ATLAS_JSON_ARRAY_FORMAT:
        case ATLAS_JSON_HASH_FORMAT:
        case ATLAS_PHASER2_FORMAT:
        case ATLAS_PHASER3_FORMAT:
        case ATLAS_PIXIJS_FORMAT:
        case ATLAS_GODOT3_SPRITE_SHEET_FORMAT:
        case ATLAS_GODOT3_TILE_SET_FORMAT:
            // PNG filename + format, size, scale attributes
            size += exporter->pages.size * (sizeof(FileName) + 64U);
            // take into account sprites
            for (size_t i = 0U; i < exporter->pages.size; ++i) {
                const AtlasPage* page = &exporter->pages.data[i];
                // name + rotated, trimmed, sourceSize, spriteSourceSize, frame attributes
                size += page->sprites.size * (sizeof(FileName) + 128U);
            }
            // metadata fixed fields
            size += 256U;
            // metadata "related_multi_packs" attribute
            size += exporter->pages.size * sizeof(FileName);
            break;
        // simple text
        case ATLAS_LIBGDX_FORMAT:
        case ATLAS_SPINE_FORMAT:
            // PNG filename, size, format, filter, repeat attributes
            size += exporter->pages.size * (sizeof(FileName) + 128U);
            // take into account sprites
            for (size_t i = 0U; i < exporter->pages.size; ++i) {
                const AtlasPage* page = &exporter->pages.data[i];
                // name, rotate, xy, size, orig, offset, index attributes
                size += page->sprites.size * (sizeof(FileName) + 128U);
            }
            break;
        case ATLAS_CODE_C_FORMAT:
        case ATLAS_CODE_LIBGDX_FORMAT:
        default:
            // fonts and images tables
            size += fontResources->size * (sizeof(FileName) + 32U);
            size += imageResources->size * (sizeof(FileName) + 32U);
            // input SVG table
            size += args->atlasInputs.size * (sizeof(FileName) + 64U);
            // fixed size (setup code)
            size += 8192U;
            break;
    }

    // add a multiplicator, just to be safe
    return size * 2;
}

static SVGTboolean atlasFormatIsSingleFile(const AtlasFormat format) {

    SVGTboolean isSingleFile;

    switch (format) {
        // XML based
        case ATLAS_XML_GENERIC_FORMAT:
        case ATLAS_COCOS2D_FORMAT:
            isSingleFile = SVGT_TRUE;
            break;
        // JSON based
        case ATLAS_JSON_ARRAY_FORMAT:
        case ATLAS_JSON_HASH_FORMAT:
        case ATLAS_PHASER2_FORMAT:
        case ATLAS_PIXIJS_FORMAT:
            isSingleFile = SVGT_FALSE;
            break;
        case ATLAS_PHASER3_FORMAT:
        case ATLAS_GODOT3_SPRITE_SHEET_FORMAT:
        case ATLAS_GODOT3_TILE_SET_FORMAT:
            isSingleFile = SVGT_TRUE;
            break;
        // simple text
        case ATLAS_LIBGDX_FORMAT:
        case ATLAS_SPINE_FORMAT:
        case ATLAS_CODE_C_FORMAT:
        case ATLAS_CODE_LIBGDX_FORMAT:
        default:
            isSingleFile = SVGT_TRUE;
            break;
    }

    return isSingleFile;
}

static SVGTboolean atlasWriteBuffer(const AtlasFormat format,
                                    const char* buffer,
                                    const char* outputPath,
                                    const char* outputName) {

    FILE* f;
    FileName fname;
    SVGTboolean ok;

    // <output path> + <atlas name> + <format extension>
    (void)sprintf(fname.name, "%s%s%s", outputPath, outputName, atlasExtensions[format]);
    // open file for writing
    f = fopen(fname.name, "wt");

    if ((ok = (f != NULL))) {
        // write buffer to file
        ok = (fputs(buffer, f) != EOF);
        // close file
        (void)fclose(f);
    }

    return ok;
}

// compare two sprites by z-order
static SVGTint atlasSpritesCompareByZ(const void* sprite0,
                                      const void* sprite1) {

    const AtlasSpritePtr* s0 = sprite0;
    const AtlasSpritePtr* s1 = sprite1;

    return ((*s0)->zOrder - (*s1)->zOrder);
}

// find the list of sprites that belong to the given SVG document (handle)
static void atlasMapSpritesCollect(const AtlasInput* input,
                                   const AtlasPageDynArray* pages,
                                   AtlasSpritePtrDynArray* sprites,
                                   FileName* svg) {

    // initialize the list of sprite pointers
    DYNARRAY_INIT(*sprites)

    // collect the list of sprites belonging to the SVG input file
    for (size_t i = 0U; i < pages->size; ++i) {
        // get the atlas page
        const AtlasPage* page = &pages->data[i];
        // loop over pages's sprites
        for (size_t j = 0U; j < page->sprites.size; ++j) {
            // get the sprite
            AtlasSprite* sprite = &page->sprites.data[j];
            // if the document handle matches, push the sprite into the list
            if (sprite->docHandle == input->docHandle) {
                DYNARRAY_PUSH_BACK(*sprites, AtlasSpritePtr, sprite)
            }
        }
    }

    // sort sprites by z-order
    if (sprites->data != NULL) {
        qsort(sprites->data, sprites->size, sizeof(AtlasSpritePtr), atlasSpritesCompareByZ);
    }

    // SVG filename (extension included)
    extractFileName(svg->name, input->fullFileName.name, SVGT_TRUE);
}

// calculate basic information for a mapped sprite
static void atlasMapSpriteInfo(const AtlasSprite* sprite,
                               FileName* png,
                               double* ofsX,
                               double* ofsY) {

    const AtlasPage* page = sprite->page;

    // atlas page, PNG file
    (void)sprintf(png->name, "%s.png", page->baseName.name);
    // make sure that the center of "destination viewport" of the original document
    // will correspond to (0, 0) in world space; in other words, if a sprite pivot
    // point is located at the center of "destination viewport", then it will be
    // (0, 0) in world space
    *ofsX = (((sprite->width * SPRITES_PIVOT_X) + sprite->originalX)) - (sprite->dstViewportWidth / 2.0);
    *ofsY = (sprite->dstViewportHeight / 2.0) - (((sprite->height * (1.0 - SPRITES_PIVOT_Y)) + sprite->originalY));
}

/************************************************************
                            XML
************************************************************/

// export the data for the given atlas page, Basic XML and Cocos2D formats
static SVGTboolean atlasPageExportXML(const AtlasPage* page,
                                      const SVGTuint pageIndex,
                                      const CommandArguments* args) {

    char* xml;
    FileName png = { 0 };
    char header[2048] = { 0 };
    FileName outputName = { 0 };
    SVGTboolean ok = SVGT_TRUE;
    // create XML document (i.e. the root node)
    xml_doc doc = xml_doc_create();
    
    // PNG filename
    (void)sprintf(png.buffer, "%s.png", page->baseName.name);

    // xml generic
    if (args->atlasFormat == ATLAS_XML_GENERIC_FORMAT) {

        // <TextureAtlas> node
        xml_node* atlasNode = xml_node_add(doc.root, "TextureAtlas", NULL);
        // <TextureAtlas> attributes
        //
        // PNG file        
        xml_attr_str_add(atlasNode, "imagePath", png.name);
        // width
        xml_attr_int_add(atlasNode, "width", page->width);
        // height
        xml_attr_int_add(atlasNode, "height", page->height);
        // format
        xml_attr_str_add(atlasNode, "format", (args->pixelFormat == FORMAT_RGBA) ? "RGBA8888" : "BGRA8888");
        // alpha premultiplication
        xml_attr_str_add(atlasNode, "premultiplyAlpha", "false");

        // loop over sprites
        for (size_t i = 0U; i < page->sprites.size; ++i) {

            const AtlasSprite* sprite = &page->sprites.data[i];

            // <sprite> node
            xml_node* spriteNode = xml_node_add(atlasNode, "sprite", NULL);
            // <sprite> attributes
            //
            // n = name of the sprite
            xml_attr_str_add(spriteNode, "n", sprite->id.name);
            // x = sprite x pos in texture
            xml_attr_int_add(spriteNode, "x", sprite->x);
            // y = sprite y pos in texture
            xml_attr_int_add(spriteNode, "y", sprite->y);
            // width = sprite width
            xml_attr_int_add(spriteNode, "w", sprite->width);
            // height = sprite height
            xml_attr_int_add(spriteNode, "h", sprite->height);
            // pivot point
            xml_attr_str_add(spriteNode, "pX", "0.5");
            xml_attr_str_add(spriteNode, "pY", "0.5");

        }

        // build header
        (void)strcpy(header, XML_HEADER_ROOT);
        (void)strcat(header, XML_HEADER_COPYRIGHT);
        (void)strcat(header, "<!-- Format:\n");
        (void)strcat(header, "'n'  => name of the sprite\n");
        (void)strcat(header, "'x'  => sprite x-position in texture\n");
        (void)strcat(header, "'y'  => sprite y-position in texture\n");
        (void)strcat(header, "'w'  => sprite width\n");
        (void)strcat(header, "'h'  => sprite height\n");
        (void)strcat(header, "'pX' => x-position of the pivot point (a normalized value in the range [0; 1], relative to sprite width)\n");
        (void)strcat(header, "'pY' => y-position of the pivot point (a normalized value in the range [0; 1], relative to sprite height)\n");
        (void)strcat(header, "-->\n");
    }
    // cocos2d
    else {
        char s[64] = { 0 };
        // <plist> node
        xml_node* plistNode = xml_node_add(doc.root, "plist", NULL);
        xml_attr_str_add(plistNode, "version", "1.0");
        // <dict> node
        xml_node* dictNode = xml_node_add(plistNode, "dict", NULL);
        // <key>frames</key>
        (void)xml_node_add(dictNode, "key", "frames");
        // <dict>
        xml_node* dictFramesNode = xml_node_add(dictNode, "dict", NULL);

        // loop over sprites
        for (size_t i = 0U; i < page->sprites.size; ++i) {

            const AtlasSprite* sprite = &page->sprites.data[i];

            // e.g. <key>orc#head</key>
            (void)xml_node_add(dictFramesNode, "key", sprite->id.name);
            // <dict>
            xml_node* spriteDictNode = xml_node_add(dictFramesNode, "dict", NULL);
            // sprite attributes
            (void)xml_node_add(spriteDictNode, "key", "aliases");
            (void)xml_node_add(spriteDictNode, "array", NULL);
            (void)xml_node_add(spriteDictNode, "key", "spriteOffset");
            (void)xml_node_add(spriteDictNode, "string", "{0,0}");
            (void)xml_node_add(spriteDictNode, "key", "spriteSize");
            (void)sprintf(s, "{%d,%d}", sprite->width, sprite->height);
            (void)xml_node_add(spriteDictNode, "string", s);
            (void)xml_node_add(spriteDictNode, "key", "spriteSourceSize");
            (void)xml_node_add(spriteDictNode, "string", s);
            (void)xml_node_add(spriteDictNode, "key", "textureRect");
            (void)sprintf(s, "{{%d,%d},{%d,%d}}", sprite->x, sprite->y, sprite->width, sprite->height);
            (void)xml_node_add(spriteDictNode, "string", s);
            (void)xml_node_add(spriteDictNode, "key", "pivot");
            (void)xml_node_add(spriteDictNode, "string", "{0.5,0.5}");
            (void)xml_node_add(spriteDictNode, "key", "textureRotated");
            (void)xml_node_add(spriteDictNode, "false", NULL);
        }

        // <key>metadata</key>
        (void)xml_node_add(dictNode, "key", "metadata");
        // <dict>
        xml_node* dictMetadataNode = xml_node_add(dictNode, "dict", NULL);
        (void)xml_node_add(dictMetadataNode, "key", "format");
        (void)xml_node_add(dictMetadataNode, "integer", "3");
        (void)xml_node_add(dictMetadataNode, "key", "pixelFormat");
        // Cocos2D does not support BGRA8888 enumeration (see https://github.com/cocos/cocos-engine/blob/v3.7.0/cocos/asset/assets/asset-enum.ts)
        (void)xml_node_add(dictMetadataNode, "string", "RGBA8888");
        (void)xml_node_add(dictMetadataNode, "key", "premultiplyAlpha");
        (void)xml_node_add(dictMetadataNode, "false", NULL);
        (void)xml_node_add(dictMetadataNode, "key", "realTextureFileName");
        (void)xml_node_add(dictMetadataNode, "string", png.name);
        (void)xml_node_add(dictMetadataNode, "key", "size");
        (void)sprintf(s, "{%d,%d}", page->width, page->height);
        (void)xml_node_add(dictMetadataNode, "string", s);
        (void)xml_node_add(dictMetadataNode, "key", "textureFileName");
        (void)xml_node_add(dictMetadataNode, "string", png.name);

        // build header
        (void)strcpy(header, XML_HEADER_ROOT);
        (void)strcat(header, XML_HEADER_ROOT_PLIST);
        (void)strcat(header, XML_HEADER_COPYRIGHT);
        (void)strcat(header, "<!-- Each sprite has a unique name, other useful properties are:\n");
        (void)strcat(header, "'spriteSize'      => sprite dimensions (width, height)\n");
        (void)strcat(header, "'textureRect'     => sprite region in texture (x, y, width, height)\n");
        (void)strcat(header, "'pivot'           => position of the pivot point (normalized values in the range [0; 1] relative, respectively, to sprite width and height)\n");
        (void)strcat(header, "'textureRotated'  => true if sprite is rotated by 90 degrees, else false (i.e. no rotation)\n");
        (void)strcat(header, "-->\n");
    }

    // get xml
    xml = xml_doc_serialize(&doc, header);

    // write the file    
    (void)sprintf(outputName.name, "%s-%d", args->atlasPrefix, pageIndex);
    ok = atlasWriteBuffer(args->atlasFormat, xml, args->outputDir.path, outputName.name);

    // we have finished
    free(xml);
    xml_doc_free(&doc);
    return ok;
}

// export atlas data file, Basic XML and Cocos2D formats
static SVGTboolean atlasExportXML(const AtlasExporter* exporter,
                                  const CommandArguments* args) {

    SVGTboolean ok = SVGT_TRUE;

    // basic XML, cocos2d; NB: they both export multiple file
    //
    // loop over atlas pages
    for (SVGTuint i = 0U; (i < exporter->pages.size) && ok; ++i) {
        // export atlas page
        ok = atlasPageExportXML(&exporter->pages.data[i], i, args);
    }

    return ok;
}

// export atlas map file, Basic XML and Cocos2D formats
static SVGTboolean atlasMapXML(const AtlasExporter* exporter,
                               const CommandArguments* args) {

    char* xml;
    FileName map = { 0 };
    char header[2048] = { 0 };
    SVGTboolean ok = SVGT_TRUE;
    // create XML document (i.e. the root node)
    xml_doc doc = xml_doc_create();

    // xml generic
    if (args->atlasFormat == ATLAS_XML_GENERIC_FORMAT) {

        // <AtlasMap> node
        xml_node* atlasMapNode = xml_node_add(doc.root, "AtlasMap", NULL);

        // loop over SVG input files
        for (size_t i = 0U; i < args->atlasInputs.size; ++i) {

            FileName svg = { 0 };
            AtlasSpritePtrDynArray sprites = { 0 };

            // collect the list of sprites belonging to the SVG input file
            atlasMapSpritesCollect(&args->atlasInputs.data[i], &exporter->pages, &sprites, &svg);

            // <map> node
            xml_node* mapNode = xml_node_add(atlasMapNode, "map", NULL);
            // the name of the original SVG input file
            xml_attr_str_add(mapNode, "svg", svg.name);

            // loop over sprites belonging to the SVG input file
            for (size_t j = 0U; j < sprites.size; ++j) {

                FileName png = { 0 };
                double ofsX = 0.0, ofsY = 0.0;
                const AtlasSprite* sprite = sprites.data[j];

                // get sprite information (map)
                atlasMapSpriteInfo(sprite, &png, &ofsX, &ofsY);

                // add <sprite> node and its attributes
                xml_node* spriteNode = xml_node_add(mapNode, "sprite", NULL);
                // sprite unique name
                xml_attr_str_add(spriteNode, MAPPED_SPRITE_NAME_FIELD, sprite->id.name);
                // atlas page, PNG file
                xml_attr_str_add(spriteNode, MAPPED_SPRITE_PAGE_FIELD, png.name);
                // sprite z-order
                xml_attr_int_add(spriteNode, MAPPED_SPRITE_Z_ORDER_FIELD, sprite->zOrder);
                // sprite instantiation offset
                xml_attr_double_add(spriteNode, MAPPED_SPRITE_INSTANTIATION_OFFSET_PREFIX"X", ofsX);
                xml_attr_double_add(spriteNode, MAPPED_SPRITE_INSTANTIATION_OFFSET_PREFIX"Y", ofsY);
            }

            // release the list of sprite pointers
            DYNARRAY_DESTROY(sprites)

            // build header
            (void)strcpy(header, XML_HEADER_ROOT);
            (void)strcat(header, XML_HEADER_COPYRIGHT);
            (void)strcat(header, "<!-- For each input file used to generate the atlas:\n");
            (void)strcat(header, "'svg' => the original SVG file\n");
            (void)strcat(header, "then it follows a list of mapped sprites\n");
            (void)strcat(header, "-->\n");
            (void)strcat(header, "<!-- Format of each mapped sprite:\n");
            (void)strcat(header, "'"MAPPED_SPRITE_NAME_FIELD"' => unique name of the sprite\n");
            (void)strcat(header, "'"MAPPED_SPRITE_PAGE_FIELD"' => atlas page, PNG file where sprite is located\n");
            (void)strcat(header, "'"MAPPED_SPRITE_Z_ORDER_FIELD"' => sprite z-order\n");
            (void)strcat(header, "'"MAPPED_SPRITE_INSTANTIATION_OFFSET_PREFIX"X""' => sprite instantiation offset, x coordinate\n");
            (void)strcat(header, "'"MAPPED_SPRITE_INSTANTIATION_OFFSET_PREFIX"Y""' => sprite instantiation offset, y coordinate\n");
            (void)strcat(header, "-->\n");
        }
    }
    // cocos2d
    else {
        // <plist> node
        xml_node* plistNode = xml_node_add(doc.root, "plist", NULL);
        xml_attr_str_add(plistNode, "version", "1.0");
        // <dict> node
        xml_node* dictNode = xml_node_add(plistNode, "dict", NULL);
        // <key>maps</key>
        (void)xml_node_add(dictNode, "key", "maps");
        xml_node* dictMapsNode = xml_node_add(dictNode, "dict", NULL);

        // loop over SVG input files
        for (size_t i = 0U; i < args->atlasInputs.size; ++i) {

            FileName svg = { 0 };
            AtlasSpritePtrDynArray sprites = { 0 };

            // collect the list of sprites belonging to the SVG input file
            atlasMapSpritesCollect(&args->atlasInputs.data[i], &exporter->pages, &sprites, &svg);

            // the name of the original SVG input file
            (void)xml_node_add(dictMapsNode, "key", svg.name);

            // according to the map format, the list of sprites can be an array (keeping order by z) or a dictionary (where sprite name is the key)
            xml_node* spritesCollectionNode = xml_node_add(dictMapsNode, (args->atlasMapFormat == ATLAS_MAP_ARRAY_FORMAT) ? "array" : "dict", NULL);

            // loop over sprites belonging to the SVG input file
            for (size_t j = 0U; j < sprites.size; ++j) {

                xml_node* spriteNode;
                char s[64] = { 0 };
                FileName png = { 0 };
                double ofsX = 0.0, ofsY = 0.0;
                const AtlasSprite* sprite = sprites.data[j];

                // get sprite information (map)
                atlasMapSpriteInfo(sprite, &png, &ofsX, &ofsY);

                if (args->atlasMapFormat == ATLAS_MAP_ARRAY_FORMAT) {
                    // the sprites list is an array, each sprite is a dictioanary
                    spriteNode = xml_node_add(spritesCollectionNode, "dict", NULL);
                    // sprite unique name
                    (void)xml_node_add(spriteNode, "key", MAPPED_SPRITE_NAME_FIELD);
                    (void)xml_node_add(spriteNode, "string", sprite->id.name);
                }
                else {
                    // the sprites list is a dictionary, each sprite is a dictionary where its name is the key
                    (void)xml_node_add(spritesCollectionNode, "key", sprite->id.name);
                    spriteNode = xml_node_add(spritesCollectionNode, "dict", NULL);
                }

                // atlas page, PNG file
                (void)xml_node_add(spriteNode, "key", MAPPED_SPRITE_PAGE_FIELD);
                (void)xml_node_add(spriteNode, "string", png.name);
                // sprite z-order
                (void)xml_node_add(spriteNode, "key", MAPPED_SPRITE_Z_ORDER_FIELD);
                (void)sprintf(s, "%d", sprite->zOrder);
                (void)xml_node_add(spriteNode, "integer", s);
                // sprite instantiation offset
                (void)xml_node_add(spriteNode, "key", MAPPED_SPRITE_INSTANTIATION_OFFSET_FIELD);
                (void)sprintf(s, "{%f,%f}", ofsX, ofsY);
                (void)xml_node_add(spriteNode, "string", s);
            }

            // release the list of sprite pointers
            DYNARRAY_DESTROY(sprites)

            // build header
            (void)strcpy(header, XML_HEADER_ROOT);
            (void)strcat(header, XML_HEADER_ROOT_PLIST);
            (void)strcat(header, XML_HEADER_COPYRIGHT);
        }
    }

    // get xml
    xml = xml_doc_serialize(&doc, header);
    // map filename (without extension)
    (void)sprintf(map.name, "%s-map", args->atlasPrefix);
    // write the file    
    ok = atlasWriteBuffer(args->atlasFormat, xml, args->outputDir.path, map.name);

    // we have finished
    free(xml);
    xml_doc_free(&doc);
    return ok;
}

/************************************************************
                           JSON
************************************************************/
static void atlasPageMetadataJSON(const AtlasPage* page,
                                  const SVGTuint pageIndex,
                                  const SVGTuint pagesCount,
                                  const CommandArguments* args) {

    jwObj_object("metadata");
    jwObj_string(METADATA_APP_FIELD, METADATA_APP_VALUE);
    jwObj_string("version", (char*)metadataAppVersion());

    if (page != NULL) {

        FileName png = { 0 };
        (void)sprintf(png.buffer, "%s.png", page->baseName.name);

        jwObj_string("image", png.name);
        //jwObj_string("format", "RGBA8888");
        jwObj_string("format", (args->pixelFormat == FORMAT_RGBA) ? "RGBA8888" : "BGRA8888");
        // alpha premultiplication
        jwObj_bool("premultiplyAlpha", SVGT_FALSE);
        jwObj_object("size");
            jwObj_int("w", page->width);
            jwObj_int("h", page->height);
        jwEnd();
        jwObj_string("scale", "1");
        if (pagesCount > 1U) {
            jwObj_array("related_multi_packs");
            for (SVGTuint i = 0U; i < pagesCount; ++i) {
                if (i != pageIndex) {
                    FileName json;
                    (void)sprintf(json.name, "%s-%d.json", args->atlasPrefix, i);
                    jwArr_string(json.name);
                }
            }
            jwEnd();
        }
    }

    jwEnd(); // end of metadata object
}

// export the data for the given atlas page, JSON-based formats
static SVGTboolean atlasPageExportJSON(const AtlasPage* page,
                                       const SVGTuint pageIndex,
                                       const SVGTuint pagesCount,
                                       const CommandArguments* args,
                                       char* buffer,
                                       const size_t bufferSize) {

    SVGTboolean ok = SVGT_TRUE;

    // Godot 3
    // Phaser 3
    if (atlasFormatIsSingleFile(args->atlasFormat)) {

        FileName png = { 0 };
        (void)sprintf(png.buffer, "%s.png", page->baseName.name);

        // a texture object
        jwArr_object();
            jwObj_string("image", png.name);
            // format
            jwObj_string("format", (args->pixelFormat == FORMAT_RGBA) ? "RGBA8888" : "BGRA8888");
            // alpha premultiplication
            jwObj_bool("premultiplyAlpha", SVGT_FALSE);

            // size object (width, height)
            jwObj_object("size");
                jwObj_int("w", page->width);
                jwObj_int("h", page->height);
            jwEnd();
            // scale
            if (args->atlasFormat == ATLAS_PHASER3_FORMAT) {
                jwObj_int("scale", 1);
            }
            // sprites / frames array of objects
            jwObj_array((args->atlasFormat == ATLAS_PHASER3_FORMAT) ? "frames" : "sprites");
            // loop over sprites
            for (size_t i = 0U; i < page->sprites.size; ++i) {
                AtlasSprite* sprite = &page->sprites.data[i];
                jwArr_object();
                    // NB: it should be "filename", but in our case it is just a name (e.g. "orc#head")
                    jwObj_string("name", sprite->id.name);
                    if (args->atlasFormat == ATLAS_PHASER3_FORMAT) {
                        jwObj_bool("rotated", SVGT_FALSE);
                        jwObj_bool("trimmed", SVGT_FALSE);
                        // sourceSize
                        jwObj_object("sourceSize");
                            jwObj_int("w", sprite->width);
                            jwObj_int("h", sprite->height);
                        jwEnd();
                        // spriteSourceSize
                        jwObj_object("spriteSourceSize");
                            jwObj_int("x", 0);
                            jwObj_int("y", 0);
                            jwObj_int("w", sprite->width);
                            jwObj_int("h", sprite->height);
                        jwEnd();
                        // frame
                        jwObj_object("frame");
                            jwObj_int("x", sprite->x);
                            jwObj_int("y", sprite->y);
                            jwObj_int("w", sprite->width);
                            jwObj_int("h", sprite->height);
                        jwEnd();
                    }
                    // Godot 3
                    else {
                        // region
                        jwObj_object("region");
                            jwObj_int("x", sprite->x);
                            jwObj_int("y", sprite->y);
                            jwObj_int("w", sprite->width);
                            jwObj_int("h", sprite->height);
                        jwEnd();
                        // margin
                        jwObj_object("margin");
                            jwObj_int("x", 0);
                            jwObj_int("y", 0);
                            jwObj_int("w", 0);
                            jwObj_int("h", 0);
                        jwEnd();
                    }
                    // pivot
                    jwObj_object("pivot");
                        jwObj_double("x", SPRITES_PIVOT_X);
                        jwObj_double("y", SPRITES_PIVOT_Y);
                    jwEnd();

                jwEnd();  // end sprite object
            }
            jwEnd();  // end sprites / frames array
        ok = (jwEnd() == JWRITE_OK); // end texture object

        // TO DO metadata
    }
    // JSON array
    // JSON hash
    // Phaser 2
    // PixiJS
    else {
        // start root object (it will clear the buffer too)
        jwOpen(buffer, (SVGTuint)bufferSize, JW_OBJECT, JW_PRETTY);
        if (args->atlasFormat == ATLAS_JSON_ARRAY_FORMAT) {
            // "frames" array
            jwObj_array("frames");
        }
        else {
            // "frames" object
            jwObj_object("frames");
        }

        // loop over sprites
        for (size_t i = 0U; i < page->sprites.size; ++i) {
            AtlasSprite* sprite = &page->sprites.data[i];
            // open sprite
            if (args->atlasFormat == ATLAS_JSON_ARRAY_FORMAT) {
                jwArr_object();
                // NB: it should be "filename", but in our case it is just a name (e.g. "orc#head")
                jwObj_string("name", sprite->id.name);
            }
            else {
                jwObj_object(sprite->id.name);
            }
                // common attributes
                jwObj_object("frame");
                    jwObj_int("x", sprite->x);
                    jwObj_int("y", sprite->y);
                    jwObj_int("w", sprite->width);
                    jwObj_int("h", sprite->height);
                jwEnd();
                jwObj_bool("rotated", SVGT_FALSE);
                jwObj_bool("trimmed", SVGT_FALSE);
                jwObj_object("spriteSourceSize");
                    jwObj_int("x", 0);
                    jwObj_int("y", 0);
                    jwObj_int("w", sprite->width);
                    jwObj_int("h", sprite->height);
                jwEnd();
                jwObj_object("sourceSize");
                    jwObj_int("w", sprite->width);
                    jwObj_int("h", sprite->height);
                jwEnd();
                // pivot
                jwObj_object("pivot");
                    jwObj_double("x", SPRITES_PIVOT_X);
                    jwObj_double("y", SPRITES_PIVOT_Y);
                jwEnd();
            jwEnd();  // end sprite
        }

        // end of "frames" array / object
        jwEnd();

        // append metadata object
        atlasPageMetadataJSON(page, pageIndex, pagesCount, args);

        // we have finished, write file
        if ((ok = (jwClose() == JWRITE_OK))) {
            FileName outputName;
            (void)sprintf(outputName.name, "%s-%d", args->atlasPrefix, pageIndex);
            ok = atlasWriteBuffer(args->atlasFormat, buffer, args->outputDir.path, outputName.name);
        }

    }

    return ok;
}

// export atlas data file, JSON-based formats
static SVGTboolean atlasExportJSON(const AtlasExporter* exporter,
                                   const CommandArguments* args,
                                   char* buffer,
                                   const size_t bufferSize) {

    SVGTboolean ok = SVGT_TRUE;
    const SVGTuint n = (SVGTuint)exporter->pages.size;

    // if the format involves a single file, open the "header" just once (Godot 3, Phaser3)
    if (atlasFormatIsSingleFile(args->atlasFormat)) {
        // start root object (it will clear the buffer too)
        jwOpen(buffer, (SVGTuint)bufferSize, JW_OBJECT, JW_PRETTY);
        // "textures" array
        jwObj_array("textures");
    }

    // loop over atlas pages
    for (SVGTuint i = 0U; (i < n) && ok; ++i) {
        // export atlas page
        ok = atlasPageExportJSON(&exporter->pages.data[i], i, n, args, buffer, bufferSize);
    }

    if (ok) {
        // if the format involves a single file, close it (Godot 3, Phaser3)
        if (atlasFormatIsSingleFile(args->atlasFormat)) {
            // end of "textures" array
            jwEnd();
            // append metadata object
            atlasPageMetadataJSON(NULL, 0U, 0U, args);
            // we have finished, write file
            if ((ok = (jwClose() == JWRITE_OK))) {
                ok = atlasWriteBuffer(args->atlasFormat, buffer, args->outputDir.path, args->atlasPrefix);
            }
        }
    }

    return ok;
}

// export atlas map file, JSON-based formats
static SVGTboolean atlasMapJSON(const AtlasExporter* exporter,
                                const CommandArguments* args,
                                char* buffer,
                                const size_t bufferSize) {

    SVGTboolean ok;

    // start root object (it will clear the buffer too)
    jwOpen(buffer, (SVGTuint)bufferSize, JW_OBJECT, JW_PRETTY);

    // "maps" object
    jwObj_object("maps");

    // loop over SVG input files
    for (size_t i = 0U; i < args->atlasInputs.size; ++i) {

        FileName svg = { 0 };
        AtlasSpritePtrDynArray sprites = { 0 };
        
        // collect the list of sprites belonging to the SVG input file
        atlasMapSpritesCollect(&args->atlasInputs.data[i], &exporter->pages, &sprites, &svg);

        // the name of the original SVG input file
        jwObj_object(svg.name);

        if (args->atlasMapFormat == ATLAS_MAP_ARRAY_FORMAT) {
            // sprites array
            jwObj_array("sprites");
        }
        else {
            // sprites object
            jwObj_object("sprites");
        }

        for (size_t j = 0U; j < sprites.size; ++j) {

            FileName png = { 0 };
            double ofsX = 0.0, ofsY = 0.0;
            AtlasSprite* sprite = sprites.data[j];

            // get sprite information (map)
            atlasMapSpriteInfo(sprite, &png, &ofsX, &ofsY);

            if (args->atlasMapFormat == ATLAS_MAP_ARRAY_FORMAT) {
                jwArr_object();
                // sprite unique name
                jwObj_string(MAPPED_SPRITE_NAME_FIELD, sprite->id.name);
            }
            else {
                // sprite unique name
                jwObj_object(sprite->id.name);
            }

            // atlas page, PNG file
            jwObj_string(MAPPED_SPRITE_PAGE_FIELD, png.name);
            // sprite z-order
            jwObj_int(MAPPED_SPRITE_Z_ORDER_FIELD, sprite->zOrder);
            // sprite instantiation offset
            jwObj_object(MAPPED_SPRITE_INSTANTIATION_OFFSET_FIELD);
                jwObj_double("x", ofsX);
                jwObj_double("y", ofsY);
            jwEnd();

            // close sprite
            jwEnd();
        }

        // end of sprites array/object
        jwEnd();
        // end of SVG input file object
        jwEnd();

        // release the list of sprite pointers
        DYNARRAY_DESTROY(sprites)
    }

    // end of "maps" array
    jwEnd();

    // append metadata object (just the app name and version)
    atlasPageMetadataJSON(NULL, 0U, 0U, args);

    // we have finished, write map file
    if ((ok = (jwClose() == JWRITE_OK))) {

        FileName map = { 0 };

        // map filename (without extension)
        (void)sprintf(map.name, "%s-map", args->atlasPrefix);

        // pass json array format, so that file extension will be ".json"
        ok = atlasWriteBuffer(ATLAS_JSON_ARRAY_FORMAT, buffer, args->outputDir.path, map.name);
    }

    return ok;
}

/************************************************************
                           Text (code)
************************************************************/
static SVGTboolean atlasExportCodeC(const ExternalResourceDynArray* fontResources,
                                    const ExternalResourceDynArray* imageResources,
                                    const CommandArguments* args,
                                    char* buffer) {

    size_t i;
    FileName f = { 0 };
    char s[1024] = { 0 };

    (void)strcat(buffer, "// AmanithSVG\n");
    (void)strcat(buffer, "#include <SVGT/svgt.h>\n");
    (void)strcat(buffer, "// standard library\n");
    (void)strcat(buffer, "#include <stdio.h>\n");
    (void)strcat(buffer, "#include <stdlib.h>\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "// an external in-memory resource\n");
    (void)strcat(buffer, "typedef struct {\n");
    (void)strcat(buffer, "    // full filename\n");
    (void)strcat(buffer, "    const char* fileName;\n");
    (void)strcat(buffer, "    // the file loaded into memory\n");
    (void)strcat(buffer, "    const SVGTubyte* buffer;\n");
    (void)strcat(buffer, "    size_t bufferSize;\n");
    (void)strcat(buffer, "    // bitwise OR of values from the SVGTResourceHint enumeration\n");
    (void)strcat(buffer, "    SVGTbitfield hints;\n");
    (void)strcat(buffer, "} ExternalResource;\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "// an input SVG file for the atlas mode\n");
    (void)strcat(buffer, "typedef struct {\n");
    (void)strcat(buffer, "    // SVG filename\n");
    (void)strcat(buffer, "    const char* fileName;\n");
    (void)strcat(buffer, "    // scale (a positive number)\n");
    (void)strcat(buffer, "    SVGTfloat scale;\n");
    (void)strcat(buffer, "    // SVGT_FALSE to pack the whole SVG document, SVGT_TRUE to pack instead each first-level element separately\n");
    (void)strcat(buffer, "    SVGTboolean explodeGroups;\n");
    (void)strcat(buffer, "    // AmanithSVG document handle\n");
    (void)strcat(buffer, "    SVGTHandle docHandle;\n");
    (void)strcat(buffer, "} AtlasInput;\n");

    // font files
    if (fontResources->size > 0U) {
        (void)strcat(buffer, "\n");
        (void)strcat(buffer, "// font files\n");
        (void)sprintf(s, "#define FONTS_COUNT %d\n", (SVGTuint)fontResources->size);
        (void)strcat(buffer, s);
        (void)strcat(buffer, "static ExternalResource fonts[FONTS_COUNT] = {\n");
        for (i = 0U; i < (fontResources->size - 1U); ++i) {
            extractFileName(f.name, fontResources->data[i].fileName.name, SVGT_TRUE);
            (void)sprintf(s, "    { \"%s\", NULL, 0U, 0U },\n", f.name);
            (void)strcat(buffer, s);
        }
        extractFileName(f.name, fontResources->data[i].fileName.name, SVGT_TRUE);
        (void)sprintf(s, "    { \"%s\", NULL, 0U, 0U }\n", f.name);
        (void)strcat(buffer, s);
        (void)strcat(buffer, "};\n");
    }

    // images files
    if (imageResources->size > 0U) {
        (void)strcat(buffer, "\n");
        (void)strcat(buffer, "// images files\n");
        (void)sprintf(s, "#define IMAGES_COUNT %d\n", (SVGTuint)imageResources->size);
        (void)strcat(buffer, s);
        (void)strcat(buffer, "static ExternalResource images[IMAGES_COUNT] = {\n");
        for (i = 0U; i < (imageResources->size - 1U); ++i) {
            extractFileName(f.name, imageResources->data[i].fileName.name, SVGT_TRUE);
            (void)sprintf(s, "    { \"%s\", NULL, 0U, 0U },\n", f.name);
            (void)strcat(buffer, s);
        }
        extractFileName(f.name, imageResources->data[i].fileName.name, SVGT_TRUE);
        (void)sprintf(s, "    { \"%s\", NULL, 0U, 0U }\n", f.name);
        (void)strcat(buffer, s);
        (void)strcat(buffer, "};\n");
    }

    // SVG files
    if (args->atlasInputs.size > 0) {
        (void)strcat(buffer, "\n");
        (void)strcat(buffer, "// SVG files\n");
        (void)sprintf(s, "#define ATLAS_INPUTS_COUNT %d\n", (SVGTuint)args->atlasInputs.size);
        (void)strcat(buffer, s);
        (void)strcat(buffer, "static AtlasInput atlasInputs[ATLAS_INPUTS_COUNT] = {\n");
        for (i = 0U; i < (args->atlasInputs.size - 1U); ++i) {
            extractFileName(f.name, args->atlasInputs.data[i].fullFileName.name, SVGT_TRUE);
            (void)sprintf(s, "    { \"%s\", %ff, %s, SVGT_INVALID_HANDLE },\n", f.name,
                                                                                args->atlasInputs.data[i].scale,
                                                                                args->atlasInputs.data[i].explodeGroups ? "SVGT_TRUE" : "SVGT_FALSE");
            (void)strcat(buffer, s);
        }
        extractFileName(f.name, args->atlasInputs.data[i].fullFileName.name, SVGT_TRUE);
        (void)sprintf(s, "    { \"%s\", %ff, %s, SVGT_INVALID_HANDLE }\n", f.name,
                                                                           args->atlasInputs.data[i].scale,
                                                                           args->atlasInputs.data[i].explodeGroups ? "SVGT_TRUE" : "SVGT_FALSE");
        (void)strcat(buffer, s);
        (void)strcat(buffer, "};\n");
    }
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "// load an external file in memory, returning the allocated buffer and its size in bytes\n");
    (void)strcat(buffer, "static SVGTubyte* loadFile(const char* fileName,\n");
    (void)strcat(buffer, "                           const SVGTuint padAmount,\n");
    (void)strcat(buffer, "                           size_t* fileSize) {\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    SVGTubyte* buffer;\n");
    (void)strcat(buffer, "    size_t size, read;\n");
    (void)strcat(buffer, "    FILE* fp = NULL;\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "#if defined(_MSC_VER) && (_MSC_VER >= 1400)\n");
    (void)strcat(buffer, "    errno_t err = fopen_s(&fp, fileName, \"rb\");\n");
    (void)strcat(buffer, "    if ((fp == NULL) || (err != 0)) {\n");
    (void)strcat(buffer, "#else\n");
    (void)strcat(buffer, "    fp = fopen(fileName, \"rb\");\n");
    (void)strcat(buffer, "    if (fp == NULL) {\n");
    (void)strcat(buffer, "#endif\n");
    (void)strcat(buffer, "        return NULL;\n");
    (void)strcat(buffer, "    }\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    (void)fseek(fp, 0, SEEK_SET);\n");
    (void)strcat(buffer, "    (void)fgetc(fp);\n");
    (void)strcat(buffer, "    if (ferror(fp) != 0) {\n");
    (void)strcat(buffer, "        (void)fclose(fp);\n");
    (void)strcat(buffer, "        return NULL;\n");
    (void)strcat(buffer, "    }\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    (void)fseek(fp, 0, SEEK_END);\n");
    (void)strcat(buffer, "    size = ftell(fp);\n");
    (void)strcat(buffer, "    (void)fseek(fp, 0, SEEK_SET);\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    if (size == 0U) {\n");
    (void)strcat(buffer, "        (void)fclose(fp);\n");
    (void)strcat(buffer, "        return NULL;\n");
    (void)strcat(buffer, "    }\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    if ((buffer = calloc(size + padAmount, sizeof(SVGTubyte))) == NULL) {\n");
    (void)strcat(buffer, "        (void)fclose(fp);\n");
    (void)strcat(buffer, "        return NULL;\n");
    (void)strcat(buffer, "    }\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    if ((read = fread(buffer, sizeof(SVGTubyte), size, fp)) != size) {\n");
    (void)strcat(buffer, "        free(buffer);\n");
    (void)strcat(buffer, "        (void)fclose(fp);\n");
    (void)strcat(buffer, "        return NULL;\n");
    (void)strcat(buffer, "    }\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    (void)fclose(fp);\n");
    (void)strcat(buffer, "    *fileSize = size;\n");
    (void)strcat(buffer, "    return buffer;\n");
    (void)strcat(buffer, "}\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "// load a font file in memory, returning the allocated buffer and its size in bytes\n");
    (void)strcat(buffer, "static SVGTubyte* loadFont(const char* fileName,\n");
    (void)strcat(buffer, "                           size_t* fileSize) {\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    // no additional pad\n");
    (void)strcat(buffer, "    return loadFile(fileName, 0U, fileSize);\n");
    (void)strcat(buffer, "}\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "// load an image file in memory, returning the allocated buffer and its size in bytes\n");
    (void)strcat(buffer, "static SVGTubyte* loadImage(const char* fileName,\n");
    (void)strcat(buffer, "                            size_t* fileSize) {\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    // no additional pad\n");
    (void)strcat(buffer, "    return loadFile(fileName, 0U, fileSize);\n");
    (void)strcat(buffer, "}\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "// load an XML / SVG file in memory\n");
    (void)strcat(buffer, "static char* loadXml(const char* fileName) {\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    size_t fileSize;\n");
    (void)strcat(buffer, "    // add a trailing '\\0'\n");
    (void)strcat(buffer, "    return (char*)loadFile(fileName, 1U, &fileSize);\n");
    (void)strcat(buffer, "}\n");
    (void)strcat(buffer, "\n");

    (void)strcat(buffer, "int main(int argc,\n");
    (void)strcat(buffer, "         char *argv[]) {\n");
    (void)strcat(buffer, "\n");

    (void)strcat(buffer, "    SVGTErrorCode err;\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    // initialize AmanithSVG library\n");
    (void)strcat(buffer, "    // svgtInit function takes as input the information relative to the device\n");
    (void)strcat(buffer, "    // screen: width/height (in pixels) and dpi (dots per inch). Such information\n");
    (void)strcat(buffer, "    // are used to resolve SVG lengths when expressed in percentage; the best way\n");
    (void)strcat(buffer, "    // to obtain screen information is to use native calls, for more information\n");
    (void)strcat(buffer, "    // please refer to https://www.amanithsvg.com/docs/api/004-initialization-finalization.html\n");
    (void)strcat(buffer, "    //\n");
    (void)strcat(buffer, "    // NB: the code given here is only an example\n");
    (void)sprintf(s, "    err = svgtInit(%d, %d, %ff);\n", args->screenWidth, args->screenHeight, args->dpi);
    (void)strcat(buffer, s);
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    // set user-agent language\n");
    (void)sprintf(s, "    err = svgtLanguageSet(\"%s\");\n", args->language);
    (void)strcat(buffer, s);
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    // set rendering quality\n");
    (void)sprintf(s, "    err = svgtConfigSet(SVGT_CONFIG_CURVES_QUALITY, %d.0f);\n", args->renderingQuality);
    (void)strcat(buffer, s);
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    // load font resources\n");
    (void)strcat(buffer, "    for (size_t i = 0U; i < FONTS_COUNT; ++i) {\n");
    (void)strcat(buffer, "        // load the font file into memory\n");
    (void)strcat(buffer, "        fonts[i].buffer = loadFont(fonts[i].fileName, &fonts[i].bufferSize);\n");
    (void)strcat(buffer, "        // provide the resource to AmanithSVG\n");
    (void)strcat(buffer, "        err = svgtResourceSet(fonts[i].fileName, fonts[i].buffer, (SVGTuint)fonts[i].bufferSize, SVGT_RESOURCE_TYPE_FONT, fonts[i].hints);\n");
    (void)strcat(buffer, "    }\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    // load image resources\n");
    (void)strcat(buffer, "    for (size_t i = 0U; i < IMAGES_COUNT; ++i) {\n");
    (void)strcat(buffer, "        // load the image file into memory\n");
    (void)strcat(buffer, "        images[i].buffer = loadImage(images[i].fileName, &images[i].bufferSize);\n");
    (void)strcat(buffer, "        // provide the resource to AmanithSVG\n");
    (void)strcat(buffer, "        err = svgtResourceSet(images[i].fileName, images[i].buffer, (SVGTuint)images[i].bufferSize, SVGT_RESOURCE_TYPE_IMAGE, images[i].hints);\n");
    (void)strcat(buffer, "    }\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    // load SVG documents\n");
    (void)strcat(buffer, "    for (size_t i = 0U; i < ATLAS_INPUTS_COUNT; ++i) {\n");
    (void)strcat(buffer, "        // allocate buffer and load SVG file\n");
    (void)strcat(buffer, "        char* xmlBuffer = loadXml(atlasInputs[i].fileName);\n");
    (void)strcat(buffer, "        // create (and parse) the SVG document\n");
    (void)strcat(buffer, "        atlasInputs[i].docHandle = svgtDocCreate(xmlBuffer);\n");
    (void)strcat(buffer, "        err = svgtGetLastError();\n");
    (void)strcat(buffer, "        // free xml buffer\n");
    (void)strcat(buffer, "        free(xmlBuffer);\n");
    (void)strcat(buffer, "    }\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    // start collecting the elements that will later be packed\n");
    (void)sprintf(s, "    err = svgtPackingBegin(%d, %d, %s, %ff);\n", args->atlasMaxDimension,
                                                                       args->atlasBorder,
                                                                       args->atlasPow2 ? "SVGT_TRUE" : "SVGT_FALSE",
                                                                       args->scale);
    (void)strcat(buffer, s);
    (void)strcat(buffer, "    // load SVG files and add them to the packing process\n");
    (void)strcat(buffer, "    for (size_t i = 0U; i < ATLAS_INPUTS_COUNT; ++i) {\n");
    (void)strcat(buffer, "        // info[0] = number of collected elements\n");
    (void)strcat(buffer, "        // info[1] = the actual number of elements that could be packed (less than or equal to info[0])\n");
    (void)strcat(buffer, "        SVGTuint info[2] = { 0, 0 };\n");
    (void)strcat(buffer, "        // add the loaded document to the collection process\n");
    (void)strcat(buffer, "        err = svgtPackingAdd(atlasInputs[i].docHandle, atlasInputs[i].explodeGroups, atlasInputs[i].scale, info);\n");
    (void)strcat(buffer, "    }\n");
    (void)strcat(buffer, "    // close the current packing process and perform the real packing algorithm\n");
    (void)strcat(buffer, "    err = svgtPackingEnd(SVGT_TRUE);\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    // get the number of atlas pages (i.e. textures)\n");
    (void)strcat(buffer, "    SVGTint pagesCount = svgtPackingBinsCount();\n");
    (void)strcat(buffer, "    // loop over atlas pages\n");
    (void)strcat(buffer, "    for (SVGTint pageIndex = 0; pageIndex < pagesCount; ++pageIndex) {\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "        // get page information\n");
    (void)strcat(buffer, "        SVGTuint info[3] = { 0 };\n");
    (void)strcat(buffer, "        // info[0] = page width, in pixels\n");
    (void)strcat(buffer, "        // info[1] = page height, in pixels\n");
    (void)strcat(buffer, "        // info[2] = number of packed elements inside the page\n");
    (void)strcat(buffer, "        err = svgtPackingBinInfo(pageIndex, info);\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "        // get packed elements\n");
    (void)strcat(buffer, "        const SVGTPackedRect* rects = svgtPackingBinRects(pageIndex);\n");
    (void)strcat(buffer, "        err = svgtGetLastError();\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "        // create the drawing surface\n");
    (void)strcat(buffer, "        SVGTHandle svgSurface = svgtSurfaceCreate(info[0], info[1]);\n");
    (void)strcat(buffer, "        err = svgtGetLastError();\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "        // clear the drawing surface\n");
    (void)sprintf(s, "        err = svgtSurfaceClear(svgSurface, %ff, %ff, %ff, %ff);\n", args->clearColor[0],
                                                                                          args->clearColor[1],
                                                                                          args->clearColor[2],
                                                                                          args->clearColor[3]);
    (void)strcat(buffer, s);
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "        // draw packed elements\n");
    (void)strcat(buffer, "        err = svgtPackingRectsDraw(rects, info[2], svgSurface, SVGT_RENDERING_QUALITY_BETTER);\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "        // do something with drawing surface (e.g. upload pixels to a GPU texture, or dump a PNG file)\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "        // destroy the drawing surface\n");
    (void)strcat(buffer, "        err = svgtSurfaceDestroy(svgSurface);\n");
    (void)strcat(buffer, "    }\n");
    (void)strcat(buffer, "\n");

    (void)strcat(buffer, "    // release SVG documents used for atlas generation\n");
    (void)strcat(buffer, "    for (size_t i = 0U; i < ATLAS_INPUTS_COUNT; ++i) {\n");
    (void)strcat(buffer, "        if (atlasInputs[i].docHandle != SVGT_INVALID_HANDLE) {\n");
    (void)strcat(buffer, "            // destroy SVG document\n");
    (void)strcat(buffer, "            err = svgtDocDestroy(atlasInputs[i].docHandle);\n");
    (void)strcat(buffer, "        }\n");
    (void)strcat(buffer, "    }\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    // release AmantihSVG library\n");
    (void)strcat(buffer, "    svgtDone();\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    // release in-memory fonts\n");
    (void)strcat(buffer, "    for (size_t i = 0U; i < FONTS_COUNT; ++i) {\n");
    (void)strcat(buffer, "        free((void*)fonts[i].buffer);\n");
    (void)strcat(buffer, "    }\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    // release in-memory images\n");
    (void)strcat(buffer, "    for (size_t i = 0U; i < IMAGES_COUNT; ++i) {\n");
    (void)strcat(buffer, "        free((void*)images[i].buffer);\n");
    (void)strcat(buffer, "    }\n");
    (void)strcat(buffer, "}\n");

    // write the file
    return atlasWriteBuffer(args->atlasFormat, buffer, args->outputDir.path, args->atlasPrefix);
}

static SVGTboolean atlasExportCodeJava(const ExternalResourceDynArray* fontResources,
                                       const ExternalResourceDynArray* imageResources,
                                       const CommandArguments* args,
                                       char* buffer) {

    size_t i;
    FileName f = { 0 };
    char s[1024] = { 0 };

    (void)strcat(buffer, "package com.mazatech.amanithsvg.yourapp;\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "import java.util.HashMap;\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "// libGDX\n");
    (void)strcat(buffer, "import com.badlogic.gdx.ApplicationAdapter;\n");
    (void)strcat(buffer, "import com.badlogic.gdx.Gdx;\n");
    (void)strcat(buffer, "import com.badlogic.gdx.graphics.g2d.SpriteBatch;\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "// AmanithSVG for libGDX\n");
    (void)strcat(buffer, "import com.mazatech.gdx.SVGAssetsGDX;\n");
    (void)strcat(buffer, "import com.mazatech.gdx.SVGAssetsConfigGDX;\n");
    (void)strcat(buffer, "import com.mazatech.gdx.SVGTextureAtlas;\n");
    (void)strcat(buffer, "import com.mazatech.gdx.SVGTextureAtlasGenerator;\n");
    (void)strcat(buffer, "import com.mazatech.gdx.SVGTextureAtlasPage;\n");
    (void)strcat(buffer, "import com.mazatech.gdx.SVGTextureAtlasRegion;\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "// AmanithSVG java binding (high level layer)\n");
    (void)strcat(buffer, "import com.mazatech.svgt.SVGColor;\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "public class Game extends ApplicationAdapter {\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    @Override\n");
    (void)strcat(buffer, "    public void create() {\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "        // NB: use the backbuffer to get the real dimensions in pixels\n");
    (void)strcat(buffer, "        int screenWidth = Gdx.graphics.getBackBufferWidth();\n");
    (void)strcat(buffer, "        int screenHeight = Gdx.graphics.getBackBufferHeight();\n");
    (void)strcat(buffer, "        // create configuration for AmanithSVG\n");
    (void)strcat(buffer, "        SVGAssetsConfigGDX config = new SVGAssetsConfigGDX(screenWidth, screenHeight, Gdx.graphics.getPpiX());\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "        // set user-agent language\n");
    (void)sprintf(s, "        config.setUserAgentLanguage(\"%s\");\n", args->language);
    (void)strcat(buffer, s);
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "        // set rendering quality\n");
    (void)sprintf(s, "        config.setCurvesQuality(%d.0f);\n", args->renderingQuality);
    (void)strcat(buffer, s);

    // font files
    if (fontResources->size > 0U) {
        (void)strcat(buffer, "\n");
        (void)strcat(buffer, "        // provide font resources to AmanithSVG\n");
        for (i = 0U; i < fontResources->size; ++i) {
            // internal filename
            extractFileName(f.name, fontResources->data[i].fileName.name, SVGT_TRUE);
            (void)sprintf(s, "        config.addFont(\"%s\");\n", f.name);
            (void)strcat(buffer, s);
        }
    }

    // image files
    if (imageResources->size > 0U) {
        (void)strcat(buffer, "\n");
        (void)strcat(buffer, "        // provide image resources to AmanithSVG\n");
        for (i = 0U; i < imageResources->size; ++i) {
            // internal filename
            extractFileName(f.name, imageResources->data[i].fileName.name, SVGT_TRUE);
            (void)sprintf(s, "        config.addImage(\"%s\");\n", f.name);
            (void)strcat(buffer, s);
        }
    }

    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "        // initialize AmanithSVG for libGDX\n");
    (void)strcat(buffer, "        svgLibGDX = new SVGAssetsGDX(config);\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "        // scale, max textures dimension, border, pow2 textures, dilate edges filter, clear color\n");
    (void)sprintf(s, "        atlasGen = svgLibGDX.createAtlasGenerator(%ff, %d, %d, %s, %s, new SVGColor(%ff, %ff, %ff, %ff));\n",
                  args->scale,
                  args->atlasMaxDimension,
                  args->atlasBorder,
                  args->atlasPow2 ? "true" : "false",
                  (args->filter == FILTER_DILATE) ? "true" : "false",
                  args->clearColor[0], args->clearColor[1], args->clearColor[2], args->clearColor[3]);
    (void)strcat(buffer, s);
    (void)strcat(buffer, "\n");
    // SVG files
    if (args->atlasInputs.size > 0) {
        (void)strcat(buffer, "        // internal path, explode groups, scale\n");
        for (i = 0U; i < args->atlasInputs.size; ++i) {
            extractFileName(f.name, args->atlasInputs.data[i].fullFileName.name, SVGT_TRUE);
            (void)sprintf(s, "        atlasGen.add(\"%s\", %s, %ff);\n", f.name,
                                                                         args->atlasInputs.data[i].explodeGroups ? "true" : "false",
                                                                         args->atlasInputs.data[i].scale);
            (void)strcat(buffer, s);
        }
    }

    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "        // do the real atlas generation\n");
    (void)strcat(buffer, "        try {\n");
    (void)strcat(buffer, "            atlas = atlasGen.generateAtlas();\n");
    (void)strcat(buffer, "        }\n");
    (void)strcat(buffer, "        catch (SVGTextureAtlasGenerator.SVGTextureAtlasPackingException e) {\n");
    (void)strcat(buffer, "            Gdx.app.log(\"AmanithSVG\", \"Some SVG elements cannot be packed!\");\n");
    (void)strcat(buffer, "        }\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "        // associate each sprite's name (i.e. SVG 'id' attribute) to the respective texture region\n");
    (void)strcat(buffer, "        spritesMap = new HashMap<>();\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "        // loop over atlas pages (i.e. textures)\n");
    (void)strcat(buffer, "        for (SVGTextureAtlasPage page : atlas.getPages()) {\n");
    (void)strcat(buffer, "            // loop over sprites belonging to the current atlas page\n");
    (void)strcat(buffer, "            for (SVGTextureAtlasRegion region : page.getRegions()) {\n");
    (void)strcat(buffer, "                // do something useful at this setup phase, for example you\n");
    (void)strcat(buffer, "                // could generate a hashmap that maintains the association\n");
    (void)strcat(buffer, "                // sprite's name - sprite, so that it is possible to request\n");
    (void)strcat(buffer, "                // a sprite given its name (SVG 'id' attribute)\n");
    (void)strcat(buffer, "                //\n");
    (void)strcat(buffer, "                // NB: the code given here is only an example\n");
    (void)strcat(buffer, "                spritesMap.put(region.getElemName(), region);\n");
    (void)strcat(buffer, "            }\n");
    (void)strcat(buffer, "        }\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "        // create the batch (used by 'render' function)\n");
    (void)strcat(buffer, "        batch = new SpriteBatch();\n");
    (void)strcat(buffer, "    }\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    @Override\n");
    (void)strcat(buffer, "    public void render() {\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "        // start drawing\n");
    (void)strcat(buffer, "        batch.begin();\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "        // do something with atlas sprites/regions\n");
    (void)strcat(buffer, "        // NB: the code given here is only an example\n");
    (void)strcat(buffer, "        SVGTextureAtlasRegion sprite = spritesMap.get(\"playIcon\");\n");
    (void)strcat(buffer, "        if (sprite != null) {\n");
    (void)strcat(buffer, "            batch.draw(sprite, 10, 20);\n");
    (void)strcat(buffer, "        }\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "        // finish drawing\n");
    (void)strcat(buffer, "        batch.end();\n");
    (void)strcat(buffer, "    }\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    @Override\n");
    (void)strcat(buffer, "    public void dispose() {\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "        // release AmanithSVG resources\n");
    (void)strcat(buffer, "        atlas.dispose();\n");
    (void)strcat(buffer, "        atlasGen.dispose();\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "        // release sprite batch\n");
    (void)strcat(buffer, "        batch.dispose();\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "        // release AmanithSVG\n");
    (void)strcat(buffer, "        svgLibGDX.dispose();\n");
    (void)strcat(buffer, "        super.dispose();\n");
    (void)strcat(buffer, "    }\n");
    (void)strcat(buffer, "\n");
    (void)strcat(buffer, "    // instance of AmanithSVG for libGDX\n");
    (void)strcat(buffer, "    private SVGAssetsGDX svgLibGDX = null;\n");
    (void)strcat(buffer, "    // SVG atlas generator\n");
    (void)strcat(buffer, "    private SVGTextureAtlasGenerator atlasGen = null;\n");
    (void)strcat(buffer, "    private SVGTextureAtlas atlas = null;\n");
    (void)strcat(buffer, "    // associate each sprite's name (i.e. SVG 'id' attribute) to the respective texture region\n");
    (void)strcat(buffer, "    private HashMap<String, SVGTextureAtlasRegion> spritesMap = null;\n");
    (void)strcat(buffer, "    // sprite batch\n");
    (void)strcat(buffer, "    private SpriteBatch batch = null;\n");
    (void)strcat(buffer, "}\n");

    // write the file
    return atlasWriteBuffer(args->atlasFormat, buffer, args->outputDir.path, args->atlasPrefix);
}

/************************************************************
                           Text
************************************************************/
static void atlasPageExportLibgdxSpine(const AtlasPage* page,
                                       char* buffer) {

    char s[128];
    FileName png = { 0 };

    (void)strcat(buffer, "\n");
    // PNG filename
    (void)sprintf(png.buffer, "%s.png\n", page->baseName.name);
    (void)strcat(buffer, png.name);
    // size
    (void)sprintf(s, "size: %d, %d\n", page->width, page->height);
    (void)strcat(buffer, s);
    // format
    // NB: libGDX and Spine do not support BGRA8888 enumeration (see http://en.esotericsoftware.com/spine-atlas-format)
    (void)strcat(buffer, "format: RGBA8888\n");
    // alpha premultiplication
    (void)strcat(buffer, "premultiplyAlpha: false\n");
    // filter
    (void)strcat(buffer, "filter: Linear, Linear\n");
    // repeat
    (void)strcat(buffer, "repeat: none\n");
    
    // sprites
    for (size_t i = 0U; i < page->sprites.size; ++i) {

        const AtlasSprite* sprite = &page->sprites.data[i];

        // name
        (void)strcat(buffer, sprite->id.name);
        (void)strcat(buffer, "\n");
        // rotate
        (void)strcat(buffer, "  rotate: false\n");
        // xy
        (void)sprintf(s, "  xy: %d, %d\n", sprite->x, sprite->y);
        (void)strcat(buffer, s);
        // size
        (void)sprintf(s, "  size: %d, %d\n", sprite->width, sprite->height);
        (void)strcat(buffer, s);
        // orig
        (void)sprintf(s, "  orig: %d, %d\n", sprite->width, sprite->height);
        (void)strcat(buffer, s);
        // offset
        (void)strcat(buffer, "  offset: 0, 0\n");
        // index
        (void)strcat(buffer, "  index: -1\n");
        // pivot
        (void)sprintf(s, "  pivot: %f, %f\n", SPRITES_PIVOT_X, SPRITES_PIVOT_Y);
        (void)strcat(buffer, s);
    }
}

static SVGTboolean atlasExportLibgdxSpine(const AtlasExporter* exporter,
                                          const CommandArguments* args,
                                          char* buffer) {

    // loop over atlas pages
    for (SVGTuint i = 0U; i < exporter->pages.size; ++i) {
        // export atlas page
        atlasPageExportLibgdxSpine(&exporter->pages.data[i], buffer);
    }

    // write the file
    return atlasWriteBuffer(args->atlasFormat, buffer, args->outputDir.path, args->atlasPrefix);
}

// export atlas data file, simple text-based formats
static SVGTboolean atlasExportTEXT(const AtlasExporter* exporter,
                                   const ExternalResourceDynArray* fontResources,
                                   const ExternalResourceDynArray* imageResources,
                                   const CommandArguments* args,
                                   char* buffer) {

    SVGTboolean ok;

    // empty buffer
    *buffer = '\0';

    switch (args->atlasFormat) {
        case ATLAS_LIBGDX_FORMAT:
        case ATLAS_SPINE_FORMAT:
            ok = atlasExportLibgdxSpine(exporter, args, buffer);
            break;
        case ATLAS_CODE_C_FORMAT:
            ok = atlasExportCodeC(fontResources, imageResources, args, buffer);
            break;
        case ATLAS_CODE_LIBGDX_FORMAT:
        default:
            ok = atlasExportCodeJava(fontResources, imageResources, args, buffer);
            break;
    }

    return ok;
}

// export atlas data file, libGDX and Spine formats
static SVGTboolean atlasMapLibgdxSpine(const AtlasExporter* exporter,
                                       const CommandArguments* args,
                                       char* buffer) {

    FileName map = { 0 };

    // loop over SVG input files
    for (size_t i = 0U; i < args->atlasInputs.size; ++i) {

        FileName svg = { 0 };
        AtlasSpritePtrDynArray sprites = { 0 };

        // collect the list of sprites belonging to the SVG input file
        atlasMapSpritesCollect(&args->atlasInputs.data[i], &exporter->pages, &sprites, &svg);

        (void)strcat(buffer, "\n");
        // the name of the original SVG input file
        (void)strcat(buffer, svg.name);
        (void)strcat(buffer, "\n");

        // loop over sprites belonging to the SVG input file
        for (size_t j = 0U; j < sprites.size; ++j) {

            char s[1024];
            FileName png = { 0 };
            double ofsX = 0.0, ofsY = 0.0;
            const AtlasSprite* sprite = sprites.data[j];

            // get sprite information (map)
            atlasMapSpriteInfo(sprite, &png, &ofsX, &ofsY);

            // unique name
            (void)strcat(buffer, sprite->id.name);
            (void)strcat(buffer, "\n");
            // atlas page, PNG file
            (void)sprintf(s, "  "MAPPED_SPRITE_PAGE_FIELD": %s\n", png.name);
            (void)strcat(buffer, s);
            // sprite z-order
            (void)sprintf(s, "  "MAPPED_SPRITE_Z_ORDER_FIELD": %d\n", sprite->zOrder);
            (void)strcat(buffer, s);
            // sprite instantiation offset
            (void)sprintf(s, "  "MAPPED_SPRITE_INSTANTIATION_OFFSET_FIELD": %f, %f\n", ofsX, ofsY);
            (void)strcat(buffer, s);
        }

        // release the list of sprite pointers
        DYNARRAY_DESTROY(sprites)
    }

    // map filename (without extension)
    (void)sprintf(map.name, "%s-map", args->atlasPrefix);
    // write the file
    return atlasWriteBuffer(args->atlasFormat, buffer, args->outputDir.path, map.name);
}

// export atlas map file, simple text-based formats
static SVGTboolean atlasMapTEXT(const AtlasExporter* exporter,
                                const CommandArguments* args,
                                char* buffer) {

    SVGTboolean ok;

    // empty buffer
    *buffer = '\0';

    switch (args->atlasFormat) {
        case ATLAS_LIBGDX_FORMAT:
        case ATLAS_SPINE_FORMAT:
            ok = atlasMapLibgdxSpine(exporter, args, buffer);
            break;
        // source code formats do not need a map file
        case ATLAS_CODE_C_FORMAT:
        case ATLAS_CODE_LIBGDX_FORMAT:
        default:
            ok = SVGT_TRUE;
            break;
    }

    return ok;
}

/************************************************************
                    public export function
************************************************************/

// export atlas content to an external file
SVGTboolean atlasExport(const AtlasExporter* exporter,
                        const ExternalResourceDynArray* fontResources,
                        const ExternalResourceDynArray* imageResources,
                        const CommandArguments* args) {

    SVGTboolean ok;

    // XML based
    if ((args->atlasFormat == ATLAS_XML_GENERIC_FORMAT) || (args->atlasFormat == ATLAS_COCOS2D_FORMAT)) {
        if ((ok = atlasExportXML(exporter, args))) {
            // export map
            ok = atlasMapXML(exporter, args);
        }
    }
    else {
        const size_t bufferSize = atlasGuessFileSize(exporter, fontResources, imageResources, args);

        if ((ok = (bufferSize > 0U))) {
            // allocate output buffer
            char* buffer = calloc(bufferSize, sizeof(char));
            if ((ok = (buffer != NULL))) {
                switch (args->atlasFormat) {
                    // JSON based
                    case ATLAS_JSON_ARRAY_FORMAT:
                    case ATLAS_JSON_HASH_FORMAT:
                    case ATLAS_PHASER2_FORMAT:
                    case ATLAS_PHASER3_FORMAT:
                    case ATLAS_PIXIJS_FORMAT:
                    case ATLAS_GODOT3_SPRITE_SHEET_FORMAT:
                    case ATLAS_GODOT3_TILE_SET_FORMAT:
                        if ((ok = atlasExportJSON(exporter, args, buffer, bufferSize))) {
                            // export map
                            ok = atlasMapJSON(exporter, args, buffer, bufferSize);
                        }
                        break;
                    // simple text
                    case ATLAS_LIBGDX_FORMAT:
                    case ATLAS_SPINE_FORMAT:
                    // simple text (code)
                    case ATLAS_CODE_C_FORMAT:
                    case ATLAS_CODE_LIBGDX_FORMAT:
                    default:
                        if ((ok = atlasExportTEXT(exporter, fontResources, imageResources, args, buffer))) {
                            // export map
                            ok = atlasMapTEXT(exporter, args, buffer);
                        }
                        break;
                }
                // release temporary buffer
                free(buffer);
            }
        }
    }

    return ok;
}
