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

#ifndef STR_UTILS_H
#define STR_UTILS_H

/*!
    \file str_utils.h
    \brief String utilities, header.
    \author Matteo Muratori
    \author Michele Fabbri
*/

#include "config.h"

/************************************************************
                       String utilities
************************************************************/

// check if the given char is a space
SVGTboolean isSpace(const char c);

// check if the given char is an hexadecimal digit
SVGTboolean isHexDigit(const char c);

// replace every occurence of 'search' with 'substitute'
void replaceChar(char* str,
                 const char search,
                 const char substitute);

// transform the given char to lower case
char lowerChar(char ch);

// transform the given char to upper case
char upperChar(char ch);

// case insensitive string compare
SVGTint strCaseCmp(const char* s1,
                   const char* s2);

// skip all spaces
const char* skipSpaces(const char* str,
                       size_t* strLength);

// skip all characters until the given terminator is reached; NB: the given terminator is NOT skipped
const char* skipUntilChar(const char* str,
                          size_t* strLength,
                          const char terminator);

// convert an hexadecimal string to integer
SVGTint axtoi(const char* str);

#endif /* STR_UTILS_H */
