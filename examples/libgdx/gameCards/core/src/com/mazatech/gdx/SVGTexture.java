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

// libGDX
import com.badlogic.gdx.Gdx;
import com.badlogic.gdx.files.FileHandle;
import com.badlogic.gdx.graphics.Pixmap;
import com.badlogic.gdx.graphics.Pixmap.Format;
import com.badlogic.gdx.graphics.Texture;
import com.badlogic.gdx.graphics.TextureData;
import com.badlogic.gdx.utils.Disposable;
import com.badlogic.gdx.utils.GdxRuntimeException;

// AmanithSVG java binding (high level layer)
import com.mazatech.svgt.SVGColor;
import com.mazatech.svgt.SVGDocument;
import com.mazatech.svgt.SVGSurface;
import com.mazatech.svgt.SVGViewport;
// AmanithSVG java binding (low level layer)
import com.mazatech.svgt.SVGTRenderingQuality;

public class SVGTexture extends Texture {

    private static class SVGTextureDataFromDoc implements TextureData, Disposable {

        private SVGTextureDataFromDoc(SVGAssetsGDX svg,
                                      String xml) {

            this(svg, xml, 0, 0, SVGColor.Clear, false);
        }

        private SVGTextureDataFromDoc(final SVGAssetsGDX svg,
                                      final String xml,
                                      int width,
                                      int height,
                                      final SVGColor clearColor,
                                      boolean dilateEdgesFix) {

            if ((xml == null) || (xml.length() == 0)) {
                throw new IllegalArgumentException("xml string is null or empty");
            }
            if (clearColor == null) {
                throw new IllegalArgumentException("clearColor is null");
            }

            _svg = svg;
            _xml = xml;
            _width = width;
            _height = height;
            _clearColor = clearColor;
            _dilateEdgesFix = dilateEdgesFix;
            _isPrepared = false;
        }

        private SVGTextureDataFromDoc(final SVGAssetsGDX svg,
                                      final SVGDocument doc,
                                      int width,
                                      int height,
                                      final SVGColor clearColor,
                                      boolean dilateEdgesFix) {

            if (doc == null) {
                throw new IllegalArgumentException("SVG document is null");
            }
            if (clearColor == null) {
                throw new IllegalArgumentException("clearColor is null");
            }

            _svg = svg;
            _xml = null;
            _width = width;
            _height = height;
            _clearColor = clearColor;
            _dilateEdgesFix = dilateEdgesFix;
            _doc = doc;
            _docExternal = true;
            _isPrepared = false;
        }

        private void loadDocument() {

            if (_doc == null) {
                // create and parse the SVG document
                _doc = _svg.createDocument(_xml);
                // unreference the XML string
                _xml = null;
            }
        }

        private void destroyDocument() {

            // destroy the native SVG document
            if ((_doc != null) && (!_docExternal)) {
                _doc.dispose();
                _doc = null;
            }       
        }

        private void calcSurfaceDimensions() {

            if ((_width < 1) || (_height < 1)) {
            
                // start with something valid
                int srfWidth = 512;
                int srfHeight = 512;

                if (_doc != null) {

                    // if the user did not specify the desired texture dimensions, try to guess something valid from the SVG itself
                    if ((_width < 1) && (_height < 1)) {
                        // round document dimensions
                        int docWidth = (int)(_doc.getWidth() + 0.5f);
                        int docHeight = (int)(_doc.getHeight() + 0.5f);
                        // if the SVG document (i.e. the outermost <svg> element) does not specify 'width' and 'height' attributes, we start with default
                        // surface dimensions, keeping the same aspect ratio of the 'viewBox' attribute (present in the outermost <svg> element)
                        if (docWidth < 1 || docHeight < 1) {
                            // get document viewport (as it appears in the 'viewBox' attribute)
                            SVGViewport docViewport = _doc.getViewport();
                            // start with desired dimensions
                            srfWidth = (int)(docViewport.getWidth() + 0.5f);
                            srfHeight = (int)(docViewport.getHeight() + 0.5f);
                        }
                        else {
                            srfWidth = docWidth;
                            srfHeight = docHeight;
                        }
                    }
                    else {
                        // the user has specified a valid width or height: calculate the other dimension according to the SVG document viewport (keeping same aspect ratio)
                        // get document viewport (as it appears in the 'viewBox' attribute)
                        SVGViewport docViewport = _doc.getViewport();
                        if (_width > 0) {
                            srfWidth = _width;
                            srfHeight = (int)((docViewport.getHeight() / docViewport.getWidth()) * srfWidth + 0.5f);
                        }
                        else {
                            srfHeight = _height;
                            srfWidth = (int)((docViewport.getWidth() / docViewport.getHeight()) * srfHeight + 0.5f);
                        }
                    }
                }

                _width = srfWidth;
                _height = srfHeight;
            }
        }

        private SVGSurface createSurface() {

            int svgMaxDimension = SVGSurface.getMaxDimension();
            int glMaxDimension = SVGTextureUtils.getGlMaxTextureDimension();
            // take care of OpenGL and AmanithSVG limitations
            int maxDimension = Math.min(svgMaxDimension, glMaxDimension);
            
            if ((_width > maxDimension) || (_height > maxDimension)) {

                float finalWidth = (float)_width;
                float finalHeight = (float)_height;
                float widthScale = (float)maxDimension / finalWidth;
                float heightScale = (float)maxDimension / finalHeight;
                float scale = Math.min(widthScale, heightScale);
                // scale desired dimensions (we want at least a 1x1 texture)
                _width = (int)Math.max(Math.floor(finalWidth * scale), 1);
                _height = (int)Math.max(Math.floor(finalHeight * scale), 1);
            }

            // create the SVG drawing surface
            return _svg.createSurface(_width, _height);
        }

        @Override
        public void dispose() {

            // destroy the native SVG document
            destroyDocument();
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

            // create and parse the SVG document
            loadDocument();
            // calculate texture dimensions
            calcSurfaceDimensions();
            // now the texture is prepared
            _isPrepared = true;
        }

        @Override
        public void consumeCustomData(int target) {

            if (!_isPrepared) {
                throw new GdxRuntimeException("Call prepare() before calling consumeCustomData()");
            }
            else {
                // create the SVG drawing surface
                SVGSurface surface = createSurface();

                if (surface != null) {
                    // draw the SVG document over the surface
                    surface.draw(_doc, _clearColor, SVGTRenderingQuality.Better);
                    // upload pixels to the GL backend
                    SVGTextureUtils.uploadPixels(target, surface, _dilateEdgesFix);
                    // destroy the drawing surface
                    surface.dispose();
                    surface = null;
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
            // internal SVGDocument (_doc member), we can regenerate the whole texture
            return true;
        }

        private final SVGAssetsGDX _svg;
        private String _xml;
        // zero or negative dimensions means that they have to be derived from SVG document
        private int _width;
        private int _height;
        private final SVGColor _clearColor;
        private final boolean _dilateEdgesFix;
        private SVGDocument _doc = null;
        private boolean _docExternal = false;
        private boolean _isPrepared;
    }

    private static class SVGTextureDataFromSurface implements TextureData {

        private SVGTextureDataFromSurface(SVGSurface surface) {

            this(surface, false);
        }

        private SVGTextureDataFromSurface(final SVGSurface surface,
                                          boolean dilateEdgesFix) {

            if (surface == null) {
                throw new IllegalArgumentException("surface is null or empty");
            }

            _surface = surface;
            _width = surface.getWidth();
            _height = surface.getHeight();
            _dilateEdgesFix = dilateEdgesFix;
            _isPrepared = true;
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

            // now the texture is prepared
            _isPrepared = true;
        }

        @Override
        public void consumeCustomData(int target) {

            if (!_isPrepared) {
                throw new GdxRuntimeException("Call prepare() before calling consumeCustomData()");
            }
            else {
                // upload pixels to the GL backend
                SVGTextureUtils.uploadPixels(target, _surface, _dilateEdgesFix);
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

            // NB: this implementation cannot cope with a EGL context loss; we don't know
            // if the passed drawing surface object still exists!
            return false;
        }

        private final SVGSurface _surface;
        private final int _width;
        private final int _height;
        private final boolean _dilateEdgesFix;
        private boolean _isPrepared;
    }

    /*
        Generate a texture from the given "internal" SVG filename.

        With the term "internal", it's intended those read-only files located on the internal storage.
        For more details about libGDX file handling, please refer to the official documentation
        (https://libgdx.com/wiki/file-handling)

        Size of the texture is derived from the information available within the SVG file:

        - if the outermost <svg> element has 'width' and 'height' attributes, such values are
          used to size the texture
        - if the outermost <svg> element does not have 'width' and 'height' attributes, the size
          of texture is determined by the width and height values of the 'viewBox' attribute
    */
    SVGTexture(SVGAssetsGDX svg,
               String internalPath) {

        this(svg, Gdx.files.internal(internalPath));
    }

    SVGTexture(SVGAssetsGDX svg,
               FileHandle file) {

        this(new SVGTextureDataFromDoc(svg, file.readString()));
    }

    /*
        Generate a texture from the given "internal" SVG filename.
        Size of texture is specified by the given 'width' and 'height'.
        Before the SVG rendering, pixels are initialized with the given 'clearColor'.

        If the 'dilateEdgesFix' flag is set to true, the rendering process will also
        perform a 1-pixel dilate post-filter; this dilate filter is useful when the
        texture has some transparent parts (i.e. pixels with alpha component = 0): such
        flag will induce TextureFilter.Linear minification/magnification filtering.

        If the 'dilateEdgesFix' flag is set to false, no additional dilate post-filter
        is applied, and the texture minification/magnification filtering is set to
        TextureFilter.Nearest.
    */
    SVGTexture(final SVGAssetsGDX svg,
               final FileHandle file,
               int width,
               int height,
               final SVGColor clearColor,
               boolean dilateEdgesFix) {

        this(new SVGTextureDataFromDoc(svg, file.readString(), width, height, clearColor, dilateEdgesFix));
    }

    // Create a texture out of an SVG document (already instantiated externally)
    SVGTexture(final SVGAssetsGDX svg,
               final SVGDocument doc,
               int width,
               int height,
               final SVGColor clearColor,
               boolean dilateEdgesFix) {

        this(new SVGTextureDataFromDoc(svg, doc, width, height, clearColor, dilateEdgesFix));
    }

    private SVGTexture(SVGTextureDataFromDoc data) {

        super(data);
        _generatedFromDoc = true;

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

    // Create a texture out of a drawing surface
    public SVGTexture(SVGSurface surface) {

        this(new SVGTextureDataFromSurface(surface));
    }

    public SVGTexture(SVGSurface surface,
                      boolean dilateEdgesFix) {

        this(new SVGTextureDataFromSurface(surface, dilateEdgesFix));
    }

    private SVGTexture(SVGTextureDataFromSurface data) {

        super(data);
        _generatedFromDoc = false;

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

        if (_generatedFromDoc) {
            SVGTextureDataFromDoc data = (SVGTextureDataFromDoc)getTextureData();
            if (data != null) {
                data.dispose();
            }
        }

        super.dispose();
    }

    private final boolean _generatedFromDoc;
}
