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
    SVGT_NO_ERROR                                 = 0,
    /* it indicates that the library has not previously been initialized through the svgtInit() function */
    SVGT_NOT_INITIALIZED_ERROR                    = 1,
    SVGT_BAD_HANDLE_ERROR                         = 2,
    SVGT_ILLEGAL_ARGUMENT_ERROR                   = 3,
    SVGT_OUT_OF_MEMORY_ERROR                      = 4,
    SVGT_PARSER_ERROR                             = 5,
    /* returned when the library detects that outermost element is not an <svg> element or there is a circular dependency (usually generated by <use> elements) */
    SVGT_INVALID_SVG_ERROR                        = 6,
    SVGT_STILL_PACKING_ERROR                      = 7,
    SVGT_NOT_PACKING_ERROR                        = 8,
    SVGT_UNKNOWN_ERROR                            = 9,

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

typedef enum {
    SVGT_VENDOR                                   = 1,
    SVGT_VERSION                                  = 2,

    SVGT_STRING_ID_FORCE_SIZE                     = SVGT_MAX_ENUM
} SVGTStringID;

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

// debug/warning/info function prototype
typedef void (*SVGTLogFunction)(const char*);

/*
    Initialize the library.

    It returns SVGT_NO_ERROR if the operation was completed successfully, else an error code.
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtInit(SVGTuint screenWidth,
                                                    SVGTuint screenHeight,
                                                    SVGTfloat dpi) SVGT_API_EXIT;

/*
    Destroy the library, freeing all allocated resources.
*/
SVGT_API_CALL void SVGT_API_ENTRY svgtDone(void) SVGT_API_EXIT;

/*
    Get the maximum dimension allowed for drawing surfaces.

    This is the maximum valid value that can be specified as 'width' and 'height' for the svgtSurfaceCreate and svgtSurfaceResize functions.
    Bigger values are silently clamped to it.
*/
SVGT_API_CALL SVGTuint svgtSurfaceMaxDimension(void) SVGT_API_EXIT;

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
    If the 'dilateEdgesFix' flag is set to SVGT_TRUE, the copy process will also perform a 1-pixel delate post-filter; this
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

    'nativeTexturePtr' must be a valid native "hardware" texture (e.g. GLuint texture "name" on OpenGL/OpenGL ES, IDirect3DBaseTexture9 on D3D9,
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

    It returns -1 (i.e. an invalid width) in the following cases:
    - the library has not previously been initialized through the svgtInit function
    - outermost element is not an <svg> element
    - outermost <svg> element doesn't have a 'width' attribute specified
    - outermost <svg> element has a 'width' attribute specified in relative measure units (i.e. em, ex, % percentage)
*/
SVGT_API_CALL SVGTfloat SVGT_API_ENTRY svgtDocWidth(SVGTHandle svgDoc) SVGT_API_EXIT;

/*
    SVG content itself optionally can provide information about the appropriate viewport region for
    the content via the 'width' and 'height' XML attributes on the outermost <svg> element.
    Use this function to get the suggested viewport height, in pixels.

    It returns -1 (i.e. an invalid height) in the following cases:
    - the library has not previously been initialized through the svgtInit function
    - outermost element is not an <svg> element
    - outermost <svg> element doesn't have a 'height' attribute specified
    - outermost <svg> element has a 'height' attribute specified in relative measure units (i.e. em, ex, % percentage)
*/
SVGT_API_CALL SVGTfloat SVGT_API_ENTRY svgtDocHeight(SVGTHandle svgDoc) SVGT_API_EXIT;

/*
    Get the document (logical) viewport to map onto the destination (drawing surface) viewport.
    When an SVG document has been created through the svgtDocCreate function, the initial value
    of its viewport is equal to the 'viewBox' attribute present in the outermost <svg> element.
    If such element does not contain the viewBox attribute, SVGT_NO_ERROR is returned and viewport
    array will be filled with zeros.

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
    If the specified SVG document is SVGT_INVALID_HANDLE, the drawing surface is cleared (or not) according to the current
    settings (see svgtClearColor and svgtClearPerform), and nothing else is drawn.

    It returns SVGT_NO_ERROR if the operation was completed successfully, else an error code.
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtDocDraw(SVGTHandle svgDoc,
                                                       SVGTHandle surface,
                                                       SVGTRenderingQuality renderingQuality) SVGT_API_EXIT;

/*
    Set the clear color (i.e. the color used to clear the whole drawing surface).
    Each color component must be a number between 0 and 1. Values outside this range
    will be clamped.

    It returns SVGT_NO_ERROR if the operation was completed successfully, else an error code.
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtClearColor(SVGTfloat r,
                                                          SVGTfloat g,
                                                          SVGTfloat b,
                                                          SVGTfloat a) SVGT_API_EXIT;

/*
    Specify if the whole drawing surface must be cleared by the svgtDocDraw function, before to draw the SVG document.

    It returns SVGT_NO_ERROR if the operation was completed successfully, else an error code.
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtClearPerform(SVGTboolean doClear) SVGT_API_EXIT;

/*
    Map a point, expressed in the document viewport system, into the surface viewport.
    The transformation will be performed according to the current document viewport (see svgtDocViewportGet) and the
    current surface viewport (see svgtSurfaceViewportGet).

    The 'dst' parameter must be an array of (at least) 2 float entries, it will be filled with:
    - dst[0] = transformed x
    - dst[1] = transformed y

    This function returns:
    - SVGT_BAD_HANDLE_ERROR if specified document (or surface) handle is not valid
    - SVGT_ILLEGAL_ARGUMENT_ERROR if 'dst' pointer is NULL or if it's not properly aligned
    - SVGT_NO_ERROR if the operation was completed successfully

    NB: floating-point values of NaN are treated as 0, values of +Infinity and -Infinity are clamped to the largest and smallest available float values.
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtPointMap(SVGTHandle svgDoc,
                                                        SVGTHandle surface,
                                                        SVGTfloat x,
                                                        SVGTfloat y,
                                                        SVGTfloat* dst) SVGT_API_EXIT;

/*!
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

/*!
    Add an SVG document to the current packing task.

    If SVGT_TRUE, 'explodeGroups' tells the packer to not pack the whole SVG document, but instead to pack each first-level element separately.
    The additional 'scale' is used to adjust the document content to the other documents involved in the current packing process.

    The 'info' parameter will return some useful information, it must be an array of (at least) 2 entries and it will be filled with:
    - info[0] = number of collected bounding boxes
    - info[1] = the actual number of packed bounding boxes (boxes whose dimensions exceed the 'maxDimension' value specified to the svgtPackingBegin function, will be discarded)
    
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

/*!
    Close the current packing task and, if specified, perform the real packing algorithm.

    All collected SVG documents/elements (actually their bounding boxes) are packed into bins for later use (i.e. atlases generation).
    After calling this function, the application could use svgtPackingBinsCount, svgtPackingBinInfo and svgtPackingDraw in order to
    get information about the resulted packed elements and draw them.

    This function returns:
    - SVGT_NOT_PACKING_ERROR if there isn't a currently open packing task
    - SVGT_NO_ERROR if the operation was completed successfully
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtPackingEnd(SVGTboolean performPacking) SVGT_API_EXIT;

/*!
    Return the number of generated bins from the last packing task.
    
    This function returns a negative number in case of errors (e.g. if the current packing task has not been previously closed by a call to svgtPackingEnd).
*/
SVGT_API_CALL SVGTint SVGT_API_ENTRY svgtPackingBinsCount(void) SVGT_API_EXIT;

/*!
    Return information about the specified bin.

    The requested bin is selected by its index; the 'binInfo' parameter must be an array of (at least) 3 entries, it will be filled with:
    - binInfo[0] = bin width, in pixels
    - binInfo[1] = bin height, in pixels
    - binInfo[2] = number of packed rectangles inside the bin

    This function returns:
    - SVGT_STILL_PACKING_ERROR if a current packing task is still open
    - SVGT_ILLEGAL_ARGUMENT_ERROR if specified 'binIdx' is not valid (must be >= 0 and less than the value returned by svgtPackingBinsCount function)
    - SVGT_ILLEGAL_ARGUMENT_ERROR if 'binInfo' pointer is NULL or if it's not properly aligned
    - SVGT_NO_ERROR if the operation was completed successfully
*/
SVGT_API_CALL SVGTErrorCode SVGT_API_ENTRY svgtPackingBinInfo(SVGTuint binIdx,
                                                              SVGTuint* binInfo) SVGT_API_EXIT;

/*!
    Get access to packed rectangles, relative to a specified bin.

    The specified 'binIdx' must be >= 0 and less than the value returned by svgtPackingBinsCount function, else a NULL pointer will be returned.
    The returned pointer contains an array of packed rectangles, whose number is equal to the one gotten through the svgtPackingBinInfo function.
    The use case for which this function was created, it's to copy and cache the result of a packing process; then when needed (e.g. requested by
    the application), the rectangles can be drawn using the svgtPackingRectsDraw function.
*/
SVGT_API_CALL const SVGTPackedRect* SVGT_API_ENTRY svgtPackingBinRects(SVGTuint binIdx) SVGT_API_EXIT;

/*!
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

/*!
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

/*!
    Get renderer and version information.
*/
SVGT_API_CALL const char* SVGT_API_ENTRY svgtGetString(SVGTStringID name) SVGT_API_EXIT;

#ifdef __cplusplus 
} /* extern "C" */
#endif

#endif /* SVGT_H */
