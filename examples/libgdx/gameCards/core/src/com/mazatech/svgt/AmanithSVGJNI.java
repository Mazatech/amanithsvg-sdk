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

package com.mazatech.svgt;

public class AmanithSVGJNI {

    public static int SVGT_INVALID_HANDLE                               = 0;

    // SVGTboolean
    public static final int SVGT_FALSE                                  = 0;
    public static final int SVGT_TRUE                                   = 1;

    // -----------------------------------------------------------------------
    // SVGTAspectRatioAlign
    //
    // Alignment indicates whether to force uniform scaling and, if so, the
    // alignment method to use in case the aspect ratio of the source viewport
    // doesn't match the aspect ratio of the destination (drawing surface)
    // viewport.
    // -----------------------------------------------------------------------
    /*
        Do not force uniform scaling.
        Scale the graphic content of the given element non-uniformly if necessary such that
        the element's bounding box exactly matches the viewport rectangle.
        NB: in this case, the <meetOrSlice> value is ignored.
    */
    public static final int SVGT_ASPECT_RATIO_ALIGN_NONE                = 0;
    /*
        Force uniform scaling.
        Align the <min-x> of the source viewport with the smallest x value of the destination (drawing surface) viewport.
        Align the <min-y> of the source viewport with the smallest y value of the destination (drawing surface) viewport.
    */
    public static final int SVGT_ASPECT_RATIO_ALIGN_XMINYMIN            = 1;
    /*
        Force uniform scaling.
        Align the <mid-x> of the source viewport with the midpoint x value of the destination (drawing surface) viewport.
        Align the <min-y> of the source viewport with the smallest y value of the destination (drawing surface) viewport.
    */
    public static final int SVGT_ASPECT_RATIO_ALIGN_XMIDYMIN            = 2;
    /*
        Force uniform scaling.
        Align the <max-x> of the source viewport with the maximum x value of the destination (drawing surface) viewport.
        Align the <min-y> of the source viewport with the smallest y value of the destination (drawing surface) viewport.
    */
    public static final int SVGT_ASPECT_RATIO_ALIGN_XMAXYMIN            = 3;
    /*
        Force uniform scaling.
        Align the <min-x> of the source viewport with the smallest x value of the destination (drawing surface) viewport.
        Align the <mid-y> of the source viewport with the midpoint y value of the destination (drawing surface) viewport.
    */
    public static final int SVGT_ASPECT_RATIO_ALIGN_XMINYMID            = 4;
    /*
        Force uniform scaling.
        Align the <mid-x> of the source viewport with the midpoint x value of the destination (drawing surface) viewport.
        Align the <mid-y> of the source viewport with the midpoint y value of the destination (drawing surface) viewport.
    */
    public static final int SVGT_ASPECT_RATIO_ALIGN_XMIDYMID            = 5;
    /*
        Force uniform scaling.
        Align the <max-x> of the source viewport with the maximum x value of the destination (drawing surface) viewport.
        Align the <mid-y> of the source viewport with the midpoint y value of the destination (drawing surface) viewport.
    */
    public static final int SVGT_ASPECT_RATIO_ALIGN_XMAXYMID            = 6;
    /*
        Force uniform scaling.
        Align the <min-x> of the source viewport with the smallest x value of the destination (drawing surface) viewport.
        Align the <max-y> of the source viewport with the maximum y value of the destination (drawing surface) viewport.
    */
    public static final int SVGT_ASPECT_RATIO_ALIGN_XMINYMAX            = 7;
    /*
        Force uniform scaling.
        Align the <mid-x> of the source viewport with the midpoint x value of the destination (drawing surface) viewport.
        Align the <max-y> of the source viewport with the maximum y value of the destination (drawing surface) viewport.
    */
    public static final int SVGT_ASPECT_RATIO_ALIGN_XMIDYMAX            = 8;
    /*
        Force uniform scaling.
        Align the <max-x> of the source viewport with the maximum x value of the destination (drawing surface) viewport.
        Align the <max-y> of the source viewport with the maximum y value of the destination (drawing surface) viewport.
    */
    public static final int SVGT_ASPECT_RATIO_ALIGN_XMAXYMAX            = 9;

    // -----------------------------------------------------------------------
    // SVGTAspectRatioMeetOrSlice
    // -----------------------------------------------------------------------
    /*
        Scale the graphic such that:
        - aspect ratio is preserved
        - the entire viewBox is visible within the viewport
        - the viewBox is scaled up as much as possible, while still meeting the other criteria

        In this case, if the aspect ratio of the graphic does not match the viewport, some of the viewport will
        extend beyond the bounds of the viewBox (i.e., the area into which the viewBox will draw will be smaller
        than the viewport).
    */
    public static final int SVGT_ASPECT_RATIO_MEET                      = 0;
    /*
        Scale the graphic such that:
        - aspect ratio is preserved
        - the entire viewport is covered by the viewBox
        - the viewBox is scaled down as much as possible, while still meeting the other criteria

        In this case, if the aspect ratio of the viewBox does not match the viewport, some of the viewBox will
        extend beyond the bounds of the viewport (i.e., the area into which the viewBox will draw is larger
        than the viewport).
    */
    public static final int SVGT_ASPECT_RATIO_SLICE                     = 1;

    // -----------------------------------------------------------------------
    // SVGTErrorCode
    // -----------------------------------------------------------------------
    // no error (i.e. the operation was completed successfully)
    public static final int SVGT_NO_ERROR                               =  0;
    // it indicates that the library has not previously been initialized through the svgtInit() function
    public static final int SVGT_NOT_INITIALIZED_ERROR                  =  1;
    // returned when one or more invalid document/surface handles are passed to AmanithSVG functions
    public static final int SVGT_BAD_HANDLE_ERROR                       =  2;
    // returned when one or more illegal arguments are passed to AmanithSVG functions
    public static final int SVGT_ILLEGAL_ARGUMENT_ERROR                 =  3;
    // all AmanithSVG functions may signal an "out of memory" error
    public static final int SVGT_OUT_OF_MEMORY_ERROR                    =  4;
    // returned when an invalid or malformed XML is passed to the svgtDocCreate
    public static final int SVGT_PARSER_ERROR                           =  5;
    // returned when a document fragment is technically in error (e.g. if an element has an attribute or property value which is not permissible according to SVG specifications or if the outermost element is not an <svg> element)
    public static final int SVGT_INVALID_SVG_ERROR                      =  6;
    // returned when a current packing task is still open, and so the operation (e.g. svgtPackingBinInfo) is not allowed
    public static final int SVGT_STILL_PACKING_ERROR                    =  7;
    // returned when there isn't a currently open packing task, and so the operation (e.g. svgtPackingAdd) is not allowed
    public static final int SVGT_NOT_PACKING_ERROR                      =  8;
    // the specified resource, via svgtResourceSet, is not valid or it does not match the given resource type
    public static final int SVGT_INVALID_RESOURCE_ERROR                 =  9;
    public static final int SVGT_UNKNOWN_ERROR                          = 10;

    // -----------------------------------------------------------------------
    // SVGTRenderingQuality
    // -----------------------------------------------------------------------
    /* disables antialiasing */
    public static final int SVGT_RENDERING_QUALITY_NONANTIALIASED       = 0;
    /* causes rendering to be done at the highest available speed */
    public static final int SVGT_RENDERING_QUALITY_FASTER               = 1;
    /* causes rendering to be done with the highest available quality */
    public static final int SVGT_RENDERING_QUALITY_BETTER               = 2;

    // -----------------------------------------------------------------------
    // SVGTConfig
    // -----------------------------------------------------------------------
    // Used by AmanithSVG geometric kernel to approximate curves with straight line
    // segments (flattening). Valid range is [1; 100], where 100 represents the best quality.
    public static final int SVGT_CONFIG_CURVES_QUALITY                  = 1;
    // The maximum number of different threads that can "work" (e.g. create surfaces, draw documents, etc) concurrently.
    // (READ-ONLY)
    public static final int SVGT_CONFIG_MAX_CURRENT_THREADS             = 2;
    // The maximum dimension allowed for drawing surfaces, in pixels. This is the maximum valid value that can be specified as
    // 'width' and 'height' for the svgtSurfaceCreate and svgtSurfaceResize functions.
    // (READ-ONLY)
    public static final int SVGT_CONFIG_MAX_SURFACE_DIMENSION           = 3;

    // -----------------------------------------------------------------------
    // SVGTStringID
    // -----------------------------------------------------------------------
    public static final int SVGT_VENDOR                                 = 1;
    public static final int SVGT_VERSION                                = 2;
    public static final int SVGT_COMPILE_CONFIG_INFO                    = 3;
    public static final int SVGT_OPENVG_VERSION                         = 4;
    public static final int SVGT_OPENVG_RENDERER                        = 5;
    public static final int SVGT_OPENVG_EXTENSIONS                      = 6;
    public static final int SVGT_OPENVG_COMPILE_CONFIG_INFO             = 7;

    // -----------------------------------------------------------------------
    // SVGTLogLevel
    // -----------------------------------------------------------------------
    /*
        Errors that prevent correct rendering from being completed, such that malformed
        SVG (xml) content, negative dimensions for geometric shapes, and all those cases
        where a document fragment is technically in error according to SVG specifications
    */
    public static final int SVGT_LOG_LEVEL_ERROR                        = (1 << 0);
    /*
        Warning conditions that need to be taken care of, such that an outermost <svg>
        element without a 'width' or 'height' attribute, a missing font resource, and so on
    */
    public static final int SVGT_LOG_LEVEL_WARNING                      = (1 << 1);
    // Informational message
    public static final int SVGT_LOG_LEVEL_INFO                         = (1 << 2);

    // -----------------------------------------------------------------------
    // SVGTResourceType
    // -----------------------------------------------------------------------
    // font resource (vector fonts like TTF and OTF are supported, bitmap fonts are not supported)
    public static final int SVGT_RESOURCE_TYPE_FONT                     = 1;
    // bitmap/image resource (only PNG and JPEG are supported; 16 bits PNG are not supported)
    public static final int SVGT_RESOURCE_TYPE_IMAGE                    = 2;

    // -----------------------------------------------------------------------
    // SVGTResourceHint
    // -----------------------------------------------------------------------
    // The given font resource must be selected when the font-family attribute matches the 'serif' generic family.
    public static final int SVGT_RESOURCE_HINT_DEFAULT_SERIF_FONT         = (1 << 0);
    // The given font resource must be selected when the font-family attribute matches the 'sans-serif' generic family.
    public static final int SVGT_RESOURCE_HINT_DEFAULT_SANS_SERIF_FONT    = (1 << 1);
    // The given font resource must be selected when the font-family attribute matches the 'monospace' generic family.
    public static final int SVGT_RESOURCE_HINT_DEFAULT_MONOSPACE_FONT     = (1 << 2);
    // The given font resource must be selected when the font-family attribute matches the 'cursive' generic family.
    public static final int SVGT_RESOURCE_HINT_DEFAULT_CURSIVE_FONT       = (1 << 3);
    // The given font resource must be selected when the font-family attribute matches the 'fantasy' generic family.
    public static final int SVGT_RESOURCE_HINT_DEFAULT_FANTASY_FONT       = (1 << 4);
    // The given font resource must be selected when the font-family attribute matches the 'system-ui' generic family.
    public static final int SVGT_RESOURCE_HINT_DEFAULT_SYSTEM_UI_FONT     = (1 << 5);
    // The given font resource must be selected when the font-family attribute matches the 'ui-serif' generic family.
    public static final int SVGT_RESOURCE_HINT_DEFAULT_UI_SERIF_FONT      = (1 << 6);
    // The given font resource must be selected when the font-family attribute matches the 'ui-sans-serif' generic family.
    public static final int SVGT_RESOURCE_HINT_DEFAULT_UI_SANS_SERIF_FONT = (1 << 7);
    // The given font resource must be selected when the font-family attribute matches the 'ui-monospace' generic family.
    public static final int SVGT_RESOURCE_HINT_DEFAULT_UI_MONOSPACE_FONT  = (1 << 8);
    // The given font resource must be selected when the font-family attribute matches the 'ui-rounded' generic family.
    public static final int SVGT_RESOURCE_HINT_DEFAULT_UI_ROUNDED_FONT    = (1 << 9);
    // The given font resource must be selected when the font-family attribute matches the 'emoji' generic family.
    public static final int SVGT_RESOURCE_HINT_DEFAULT_EMOJI_FONT         = (1 << 10);
    // The given font resource must be selected when the font-family attribute matches the 'math' generic family.
    public static final int SVGT_RESOURCE_HINT_DEFAULT_MATH_FONT          = (1 << 11);
    // The given font resource must be selected when the font-family attribute matches the 'fangsong' generic family.
    public static final int SVGT_RESOURCE_HINT_DEFAULT_FANGSONG_FONT      = (1 << 12);

    // Retrieve the error code generated by the last called API function.
    public final static native int svgtGetLastError();
    // Initialize the library.
    public final static native int svgtInit(int screenWidth, int screenHeight, float dpi);
    // Destroy the library, freeing all allocated resources.
    public final static native void svgtDone();
    // Configure parameters and thresholds for the AmanithSVG library.
    public final static native int svgtConfigSet(int config, float value);
    // Get the current value relative to the specified configuration parameter.
    public final static native float svgtConfigGet(int config);
    // Set the system / user-agent language (list of languages separated by semicolon).
    public final static native int svgtLanguageSet(final String languages);
    // Instruct the library about an external resource; NB: the given buffer must be direct!
    public final static native int svgtResourceSet(final String id, final java.nio.ByteBuffer buffer, int type, int hints);
    // Get the maximum number of different threads that can "work" (e.g. create surfaces, create documents and draw them) concurrently.
    public final static native int svgtMaxCurrentThreads();
    // Get the maximum dimension allowed for drawing surfaces.
    public final static native int svgtSurfaceMaxDimension();
    // Set the log buffer for the current thread.
    public final static native int svgtLogBufferSet(java.nio.ByteBuffer logBuffer, int logLevel);
    // Append the given message to the log buffer.
    public final static native int svgtLogPrint(final String message, int level);
    // Get information about the current thread log buffer.
    public final static native int svgtLogBufferInfo(int[] info, int offset);
    // Create a new drawing surface, specifying its dimensions in pixels.
    public final static native int svgtSurfaceCreate(int width, int height);
    // Destroy a previously created drawing surface.
    public final static native int svgtSurfaceDestroy(int surface);
    // Resize a drawing surface, specifying new dimensions in pixels.
    public final static native int svgtSurfaceResize(int surface, int newWidth, int newHeight);
    // Get width dimension (in pixels), of the specified drawing surface.
    public final static native int svgtSurfaceWidth(int surface);
    // Get height dimension (in pixels), of the specified drawing surface.
    public final static native int svgtSurfaceHeight(int surface);
    // Get access to the drawing surface pixels. NB: the returned buffer is a direct buffer referring to the surface pixels memory.
    public final static native java.nio.ByteBuffer svgtSurfacePixels(int surface);
    // Copy drawing surface content into the specified pixels array.
    public final static native int svgtSurfaceCopyA(int surface, int[] dstPixels32, int offset, int redBlueSwap, int dilateEdgesFix);
    // Copy drawing surface content into the specified pixels buffer.
    public final static native int svgtSurfaceCopyB(int surface, java.nio.IntBuffer dstPixels32, int redBlueSwap, int dilateEdgesFix);
    // Get current destination viewport (i.e. a drawing surface rectangular area), where to map the source document viewport.
    public final static native int svgtSurfaceViewportGet(int surface, float[] viewport, int offset);
    // Set destination viewport (i.e. a drawing surface rectangular area), where to map the source document viewport.
    public final static native int svgtSurfaceViewportSet(int surface, final float[] viewport, int offset);
    // Clear the whole drawing surface with the given color.
    public final static native int svgtSurfaceClear(int surface, float r, float g, float b, float a);
    // Create and load an SVG document, specifying the whole xml string.
    public final static native int svgtDocCreate(final String xmlText);
    // Destroy a previously created SVG document.
    public final static native int svgtDocDestroy(int svgDoc);
    // Get the suggested viewport width ('width' XML attribute on the outermost <svg> element), in pixels.
    public final static native float svgtDocWidth(int svgDoc);
    // Get the suggested viewport height ('height' XML attribute on the outermost <svg> element), in pixels.
    public final static native float svgtDocHeight(int svgDoc);
    // Get the document (logical) viewport to map onto the destination (drawing surface) viewport.
    public final static native int svgtDocViewportGet(int svgDoc, float[] viewport, int offset);
    // Set the document (logical) viewport to map onto the destination (drawing surface) viewport.
    public final static native int svgtDocViewportSet(int svgDoc, final float[] viewport, int offset);
    // Get the document alignment: it indicates whether to force uniform scaling and, if so, the alignment method to use.
    public final static native int svgtDocViewportAlignmentGet(int svgDoc, int[] values, int offset);
    // Set the document alignment: it indicates whether to force uniform scaling and, if so, the alignment method to use.
    public final static native int svgtDocViewportAlignmentSet(int svgDoc, final int[] values, int offset);
    // Draw an SVG document, on the specified drawing surface, with the given rendering quality.
    public final static native int svgtDocDraw(int svgDoc, int surface, int renderingQuality);
    // Map a point, expressed in the document viewport system, into the surface viewport.
    public final static native int svgtPointMap(int svgDoc, int surface, float x, float y, float[] dst, int offset);
    // Start a packing task: one or more SVG documents will be collected and packed into bins, for the generation of atlases.
    public final static native int svgtPackingBegin(int maxDimension, int border, int pow2Bins, float scale);
    // Add an SVG document to the current packing task.
    public final static native int svgtPackingAdd(int svgDoc, int explodeGroups, float scale, int[] info, int offset);
    // Close the current packing task and, if specified, perform the real packing algorithm.
    public final static native int svgtPackingEnd(int performPacking);
    // Return the number of generated bins from the last packing task.
    public final static native int svgtPackingBinsCount();
    // Return information about the specified bin.
    public final static native int svgtPackingBinInfo(int binIdx, int[] binInfo, int offset);
    // Get access to packed rectangles, relative to a specified bin.
    public final static native java.nio.ByteBuffer svgtPackingBinRects(int binIdx);
    // Draw a set of packed SVG documents/elements over the specified drawing surface.
    public final static native int svgtPackingDraw(int binIdx, int startRectIdx, int rectsCount, int surface, int renderingQuality);
    public final static native int svgtPackingRectsDraw(java.nio.ByteBuffer rects, int surface, int renderingQuality);
    // Get renderer and version information.
    public final static native String svgtGetString(int name);

    //------------------------------------------------------------------------------------------
    //                        Misc utilities for JAVA/JNI interworking
    //------------------------------------------------------------------------------------------
    public final static native int svgtPackedRectSize();
    public final static native String svgtPackedRectName(long namePtr);
}
