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

#ifndef ATLAS_H
#define ATLAS_H

/*!
    \file atlas.h
    \brief Atlas related structures.
    \author Matteo Muratori
    \author Michele Fabbri
*/

#include "file_utils.h"
#include "str_hashmap.h"

typedef enum {
    // XML based
    ATLAS_XML_GENERIC_FORMAT         =  0,
    ATLAS_COCOS2D_FORMAT             =  1,
    // JSON based
    ATLAS_JSON_ARRAY_FORMAT          =  2,
    ATLAS_JSON_HASH_FORMAT           =  3,
    ATLAS_PHASER2_FORMAT             =  4,
    ATLAS_PHASER3_FORMAT             =  5,
    ATLAS_PIXIJS_FORMAT              =  6,
    ATLAS_GODOT3_SPRITE_SHEET_FORMAT =  7,
    ATLAS_GODOT3_TILE_SET_FORMAT     =  8,
    // simple text
    ATLAS_LIBGDX_FORMAT              =  9,
    ATLAS_SPINE_FORMAT               = 10,
    // simple text (code)
    ATLAS_CODE_C_FORMAT              = 11,
    ATLAS_CODE_LIBGDX_FORMAT         = 12
} AtlasFormat;

typedef enum {
    ATLAS_MAP_ARRAY_FORMAT           = 0,
    ATLAS_MAP_HASH_FORMAT            = 1
} AtlasMapFormat;

// an input SVG file for the atlas mode
typedef struct {
    // full filename
    FileName fullFileName;
    // base filename, without path and without extension
    FileName baseFileName;
    // scale (a positive number)
    SVGTfloat scale;
    // SVGT_FALSE to pack the whole SVG document, SVGT_TRUE to pack instead each first-level element separately
    SVGTboolean explodeGroups;
    // AmanithSVG document handle
    SVGTHandle docHandle;
    // hashmap used to generate unique element id
    StringHashMap elemHashmap;
} AtlasInput;

// an array of atlas input
DYNARRAY_DECLARE(AtlasInputDynArray, AtlasInput)

// forward declaration
struct AtlasPage;

// an atlas sprite
typedef struct {
    // pointer to the atlas page
    struct AtlasPage* page;
    // <svg file name> + '#' + <element 'id' attribute> (e.g. orc#head)
    FileName id;
    // original corner
    SVGTint originalX;
    SVGTint originalY;
    // corner position, within its atlas page
    SVGTint x;
    SVGTint y;
    // dimensions
    SVGTint width;
    SVGTint height;
    // SVG document handle
    SVGTHandle docHandle;
    /* 0 for the whole SVG, else the element (tree) index. */
    //SVGTuint elemIdx;

    // z-order
    SVGTint zOrder;
    // the used destination viewport width
    SVGTfloat dstViewportWidth;
    // the used destination viewport height
    SVGTfloat dstViewportHeight;

} AtlasSprite, *AtlasSpritePtr;

// an array of atlas sprites
DYNARRAY_DECLARE(AtlasSpriteDynArray, AtlasSprite)
DYNARRAY_DECLARE(AtlasSpritePtrDynArray, AtlasSpritePtr)

typedef struct AtlasPage {
    // base filename (e.g. myatlas-1 = <prefix>-<index>)
    FileName baseName;
    // width, in pixels
    SVGTuint width;
    // height, in pixels
    SVGTuint height;
    // list of sprites
    AtlasSpriteDynArray sprites;
} AtlasPage;

// an array of atlas pages
DYNARRAY_DECLARE(AtlasPageDynArray, AtlasPage)

#endif /* ATLAS_H */
