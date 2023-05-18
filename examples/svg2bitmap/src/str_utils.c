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
    \file str_utils.c
    \brief String utilities, implementation.
    \author Matteo Muratori
    \author Michele Fabbri
*/

#include <string.h>
#include "str_utils.h"

/************************************************************
                       String utilities
************************************************************/

typedef enum {
    SVG_ASCII_ALNUM  = 1 << 0,
    SVG_ASCII_ALPHA  = 1 << 1,
    SVG_ASCII_CNTRL  = 1 << 2,  // control character
    SVG_ASCII_DIGIT  = 1 << 3,  // digit [0..9]
    SVG_ASCII_GRAPH  = 1 << 4,
    SVG_ASCII_LOWER  = 1 << 5,  // lower case letter
    SVG_ASCII_PRINT  = 1 << 6,
    SVG_ASCII_PUNCT  = 1 << 7,  // punctuation character
    SVG_ASCII_SPACE  = 1 << 8,  // tab, carriage return, newline, vertical tab or form feed
    SVG_ASCII_UPPER  = 1 << 9,  // upper case letter
    SVG_ASCII_XDIGIT = 1 << 10  // hexadecimal digit
} SVGAsciiType;

static const SVGTshort asciiTableMask[256] = {
    0x004, 0x004, 0x004, 0x004, 0x004, 0x004, 0x004, 0x004,
    0x004, 0x104, 0x104, 0x004, 0x104, 0x104, 0x004, 0x004,
    0x004, 0x004, 0x004, 0x004, 0x004, 0x004, 0x004, 0x004,
    0x004, 0x004, 0x004, 0x004, 0x004, 0x004, 0x004, 0x004,
    0x140, 0x0d0, 0x0d0, 0x0d0, 0x0d0, 0x0d0, 0x0d0, 0x0d0,
    0x0d0, 0x0d0, 0x0d0, 0x0d0, 0x0d0, 0x0d0, 0x0d0, 0x0d0,
    0x459, 0x459, 0x459, 0x459, 0x459, 0x459, 0x459, 0x459,
    0x459, 0x459, 0x0d0, 0x0d0, 0x0d0, 0x0d0, 0x0d0, 0x0d0,
    0x0d0, 0x653, 0x653, 0x653, 0x653, 0x653, 0x653, 0x253,
    0x253, 0x253, 0x253, 0x253, 0x253, 0x253, 0x253, 0x253,
    0x253, 0x253, 0x253, 0x253, 0x253, 0x253, 0x253, 0x253,
    0x253, 0x253, 0x253, 0x0d0, 0x0d0, 0x0d0, 0x0d0, 0x0d0,
    0x0d0, 0x473, 0x473, 0x473, 0x473, 0x473, 0x473, 0x073,
    0x073, 0x073, 0x073, 0x073, 0x073, 0x073, 0x073, 0x073,
    0x073, 0x073, 0x073, 0x073, 0x073, 0x073, 0x073, 0x073,
    0x073, 0x073, 0x073, 0x0d0, 0x0d0, 0x0d0, 0x0d0, 0x004
    // the upper 128 are all zeroes
};

// check if the given char is a space
SVGTboolean isSpace(const char c) {

    return ((asciiTableMask[(SVGTubyte)(c)] & SVG_ASCII_SPACE) != 0) ? SVGT_TRUE : SVGT_FALSE;
}

// check if the given char is an hexadecimal digit
SVGTboolean isHexDigit(const char c) {

    return ((asciiTableMask[(SVGTubyte)(c)] & SVG_ASCII_XDIGIT) != 0) ? SVGT_TRUE : SVGT_FALSE;
}


// replace every occurence of 'search' with 'substitute'
void replaceChar(char* str,
                 const char search,
                 const char substitute) {

    size_t i;
    const size_t l = strlen(str);

    for (i = 0U; i < l; ++i) {
        if (str[i] == search) {
            str[i] = substitute;
        }
    }
}

// transform the given char to lower case
char lowerChar(char ch) {

    if ((ch >= 'A') && (ch <= 'Z')) {
        ch = 'a' + (ch - 'A');
    }

    return ch;
}

// transform the given char to upper case
char upperChar(char ch) {

    if ((ch >= 'a') && (ch <= 'z')) {
        ch = 'A' + (ch - 'a');
    }

    return ch;
}

// case insensitive string compare
SVGTint strCaseCmp(const char* s1,
                   const char* s2) {

    while (lowerChar(*s1) == lowerChar(*s2++)) {
        if (*s1++ == '\0') {
            return 0;
        }
    }

    return (lowerChar(*s1) - lowerChar(*--s2));
}

// skip all spaces
const char* skipSpaces(const char* str,
                       size_t* strLength) {
    
    size_t sLen = *strLength;

    while (*str && isSpace(*str)) {
        str++;
        sLen--;
    }
    *strLength = sLen;
    return str;
}

// skip all characters until the given terminator is reached; NB: the given terminator is NOT skipped
const char* skipUntilChar(const char* str,
                          size_t* strLength,
                          const char terminator) {

    size_t sLen = *strLength;

    while ((*str != '\0') && (*str != terminator)) {
        str++;
        sLen--;
    }

    *strLength = sLen;
    return str;
}

// convert an hexadecimal string to integer
SVGTint axtoi(const char* str) {

    const char* s;
    SVGTuint j, res, steps;

    if ((!str) || (*str == '\0')) {
        return 0;
    }

    s = str;
    if (*s == '0') {
        s++;
        if (*s == '\0') {
            return 0;
        }
        else
        if ((*s == 'x') || (*s == 'X')) {
            s++;
        }
    }

    res = 0;
    steps = 0;

    while ((*s != '\0') && (steps <= 7)) {
        if ((*s >= '0') && (*s <= '9')) {
            j = (*s - '0');
        }
        else
        if ((*s >= 'a') && (*s <= 'f')) {
            j = (*s - 'a' + 10);
        }
        else
        if ((*s >= 'A') && (*s <= 'F')) {
            j = (*s - 'A' + 10);
        }
        else {
            // malformed string
            return 0;
        }
        if (steps == 0) {
            res = j;
        }
        else {
            res = (res << 4) + j;
        }
        s++;
        steps++;
    }

    return (SVGTint)res;
}
