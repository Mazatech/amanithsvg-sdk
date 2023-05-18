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
    \file file_utils.c
    \brief File utilities, implementation.
    \author Matteo Muratori
    \author Michele Fabbri
*/

#include <string.h>
#if (defined(WIN32) || defined(_WIN32) || defined(__WIN32__)) || (defined(WIN64) || defined(_WIN64) || defined(__WIN64__))
    // Windows
    #include <windows.h>
    // for _access() function
    #include <io.h> 
#else
    // OSX / Unix / Linux
    #include <dirent.h>
    // for access() function
    #include <unistd.h>
#endif
// for stat() function
#include <sys/types.h>
#include <sys/stat.h>
// string and file utilities
#include "str_utils.h"
#include "file_utils.h"

/************************************************************
                      Files utilities
************************************************************/

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

// extract the file name part from a given full path name (e.g. removeFileExt("./subdir/myfile.txt") returns "./subdir/myfile")
void removeFileExt(char* fileNameWithoutExtension,
                   const char* fullFileName) {

    char* ext;

    (void)strcpy(fileNameWithoutExtension, fullFileName);
    ext = strrchr(fileNameWithoutExtension, '.');

    if (ext != NULL) {
        *ext = '\0';
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

// scan a directory, searching for files
SVGTboolean scanPath(FileSearchResult* searchResult,
                     const FileSearchSettings* searchSettings,
                     // the path to scan, it must be non NULL
                     const char* directory) {

    SVGTboolean ok = SVGT_TRUE;

#if (defined(WIN32) || defined(_WIN32) || defined(__WIN32__)) || (defined(WIN64) || defined(_WIN64) || defined(__WIN64__))

    // Windows (search for all files)
    HANDLE handle;
    WIN32_FIND_DATAA data;
    char searchAll[_MAX_PATH];

    (void)strcpy(searchAll, directory);
    (void)strcat(searchAll, "*");
    // find first file
    handle = FindFirstFileA(searchAll, &data);

    if (handle == INVALID_HANDLE_VALUE) {
        ok = SVGT_FALSE;
    }
    else {
        do {
            // enter a sub-directory
            if (data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) {
                if (searchSettings->scanRecursively) {
                    // we don't wan to select 'current' and 'previous' directories
                    if ((strcmp(data.cFileName, ".") != 0) && (strcmp(data.cFileName, "..") != 0)) {
                        char nextPath[_MAX_PATH];
                        (void)strcpy(nextPath, directory);
                        (void)strcat(nextPath, data.cFileName);
                        (void)strcat(nextPath, TRAILER_PATH_DELIMITER_STR);
                        ok = scanPath(searchResult, searchSettings, nextPath);
                    }
                }
            }
            else {
                // we have found a file
                SVGTboolean fileFound = SVGT_TRUE;

                if (searchSettings->fileExtFilter) {
                    char fileExt[_MAX_EXT];
                    // extract file extension
                    extractFileExt(fileExt, data.cFileName);
                    // if the file extension does not match the filter, do not output a new entry
                    if (strCaseCmp(fileExt, searchSettings->fileExtFilter) != 0) {
                        fileFound = SVGT_FALSE;
                    }
                }

                if (fileFound) {
                    // the final filename to be appended to the output list
                    FileName f, baseFileName;
                    // extract the base file name (i.e. the file name without extension)                    
                    extractFileName(baseFileName.name, data.cFileName, SVGT_TRUE);
                    // add full path, if specified
                    if (searchSettings->addPath) {
                        (void)strcpy(f.name, directory);
                        (void)strcat(f.name, baseFileName.name);
                    }
                    else {
                        (void)strcpy(f.name, baseFileName.name);
                    }
                    DYNARRAY_PUSH_BACK(searchResult->fileNames, FileName, f)
                }
            }
        } while (ok && (FindNextFileA(handle, &data) != 0));

        // close the file search
        (void)FindClose(handle);
    }

#else

    // OSX / Unix / Linux
    DIR* handle = opendir(directory);

    if (handle) {
        struct dirent *rc;
        while (ok && ((rc = readdir(handle)) != NULL)) {
            // enter a sub-directory
            if (rc->d_type == DT_DIR) {
                if (searchSettings->scanRecursively) {
                    // we don't wan to select 'current' and 'previous' directories
                    if ((strcmp(rc->d_name, ".") != 0) && (strcmp(rc->d_name, "..") != 0)) {
                        char nextPath[_MAX_PATH];
                        (void)strcpy(nextPath, directory);
                        (void)strcat(nextPath, rc->d_name);
                        ok = scanPath(searchResult, searchSettings, nextPath);
                    }
                }
            }
            else {
                // we have found a file
                SVGTboolean fileFound = SVGT_TRUE;

                if (searchSettings->fileExtFilter) {
                    char fileExt[_MAX_EXT];
                    // extract file extension
                    extractFileExt(fileExt, rc->d_name);
                    // if the file extension does not match the filter, do not output a new entry
                    if (strCaseCmp(fileExt, searchSettings->fileExtFilter) != 0) {
                        fileFound = SVGT_FALSE;
                    }
                }

                if (fileFound) {
                    // the final filename to be appended to the output list
                    FileName f, baseFileName;
                    // extract the base file name (i.e. the file name without extension)                    
                    extractFileName(baseFileName.name, rc->d_name, SVGT_TRUE);
                    // add full path, if specified
                    if (searchSettings->addPath) {
                        (void)strcpy(f.name, directory);
                        (void)strcat(f.name, baseFileName.name);
                    }
                    else {
                        (void)strcpy(f.name, baseFileName.name);
                    }
                    DYNARRAY_PUSH_BACK(searchResult->fileNames, FileName, f)
                }
            }
        }
        // close the file search
        closedir(handle);
    }
    else {
        ok = SVGT_FALSE;
    }

#endif

    return ok;
}

// ensure that every slash present within the path, will be set according to the current operating system type ('\' for Windows, ' / ' for *nix)
void fixPath(char* filePath,
             // SVGT_TRUE = fixed path is ensured to include a path delimiter as last character
             const SVGTboolean ensureFinalDelimiter) {

    if (TRAILER_PATH_DELIMITER == '/') {
        replaceChar(filePath, '\\', '/');
    }
    else {
        replaceChar(filePath, '/', '\\');
    }

    if (ensureFinalDelimiter) {
        const char lastChar = filePath[strlen(filePath) - 1];
        if (lastChar != TRAILER_PATH_DELIMITER) {
            (void)strcat(filePath, TRAILER_PATH_DELIMITER_STR);
        }
    }
}

static SVGTboolean fileStatusCheck(const char* absolutePath,
                                   const SVGTshort fileMode) {

    FileName path;
    size_t l = strlen(absolutePath);
    SVGTboolean result = SVGT_FALSE;

    // remove a possible trailer path delimiter at the end
    (void)strcpy(path.name, absolutePath);
    if (path.name[l - 1] == TRAILER_PATH_DELIMITER) {
        path.name[l - 1] = 0;
    }

    // to check the file existence it has been used the non - ANSI access() function. This function is present on
    // Windows, SUNOS 5.4+, LINUX 1.1.46+ and conforming to SVID, AT&T, POSIX, X / OPEN, and BSD 4.3 */
#if (defined(WIN32) || defined(_WIN32) || defined(__WIN32__)) || (defined(WIN64) || defined(_WIN64) || defined(__WIN64__))
    // Windows
    if (_access(path.name, 0) == 0) {
        struct stat status;
        (void)stat(path.name, &status);
        result = ((status.st_mode & fileMode) != 0) ? SVGT_TRUE : SVGT_FALSE;
    }
#else
    // OSX / Unix / Linux
    if (access(path.name, 0) == 0) {
        struct stat status;
        (void)stat(path.name, &status);
        result = ((status.st_mode & fileMode) != 0) ? SVGT_TRUE : SVGT_FALSE;
    }
#endif

    return result;
}

// check if the specified path exists and if it really is a directory
SVGTboolean directoryExists(const char* absolutePath) {

    return fileStatusCheck(absolutePath, S_IFDIR);
}

// check if the specified path exists and if it really is a regular file
SVGTboolean fileExists(const char* absoluteFileName) {

    return fileStatusCheck(absoluteFileName, S_IFREG);
}

// load an external file in memory, returning the allocated buffer and its size in bytes
SVGTubyte* loadFile(const char* fileName,
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
