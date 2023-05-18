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
package com.mazatech.gdx;

// Java
import java.nio.Buffer;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;

// libGDX
import com.badlogic.gdx.graphics.Pixmap;
import com.badlogic.gdx.graphics.Pixmap.Format;
import com.badlogic.gdx.graphics.Texture;
import com.badlogic.gdx.graphics.TextureData;
import com.badlogic.gdx.utils.Disposable;
import com.badlogic.gdx.utils.GdxRuntimeException;

// AmanithSVG java binding (high level layer)
import com.mazatech.svgt.SVGColor;
import com.mazatech.svgt.SVGPacker;
import com.mazatech.svgt.SVGSurface;
// AmanithSVG java binding (low level layer)
import com.mazatech.svgt.AmanithSVG;
import com.mazatech.svgt.SVGTError;
import com.mazatech.svgt.SVGTHandle;
import com.mazatech.svgt.SVGTRenderingQuality;

public class SVGTextureAtlasPage extends Texture {

    private static class SVGTextureAtlasPageData implements TextureData, Disposable {

        private SVGTextureAtlasPageData(final SVGAssetsGDX svg,
                                        final SVGPacker.SVGPackedBin packerPage,
                                        boolean dilateEdgesFix,
                                        final SVGColor clearColor) {

            if (packerPage == null) {
                throw new IllegalArgumentException("packerPage == null");
            }
            if (clearColor == null) {
                throw new IllegalArgumentException("clearColor == null");
            }

            // get native rectangles from packer
            ByteBuffer nativeRects = packerPage.getNativeRects();

            _svg = svg;
            _width = packerPage.getWidth();
            _height = packerPage.getHeight();
            _nativeRectsCount = packerPage.getRectsCount();
            // copy the native rectangles
            _nativeRectsCopy = ByteBuffer.allocateDirect(nativeRects.capacity());
            _nativeRectsCopy.order(ByteOrder.nativeOrder());
            _nativeRectsCopy.put(nativeRects);
            // rewind buffer; NB: the cast is necessary, because Java 9 introduces overridden
            // methods with covariant return types for the some methods in java.nio.ByteBuffer
            // that are used by the driver. By casting to base Buffer we are always safe. See also
            // http://github.com/libgdx/libgdx/pull/6331 and http://stackoverflow.com/a/58435689/7912520
            ((Buffer)_nativeRectsCopy).rewind();
            _clearColor = clearColor;
            _dilateEdgesFix = dilateEdgesFix;
            _isPrepared = false;
        }

        public int getRectsCount() {

            return _nativeRectsCount;
        }

        public ByteBuffer getRects() {

            ByteBuffer result = _nativeRectsCopy.asReadOnlyBuffer();
            result.order(ByteOrder.nativeOrder());
            return result;
        }

        @Override
        public void dispose() {
            
            _nativeRectsCopy = null;
        }

        @Override
        public TextureDataType getType() {

            return TextureDataType.Custom;
        }

        @Override
        public boolean isPrepared() {

            return _isPrepared;
        }

        // Prepares the TextureData for a call to consumeCustomData(int target).
        // NB: this method can be called from a non OpenGL thread and should thus not interact with OpenGL
        @Override
        public void prepare() {

            if (_isPrepared) {
                throw new GdxRuntimeException("Already prepared");
            }
            else {
                // now the texture is prepared
                _isPrepared = true;
            }
        }

        @Override
        public void consumeCustomData(int target) {

            if (!_isPrepared) {
                throw new GdxRuntimeException("Call prepare() before calling consumeCustomData()");
            }
            else {
                // create the SVG drawing surface
                SVGSurface surface = _svg.createSurface(_width, _height);

                if (surface != null) {
                    // draw packed rectangles/elements
                    SVGTError err = surface.draw(_nativeRectsCopy, _clearColor, SVGTRenderingQuality.Better);
                    if (err == SVGTError.None) {
                        // upload pixels to the GL backend
                        SVGTextureUtils.uploadPixels(target, surface, _dilateEdgesFix);
                        // destroy the drawing surface
                        surface.dispose();
                        surface = null;
                    }
                }

                // NB: the texture still remains prepared
            }
        }

        @Override
        public Pixmap consumePixmap() {

            throw new GdxRuntimeException("This TextureData implementation does not return a Pixmap");
        }

        @Override
        public boolean disposePixmap() {

            throw new GdxRuntimeException("This TextureData implementation does not return a Pixmap");
        }

        @Override
        public int getWidth() {

            return _width;
        }

        @Override
        public int getHeight() {

            return _height;
        }

        @Override
        public Format getFormat() {

            return Format.RGBA8888;
        }

        @Override
        public boolean useMipMaps() {

            return false;
        }

        @Override
        public boolean isManaged() {

            // NB: this implementation can cope with a EGL context loss, because from the
            // internal copy of packed rects (_nativeRectsCopy member), we can regenerate the whole texture
            return true;
        }

        private final SVGAssetsGDX _svg;
        private final int _width;
        private final int _height;
        private final SVGColor _clearColor;
        private final int _nativeRectsCount;
        private ByteBuffer _nativeRectsCopy;
        private final boolean _dilateEdgesFix;
        private boolean _isPrepared;
    }

    SVGTextureAtlasPage(final SVGAssetsGDX svg,
                        final SVGPacker.SVGPackedBin packerPage,
                        boolean dilateEdgesFix,
                        final SVGColor clearColor) {

        this(new SVGTextureAtlasPageData(svg, packerPage, dilateEdgesFix, clearColor));
    }

    private SVGTextureAtlasPage(SVGTextureAtlasPageData data) {

        super(data);

        // build sprite regions
        buildRegions(data.getRectsCount(), data.getRects());

        // set min/mag filters
        if (data._dilateEdgesFix) {
            setFilter(TextureFilter.Linear, TextureFilter.Linear);
        }
        else {
            setFilter(TextureFilter.Nearest, TextureFilter.Nearest);
        }
        // set wrap mode
        setWrap(TextureWrap.ClampToEdge, TextureWrap.ClampToEdge);
    }

    private void buildRegions(int regionsCount,
                              final ByteBuffer nativeRects) {

        int nativeRectSize = AmanithSVG.svgtPackedRectSize();
        String arch = System.getProperty("os.arch", "").toLowerCase();
        boolean is64Bit = arch.contains("64") || arch.startsWith("armv8");
        int padBytes = is64Bit ? (nativeRectSize - 52) : (nativeRectSize - 48);

        _regions = new SVGTextureAtlasRegion[regionsCount];

        for (int i = 0; i < regionsCount; ++i) {
            
            long elemNamePtr = is64Bit ? nativeRects.getLong() : (long)nativeRects.getInt();
            String elemName = AmanithSVG.svgtPackedRectName(elemNamePtr);
            int originalX = nativeRects.getInt();
            int originalY = nativeRects.getInt();
            int x = nativeRects.getInt();
            int y = nativeRects.getInt();
            int width = nativeRects.getInt();
            int height = nativeRects.getInt();
            int docHandle = nativeRects.getInt();
            int elemIdx = nativeRects.getInt();
            int zOrder = nativeRects.getInt();
            float dstViewportWidth = nativeRects.getFloat();
            float dstViewportHeight = nativeRects.getFloat();
            for (int j = 0; j < padBytes; ++j) {
                byte pad = nativeRects.get();
            }

            _regions[i] = new SVGTextureAtlasRegion(this, elemName, originalX, originalY, x, y, width, height, new SVGTHandle(docHandle), zOrder, dstViewportWidth, dstViewportHeight);
        }
    }

    /* returns all regions in the texture */
    public SVGTextureAtlasRegion[] getRegions() {

        return _regions;
    }

    @Override
    public void setFilter(TextureFilter minFilter,
                          TextureFilter magFilter) {

        // we don't support mipmaps, so we have to patch minification filter
        if ((minFilter == TextureFilter.MipMap) ||
            (minFilter == TextureFilter.MipMapLinearNearest) ||
            (minFilter == TextureFilter.MipMapNearestLinear) ||
            (minFilter == TextureFilter.MipMapLinearLinear)) {
            minFilter = TextureFilter.Linear;
        }
        else
        if (minFilter == TextureFilter.MipMapNearestNearest) {
            minFilter = TextureFilter.Nearest;
        }

        // we don't support mipmaps, so we have to patch magnification filter
        if ((magFilter == TextureFilter.MipMap) ||
            (magFilter == TextureFilter.MipMapLinearNearest) ||
            (magFilter == TextureFilter.MipMapNearestLinear) ||
            (magFilter == TextureFilter.MipMapLinearLinear)) {
            magFilter = TextureFilter.Linear;
        }
        else
        if (magFilter == TextureFilter.MipMapNearestNearest) {
            magFilter = TextureFilter.Nearest;
        }

        super.setFilter(minFilter, magFilter);
    }

    @Override
    public void dispose() {

        SVGTextureAtlasPageData data = (SVGTextureAtlasPageData)getTextureData();

        if (data != null) {
            data.dispose();
        }

        super.dispose();
    }

    private SVGTextureAtlasRegion[] _regions = null;
}
