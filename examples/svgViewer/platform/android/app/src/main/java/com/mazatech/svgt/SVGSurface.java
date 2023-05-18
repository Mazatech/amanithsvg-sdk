/****************************************************************************
** Copyright (c) 2013-2023 Mazatech S.r.l.
** All rights reserved.
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

/*
    Drawing surface.

    A drawing surface is just a rectangular area made of pixels, where each pixel is represented internally by a 32bit unsigned integer.
    A pixel is made of four 8-bit components: red, green, blue, alpha.
 
    Coordinate system is the same of SVG specifications: top/left pixel has coordinate (0, 0), with the positive x-axis pointing towards
    the right and the positive y-axis pointing down.
*/
public class SVGSurface {

    // Constructor.
    SVGSurface(int handle) {

        SVGTError err;
        float[] viewport = new float[4];

        // wrap the native handle
        _surface = new SVGTHandle(handle);

        // get surface viewport
        if ((err = AmanithSVG.svgtSurfaceViewportGet(_surface, viewport)) == SVGTError.None) {
            _viewport = new SVGViewport(viewport[0], viewport[1], viewport[2], viewport[3]);
        }
        else {
            _viewport = null;
            throw new IllegalStateException("Error getting surface viewport; error is " + err);
        }
    }

    public void dispose() {

        // dispose unmanaged resources
        if (AmanithSVG.svgtSurfaceDestroy(_surface) != SVGTError.None) {
            // if we get an error here, it is highly likely that the code is being executed by
            // a thread other than the one in which the surface was created (i.e. in a thread
            // different than the rendering thread)
        }
        _surface = null;
    }

    // return the power of two value greater (or equal) to a specified value
    private int pow2Get(int value) {

        int result;

        if (value >= 0x40000000) {
            result = 0x40000000;
        }
        else {
            result = 1;
            while (result < value) {
                result <<= 1;
            }
        }

        return result;
    }

    /*
        Resize the surface, specifying new dimensions in pixels; it returns true if the operation was completed successfully, else false.

        After resizing, the surface viewport will be reset to the whole surface, and the relative transformation will be reset to
        identity (pivot = [0; 0], angle = 0, post-translation = [0; 0]).
    */
    public SVGTError resize(int newWidth,
                            int newHeight) {

        SVGTError err = AmanithSVG.svgtSurfaceResize(_surface, newWidth, newHeight);

        if (err == SVGTError.None) {
            
            // svgtSurfaceResize will reset the surface viewport, so we must perform the same operation here
            float[] viewport = new float[4];

            // get surface viewport
            if ((err = AmanithSVG.svgtSurfaceViewportGet(_surface, viewport)) == SVGTError.None) {
                _viewport = new SVGViewport(viewport[0], viewport[1], viewport[2], viewport[3]);
            }
            else {
                _viewport = null;
                throw new IllegalStateException("Error getting surface viewport; error is " + err);
            }
        }

        return err;
    }

    /*
        Draw an SVG document, on this drawing surface.

        First the drawing surface is cleared if a valid (i.e. not null) clear color is provided.
        Then the specified document, if valid, is drawn.
    */
    public SVGTError draw(final SVGDocument document,
                          final SVGColor clearColor,
                          SVGTRenderingQuality renderingQuality) {

        SVGTError err = SVGTError.None;

        if (clearColor != null) {
            // clear the surface
            err = AmanithSVG.svgtSurfaceClear(_surface, clearColor.getRed(), clearColor.getGreen(), clearColor.getBlue(), clearColor.getAlpha());
        }

        if ((err == SVGTError.None) && (document != null)) {
            // update document viewport (AmanithSVG backend)
            if ((err = document.updateViewport()) == SVGTError.None) {
                // update surface viewport (AmanithSVG backend)
                if ((err = updateViewport()) == SVGTError.None) {
                    // draw the document
                    err = AmanithSVG.svgtDocDraw(document.getHandle(), _surface, renderingQuality);
                }
            }
        }

        return err;
    }

    public SVGTError draw(final java.nio.ByteBuffer rects,
                          final SVGColor clearColor,
                          SVGTRenderingQuality renderingQuality) {

        SVGTError err = SVGTError.None;

        if (clearColor != null) {
            // clear the surface
            err = AmanithSVG.svgtSurfaceClear(_surface, clearColor.getRed(), clearColor.getGreen(), clearColor.getBlue(), clearColor.getAlpha());
        }

        if ((err == SVGTError.None) && (rects != null)) {
            // draw rectangles/elements
            err = AmanithSVG.svgtPackingRectsDraw(rects, _surface, renderingQuality);
        }

        return err;
    }

    public SVGTError copy(java.nio.IntBuffer dstPixels32,
                          boolean redBlueSwap,
                          boolean dilateEdgesFix) {

        if (dstPixels32 == null) {
            throw new IllegalArgumentException("dstPixels32 == null");
        }

        return AmanithSVG.svgtSurfaceCopy(_surface, dstPixels32, redBlueSwap, dilateEdgesFix);
    }

    public SVGTError copy(int[] dstPixels32,
                          boolean redBlueSwap,
                          boolean dilateEdgesFix) {

        if (dstPixels32 == null) {
            throw new IllegalArgumentException("dstPixels32 == null");
        }

        return AmanithSVG.svgtSurfaceCopy(_surface, dstPixels32, redBlueSwap, dilateEdgesFix);
    }

    // AmanithSVG surface handle (read only).
    public SVGTHandle getHandle() {
        
        return _surface;
    }

    // Get current surface width, in pixels.
    public int getWidth() {

        return AmanithSVG.svgtSurfaceWidth(_surface);
    }

    // Get current surface height, in pixels.
    public int getHeight() {

        return AmanithSVG.svgtSurfaceHeight(_surface);
    }

    public int getWidthPow2() {

        return pow2Get(getWidth());
    }

    public int getHeightPow2() {

        return pow2Get(getHeight());
    }

    public java.nio.ByteBuffer getPixels() {

        return AmanithSVG.svgtSurfacePixels(_surface);
    }

    /*
        The surface viewport (i.e. a drawing surface rectangular area), where to map the source document viewport.
        The combined use of surface and document viewport, induces a transformation matrix, that will be used to draw
        the whole SVG document. The induced matrix grants that the document viewport is mapped onto the surface
        viewport (respecting the specified alignment): all SVG content will be drawn accordingly.
    */
    public SVGViewport getViewport() {
        
        return _viewport;
    }

    public void setViewport(final SVGViewport viewport) {

        if (viewport == null) {
            throw new IllegalArgumentException("viewport == null");
        }
        else {
            _viewport.set(viewport.getX(), viewport.getY(), viewport.getWidth(), viewport.getHeight());
        }
    }

    // The maximum width/height dimension that can be specified to the SVGSurface.Resize and SVGAssets.CreateSurface functions.
    public static int getMaxDimension() {

        return AmanithSVG.svgtSurfaceMaxDimension();
    }

    // If needed, update surface viewport at AmanithSVG backend side
    SVGTError updateViewport() {

        SVGTError err = SVGTError.None;

        // set surface viewport (AmanithSVG backend)
        if ((_viewport != null) && _viewport.isChanged()) {
            
            float[] viewport = new float[] {_viewport.getX(), _viewport.getY(), _viewport.getWidth(), _viewport.getHeight() };

            // upload new values to the backend
            if ((err = AmanithSVG.svgtSurfaceViewportSet(_surface, viewport)) == SVGTError.None) {
                _viewport.setChanged(false);
            }
        }

        return err;
    }

    // Surface native handle.
    private SVGTHandle _surface;
    // Viewport.
    private SVGViewport _viewport;
}
