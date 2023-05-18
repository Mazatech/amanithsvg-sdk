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

#ifndef FILE_UTILS_H
#define FILE_UTILS_H

/*!
    \file file_utils.h
    \brief File utilities, header.
    \author Matteo Muratori
    \author Michele Fabbri
*/

// AmanithSVG
#include <SVGT/svgt.h>
#include "dynarray.h"

#if !defined(_MAX_PATH)
    // max length of full pathname
    #define _MAX_PATH                  260
#endif
#if !defined(_MAX_DRIVE)
    // max length of drive component
    #define _MAX_DRIVE                 3
#endif
#if !defined(_MAX_DIR)
    // max length of path component
    #define _MAX_DIR                   256
#endif
#if !defined(_MAX_FNAME)
    // max length of file name component
    #define _MAX_FNAME                 256
#endif
#if !defined(_MAX_EXT)
    // max length of extension component
    #define _MAX_EXT                   256
#endif

// a type representing a directory/path
typedef struct {
    char path[_MAX_PATH];
} Directory;

// a type representing a full filename (i.e. a path plus a name plus an extension)
typedef union {
    char buffer[_MAX_PATH + _MAX_FNAME + _MAX_EXT + 28];
    char name[_MAX_PATH + _MAX_FNAME + _MAX_EXT];
} FileName;

/************************************************************
                      Files utilities
************************************************************/

// an array of file names
DYNARRAY_DECLARE(FileNameDynArray, FileName)

typedef struct {
    // SVGT_TRUE = recursive scan on (i.e. the scan operation will be propagated recursively on all sub-directories), SVGT_FALSE = recursive scan off
    SVGTboolean scanRecursively;
    // SVGT_TRUE = add path to returned file names, SVGT_FALSE = do not add path to returned file names
    SVGTboolean addPath;
    // an optional filter, if non NULL only files that match this file extension will be added to the output list
    const char* fileExtFilter;
} FileSearchSettings;

typedef struct {
    // the search result, a list of file names
    FileNameDynArray fileNames;
} FileSearchResult;

// extract the file name part from a given full path name (e.g. extractFileName("./subdir/myfile.txt") returns "myfile")
void extractFileName(char* fileName,
                     const char* fullFileName,
                     const SVGTboolean includeExtension);

// extract the file name part from a given full path name (e.g. removeFileExt("./subdir/myfile.txt") returns "./subdir/myfile")
void removeFileExt(char* fileNameWithoutExtension,
                   const char* fullFileName);

// extract the extension part from a given full path name (e.g. extractFileExt("./subdir/myfile.txt") returns "txt")
void extractFileExt(char* fileExt,
                    const char* fullFileName);

// scan a directory, searching for files
SVGTboolean scanPath(FileSearchResult* searchResult,
                     const FileSearchSettings* searchSettings,
                     // the path to scan, it must be non NULL
                     const char* directory);

// ensure that every slash present within the path, will be set according to the current operating system type ('\' for Windows, ' / ' for *nix)
void fixPath(char* filePath,
             // SVGT_TRUE = fixed path is ensured to include a path delimiter as last character
             const SVGTboolean ensureFinalDelimiter);

// check if the specified path exists and if it really is a directory
SVGTboolean directoryExists(const char* absolutePath);

// check if the specified path exists and if it really is a regular file
SVGTboolean fileExists(const char* absoluteFileName);

// load an external file in memory, returning the allocated buffer and its size in bytes
SVGTubyte* loadFile(const char* fileName,
                    const SVGTuint padAmount,
                    size_t* fileSize);

#endif /* FILE_UTILS_H */
