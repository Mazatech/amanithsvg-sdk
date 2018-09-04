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

#ifndef SVGTPLATFORM_H
#define SVGTPLATFORM_H

#ifdef __cplusplus
extern "C" {
#endif

/* Compiler detection */
#if defined(_MSC_VER)
    #define SVG_CC_MSVC _MSC_VER
#elif (defined(__GNUC__) && !defined(__ARMCC_VERSION)) || (defined(__SYMBIAN32__) && defined (__GCC32__))
    #define SVG_CC_GCC
    #if (__GNUC__ >= 4) || (__GNUC__ == 3 && __GNUC_MINOR__ >= 4)
        #define SVG_GCC_HAS_CLASS_VISIBILITY
    #else
        #undef SVG_GCC_HAS_CLASS_VISIBILITY
    #endif
#elif defined(__ARMCC_VERSION) || (defined(__SYMBIAN32__) && defined(__ARMCC_2__))
    #define SVG_CC_ARMCC __ARMCC_VERSION
#else
    #error Unsupported compiler!
#endif

/* SVGT_API_CALL definition */
#if defined(SVG_MAKE_STATIC_LIBRARY)
    #undef SVG_MAKE_DYNAMIC_LIBRARY
    /* For pre-linked static libraries (iOS), we want to show/export only SVGT API public functions */
    #if defined(SVG_CC_GCC)
        #if defined(SVG_GCC_HAS_CLASS_VISIBILITY)
            #define SVGT_API_CALL __attribute__((visibility("default"))) extern
        #else
            #define SVGT_API_CALL extern
        #endif
    #else
        #define SVGT_API_CALL
    #endif
#else
    #if defined(SVG_CC_MSVC) || defined(SVG_CC_ARMCC) || (defined(SVG_CC_GCC) && (defined(_WIN32_WCE) || defined(__MINGW32__) || defined(__MINGW64__)))
        #if defined(SVG_MAKE_DYNAMIC_LIBRARY)
            #define SVGT_API_CALL __declspec(dllexport)
        #else
            #define SVGT_API_CALL __declspec(dllimport)
        #endif
    #elif defined(SVG_CC_GCC)
        #if defined(SVG_MAKE_DYNAMIC_LIBRARY)
            #if defined(SVG_GCC_HAS_CLASS_VISIBILITY)
                #define SVGT_API_CALL __attribute__((visibility("default"))) extern
            #else
                #define SVGT_API_CALL extern
            #endif
        #else
            #define SVGT_API_CALL extern
        #endif
    #else
        #undef SVGT_API_CALL
    #endif
#endif

#ifndef SVGT_API_ENTRY
    #define SVGT_API_ENTRY
#endif

#ifndef SVGT_API_EXIT
    #define SVGT_API_EXIT
#endif

typedef float          SVGTfloat;
typedef signed char    SVGTbyte;
typedef unsigned char  SVGTubyte;
typedef signed short   SVGTshort;
typedef signed int     SVGTint;
typedef unsigned int   SVGTuint;
typedef unsigned int   SVGTbitfield;

#ifdef __cplusplus 
} /* extern "C" */
#endif

#endif /* SVGTPLATFORM_H */
