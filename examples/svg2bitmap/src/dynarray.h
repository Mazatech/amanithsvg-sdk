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

#ifndef DYNARRAY_H
#define DYNARRAY_H

/*!
    \file dynarray.h
    \brief Dynamic arrays, header.
    \author Matteo Muratori
    \author Michele Fabbri
*/

#include <stdlib.h>

/************************************************************
                      Dynamic arrays
************************************************************/
// array declaration
#define DYNARRAY_DECLARE(_typeName, _itemTypeName) \
    typedef struct { \
        _itemTypeName* data; \
        size_t size; \
        size_t capacity; \
    } _typeName;

// array initialization
#define DYNARRAY_INIT(_dynArray) \
    (_dynArray).data = NULL; \
    (_dynArray).size = 0U; \
    (_dynArray).capacity = 0U;

// array initialization, with initial allocation too
#define DYNARRAY_INIT_ALLOCATE(_dynArray, _itemTypeName, _capacity) \
    (_dynArray).data = calloc((_capacity), sizeof(_itemTypeName)); \
    if ((_dynArray).data != NULL) { \
        (_dynArray).capacity = (_capacity); \
    } \
    else { \
        (_dynArray).capacity = 0U; \
    } \
    (_dynArray).size = 0U;

// array destruction
#define DYNARRAY_DESTROY(_dynArray) \
    if ((_dynArray).data != NULL) { \
        free((_dynArray).data); \
    }

// push an element at the end of a dynamic array; if space is not enought, the array is expanded by additional 64 elements
#define DYNARRAY_PUSH_BACK(_dynArray, _itemTypeName, _val) \
    if ((_dynArray).size >= (_dynArray).capacity) { \
        _itemTypeName* tmpData = (_itemTypeName *)realloc((_dynArray).data, ((_dynArray).capacity + 64) * sizeof(_itemTypeName)); \
        if (tmpData != NULL) { \
            (_dynArray).data = tmpData; \
            (_dynArray).capacity += 64; \
            (_dynArray).data[(_dynArray).size++] = (_val); \
        } \
    } \
    else { \
        (_dynArray).data[(_dynArray).size++] = (_val); \
    }

#endif /* DYNARRAY_H */
