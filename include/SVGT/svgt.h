/****************************************************************************
** Copyright (C) 2013-2023 Mazatech S.r.l. All rights reserved.
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

#ifndef SVGT_H
#define SVGT_H

#include <SVGT/svgtplatform.h>

#if defined(__cplusplus)
extern "C" {
#endif

#ifndef SVGT_MAX_ENUM
     #define SVGT_MAX_ENUM 0x7FFFFFFF
#endif

typedef SVGTuint SVGTHandle;

#define SVGT_INVALID_HANDLE ((SVGTHandle)0)

typedef enum {
    SVGT_FALSE                                    = 0,
    SVGT_TRUE                                     = 1,

    SVGT_BOOLEAN_FORCE_SIZE                       = SVGT_MAX_ENUM
} SVGTboolean;

/*
    Alignment indicates whether to force uniform scaling and, if so, the alignment method to use in case the aspect ratio of the source
    viewport doesn't match the aspect ratio of the destination (drawing surface) viewport.
*/
typedef enum {
    /*
        Do not force uniform scaling.
        Scale the graphic content of the given element non-uniformly if necessary such that
        the element's bounding box exactly matches the viewport rectangle.
        NB: in this case, the <meetOrSlice> value is ignored.
    */
    SVGT_ASPECT_RATIO_ALIGN_NONE                  = 0,

    /*
        Force uniform scaling.
        Align the <min-x> of the source viewport with the smallest x value of the destination (drawing surface) viewport.
        Align the <min-y> of the source viewport with the smallest y value of the destination (drawing surface) viewport.
    */
    SVGT_ASPECT_RATIO_ALIGN_XMINYMIN              = 1,

    /*
        Force uniform scaling.
        Align the <mid-x> of the source viewport with the midpoint x value of the destination (drawing surface) viewport.
        Align the <min-y> of the source viewport with the smallest y value of the destination (drawing surface) viewport.
    */
    SVGT_ASPECT_RATIO_ALIGN_XMIDYMIN              = 2,

    /*
        Force uniform scaling.
        Align the <max-x> of the source viewport with the maximum x value of the destination (drawing surface) viewport.
        Align the <min-y> of the source viewport with the smallest y value of the destination (drawing surface) viewport.
    */
    SVGT_ASPECT_RATIO_ALIGN_XMAXYMIN              = 3,

    /*
        Force uniform scaling.
        Align the <min-x> of the source viewport with the smallest x value of the destination (drawing surface) viewport.
        Align the <mid-y> of the source viewport with the midpoint y value of the destination (drawing surface) viewport.
    */
    SVGT_ASPECT_RATIO_ALIGN_XMINYMID              = 4,

    /*
        Force uniform scaling.
        Align the <mid-x> of the source viewport with the midpoint x value of the destination (drawing surface) viewport.
        Align the <mid-y> of the source viewport with the midpoint y value of the destination (drawing surface) viewport.
    */
    SVGT_ASPECT_RATIO_ALIGN_XMIDYMID              = 5,

    /*
        Force uniform scaling.
        Align the <max-x> of the source viewport with the maximum x value of the destination (drawing surface) viewport.
        Align the <mid-y> of the source viewport with the midpoint y value of the destination (drawing surface) viewport.
    */
    SVGT_ASPECT_RATIO_ALIGN_XMAXYMID              = 6,

    /*
        Force uniform scaling.
        Align the <min-x> of the source viewport with the smallest x value of the destination (drawing surface) viewport.
        Align the <max-y> of the source viewport with the maximum y value of the destination (drawing surface) viewport.
    */
    SVGT_ASPECT_RATIO_ALIGN_XMINYMAX              = 7,

    /*
        Force uniform scaling.
        Align the <mid-x> of the source viewport with the midpoint x value of the destination (drawing surface) viewport.
        Align the <max-y> of the source viewport with the maximum y value of the destination (drawing surface) viewport.
    */
    SVGT_ASPECT_RATIO_ALIGN_XMIDYMAX              = 8,

    /*
        Force uniform scaling.
        Align the <max-x> of the source viewport with the maximum x value of the destination (drawing surface) viewport.
        Align the <max-y> of the source viewport with the maximum y value of the destination (drawing surface) viewport.
    */
    SVGT_ASPECT_RATIO_ALIGN_XMAXYMAX              = 9,

    SVGT_ASPECT_RATIO_ALIGN_FORCE_SIZE            = SVGT_MAX_ENUM
} SVGTAspectRatioAlign;

typedef enum {
    /*
        Scale the graphic such that:
        - aspect ratio is preserved
        - the entire viewBox is visible within the viewport
        - the viewBox is scaled up as much as possible, while still meeting the other criteria

        In this case, if the aspect ratio of the graphic does not match the viewport, some of the viewport will
        extend beyond the bounds of the viewBox (i.e., the area into which the viewBox will draw will be smaller
        than the viewport).
    */
    SVGT_ASPECT_RATIO_MEET                        = 0,

    /*
        Scale the graphic such that:
        - aspect ratio is preserved
        - the entire viewport is covered by the viewBox
        - the viewBox is scaled down as much as possible, while still meeting the other criteria
        
        In this case, if the aspect ratio of the viewBox does not match the viewport, some of the viewBox will
        extend beyond the bounds of the viewport (i.e., the area into which the viewBox will draw is larger
        than the viewport).
    */
    SVGT_ASPECT_RATIO_SLICE                       = 1,

    SVGT_ASPECT_RATIO_MEETSLICE_FORCE_SIZE        = SVGT_MAX_ENUM
} SVGTAspectRatioMeetOrSlice;

typedef enum {
    /* no error (i.e. the operation was completed successfully) */
    SVGT_NO_ERROR                                 =  0,
    /* it indicates that the library has not previously been initialized through the svgtInit() function */
    SVGT_NOT_INITIALIZED_ERROR                    =  1,
    /* returned when one or more invalid document/surface handles are passed to AmanithSVG functions */
    SVGT_BAD_HANDLE_ERROR                         =  2,
    /* returned when one or more illegal arguments are passed to AmanithSVG functions */
    SVGT_ILLEGAL_ARGUMENT_ERROR                   =  3,
    /* all AmanithSVG functions may signal an "out of memory" error */
    SVGT_OUT_OF_MEMORY_ERROR                      =  4,
    /* returned when an invalid or malformed XML is passed to the svgtDocCreate */
    SVGT_PARSER_ERROR                             =  5,
    /* returned when a document fragment is technically in error (e.g. if an element has an attribute or property value which is not permissible according to SVG specification or if the outermost element is not an <svg> element) */
    SVGT_INVALID_SVG_ERROR                        =  6,
    /* returned when a current packing task is still open, and so the operation (e.g. svgtPackingBinInfo) is not allowed */
    SVGT_STILL_PACKING_ERROR                      =  7,
    /* returned when there isn't a currently open packing task, and so the operation (e.g. svgtPackingAdd) is not allowed */
    SVGT_NOT_PACKING_ERROR                        =  8,
    /* the specified resource, via svgtResourceSet, is not valid or it does not match the given resource type */
    SVGT_INVALID_RESOURCE_ERROR                   =  9,

    SVGT_UNKNOWN_ERROR                            = 10,

    SVGT_ERROR_CODE_FORCE_SIZE                    = SVGT_MAX_ENUM
} SVGTErrorCode;

/* SVG document profile. */
typedef enum {
    /* SVG Full 1.1 */
    SVGT_FULL11_PROFILE                           = 0,
    /* SVG Basic 1.1 */
    SVGT_BASIC11_PROFILE                          = 1,
    /* SVG Tiny 1.2 */
    SVGT_TINY12_PROFILE                           = 2,
    /* Unknown/undefined */
    SVGT_UNKNOWN_PROFILE                          = 3,

    SVGT_PROFILE_FORCE_SIZE                       = SVGT_MAX_ENUM
} SVGTProfile;

typedef enum {
    /* disables antialiasing */
    SVGT_RENDERING_QUALITY_NONANTIALIASED         = 0,
    /* causes rendering to be done at the highest available speed */
    SVGT_RENDERING_QUALITY_FASTER                 = 1,
    /* causes rendering to be done with the highest available quality */
    SVGT_RENDERING_QUALITY_BETTER                 = 2,

    SVGT_RENDERING_QUALITY_FORCE_SIZE             = SVGT_MAX_ENUM
} SVGTRenderingQuality;

/*
    Configuration parameters
*/
typedef enum {
    // Used by AmanithSVG geometric kernel to approximate curves with straight line
    // segments (flattening). Valid range is [1; 100], where 100 represents the best quality.
    SVGT_CONFIG_CURVES_QUALITY                    = 1,
    // The maximum number of different threads that can "work" (e.g. create surfaces, draw documents, etc) concurrently.
    // (READ-ONLY)
    SVGT_CONFIG_MAX_CURRENT_THREADS               = 2,
    // The maximum dimension allowed for drawing surfaces, in pixels. This is the maximum valid value that can be specified as
    // 'width' and 'height' for the svgtSurfaceCreate and svgtSurfaceResize functions.
    // (READ-ONLY)
    SVGT_CONFIG_MAX_SURFACE_DIMENSION             = 3,

    SVGT_CONFIG_FORCE_SIZE                        = SVGT_MAX_ENUM
} SVGTConfig;

typedef enum {
    SVGT_VENDOR                                   = 1,
    SVGT_VERSION                                  = 2,
    SVGT_COMPILE_CONFIG_INFO                      = 3,
    SVGT_OPENVG_VERSION                           = 4,
    SVGT_OPENVG_RENDERER                          = 5,
    SVGT_OPENVG_EXTENSIONS                        = 6,
    SVGT_OPENVG_COMPILE_CONFIG_INFO               = 7,

    SVGT_STRING_ID_FORCE_SIZE                     = SVGT_MAX_ENUM
} SVGTStringID;

typedef enum {
    /*
        Errors that prevent correct rendering from being completed, such that malformed
        SVG (xml) content, negative dimensions for geometric shapes, and all those cases
        where a document fragment is technically in error according to SVG specifications
    */
    SVGT_LOG_LEVEL_ERROR                          = (1 << 0),
    /*
        Warning conditions that need to be taken care of, such that an outermost <svg>
        element without a 'width' or 'height' attribute, a missing font resource, and so on
    */
    SVGT_LOG_LEVEL_WARNING                        = (1 << 1),
    /* Informational message */
    SVGT_LOG_LEVEL_INFO                           = (1 << 2),

    SVGT_LOG_LEVEL_FORCE_SIZE                     = SVGT_MAX_ENUM
} SVGTLogLevel;

/* All levels enabled */
#define SVGT_LOG_LEVEL_ALL                        ((1 << 3) - 1)

//! The type of an external resource.
typedef enum {
    /* Font resource (vector fonts like OTF, TTF and WOFF are supported, bitmap fonts are not supported). */
    SVGT_RESOURCE_TYPE_FONT = 1,
    /* Bitmap/image resource (only PNG and JPEG are supported; 16 bits PNG are not supported). */
    SVGT_RESOURCE_TYPE_IMAGE = 2,

    SVGT_RESOURCE_TYPE_FORCE_SIZE = SVGT_MAX_ENUM
} SVGTResourceType;

//! Resource hints.
typedef enum {
    //! The given font resource must be selected when the font-family attribute matches the 'serif' generic family.
    SVGT_RESOURCE_HINT_DEFAULT_SERIF_FONT         = (1 << 0),
    //! The given font resource must be selected when the font-family attribute matches the 'sans-serif' generic family.
    SVGT_RESOURCE_HINT_DEFAULT_SANS_SERIF_FONT    = (1 << 1),
    //! The given font resource must be selected when the font-family attribute matches the 'monospace' generic family.
    SVGT_RESOURCE_HINT_DEFAULT_MONOSPACE_FONT     = (1 << 2),
    //! The given font resource must be selected when the font-family attribute matches the 'cursive' generic family.
    SVGT_RESOURCE_HINT_DEFAULT_CURSIVE_FONT       = (1 << 3),
    //! The given font resource must be selected when the font-family attribute matches the 'fantasy' generic family.
    SVGT_RESOURCE_HINT_DEFAULT_FANTASY_FONT       = (1 << 4),
    //! The given font resource must be selected when the font-family attribute matches the 'system-ui' generic family.
    SVGT_RESOURCE_HINT_DEFAULT_SYSTEM_UI_FONT     = (1 << 5),
    //! The given font resource must be selected when the font-family attribute matches the 'ui-serif' generic family.
    SVGT_RESOURCE_HINT_DEFAULT_UI_SERIF_FONT      = (1 << 6),
    //! The given font resource must be selected when the font-family attribute matches the 'ui-sans-serif' generic family.
    SVGT_RESOURCE_HINT_DEFAULT_UI_SANS_SERIF_FONT = (1 << 7),
    //! The given font resource must be selected when the font-family attribute matches the 'ui-monospace' generic family.
    SVGT_RESOURCE_HINT_DEFAULT_UI_MONOSPACE_FONT  = (1 << 8),
    //! The given font resource must be selected when the font-family attribute matches the 'ui-rounded' generic family.
    SVGT_RESOURCE_HINT_DEFAULT_UI_ROUNDED_FONT    = (1 << 9),
    //! The given font resource must be selected when the font-family attribute matches the 'emoji' generic family.
    SVGT_RESOURCE_HINT_DEFAULT_EMOJI_FONT         = (1 << 10),
    //! The given font resource must be selected when the font-family attribute matches the 'math' generic family.
    SVGT_RESOURCE_HINT_DEFAULT_MATH_FONT          = (1 << 11),
    //! The given font resource must be selected when the font-family attribute matches the 'fangsong' generic family.
    SVGT_RESOURCE_HINT_DEFAULT_FANGSONG_FONT      = (1 << 12),

    SVGT_RESOURCE_HINT_SIZE = SVGT_MAX_ENUM

} SVGTResourceHint;

/* Hint relative to "all default fonts" */
#define SVGT_RESOURCE_HINT_DEFAULT_FONT_ALL       ((1 << 13) - 1)

/* Packed rectangle */
typedef struct {
    /* 'id' attribute, NULL if not present. */
    const char* elemName;
    /* Original rectangle corner. */
    SVGTint originalX;
    SVGTint originalY;
    /* Rectangle corner position. */
    SVGTint x;
    SVGTint y;
    /* Rectangle dimensions. */
    SVGTint width;
    SVGTint height;
    /* SVG document handle. */
    SVGTHandle docHandle;
    /* 0 for the whole SVG, else the element (tree) index. */
    SVGTuint elemIdx;
    /* Z-order. */
    SVGTint zOrder;
    /* The used destination viewport width (induced by packing scale factor). */
    SVGTfloat dstViewportWidth;
    /* The used destination viewport height (induced by packing scale factor). */
    SVGTfloat dstViewportHeight;
} SVGTPackedRect;

/* Function prototypes */
#ifndef SVGT_API_CALL
    #error SVGT_API_CALL must be defined
#endif

#ifndef SVGT_API_ENTRY
    #error SVGT_API_ENTRY must be defined
#endif

#ifndef SVGT_API_EXIT
    #error SVGT_API_EXIT must be defined
#endif

/*
    Retrieve the error code generated by the last called API function.
    The last-error code is maintained on a per-thread basis. Multiple threads do not overwrite each other's last-error code.

    Not all functions set the last-error code; however those who do, they set the last-error code both in the event of an error and of success.
    So svgtGetLastError should be called immediately when an API function's return value indicates that such a call will return useful data.

    If the API function is not documented to set the last-error code, the value returned by this function is simply the most recent last-error code to have been set.
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtGetLastError(void) SVGT_API_EXIT;

/*
    Initialize the library.
    This function does not set the last-error code (see svgtGetLastError).

    It returns SVGT_NO_ERROR if the operation was completed successfully, else an error code.
    NB: in multi-thread applications, this function must be called once, before any created/spawned thread makes use of the library functions.
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtInit(SVGTuint screenWidth,
                                                    SVGTuint screenHeight,
                                                    SVGTfloat dpi) SVGT_API_EXIT;

/*
    Destroy the library, freeing all allocated resources.
    NB: in multi-thread applications, this function must be called once, when all created/spawned threads have finished using the library functions.

    NB: this function does not set the last-error code (see svgtGetLastError).
*/
SVGT_API_CALL void SVGT_API_ENTRY svgtDone(void) SVGT_API_EXIT;

/*
    Configure parameters and thresholds for the AmanithSVG library.
    This function does not set the last-error code (see svgtGetLastError).

    This function can be called at any time, but it will have effect only if:

    - the library has not been already initialized by a previous call to svgtInit, or
    - the library has been already initialized (i.e. svgtInit has been already called) but no other
      functions, except for svgtMaxCurrentThreads, svgtSurfaceMaxDimension, svgtConfigGet,
      svgtLanguageSet, svgtResourceSet have been called.

    When 'config' refers to an integer parameter, the given 'value' is converted to an integer
    using a mathematical floor operation.

    Certain SVGTConfig values refer to read-only parameters (e.g. SVGT_CONFIG_MAX_SURFACE_DIMENSION).
    Calling svgtConfigSet on these parameters has no effect (SVGT_NO_ERROR is returned).

    This function returns:
    - SVGT_ILLEGAL_ARGUMENT_ERROR if the specified value is not valid for the given configuration parameter
    - SVGT_NO_ERROR if the library has been already initialized and one or more functions
      different than svgtMaxCurrentThreads, svgtSurfaceMaxDimension, svgtResourceSet, have been called.
      In this case the function does nothing: by calling svgtConfigGet, you will get back the same value
      that the configuration parameter had before the call to svgtConfigSet.
    - SVGT_NO_ERROR in all other cases (i.e. in case of success).
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtConfigSet(SVGTConfig config,
                                                         SVGTfloat value) SVGT_API_EXIT;

/*
    Get the current value relative to the specified configuration parameter.
    This function does not set the last-error code (see svgtGetLastError).

    If the given parameter is invalid (i.e. it does not correspond to any
    value of the SVGTConfig enum type), a negative number is returned.
*/
SVGT_API_CALL SVGTfloat SVGT_API_ENTRY svgtConfigGet(SVGTConfig config) SVGT_API_EXIT;

/*
    Set the system / user-agent language.
    This function does not set the last-error code (see svgtGetLastError).

    This setting will affect the conditional rendering of <switch> elements and
    elements with 'systemLanguage' attribute specified. The given argument must
    be a non-NULL, non-empty list of languages separated by semicolon (e.g. en-US;en-GB;it;es)

    This function can be called at any time, but it will have effect only if:

    - the library has not been already initialized by a previous call to svgtInit, or
    - the library has been already initialized (i.e. svgtInit has been already called) but no other
      functions, except for svgtMaxCurrentThreads, svgtSurfaceMaxDimension, svgtConfigGet,
      svgtConfigSet, svgtResourceSet have been called.

    This function returns:
    - SVGT_ILLEGAL_ARGUMENT_ERROR if the given string is NULL or empty
    - SVGT_NO_ERROR if the library has been already initialized and one or more functions
      different than svgtMaxCurrentThreads, svgtSurfaceMaxDimension, svgtResourceSet, have been called.
      In this case the function does nothing.
    - SVGT_NO_ERROR in all other cases (i.e. in case of success).

    NB: the library starts with a default "en" (generic English).
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtLanguageSet(const char* languages) SVGT_API_EXIT;

/*
    Instruct the library about an external resource.
    All resources must be specified in advance after the call to svgtInit
    and before to create/spawn threads (i.e. no other functions, except for
    svgtMaxCurrentThreads, svgtSurfaceMaxDimension, svgtConfigGet, svgtConfigSet,
    svgtLanguageSet have been called).

    This function does not set the last-error code (see svgtGetLastError).

    'id' must be a not-empty null-terminated string.

    'buffer' must point to a read-only area containing the resource file.
    Such read-only buffer must be a valid and immutable memory area throughout the
    whole life of the application that uses the library. In multi-thread applications
    the 'buffer' memory must be accessible (readable) by all threads.

    'type' specifies the type of resource.

    'hints' must be a valid bitwise OR of values from the SVGTResourceHint enumeration.

    If a previous resource has been specified with the given 'id', the new provided
    'buffer' pointer is used.

    This function returns:
    - SVGT_NOT_INITIALIZED_ERROR if the library has not yet been initialized (see svgtInit)
    - SVGT_ILLEGAL_ARGUMENT_ERROR if 'id' is NULL or an empty string
    - SVGT_ILLEGAL_ARGUMENT_ERROR if 'buffer' is NULL or if 'bufferSize' is zero
    - SVGT_ILLEGAL_ARGUMENT_ERROR if 'type' is not a valid value from the SVGTResourceType enumeration
    - SVGT_ILLEGAL_ARGUMENT_ERROR if 'hints' is not a bitwise OR of values from the SVGTResourceHint enumeration
    - SVGT_ILLEGAL_ARGUMENT_ERROR if 'type' is SVGT_RESOURCE_TYPE_IMAGE and 'hints' is different than zero
    - SVGT_INVALID_RESOURCE_ERROR if 'type' is SVGT_RESOURCE_TYPE_FONT and the given buffer does not contain a valid font file
    - SVGT_INVALID_RESOURCE_ERROR if 'type' is SVGT_RESOURCE_TYPE_IMAGE and the given buffer does not contain a valid bitmap file
    - SVGT_NO_ERROR if the operation was completed successfully

    NB: vector fonts like OTF, TTF and WOFF are supported, bitmap fonts are not supported; as for images, only JPEG and PNG are
    supported (16 bits PNG are not supported).
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtResourceSet(const char* id,
                                                           const SVGTubyte* buffer,
                                                           SVGTuint bufferSize,
                                                           SVGTResourceType type,
                                                           SVGTbitfield hints) SVGT_API_EXIT;

/*
    Get the maximum number of different threads that can "work" (e.g. create surfaces, create documents and draw them) concurrently.
    This function does not set the last-error code (see svgtGetLastError).

    In multi-thread applications, each thread can only draw documents it has created, on surfaces it has created.
    In other words, a thread cannot draw a document created by another thread or on surfaces belonging to other threads.

    The function can be called at any time and always returns a valid value.

    NB: this function is just a shortcut to svgtConfigGet(SVGT_CONFIG_MAX_CURRENT_THREADS) and may be removed in the future.
*/
SVGT_API_CALL SVGTuint SVGT_API_ENTRY svgtMaxCurrentThreads(void) SVGT_API_EXIT;

/*
    Get the maximum dimension allowed for drawing surfaces.
    This function does not set the last-error code (see svgtGetLastError).

    This is the maximum valid value that can be specified as 'width' and 'height' for the svgtSurfaceCreate and svgtSurfaceResize functions.
    The function can be called at any time and always returns a valid value.

    NB: this function is just a shortcut to svgtConfigGet(SVGT_CONFIG_MAX_SURFACE_DIMENSION) and may be removed in the future.
*/
SVGT_API_CALL SVGTuint SVGT_API_ENTRY svgtSurfaceMaxDimension(void) SVGT_API_EXIT;

/*
    Set the log buffer for the current thread.
    It is important to not specify the same buffer memory for different threads, because AmanithSVG does not synchronize write
    operations to the provided log buffer: in other words, each thread must provide its own log buffer.

    'logBuffer' can be NULL or a valid pointer to a characters buffer where AmanithSVG can log messages. If NULL, logging is disabled.
    Messages will be appended one after the other, as long as there is enough free space within the buffer. When there is no
    more space, messages will stop being written to the buffer.

    'logBufferCapacity' is the capacity of 'logBuffer', in characters. If zero is specified, logging is disabled.
    AmanithSVG has no way of checking the actual buffer capacity, so it is up to the caller to specify the correct value, otherwise
    memory errors will be possible (e.g. buffer overrun).

    'logLevel' must be a bitwise OR of the desired SVGTLogLevel values. If zero is specified, logging is disabled.

    NB: after calling this function, the buffer will be initialized as empty (i.e. a '\0' character will be written at the beginning).

    This function returns:
    - SVGT_NOT_INITIALIZED_ERROR if the library has not yet been initialized (see svgtInit)
    - SVGT_ILLEGAL_ARGUMENT_ERROR if 'logLevel' is not a bitwise OR of values from the SVGTLogLevel enumeration
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtLogBufferSet(char* logBuffer,
                                                            SVGTuint logBufferCapacity,
                                                            SVGTbitfield logLevel) SVGT_API_EXIT;

/*
    Append the given message to the log buffer.
    Log buffer must have been already set through svgtLogBufferSet function, otherwise the message will be discarded.

    'message' must be a null-terminated string.

    'level' represents the message severity, and it determines its importance.

    This function returns:
    - SVGT_NOT_INITIALIZED_ERROR if the library has not yet been initialized (see svgtInit)
    - SVGT_ILLEGAL_ARGUMENT_ERROR if 'message' is null or if 'level' is not a value from the SVGTLogLevel enumeration
    - SVGT_NO_ERROR if no buffer has been set (via svgtLogBufferSet), or if the operation was completed successfully
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtLogPrint(const char* message,
                                                        SVGTLogLevel level) SVGT_API_EXIT;

/*
    Get information about the current thread log buffer.
    The 'info' parameter must be an array of (at least) 4 entries, it will be filled with:

    - info[0] = log level (a bitwise OR of SVGTLogLevel enumeration)
    - info[1] = buffer capacity, in characters
    - info[2] = current length, in characters (i.e. the total number of characters written, included the trailing '\0')
    - info[3] = fullness (SVGT_TRUE if buffer is full, else SVGT_FALSE)

    NB: please note that buffer fullness does not correspond, in general, to the length == capacity condition.
    So in order to know if buffer is full (i.e. AmanithSVG can no longer write any messages to it), simply
    check the returned information in info[3] entry.

    This function returns:
    - SVGT_NOT_INITIALIZED_ERROR if the library has not yet been initialized (see svgtInit)
    - SVGT_ILLEGAL_ARGUMENT_ERROR if 'info' pointer is NULL or if it's not properly aligned
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtLogBufferInfo(SVGTuint* info) SVGT_API_EXIT;

/*
    Create a new drawing surface, specifying its dimensions in pixels.
    
    Specified width and height must be greater than zero; they are silently clamped to the value returned by the svgtSurfaceMaxDimension function.
    The user should call svgtSurfaceWidth, svgtSurfaceHeight after svgtSurfaceCreate in order to check real drawing surface dimensions.

    Return SVGT_INVALID_HANDLE in case of errors, else a valid drawing surface handle.
*/
SVGT_API_CALL SVGTHandle SVGT_API_ENTRY svgtSurfaceCreate(SVGTuint width,
                                                          SVGTuint height) SVGT_API_EXIT;

/*
    Destroy a previously created drawing surface.

    It returns SVGT_NO_ERROR if the operation was completed successfully, else an error code.
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtSurfaceDestroy(SVGTHandle surface) SVGT_API_EXIT;

/*
    Resize a drawing surface, specifying new dimensions in pixels.
    
    Specified newWidth and newHeight must be greater than zero; they are silently clamped to the value returned by the svgtSurfaceMaxDimension function.
    The user should call svgtSurfaceWidth, svgtSurfaceHeight after svgtSurfaceResize in order to check real drawing surface dimensions.

    After resizing, the surface viewport will be reset to the whole surface (see svgtSurfaceViewportGet / svgtSurfaceViewportSet).

    It returns SVGT_NO_ERROR if the operation was completed successfully, else an error code.
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtSurfaceResize(SVGTHandle surface,
                                                             SVGTuint newWidth,
                                                             SVGTuint newHeight) SVGT_API_EXIT;

/*
    Get width dimension (in pixels), of the specified drawing surface.
    If the specified surface handle is not valid, 0 is returned.
*/
SVGT_API_CALL SVGTuint SVGT_API_ENTRY svgtSurfaceWidth(SVGTHandle surface) SVGT_API_EXIT;

/*
    Get height dimension (in pixels), of the specified drawing surface.
    If the specified surface handle is not valid, 0 is returned.
*/
SVGT_API_CALL SVGTuint SVGT_API_ENTRY svgtSurfaceHeight(SVGTHandle surface) SVGT_API_EXIT;

/*
    Get access to the drawing surface pixels.
    If the specified surface handle is not valid, NULL is returned.

    Please use this function to access surface pixels for read-only purposes (e.g. blit the surface
    on the screen, according to the platform graphic subsystem, upload pixels into a GPU texture, and so on).
    Writing or modifying surface pixels by hand is still possible, but not advisable.
*/
SVGT_API_CALL const void* SVGT_API_ENTRY svgtSurfacePixels(SVGTHandle surface) SVGT_API_EXIT;

/*
    Copy drawing surface content into the specified pixels buffer.

    This function is useful for managed environments (e.g. C#, Unity, Java, Android), where the use of a direct pixels
    access (i.e. svgtSurfacePixels) is not advisable nor comfortable.
    If the 'redBlueSwap' flag is set to SVGT_TRUE, the copy process will also swap red and blue channels for each pixel; this
    kind of swap could be useful when dealing with OpenGL/Direct3D texture uploads (RGBA or BGRA formats).
    If the 'dilateEdgesFix' flag is set to SVGT_TRUE, the copy process will also perform a 1-pixel dilate post-filter; this
    dilate filter could be useful when surface pixels will be uploaded to OpenGL/Direct3D bilinear-filtered textures.

    This function returns:
    - SVGT_BAD_HANDLE_ERROR if the specified surface handle is not valid
    - SVGT_ILLEGAL_ARGUMENT_ERROR if 'dstPixels32' pointer is NULL or if it's not properly aligned
    - SVGT_NO_ERROR if the operation was completed successfully
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtSurfaceCopy(SVGTHandle surface,
                                                           void* dstPixels32,
                                                           SVGTboolean redBlueSwap,
                                                           SVGTboolean dilateEdgesFix) SVGT_API_EXIT;

/*
    Associate a native "hardware" texture to a drawing surface, setting parameters for the copy&destroy.

    'nativeTexturePtr' must be a valid native "hardware" texture (e.g. GLuint texture "name" on OpenGL/OpenGL ES,
    ID3D11Resource on D3D11, on Metal the id<MTLTexture> pointer).

    'nativeTextureWidth' and 'nativeTextureHeight' must be greater than zero.

    ##########################################
    # NB: this function is Unity specific!!! #
    ##########################################
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtSurfaceTexturePtrSet(SVGTHandle surface,
                                                                    void* nativeTexturePtr,
                                                                    SVGTuint nativeTextureWidth,
                                                                    SVGTuint nativeTextureHeight,
                                                                    SVGTboolean nativeTextureIsBGRA,
                                                                    SVGTboolean dilateEdgesFix) SVGT_API_EXIT;

/*
    Get the native code callback to queue for Unity's renderer to invoke.

    ##########################################
    # NB: this function is Unity specific!!! #
    ##########################################
*/
SVGT_API_CALL void* SVGT_API_ENTRY svgtSurfaceTextureCopyAndDestroyFuncGet(void) SVGT_API_EXIT;

/*
    Get current destination viewport (i.e. a drawing surface rectangular area), where to map the source document viewport.

    The 'viewport' parameter must be an array of (at least) 4 float entries, it will be filled with:
    - viewport[0] = top/left x
    - viewport[1] = top/left y
    - viewport[2] = width
    - viewport[3] = height

    This function returns:
    - SVGT_BAD_HANDLE_ERROR if specified surface handle is not valid
    - SVGT_ILLEGAL_ARGUMENT_ERROR if 'viewport' pointer is NULL or if it's not properly aligned
    - SVGT_NO_ERROR if the operation was completed successfully
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtSurfaceViewportGet(SVGTHandle surface,
                                                                  SVGTfloat* viewport) SVGT_API_EXIT;

/*
    Set destination viewport (i.e. a drawing surface rectangular area), where to map the source document viewport.

    The 'viewport' parameter must be an array of (at least) 4 float entries, it must contain:
    - viewport[0] = top/left x
    - viewport[1] = top/left y
    - viewport[2] = width
    - viewport[3] = height

    The combined use of svgtDocViewportSet and svgtSurfaceViewportSet induces a transformation matrix, that will be used
    to draw the whole SVG document. The induced matrix grants that the document viewport is mapped onto the surface
    viewport (respecting the specified alignment): all SVG content will be drawn accordingly.

    This function returns:
    - SVGT_BAD_HANDLE_ERROR if specified surface handle is not valid
    - SVGT_ILLEGAL_ARGUMENT_ERROR if 'viewport' pointer is NULL or if it's not properly aligned
    - SVGT_ILLEGAL_ARGUMENT_ERROR if specified viewport width or height are less than or equal zero
    - SVGT_NO_ERROR if the operation was completed successfully

    NB: floating-point values of NaN are treated as 0, values of +Infinity and -Infinity are clamped to the largest and smallest available float values.
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtSurfaceViewportSet(SVGTHandle surface,
                                                                  SVGTfloat* viewport) SVGT_API_EXIT;

/*
    Clear the whole drawing surface with the given color.
    Each color component must be a number between 0 and 1. Values outside this range will be clamped.

    It returns SVGT_NO_ERROR if the operation was completed successfully, else an error code.

    NB: floating-point values of NaN are treated as 0, values of +Infinity and -Infinity are clamped to the largest and smallest available float values.
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtSurfaceClear(SVGTHandle surface,
                                                            SVGTfloat r,
                                                            SVGTfloat g,
                                                            SVGTfloat b,
                                                            SVGTfloat a) SVGT_API_EXIT;

/*
    Create and load an SVG document, specifying the whole xml string.

    Return SVGT_INVALID_HANDLE in case of errors, else a valid document handle.
*/
SVGT_API_CALL SVGTHandle SVGT_API_ENTRY svgtDocCreate(const char* xmlText) SVGT_API_EXIT;

/*
    Destroy a previously created SVG document.

    It returns SVGT_NO_ERROR if the operation was completed successfully, else an error code.
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtDocDestroy(SVGTHandle svgDoc) SVGT_API_EXIT;

/*
    SVG content itself optionally can provide information about the appropriate viewport region for
    the content via the 'width' and 'height' XML attributes on the outermost <svg> element.
    Use this function to get the suggested viewport width, in pixels.

    It returns a negative number (i.e. an invalid width) in the following cases:
    - the library has not previously been initialized through the svgtInit function
    - outermost element is not an <svg> element
    - outermost <svg> element doesn't have a 'width' attribute specified
    - outermost <svg> element has a negative 'width' attribute specified (e.g. width="-50")
    - outermost <svg> element has a 'width' attribute specified in relative measure units (i.e. em, ex, % percentage)
    In all such cases the last-error code will be set to SVGT_INVALID_SVG_ERROR.
*/
SVGT_API_CALL SVGTfloat SVGT_API_ENTRY svgtDocWidth(SVGTHandle svgDoc) SVGT_API_EXIT;

/*
    SVG content itself optionally can provide information about the appropriate viewport region for
    the content via the 'width' and 'height' XML attributes on the outermost <svg> element.
    Use this function to get the suggested viewport height, in pixels.

    It returns a negative number (i.e. an invalid height) in the following cases:
    - the library has not previously been initialized through the svgtInit function
    - outermost element is not an <svg> element
    - outermost <svg> element doesn't have a 'height' attribute specified
    - outermost <svg> element has a negative 'height' attribute specified (e.g. height="-30")
    - outermost <svg> element has a 'height' attribute specified in relative measure units (i.e. em, ex, % percentage)
    In all such cases the last-error code will be set to SVGT_INVALID_SVG_ERROR.
*/
SVGT_API_CALL SVGTfloat SVGT_API_ENTRY svgtDocHeight(SVGTHandle svgDoc) SVGT_API_EXIT;

/*
    Get the document (logical) viewport to map onto the destination (drawing surface) viewport.
    When an SVG document has been created through the svgtDocCreate function, the initial value
    of its viewport is equal to the 'viewBox' attribute present in the outermost <svg> element.
    If such element does not contain the 'viewBox' attribute, the initial value of document
    viewport is calculated as follows:

    - if both 'width' and 'height' attributes are not present, the document viewport starts as
    (0, 0, screenWidth, screenHeight), where the last two values are the ones provided to
    the svgtInit function
    - if both 'width' and 'height' attributes are present, the document viewport starts as
    (0, 0, max(width, 0), max(height, 0))
    
    The 'viewport' parameter must be an array of (at least) 4 float entries, it will be filled with:
    - viewport[0] = top/left x
    - viewport[1] = top/left y
    - viewport[2] = width
    - viewport[3] = height

    This function returns:
    - SVGT_BAD_HANDLE_ERROR if specified document handle is not valid
    - SVGT_ILLEGAL_ARGUMENT_ERROR if 'viewport' pointer is NULL or if it's not properly aligned
    - SVGT_NO_ERROR if the operation was completed successfully
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtDocViewportGet(SVGTHandle svgDoc,
                                                              SVGTfloat* viewport) SVGT_API_EXIT;

/*
    Set the document (logical) viewport to map onto the destination (drawing surface) viewport.

    The 'viewport' parameter must be an array of (at least) 4 float entries, it must contain:
    - viewport[0] = top/left x
    - viewport[1] = top/left y
    - viewport[2] = width
    - viewport[3] = height

    This function returns:
    - SVGT_BAD_HANDLE_ERROR if specified document handle is not valid
    - SVGT_ILLEGAL_ARGUMENT_ERROR if 'viewport' pointer is NULL or if it's not properly aligned
    - SVGT_ILLEGAL_ARGUMENT_ERROR if specified viewport width or height are less than or equal zero
    - SVGT_NO_ERROR if the operation was completed successfully

    NB: floating-point values of NaN are treated as 0, values of +Infinity and -Infinity are clamped to the largest and smallest available float values.
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtDocViewportSet(SVGTHandle svgDoc,
                                                              SVGTfloat* viewport) SVGT_API_EXIT;

/*
    Get the document alignment.
    The alignment parameter indicates whether to force uniform scaling and, if so, the alignment method to use in case
    the aspect ratio of the document viewport doesn't match the aspect ratio of the surface viewport.

    The 'values' parameter must be an array of (at least) 2 unsigned integers entries, it will be filled with:
    - values[0] = alignment (see SVGTAspectRatioAlign)
    - values[1] = meetOrSlice (see SVGTAspectRatioMeetOrSlice)

    This function returns:
    - SVGT_BAD_HANDLE_ERROR if specified document handle is not valid
    - SVGT_ILLEGAL_ARGUMENT_ERROR if 'values' pointer is NULL or if it's not properly aligned
    - SVGT_NO_ERROR if the operation was completed successfully
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtDocViewportAlignmentGet(SVGTHandle svgDoc,
                                                                       SVGTuint* values) SVGT_API_EXIT;

/*
    Set the document alignment.
    The alignment parameter indicates whether to force uniform scaling and, if so, the alignment method to use in case
    the aspect ratio of the document viewport doesn't match the aspect ratio of the surface viewport.

    The 'values' parameter must be an array of (at least) 2 unsigned integers entries, it must contain:
    - values[0] = alignment (see SVGTAspectRatioAlign)
    - values[1] = meetOrSlice (see SVGTAspectRatioMeetOrSlice)

    This function returns:
    - SVGT_BAD_HANDLE_ERROR if specified document handle is not valid
    - SVGT_ILLEGAL_ARGUMENT_ERROR if 'viewport' pointer is NULL or if it's not properly aligned
    - SVGT_ILLEGAL_ARGUMENT_ERROR if specified alignment is not a valid SVGTAspectRatioAlign value
    - SVGT_ILLEGAL_ARGUMENT_ERROR if specified meetOrSlice is not a valid SVGTAspectRatioMeetOrSlice value
    - SVGT_NO_ERROR if the operation was completed successfully
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtDocViewportAlignmentSet(SVGTHandle svgDoc,
                                                                       SVGTuint* values) SVGT_API_EXIT;

/*
    Draw an SVG document, over the specified drawing surface, with the given rendering quality.

    This function returns:
    - SVGT_BAD_HANDLE_ERROR if specified document handle is not valid
    - SVGT_BAD_HANDLE_ERROR if specified surface handle is not valid
    - SVGT_ILLEGAL_ARGUMENT_ERROR if specified rendering quality is not a valid value from the SVGTRenderingQuality enumeration
    - SVGT_NO_ERROR if the operation was completed successfully
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtDocDraw(SVGTHandle svgDoc,
                                                       SVGTHandle surface,
                                                       SVGTRenderingQuality renderingQuality) SVGT_API_EXIT;

/*
    Map a point, expressed in the document viewport system, into the surface viewport.
    The transformation will be performed according to the current document viewport (see svgtDocViewportGet) and the
    current surface viewport (see svgtSurfaceViewportGet).

    The 'dst' parameter must be an array of (at least) 2 float entries, it will be filled with:
    - dst[0] = transformed x
    - dst[1] = transformed y

    This function returns:
    - SVGT_BAD_HANDLE_ERROR if specified document handle is not valid
    - SVGT_BAD_HANDLE_ERROR if specified surface handle is not valid
    - SVGT_ILLEGAL_ARGUMENT_ERROR if 'dst' pointer is NULL or if it's not properly aligned
    - SVGT_NO_ERROR if the operation was completed successfully

    NB: floating-point values of NaN are treated as 0, values of +Infinity and -Infinity are clamped to the largest and smallest available float values.
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtPointMap(SVGTHandle svgDoc,
                                                        SVGTHandle surface,
                                                        SVGTfloat x,
                                                        SVGTfloat y,
                                                        SVGTfloat* dst) SVGT_API_EXIT;

/*
    Start a packing task: one or more SVG documents will be collected and packed into bins, for the generation of atlases.

    Every collected SVG document/element will be packed into rectangular bins, whose dimensions won't exceed the specified 'maxDimension', in pixels.
    If SVGT_TRUE, 'pow2Bins' will force bins to have power-of-two dimensions.
    Each rectangle will be separated from the others by the specified 'border', in pixels.
    The specified 'scale' factor will be applied to all collected SVG documents/elements, in order to realize resolution-independent atlases.

    This function returns:
    - SVGT_STILL_PACKING_ERROR if a current packing task is still open
    - SVGT_ILLEGAL_ARGUMENT_ERROR if specified 'maxDimension' is 0
    - SVGT_ILLEGAL_ARGUMENT_ERROR if 'pow2Bins' is SVGT_TRUE and the specified 'maxDimension' is not a power-of-two number
    - SVGT_ILLEGAL_ARGUMENT_ERROR if specified 'border' itself would exceed the specified 'maxDimension' (border must allow a packable region of at least one pixel)
    - SVGT_ILLEGAL_ARGUMENT_ERROR if specified 'scale' factor is less than or equal 0
    - SVGT_NO_ERROR if the operation was completed successfully

    NB: floating-point values of NaN are treated as 0, values of +Infinity and -Infinity are clamped to the largest and smallest available float values.
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtPackingBegin(SVGTuint maxDimension,
                                                            SVGTuint border,
                                                            SVGTboolean pow2Bins,
                                                            SVGTfloat scale) SVGT_API_EXIT;

/*
    Add an SVG document to the current packing task.

    If SVGT_TRUE, 'explodeGroups' tells the packer to not pack the whole SVG document, but instead to pack each first-level element separately.
    The additional 'scale' is used to adjust the document content to the other documents involved in the current packing process.

    The 'info' parameter will return some useful information, it must be an array of (at least) 2 entries and it will be filled with:
    - info[0] = number of collected elements
    - info[1] = the actual number of elements that could be packed (less than or equal to info[0], because elements whose dimensions
                exceed the 'maxDimension' value provided to the svgtPackingBegin function, will be discarded)
    
    This function returns:
    - SVGT_NOT_PACKING_ERROR if there isn't a currently open packing task
    - SVGT_BAD_HANDLE_ERROR if specified document handle is not valid
    - SVGT_ILLEGAL_ARGUMENT_ERROR if specified 'scale' factor is less than or equal 0
    - SVGT_ILLEGAL_ARGUMENT_ERROR if 'info' pointer is NULL or if it's not properly aligned
    - SVGT_NO_ERROR if the operation was completed successfully

    NB: floating-point values of NaN are treated as 0, values of +Infinity and -Infinity are clamped to the largest and smallest available float values.
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtPackingAdd(SVGTHandle svgDoc,
                                                          SVGTboolean explodeGroups,
                                                          SVGTfloat scale,
                                                          SVGTuint* info) SVGT_API_EXIT;

/*
    Close the current packing task and, if specified, perform the real packing algorithm.

    All collected SVG documents/elements (actually their bounding boxes) are packed into bins for later use (i.e. atlases generation).
    After calling this function, the application could use svgtPackingBinsCount, svgtPackingBinInfo and svgtPackingDraw in order to
    get information about the resulted packed elements and draw them.

    This function returns:
    - SVGT_NOT_PACKING_ERROR if there isn't a currently open packing task
    - SVGT_NO_ERROR if the operation was completed successfully
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtPackingEnd(SVGTboolean performPacking) SVGT_API_EXIT;

/*
    Return the number of generated bins from the last packing task.
    
    This function returns a negative number in case of errors (e.g. if the current packing task has not been previously closed by a call to svgtPackingEnd).
*/
SVGT_API_CALL SVGTint SVGT_API_ENTRY svgtPackingBinsCount(void) SVGT_API_EXIT;

/*
    Return information about the specified bin.

    The requested bin is selected by its index; the 'binInfo' parameter must be an array of (at least) 3 entries, it will be filled with:
    - binInfo[0] = bin width, in pixels
    - binInfo[1] = bin height, in pixels
    - binInfo[2] = number of packed elements inside the bin

    This function returns:
    - SVGT_STILL_PACKING_ERROR if a current packing task is still open
    - SVGT_ILLEGAL_ARGUMENT_ERROR if specified 'binIdx' is not valid (must be >= 0 and less than the value returned by svgtPackingBinsCount function)
    - SVGT_ILLEGAL_ARGUMENT_ERROR if 'binInfo' pointer is NULL or if it's not properly aligned
    - SVGT_NO_ERROR if the operation was completed successfully
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtPackingBinInfo(SVGTuint binIdx,
                                                              SVGTuint* binInfo) SVGT_API_EXIT;

/*
    Get access to packed elements, relative to a specified bin.

    The specified 'binIdx' must be >= 0 and less than the value returned by svgtPackingBinsCount function, else a NULL pointer will be returned.
    The returned pointer contains an array of packed rectangles, whose number is equal to the one gotten through the svgtPackingBinInfo function.
    The use case for which this function was created, it's to copy and cache the result of a packing process; then when needed (e.g. requested by
    the application), the rectangles can be drawn using the svgtPackingRectsDraw function.
*/
SVGT_API_CALL const SVGTPackedRect* SVGT_API_ENTRY svgtPackingBinRects(SVGTuint binIdx) SVGT_API_EXIT;

/*
    Draw a set of packed SVG documents/elements over the specified drawing surface.

    The drawing surface is cleared (or not) according to the current settings (see svgtClearColor and svgtClearPerform).
    After calling svgtPackingEnd, the application could use this function in order to draw packed elements before to start another
    packing task with svgtPackingBegin.

    This function returns:
    - SVGT_STILL_PACKING_ERROR if a current packing task is still open
    - SVGT_ILLEGAL_ARGUMENT_ERROR if specified 'binIdx' is not valid (must be >= 0 and less than the value returned by svgtPackingBinsCount function)
    - SVGT_ILLEGAL_ARGUMENT_ERROR if specified 'startRectIdx', along with 'rectsCount', identifies an invalid range of rectangles; defined:

        maxCount = binInfo[2] (see svgtPackingBinInfo)
        endRectIdx = 'startRectIdx' + 'rectsCount' - 1

    it must be ensured that 'startRectIdx' < maxCount and 'endRectIdx' < maxCount, else SVGT_ILLEGAL_ARGUMENT_ERROR is returned.

    - SVGT_BAD_HANDLE_ERROR if specified surface handle is not valid
    - SVGT_NO_ERROR if the operation was completed successfully
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtPackingDraw(SVGTuint binIdx,
                                                           SVGTuint startRectIdx,
                                                           SVGTuint rectsCount,
                                                           SVGTHandle surface,
                                                           SVGTRenderingQuality renderingQuality) SVGT_API_EXIT;

/*
    Draw a set of packed SVG documents/elements over the specified drawing surface.

    The drawing surface is cleared (or not) according to the current settings (see svgtClearColor and svgtClearPerform).
    The specified rectangles MUST NOT point to the memory returned by svgtPackingBinRects.

    This function returns:
    - SVGT_ILLEGAL_ARGUMENT_ERROR if 'rects' pointer is NULL or if it's not properly aligned
    - SVGT_BAD_HANDLE_ERROR if specified surface handle is not valid
    - SVGT_NO_ERROR if the operation was completed successfully
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtPackingRectsDraw(const SVGTPackedRect* rects,
                                                                SVGTuint rectsCount,
                                                                SVGTHandle surface,
                                                                SVGTRenderingQuality renderingQuality) SVGT_API_EXIT;

/*
    Get renderer and version information.
    This function does not set the last-error code (see svgtGetLastError).

    If specified 'name' is not a valid value from the SVGTStringID enumeration, an empty string is returned.
*/
SVGT_API_CALL const char* SVGT_API_ENTRY svgtGetString(SVGTStringID name) SVGT_API_EXIT;

#ifdef __cplusplus 
} /* extern "C" */
#endif

#endif /* SVGT_H */
