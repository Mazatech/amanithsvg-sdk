/****************************************************************************
** Copyright (c) 2013-2018 Mazatech S.r.l.
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
package com.mazatech.amanithsvg.svgplayer;

import android.content.Context;
import android.graphics.PixelFormat;

import static android.opengl.EGL14.EGL_CONTEXT_CLIENT_VERSION;
import static android.opengl.GLES11Ext.GL_BGRA;
import static com.mazatech.svgt.AmanithSVG.SVGT_VENDOR;
import static com.mazatech.svgt.AmanithSVG.SVGT_VERSION;

import android.graphics.PointF;
import android.graphics.Rect;
import android.opengl.GLSurfaceView;
import android.support.v7.app.AlertDialog;
import android.util.DisplayMetrics;
import android.view.MotionEvent;

import java.io.BufferedReader;
import java.io.FileReader;
import java.io.IOException;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.nio.FloatBuffer;
import java.nio.IntBuffer;
import java.util.Calendar;

import javax.microedition.khronos.egl.EGL10;
import javax.microedition.khronos.egl.EGLConfig;
import javax.microedition.khronos.egl.EGLContext;
import javax.microedition.khronos.egl.EGLDisplay;
import javax.microedition.khronos.opengles.GL10;
import javax.microedition.khronos.opengles.GL11;

import com.mazatech.svgt.AmanithSVG;
import com.mazatech.svgt.SVGTError;
import com.mazatech.svgt.SVGTHandle;
import com.mazatech.svgt.SVGTRenderingQuality;

public class SvgPlayerView extends GLSurfaceView {

    // touch state
    private static final int TOUCH_MODE_NONE = 0;
    private static final int TOUCH_MODE_DOWN = 1;

    // menu commands
    public static final int SVGPLAYER_ABOUT_CMD = 0;

    // background pattern (dimensions and ARGB colors)
    static final int BACKGROUND_PATTERN_WIDTH = 32;
    static final int BACKGROUND_PATTERN_HEIGHT = 32;
    static final int BACKGROUND_PATTERN_COL0 = 0xFF808080;
    static final int BACKGROUND_PATTERN_COL1 = 0xFFC0C0C0;

    // AmanithSVG instance
    private AmanithSVG svgt;
    // SVG surface and document
    SVGTHandle svgSurface;
    SVGTHandle svgDoc;

    // OpenGL ES support of BGRA textures
    boolean bgraSupport;
    // OpenGL ES support of npot textures
    boolean npotSupport;
    // OpenGL texture used to draw the pattern background
    int[] patternTexture;
    // OpenGL texture used to blit the AmanithSVG surface
    int[] surfaceTexture;
    float[] surfaceTranslation;
    // the view renderer
    Renderer renderer;
    // SVG file content
    String xmlText;
    // touch state
    int touchMode;
    PointF touchStartPoint;

    SvgPlayerView(Context context, String filePath) {

        super(context);

        // create AmanithSVG instance
        svgt = new AmanithSVG();
        patternTexture = new int[] { 0 };
        surfaceTexture = new int[] { 0 };
        surfaceTranslation = new float[] { 0.0f, 0.0f };
        touchMode = TOUCH_MODE_NONE;
        touchStartPoint = new PointF();
        // load SVG content
        xmlText = loadXml(filePath);

        // ask for a 32-bit surface with alpha
        getHolder().setFormat(PixelFormat.TRANSLUCENT);
        // setup the context factory for OpenGL ES 1.1 rendering
        setEGLContextFactory(new ContextFactory());
        // ask for 32bit RGBA (we are not interested in depth, stencil nor aa samples)
        setEGLConfigChooser(new ConfigChooser(8, 8, 8, 8, 0, 0, 0));
        setPreserveEGLContextOnPause(true);

        // set the renderer responsible for frame rendering
        renderer = new SvgPlayerViewRenderer(this);
        setRenderer(renderer);
        setRenderMode(RENDERMODE_CONTINUOUSLY);

        // request focus
        setFocusable(true);
        requestFocus();
    }

    // return the power of two value greater (or equal) to a specified value
    private int pow2Get(int value) {

        int v = 1;

        if (value >= 0x40000000) {
            return 0x40000000;
        }
        while (v < value) {
            v <<= 1;
        }
        return v;
    }

    // swap R and B components (ARGB <--> ABGR and vice versa)
    private int swapRedBlue(int p) {
        // swap R <--> B
        int ag = p & 0xFF00FF00;
        int rb = p & 0x00FF00FF;
        int r = rb >> 16;
        int b = rb & 0xFF;
        return ag | (b << 16) | r;
    }

    private void boxFit(int[] srcRect, int dstWidth, int dstHeight) {

        float widthScale = (float)dstWidth / (float)(srcRect[0]);
        float heightScale = (float)dstHeight / (float)(srcRect[1]);
        float scale = Math.min(widthScale, heightScale);
        srcRect[0] = Math.round(srcRect[0] * scale);
        srcRect[1] = Math.round(srcRect[1] * scale);
    }

    private Rect surfaceDimensionsCalc(SVGTHandle doc, int maxWidth, int maxHeight) {

        Rect result = new Rect();

        if (doc != null) {
            int[] srfRect = new int[] { 0, 0 };
            int maxAllowedDimension = svgt.svgtSurfaceMaxDimension();
            // round document dimensions
            int svgWidth = Math.round(svgt.svgtDocWidth(doc));
            int svgHeight = Math.round(svgt.svgtDocHeight(doc));
            // if the SVG document (i.e. the outermost <svg> element) does not specify 'width' and 'height' attributes, we start with default
            // surface dimensions, keeping the same aspect ratio of the 'viewBox' attribute (present in the outermost <svg> element)
            if ((svgWidth < 1) || (svgHeight < 1)) {
                float[] docViewport = new float[4];
                // get document viewport (as it appears in the 'viewBox' attribute)
                if (svgt.svgtDocViewportGet(doc, docViewport) == SVGTError.None) {
                    // start with desired dimensions
                    srfRect[0] = Math.round(docViewport[2]);
                    srfRect[1] = Math.round(docViewport[3]);
                    if ((srfRect[0] > maxWidth) || (srfRect[1] > maxHeight)) {
                        // adapt desired dimensions to max bounds
                        boxFit(srfRect, maxWidth, maxHeight);
                    }
                }
                else {
                    // just start with something valid
                    srfRect[0] = (maxWidth / 3) + 1;
                    srfRect[1] = (maxHeight / 3) + 1;
                }
            }
            else {
                // start with desired dimensions
                srfRect[0] = svgWidth;
                srfRect[1] = svgHeight;
                if ((srfRect[0] > maxWidth) || (srfRect[1] > maxHeight)) {
                    // adapt desired dimensions to screen bounds
                    boxFit(srfRect, maxWidth, maxHeight);
                }
            }

            if ((srfRect[0] < 1) || (srfRect[1] < 1)) {
                // just start with something valid
                srfRect[0] = (maxWidth / 3) + 1;
                srfRect[1] = (maxHeight / 3) + 1;
            }

            // take care of the maximum allowed dimension for drawing surfaces
            if ((srfRect[0] > maxAllowedDimension) || (srfRect[1] > maxAllowedDimension)) {
                boxFit(srfRect, maxAllowedDimension, maxAllowedDimension);
            }

            result.set(0, 0, srfRect[0], srfRect[1]);
        }

        return result;
    }

    private String loadXml(String filePath) {

        // read text from file
        StringBuilder stringBuilder = new StringBuilder();

        try {
            BufferedReader reader = new BufferedReader(new FileReader(filePath));
            String line;
            while ((line = reader.readLine()) != null) {
                stringBuilder.append(line);
                stringBuilder.append('\n');
            }
            reader.close();
        }
        catch (IOException e) {
            return null;
        }
        return stringBuilder.toString();
    }

    public SVGTHandle loadSvg(String xmlText) {

        return (xmlText != null) ? svgt.svgtDocCreate(xmlText) : null;
    }

    private void genPatternTexture(GL11 gl) {

        int i, j;
        IntBuffer pixelsBuffer;
        // select best format, in order to avoid swizzling
        int format = (bgraSupport) ? GL_BGRA : GL10.GL_RGBA;
        // allocate pixels
        int[] pixels = new int[BACKGROUND_PATTERN_WIDTH * BACKGROUND_PATTERN_HEIGHT];      

        for (i = 0; i < BACKGROUND_PATTERN_HEIGHT; ++i) {
            if (bgraSupport) {
                // use ARGB pixel format
                for (j = 0; j < BACKGROUND_PATTERN_WIDTH; ++j) {
                    if (i < BACKGROUND_PATTERN_HEIGHT / 2) {
                        pixels[i * BACKGROUND_PATTERN_WIDTH + j] = (j < BACKGROUND_PATTERN_WIDTH / 2) ? BACKGROUND_PATTERN_COL0 : BACKGROUND_PATTERN_COL1;
                    }
                    else {
                        pixels[i * BACKGROUND_PATTERN_WIDTH + j] = (j < BACKGROUND_PATTERN_WIDTH / 2) ? BACKGROUND_PATTERN_COL1 : BACKGROUND_PATTERN_COL0;
                    }
                }
            }
            else {
                // use ABGR pixel format
                for (j = 0; j < BACKGROUND_PATTERN_WIDTH; ++j) {
                    if (i < BACKGROUND_PATTERN_HEIGHT / 2) {
                        pixels[i * BACKGROUND_PATTERN_WIDTH + j] = (j < BACKGROUND_PATTERN_WIDTH / 2) ? swapRedBlue(BACKGROUND_PATTERN_COL0) : swapRedBlue(BACKGROUND_PATTERN_COL1);
                    }
                    else {
                        pixels[i * BACKGROUND_PATTERN_WIDTH + j] = (j < BACKGROUND_PATTERN_WIDTH / 2) ? swapRedBlue(BACKGROUND_PATTERN_COL1) : swapRedBlue(BACKGROUND_PATTERN_COL0);
                    }
                }
            }
        }

        pixelsBuffer = IntBuffer.wrap(pixels);

        gl.glGenTextures(1, patternTexture, 0);
        gl.glEnable(GL10.GL_TEXTURE_2D);
        gl.glBindTexture(GL10.GL_TEXTURE_2D, patternTexture[0]);
        gl.glTexParameteri(GL10.GL_TEXTURE_2D, GL10.GL_TEXTURE_MIN_FILTER, GL10.GL_NEAREST);
        gl.glTexParameteri(GL10.GL_TEXTURE_2D, GL10.GL_TEXTURE_MAG_FILTER, GL10.GL_NEAREST);
        gl.glTexParameteri(GL10.GL_TEXTURE_2D, GL10.GL_TEXTURE_WRAP_S, GL10.GL_REPEAT);
        gl.glTexParameteri(GL10.GL_TEXTURE_2D, GL10.GL_TEXTURE_WRAP_T, GL10.GL_REPEAT);
        gl.glTexImage2D(GL10.GL_TEXTURE_2D, 0, format, BACKGROUND_PATTERN_WIDTH, BACKGROUND_PATTERN_HEIGHT, 0, format, GL10.GL_UNSIGNED_BYTE, pixelsBuffer);
    }

    private void genSurfaceTexture(GL11 gl) {

        int[] maxTextureSize = new int[1];
        // get AmanithSVG surface dimensions and pixels pointer
        int surfaceWidth = svgt.svgtSurfaceWidth(svgSurface);
        int surfaceHeight = svgt.svgtSurfaceHeight(svgSurface);

        // get maximum texture size
        gl.glGetIntegerv(GL10.GL_MAX_TEXTURE_SIZE, maxTextureSize, 0);
        if ((surfaceWidth <= maxTextureSize[0]) && (surfaceHeight <= maxTextureSize[0])) {
            int format = (bgraSupport) ? GL_BGRA : GL10.GL_RGBA;
            int texWidth = npotSupport ? surfaceWidth : pow2Get(surfaceWidth);
            int texHeight = npotSupport ? surfaceHeight : pow2Get(surfaceHeight);

            // generate OpenGL ES texture
            gl.glGenTextures(1, surfaceTexture, 0);
            gl.glEnable(GL10.GL_TEXTURE_2D);
            gl.glBindTexture(GL10.GL_TEXTURE_2D, surfaceTexture[0]);
            gl.glTexParameteri(GL10.GL_TEXTURE_2D, GL10.GL_TEXTURE_MIN_FILTER, GL10.GL_NEAREST);
            gl.glTexParameteri(GL10.GL_TEXTURE_2D, GL10.GL_TEXTURE_MAG_FILTER, GL10.GL_NEAREST);
            gl.glTexParameteri(GL10.GL_TEXTURE_2D, GL10.GL_TEXTURE_WRAP_S, GL10.GL_CLAMP_TO_EDGE);
            gl.glTexParameteri(GL10.GL_TEXTURE_2D, GL10.GL_TEXTURE_WRAP_T, GL10.GL_CLAMP_TO_EDGE);
            // upload pixels
            if (bgraSupport) {
                // get AmanithSVG surface pixels
                java.nio.ByteBuffer surfacePixels = svgt.svgtSurfacePixels(svgSurface);
                gl.glTexImage2D(GL10.GL_TEXTURE_2D, 0, format, texWidth, texHeight, 0, format, GL10.GL_UNSIGNED_BYTE, surfacePixels);
            }
            else {
                int[] rgbaPixels = new int[surfaceWidth * surfaceHeight];
                if (svgt.svgtSurfaceCopy(svgSurface, rgbaPixels, true, false) == SVGTError.None) {
                    gl.glTexImage2D(GL10.GL_TEXTURE_2D, 0, format, texWidth, texHeight, 0, format, GL10.GL_UNSIGNED_BYTE, null);
                    gl.glTexSubImage2D(GL10.GL_TEXTURE_2D, 0, 0, 0, surfaceWidth, surfaceHeight, format, GL10.GL_UNSIGNED_BYTE, IntBuffer.wrap(rgbaPixels));
                }
            }
        }
    }

    private void drawBackgroundTexture(GL11 gl) {

        float u = (float)getWidth() / BACKGROUND_PATTERN_WIDTH;
        float v = (float)getHeight() / BACKGROUND_PATTERN_HEIGHT;

        gl.glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
        gl.glDisable(GL10.GL_BLEND);
        gl.glEnable(GL10.GL_TEXTURE_2D);
        gl.glBindTexture(GL10.GL_TEXTURE_2D, patternTexture[0]);
        // simply put a quad, covering the whole view
        texturedRectangleDraw(gl, 0.0f, 0.0f, (float)getWidth(), (float)getHeight(), u, v);
    }

    private void drawSurfaceTexture(GL11 gl) {

        float u, v;
        // get AmanithSVG surface dimensions
        float surfaceWidth = (float)svgt.svgtSurfaceWidth(svgSurface);
        float surfaceHeight = (float)svgt.svgtSurfaceHeight(svgSurface);
        float tx = Math.round(surfaceTranslation[0]);
        float ty = Math.round(surfaceTranslation[1]);
        
        if (npotSupport) {
            u = 1.0f;
            v = 1.0f;
        }
        else {
            // greater (or equal) power of two values
            float texWidth = (float)pow2Get(svgt.svgtSurfaceWidth(svgSurface));
            float texHeight = (float)pow2Get(svgt.svgtSurfaceHeight(svgSurface));
            u = (surfaceWidth - 0.5f) / texWidth;
            v = (surfaceHeight - 0.5f) / texHeight;
        }

        // enable per-pixel alpha blending, using surface texture as source
        gl.glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
        gl.glEnable(GL10.GL_BLEND);
        gl.glEnable(GL10.GL_TEXTURE_2D);
        gl.glBindTexture(GL10.GL_TEXTURE_2D, surfaceTexture[0]);
        // simply put a quad
        texturedRectangleDraw(gl, tx, ty, surfaceWidth, surfaceHeight, u, v);

        // draw a solid black frame surrounding the SVG document
        gl.glColor4f(0.0f, 0.0f, 0.0f, 1.0f);
        gl.glDisable(GL10.GL_BLEND);
        gl.glDisable(GL10.GL_TEXTURE_2D);
        rectangleDraw(gl, tx, ty, surfaceWidth, 2.0f);
        rectangleDraw(gl, tx, ty, 2.0f, surfaceHeight);
        rectangleDraw(gl, tx, ty + surfaceHeight - 2.0f, surfaceWidth, 2.0f);
        rectangleDraw(gl, tx + surfaceWidth - 2.0f, ty, 2.0f, surfaceHeight);
    }

    /*****************************************************************
                                 OpenGL ES
    *****************************************************************/
    private void glesInit(GL11 gl) {

        String extensions = gl.glGetString(GL10.GL_EXTENSIONS);

        // check useful GL extensions
        bgraSupport = ((extensions.contains("GL_EXT_texture_format_BGRA8888")) || (extensions.contains("GL_IMG_texture_format_BGRA8888")));
        npotSupport = ((extensions.contains("GL_OES_texture_npot")) || (extensions.contains("GL_APPLE_texture_2D_limited_npot")));

        // set basic OpenGL states
        gl.glDisable(GL10.GL_LIGHTING);
        gl.glShadeModel(GL10.GL_FLAT);
        gl.glHint(GL10.GL_PERSPECTIVE_CORRECTION_HINT, GL10.GL_NICEST);
        gl.glDisable(GL10.GL_CULL_FACE);
        gl.glDisable(GL10.GL_ALPHA_TEST);
        gl.glDisable(GL10.GL_SCISSOR_TEST);
        gl.glDisable(GL10.GL_DEPTH_TEST);
        gl.glDisable(GL10.GL_STENCIL_TEST);
        gl.glDisable(GL10.GL_BLEND);
        gl.glDepthMask(false);
        gl.glColorMask(true, true, true, true);
        gl.glBlendFunc(GL10.GL_SRC_ALPHA, GL10.GL_ONE_MINUS_SRC_ALPHA);
        gl.glMatrixMode(GL10.GL_PROJECTION);
        gl.glLoadIdentity();
        gl.glMatrixMode(GL10.GL_MODELVIEW);
        gl.glLoadIdentity();
        gl.glClearColor(0.0f, 1.0f, 1.0f, 1.0f);

        // generate pattern texture, used to draw the background
        genPatternTexture(gl);
    }

    private FloatBuffer glesFloatBuffer(float[] arr) {

        ByteBuffer bb = ByteBuffer.allocateDirect(arr.length * 4);
        bb.order(ByteOrder.nativeOrder());
        FloatBuffer fb = bb.asFloatBuffer();
        fb.put(arr);
        fb.position(0);
        return fb;
    }

    // load projection matrix
    private void projectionLoad(GL11 gl,
                                float left,
                                float right,
                                float bottom,
                                float top) {

        float[] prjMatrix = new float[] {
            2.0f / (right - left),            0.0f,                             0.0f, 0.0f,
            0.0f,                             2.0f / (top - bottom),            0.0f, 0.0f,
            0.0f,                             0.0f,                             1.0f, 0.0f,
            -(right + left) / (right - left), -(top + bottom) / (top - bottom), 0.0f, 1.0f
        };
        gl.glMatrixMode(GL10.GL_PROJECTION);
        gl.glLoadMatrixf(prjMatrix, 0);
    }

    private void texturedRectangleDraw(GL11 gl,
                                       float x,
                                       float y,
                                       float width,
                                       float height,
                                       float u,
                                       float v) {

        // 4 vertices
        float[] xy = new float[] {
            x, y,
            x + width, y,
            x, y + height,
            x + width, y + height
        };
        FloatBuffer xyBuffer = glesFloatBuffer(xy);
        float[] uv = new float[] {
            0.0f, 0.0f,
            u, 0.0f,
            0.0f, v,
            u, v
        };
        FloatBuffer uvBuffer = glesFloatBuffer(uv);

        gl.glVertexPointer(2, GL10.GL_FLOAT, 0, xyBuffer);
        gl.glEnableClientState(GL10.GL_VERTEX_ARRAY);
        gl.glTexCoordPointer(2, GL10.GL_FLOAT, 0, uvBuffer);
        gl.glEnableClientState(GL10.GL_TEXTURE_COORD_ARRAY);
        gl.glDrawArrays(GL10.GL_TRIANGLE_STRIP, 0, 4);
    }

    private void rectangleDraw(GL11 gl,
                               float x,
                               float y,
                               float width,
                               float height) {

        float[] xy = new float[] {
            x, y,
            x + width, y,
            x, y + height,
            x + width, y + height
        };
        FloatBuffer xyBuffer = glesFloatBuffer(xy);

        gl.glVertexPointer(2, GL10.GL_FLOAT, 0, xyBuffer);
        gl.glEnableClientState(GL10.GL_VERTEX_ARRAY);
        gl.glDisableClientState(GL10.GL_TEXTURE_COORD_ARRAY);
        gl.glDrawArrays(GL10.GL_TRIANGLE_STRIP, 0, 4);
    }

    private void deltaTranslation(float dx, float dy) {

        surfaceTranslation[0] += dx;
        surfaceTranslation[1] -= dy;
    }

    /*****************************************************************
                                 Tutorial
    *****************************************************************/
    private void playerInit(GL11 gl) {

        // get display metrics
        DisplayMetrics metrics = getResources().getDisplayMetrics();

        // initialize OpenGL ES
        glesInit(gl);

        // initialize AmanithSVG library
        if (svgt.svgtInit(metrics.widthPixels, metrics.heightPixels, metrics.xdpi) == SVGTError.None) {
            // load the SVG document
            if (xmlText != null) {
                svgDoc = loadSvg(xmlText);
            }
        }
    }

    // destroy SVG resources allocated by the player
    private void playerDestroy() {

        // destroy the SVG document
        if (svgDoc != null) {
            svgt.svgtDocDestroy(svgDoc);
        }
        // destroy the drawing surface
        if (svgSurface != null) {
            svgt.svgtSurfaceDestroy(svgSurface);
        }
        // deinitialize AmanithSVG library
        svgt.svgtDone();
    }

    private void playerDraw(GL11 gl) {

        // clear OpenGL buffer
        gl.glClear(GL10.GL_COLOR_BUFFER_BIT);
        // draw pattern background
        drawBackgroundTexture(gl);
        // put AmanithSVG surface using per-pixel alpha blend
        if ((svgDoc != null) && (svgSurface != null)) {
            drawSurfaceTexture(gl);
        }
    }

    private void svgDraw(GL11 gl, int width, int height) {

        if (svgSurface != null) {
            // if new desired dimensions are equal to current ones, simply exit
            if ((width == svgt.svgtSurfaceWidth(svgSurface)) && (height == svgt.svgtSurfaceHeight(svgSurface))) {
                return;
            }
            // destroy current surface texture
            if (surfaceTexture[0] != 0) {
                gl.glDeleteTextures(1, surfaceTexture, 0);
                surfaceTexture[0] = 0;
            }
            // resize AmanithSVG surface
            svgt.svgtSurfaceResize(svgSurface, width, height);
        }
        else {
            // first time, we must create AmanithSVG surface
            svgSurface = svgt.svgtSurfaceCreate(width, height);
            // clear the drawing surface (full transparent white) at every svgtDocDraw call
            svgt.svgtClearColor(1.0f, 1.0f, 1.0f, 0.0f);
            svgt.svgtClearPerform(true);
        }
        // draw the SVG document
        svgt.svgtDocDraw(svgDoc, svgSurface, SVGTRenderingQuality.Better);
        // create surface texture
        genSurfaceTexture(gl);
    }

    private void playerResize(GL11 gl, int width, int height) {

        // create / resize the AmanithSVG surface such that it is centered within the OpenGL view
        if (svgDoc != null) {
            // calculate AmanithSVG surface dimensions
            Rect srfRect = surfaceDimensionsCalc(svgDoc, width, height);
            // create / resize AmanithSVG surface, then draw the loaded SVG document
            svgDraw(gl, srfRect.width(), srfRect.height());
            // center AmanithSVG surface within the OpenGL view
            surfaceTranslation[0] = (float)(width - svgt.svgtSurfaceWidth(svgSurface)) * 0.5f;
            surfaceTranslation[1] = (float)(height - svgt.svgtSurfaceHeight(svgSurface)) * 0.5f;
        }

        // set OpenGL viewport and projection
        gl.glViewport(0, 0, width, height);
        projectionLoad(gl, 0.0f, width, 0.0f, height);
    }

    boolean playerMenuOption(int option) {

        boolean consumed = true;

        switch (option) {

            case SVGPLAYER_ABOUT_CMD:
                aboutDialog();
                break;

            default:
                consumed = false;
                break;
        }

        return consumed;
    }

    private void playerTouchDown(float x, float y) {

        touchStartPoint.set(x, y);
        touchMode = TOUCH_MODE_DOWN;
    }

    private void playerTouchUp(float x, float y) {

        touchMode = TOUCH_MODE_NONE;
    }

    private void playerTouchMove(float x, float y) {

        if (touchMode == TOUCH_MODE_DOWN) {
            deltaTranslation(x - touchStartPoint.x, y - touchStartPoint.y);
            touchStartPoint.set(x, y);
        }
    }

    private static class ContextFactory implements GLSurfaceView.EGLContextFactory {

        public EGLContext createContext(EGL10 egl, EGLDisplay display, EGLConfig eglConfig) {

            int[] contextAttribs = {
                EGL_CONTEXT_CLIENT_VERSION, 1,
                EGL10.EGL_NONE
            };
            return egl.eglCreateContext(display, eglConfig, EGL10.EGL_NO_CONTEXT, contextAttribs);
        }

        public void destroyContext(EGL10 egl, EGLDisplay display, EGLContext context) {

            egl.eglDestroyContext(display, context);
        }
    }

    private static class ConfigChooser implements GLSurfaceView.EGLConfigChooser {

        public ConfigChooser(int r, int g, int b, int a, int depth, int stencil, int samples) {

            // keep track of desired settings
            redSize = r;
            greenSize = g;
            blueSize = b;
            alphaSize = a;
            depthSize = depth;
            stencilSize = stencil;
            aaSamples = samples;
        }

        private int findConfigAttrib(EGL10 egl, EGLDisplay display, EGLConfig config, int attribute, int defaultValue) {

            if (egl.eglGetConfigAttrib(display, config, attribute, tmpValue)) {
                return tmpValue[0];
            }
            return defaultValue;
        }

        private EGLConfig chooseConfigImpl(EGL10 egl, EGLDisplay display, EGLConfig[] configs) {

            EGLConfig result = null;

            for (EGLConfig config : configs) {
                // we need at least depthSize and stencilSize bits
                int d = findConfigAttrib(egl, display, config, EGL10.EGL_DEPTH_SIZE, 0);
                int s = findConfigAttrib(egl, display, config, EGL10.EGL_STENCIL_SIZE, 0);
                if ((d < depthSize) || (s < stencilSize)) {
                    continue;
                }
                // we want an exact match for red/green/blue/alpha
                int r = findConfigAttrib(egl, display, config, EGL10.EGL_RED_SIZE, 0);
                int g = findConfigAttrib(egl, display, config, EGL10.EGL_GREEN_SIZE, 0);
                int b = findConfigAttrib(egl, display, config, EGL10.EGL_BLUE_SIZE, 0);
                int a = findConfigAttrib(egl, display, config, EGL10.EGL_ALPHA_SIZE, 0);

                if ((r == redSize) && (g == greenSize) && (b == blueSize) && (a == alphaSize)) {

                    int sampleBuffers = findConfigAttrib(egl, display, config, EGL10.EGL_SAMPLE_BUFFERS, 0);
                    int samples = findConfigAttrib(egl, display, config, EGL10.EGL_SAMPLES, 0);

                    if (aaSamples == 0) {
                        // we don't want sample buffers, so discard those configurations that have them
                        if (sampleBuffers == 0) {
                            result = config;
                            break;
                        }
                    }
                    else {
                        if ((sampleBuffers > 0) && (samples > 0)) {
                            // select the highest number of aa samples
                            if (samples > aaSamples) {
                                aaSamples = samples;
                                result = config;
                            }
                        }
                    }
                }
            }

            return result;
        }

        private EGLConfig chooseConfig(EGL10 egl, EGLDisplay display, EGLConfig[] configs) {

            return chooseConfigImpl(egl, display, configs);
        }

        // must be implemented by a GLSurfaceView.EGLConfigChooser
        public EGLConfig chooseConfig(EGL10 egl, EGLDisplay display) {

            // get the number of minimally matching EGL configurations
            int[] num_config = new int[1];
            egl.eglChooseConfig(display, configAttribs, null, 0, num_config);
            int numConfigs = num_config[0];
            if (numConfigs <= 0) {
                throw new IllegalArgumentException("No configs match configSpec");
            }
            // allocate then read the array of minimally matching EGL configs
            EGLConfig[] configs = new EGLConfig[numConfigs];
            egl.eglChooseConfig(display, configAttribs, configs, numConfigs, num_config);
            // now return the "best" one
            return chooseConfig(egl, display, configs);
        }

        // subclasses can adjust these values
        private int redSize;
        private int greenSize;
        private int blueSize;
        private int alphaSize;
        private int depthSize;
        private int stencilSize;
        private int aaSamples;
        private int[] tmpValue = new int[1];
        // we start with a minimum size of 4 bits for red/green/blue, but will perform actual matching in chooseConfig() below.
        private static int EGL_OPENGL_ES_BIT = 1;
        private static int[] configAttribs = {
            EGL10.EGL_RED_SIZE, 4,
            EGL10.EGL_GREEN_SIZE, 4,
            EGL10.EGL_BLUE_SIZE, 4,
            EGL10.EGL_RENDERABLE_TYPE, EGL_OPENGL_ES_BIT,
            EGL10.EGL_NONE
        };
    }

    private static class SvgPlayerViewRenderer implements GLSurfaceView.Renderer {

        private SvgPlayerView view;

        SvgPlayerViewRenderer(SvgPlayerView myview) {

            // keep track of view
            view = myview;
        }

        public void onSurfaceCreated(GL10 gl, EGLConfig config) {

            view.playerInit((GL11)gl);
        }

        public void onDrawFrame(GL10 gl) {

            view.playerDraw((GL11)gl);
        }

        public void onSurfaceChanged(GL10 gl, int width, int height) {

            view.playerResize((GL11)gl, width, height);
        }
    }

    private void messageDialog(String title, String msg) {

        AlertDialog dialog = new AlertDialog.Builder(getContext()).create();

        // show message
        dialog.setTitle(title);
        dialog.setMessage(msg);
        dialog.show();
    }

    private void aboutDialog() {

        String msg = "";
        String year = String.valueOf(Calendar.getInstance().get(Calendar.YEAR));

        msg += "AmanithSVG - www.mazatech.com\n";
        msg += "Copyright 2013-" + year + " by Mazatech Srl. All Rights Reserved.\n\n";
        msg += "AmanithSVG driver informations:\n\n";
        // vendor
        msg += "Vendor: " + svgt.svgtGetString(SVGT_VENDOR) + "\n";
        // version
        msg += "Version: " + svgt.svgtGetString(SVGT_VERSION) + "\n";
        messageDialog("About AmanithSVG", msg);
    }

    @Override
    protected void onDetachedFromWindow() {

        // destroy SVG resources allocated by the player
        playerDestroy();
        super.onDetachedFromWindow();
    }

    @Override
    public boolean onTouchEvent(MotionEvent event) {

        switch (event.getAction() & MotionEvent.ACTION_MASK) {

            case MotionEvent.ACTION_DOWN:
                playerTouchDown(event.getX(), event.getY());
                break;

            case MotionEvent.ACTION_UP:
                playerTouchUp(event.getX(), event.getY());
                break;

            case MotionEvent.ACTION_MOVE:
                playerTouchMove(event.getX(), event.getY());
                break;
        }

        return true;
    }
}
