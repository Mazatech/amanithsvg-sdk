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
package com.mazatech.amanithsvg.svgviewer;

import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.nio.FloatBuffer;
import java.util.Calendar;

import javax.microedition.khronos.egl.EGL10;
import javax.microedition.khronos.egl.EGLConfig;
import javax.microedition.khronos.egl.EGLContext;
import javax.microedition.khronos.egl.EGLDisplay;
import javax.microedition.khronos.opengles.GL10;
import javax.microedition.khronos.opengles.GL11;

import android.content.Context;
import android.graphics.PixelFormat;
import static android.opengl.EGL14.EGL_CONTEXT_CLIENT_VERSION;
import android.graphics.PointF;
import android.graphics.Rect;
import android.opengl.GLSurfaceView;
import android.support.annotation.NonNull;
import android.support.v7.app.AlertDialog;
import android.util.Log;
import android.view.MotionEvent;

// AmanithSVG for Android
import com.mazatech.android.SVGAssetsAndroid;

// AmanithSVG (high level layer)
import com.mazatech.svgt.AmanithSVG;
import com.mazatech.svgt.SVGColor;
import com.mazatech.svgt.SVGDocument;
import com.mazatech.svgt.SVGSurface;
import com.mazatech.svgt.SVGTError;
import com.mazatech.svgt.SVGViewport;
// AmanithSVG (low level layer)
import com.mazatech.svgt.SVGTLogLevel;
import com.mazatech.svgt.SVGTRenderingQuality;
import com.mazatech.svgt.SVGTStringID;

public class SvgViewerView extends GLSurfaceView {

    private static final String LOG_TAG = "SvgViewerView";

    // touch state
    private static final int TOUCH_MODE_NONE = 0;
    private static final int TOUCH_MODE_DOWN = 1;

    // menu commands
    public static final int SVGVIEWER_ABOUT_CMD = 1;

    // background pattern (dimensions and ARGB colors)
    static final int BACKGROUND_PATTERN_WIDTH = 32;
    static final int BACKGROUND_PATTERN_HEIGHT = 32;
    static final int BACKGROUND_PATTERN_COL0 = 0xFF808080;
    static final int BACKGROUND_PATTERN_COL1 = 0xFFC0C0C0;

    // AmanithSVG for Android instance
    private final SVGAssetsAndroid svg;
    // SVG surface and document
    private SVGSurface svgSurface;
    private SVGDocument svgDoc;
    // OpenGL texture used to draw the pattern background
    private int patternTexture;
    // OpenGL texture used to blit the AmanithSVG surface
    private int surfaceTexture;
    private final float[] surfaceTranslation;
    // SVG file name
    private final String fullFileName;
    // touch state
    private int touchMode;
    private final PointF touchStartPoint;

    SvgViewerView(Context context,
                  SVGAssetsAndroid svgInstance,
                  String filePath) {

        super(context);

        svg = svgInstance;
        svgDoc = null;
        svgSurface = null;

        patternTexture = 0;
        surfaceTexture = 0;
        surfaceTranslation = new float[] { 0.0f, 0.0f };
        touchMode = TOUCH_MODE_NONE;
        touchStartPoint = new PointF();
        // keep track of filename
        fullFileName = filePath;

        // ask for a 32-bit surface with alpha
        getHolder().setFormat(PixelFormat.TRANSLUCENT);
        // setup the context factory for OpenGL ES 1.1 rendering
        setEGLContextFactory(new ContextFactory(this));
        // ask for 32bit RGBA (we are not interested in depth, stencil nor aa samples)
        setEGLConfigChooser(new ConfigChooser(8, 8, 8, 8, 0, 0, 0));
        setPreserveEGLContextOnPause(true);

        // set the renderer responsible for frame rendering
        Renderer renderer = new SvgViewerViewRenderer(this);
        setRenderer(renderer);
        setRenderMode(RENDERMODE_CONTINUOUSLY);

        // request focus
        setFocusable(true);
        requestFocus();
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

    private void boxFit(@NonNull int[] srcRect,
                        int dstWidth,
                        int dstHeight) {

        float widthScale = (float)dstWidth / (float)(srcRect[0]);
        float heightScale = (float)dstHeight / (float)(srcRect[1]);
        float scale = Math.min(widthScale, heightScale);
        srcRect[0] = Math.round(srcRect[0] * scale);
        srcRect[1] = Math.round(srcRect[1] * scale);
    }

    private @NonNull Rect surfaceDimensionsCalc(SVGDocument doc,
                                                int maxWidth,
                                                int maxHeight) {

        Rect result = new Rect();

        if (doc != null) {
            int[] srfRect = new int[] { 0, 0 };
            int maxAllowedDimension = AmanithSVG.svgtSurfaceMaxDimension();
            // round document dimensions
            int svgWidth = Math.round(doc.getWidth());
            int svgHeight = Math.round(doc.getHeight());
            // if the SVG document (i.e. the outermost <svg> element) does not specify 'width' and 'height' attributes, we start with default
            // surface dimensions, keeping the same aspect ratio of the 'viewBox' attribute (present in the outermost <svg> element)
            if ((svgWidth < 1) || (svgHeight < 1)) {
                SVGViewport docViewport = doc.getViewport();
                // start with desired dimensions
                srfRect[0] = Math.round(docViewport.getWidth());
                srfRect[1] = Math.round(docViewport.getHeight());
                if ((srfRect[0] > maxWidth) || (srfRect[1] > maxHeight)) {
                    // adapt desired dimensions to max bounds
                    boxFit(srfRect, maxWidth, maxHeight);
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

    private void genPatternTexture(GL11 gl) {

        int i, j;
        // allocate pixels
        int[] pixels = new int[BACKGROUND_PATTERN_WIDTH * BACKGROUND_PATTERN_HEIGHT];
        int col0 = svg.glSupportBGRA(gl) ? BACKGROUND_PATTERN_COL0 : swapRedBlue(BACKGROUND_PATTERN_COL0);
        int col1 = svg.glSupportBGRA(gl) ? BACKGROUND_PATTERN_COL1 : swapRedBlue(BACKGROUND_PATTERN_COL1);

        for (i = 0; i < BACKGROUND_PATTERN_HEIGHT; ++i) {
            for (j = 0; j < BACKGROUND_PATTERN_WIDTH; ++j) {
                if (i < BACKGROUND_PATTERN_HEIGHT / 2) {
                    pixels[i * BACKGROUND_PATTERN_WIDTH + j] = (j < BACKGROUND_PATTERN_WIDTH / 2) ? col0 : col1;
                }
                else {
                    pixels[i * BACKGROUND_PATTERN_WIDTH + j] = (j < BACKGROUND_PATTERN_WIDTH / 2) ? col1 : col0;
                }
            }
        }

        patternTexture = svg.createTexture(gl, BACKGROUND_PATTERN_WIDTH, BACKGROUND_PATTERN_HEIGHT, pixels);
    }

    private void drawBackgroundTexture(@NonNull GL11 gl) {

        float u = (float)getWidth() / BACKGROUND_PATTERN_WIDTH;
        float v = (float)getHeight() / BACKGROUND_PATTERN_HEIGHT;

        gl.glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
        gl.glDisable(GL10.GL_BLEND);
        gl.glEnable(GL10.GL_TEXTURE_2D);
        gl.glBindTexture(GL10.GL_TEXTURE_2D, patternTexture);
        // simply put a quad, covering the whole view
        texturedRectangleDraw(gl, 0.0f, 0.0f, (float)getWidth(), (float)getHeight(), u, v);
    }

    private void drawSurfaceTexture(@NonNull GL11 gl) {

        float u, v;
        // get AmanithSVG surface dimensions
        float surfaceWidth = svgSurface.getWidth();
        float surfaceHeight = svgSurface.getHeight();
        float tx = Math.round(surfaceTranslation[0]);
        float ty = Math.round(surfaceTranslation[1]);
        
        if (svg.glSupportNPOT(gl)) {
            u = 1.0f;
            v = 1.0f;
        }
        else {
            // greater (or equal) power of two values
            float texWidth = svgSurface.getWidthPow2();
            float texHeight = svgSurface.getHeightPow2();
            u = (surfaceWidth - 0.5f) / texWidth;
            v = (surfaceHeight - 0.5f) / texHeight;
        }

        // enable per-pixel alpha blending, using surface texture as source
        gl.glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
        gl.glEnable(GL10.GL_BLEND);
        gl.glEnable(GL10.GL_TEXTURE_2D);
        gl.glBindTexture(GL10.GL_TEXTURE_2D, surfaceTexture);
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
    private void glesInit(@NonNull GL11 gl) {

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

    private @NonNull FloatBuffer glesFloatBuffer(@NonNull float[] arr) {

        ByteBuffer bb = ByteBuffer.allocateDirect(arr.length * 4);
        bb.order(ByteOrder.nativeOrder());
        FloatBuffer fb = bb.asFloatBuffer();
        fb.put(arr);
        fb.position(0);
        return fb;
    }

    // load projection matrix
    private void projectionLoad(@NonNull GL11 gl,
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

    private void texturedRectangleDraw(@NonNull GL11 gl,
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

    private void rectangleDraw(@NonNull GL11 gl,
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
                              AmanithSVG Log
    *****************************************************************/
    // clear AmanithSVG log buffer
    private void logClear() {

        if (svg.logClear(false) == SVGTError.None) {
            // keep track of the SVG filename within AmanithSVG log buffer
            svg.logInfo("Loading and parsing SVG file " + fullFileName);
        }
        else {
            Log.w(LOG_TAG, "Error setting AmanithSVG log buffer\n");
        }
    }

    // output AmanithSVG log content, using Android Studio Logcat window
    private void logOutput(String headerMessage,
                           boolean stopLogging) {

        // get AmanithSVG log content as a string
        String str = svg.getLog();

        // avoid to print empty log
        if (!str.isEmpty()) {
            //Log.d(LOG_TAG, headerMessage);
            Log.d(LOG_TAG, str);
        }

        if (stopLogging) {
            // make sure AmanithSVG no longer uses a log buffer (i.e. disable logging)
            if (svg.logClear(true) != SVGTError.None) {
                Log.w(LOG_TAG, "Error stopping AmanithSVG logging\n");
            }
        }
    }

    /*****************************************************************
                                 Viewer
    *****************************************************************/
    private void viewerInit(GL11 gl) {

        // initialize OpenGL ES
        glesInit(gl);

        // load the SVG document
        if (fullFileName != null) {
            svgDoc = svg.createDocumentFromFile(fullFileName);
        }
    }

    // destroy SVG resources allocated by the viewer
    private void viewerDestroy() {

        // destroy the SVG document
        if (svgDoc != null) {
            svgDoc.dispose();
            svgDoc = null;
        }

        // destroy the drawing surface
        if (svgSurface != null) {
            svgSurface.dispose();
            svgSurface = null;
        }
    }

    private void viewerDraw(@NonNull GL11 gl) {

        // clear OpenGL buffer
        gl.glClear(GL10.GL_COLOR_BUFFER_BIT);
        // draw pattern background
        drawBackgroundTexture(gl);
        // put AmanithSVG surface using per-pixel alpha blend
        if ((svgDoc != null) && (svgSurface != null)) {
            drawSurfaceTexture(gl);
        }
    }

    private void svgDraw(@NonNull GL11 gl,
                         int width,
                         int height) {

        if (svgSurface != null) {
            // if new desired surface dimensions are equal to current ones, simply exit
            // because it is useless to resize the surface (and destroy the GL texture)
            // with the same dimensions and then draw the same svg on it
            if ((width == svgSurface.getWidth()) && (height == svgSurface.getHeight())) {
                // this is the case, for example, when phone/device is rotated from portrait to landscape (or viceversa)
                return;
            }
            else {
                // destroy current surface texture
                if (surfaceTexture != 0) {
                    gl.glDeleteTextures(1, new int[] { surfaceTexture }, 0);
                    surfaceTexture = 0;
                }
                // resize AmanithSVG surface
                svgSurface.resize(width, height);
            }
        }
        else {
            // first time, we must create AmanithSVG surface
            svgSurface = svg.createSurface(width, height);
        }

        if ((svgDoc != null) && (svgSurface != null)) {
            // clear the drawing surface and draw the SVG document
            svgSurface.draw(svgDoc, SVGColor.Clear, SVGTRenderingQuality.Better);
            // create surface texture
            surfaceTexture = svg.createTexture(gl, svgSurface);
        }
    }

    private void viewerResize(@NonNull GL11 gl,
                              int width,
                              int height) {

        // create / resize the AmanithSVG surface such that it is centered within the OpenGL view
        if (svgDoc != null) {
            // calculate AmanithSVG surface dimensions
            Rect srfRect = surfaceDimensionsCalc(svgDoc, width, height);
            // create / resize AmanithSVG surface, then draw the loaded SVG document
            svgDraw(gl, srfRect.width(), srfRect.height());
            // center AmanithSVG surface within the OpenGL view
            surfaceTranslation[0] = (width - svgSurface.getWidth()) * 0.5f;
            surfaceTranslation[1] = (height - svgSurface.getHeight()) * 0.5f;
        }

        // set OpenGL viewport and projection
        gl.glViewport(0, 0, width, height);
        projectionLoad(gl, 0.0f, width, 0.0f, height);
    }

    boolean viewerMenuOption(int option) {

        boolean consumed;

        switch (option) {

            case SVGVIEWER_ABOUT_CMD:
                aboutDialog();
                consumed = true;
                break;

            default:
                consumed = false;
                break;
        }

        return consumed;
    }

    private void viewerTouchDown(float x, float y) {

        touchStartPoint.set(x, y);
        touchMode = TOUCH_MODE_DOWN;
    }

    private void viewerTouchUp(float x, float y) {

        touchMode = TOUCH_MODE_NONE;
    }

    private void viewerTouchMove(float x, float y) {

        if (touchMode == TOUCH_MODE_DOWN) {
            deltaTranslation(x - touchStartPoint.x, y - touchStartPoint.y);
            touchStartPoint.set(x, y);
        }
    }

    // interface for customizing the eglCreateContext and eglDestroyContext calls
    private static class ContextFactory implements GLSurfaceView.EGLContextFactory {

        // the view that created this context factory
        final private SvgViewerView view;

        ContextFactory(SvgViewerView myView) {

            // keep track of view
            view = myView;
        }

        public EGLContext createContext(@NonNull EGL10 egl, EGLDisplay display, EGLConfig eglConfig) {

            int[] contextAttribs = {
                // OpenGL ES 1.1
                EGL_CONTEXT_CLIENT_VERSION, 1,
                EGL10.EGL_NONE
            };

            return egl.eglCreateContext(display, eglConfig, EGL10.EGL_NO_CONTEXT, contextAttribs);
        }

        public void destroyContext(@NonNull EGL10 egl, EGLDisplay display, EGLContext context) {

            // destroy SVG resources allocated by the viewer
            // NB: because GLSurfaceView.Renderer has no 'onSurfaceDestroyed' event, this is the only
            // possible place to intercept rendering termination within the rendering thread
            view.viewerDestroy();

            egl.eglDestroyContext(display, context);
        }
    }

    // interface for choosing an EGLConfig configuration from a list of potential configurations
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

        private int findConfigAttrib(@NonNull EGL10 egl, EGLDisplay display, EGLConfig config, int attribute, int defaultValue) {

            return egl.eglGetConfigAttrib(display, config, attribute, tmpValue) ? tmpValue[0] : defaultValue;
        }

        private EGLConfig chooseConfigImpl(EGL10 egl, EGLDisplay display, @NonNull EGLConfig[] configs) {

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
        public EGLConfig chooseConfig(@NonNull EGL10 egl, EGLDisplay display) {

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
        private final int redSize;
        private final int greenSize;
        private final int blueSize;
        private final int alphaSize;
        private final int depthSize;
        private final int stencilSize;
        private int aaSamples;
        private final int[] tmpValue = new int[1];
        // we start with a minimum size of 4 bits for red/green/blue, but will perform actual matching in chooseConfig() below.
        private static final int EGL_OPENGL_ES_BIT = 1;
        private static final int[] configAttribs = {
            EGL10.EGL_RED_SIZE, 4,
            EGL10.EGL_GREEN_SIZE, 4,
            EGL10.EGL_BLUE_SIZE, 4,
            EGL10.EGL_RENDERABLE_TYPE, EGL_OPENGL_ES_BIT,
            EGL10.EGL_NONE
        };
    }

    private static class SvgViewerViewRenderer implements GLSurfaceView.Renderer {

        // the view that created this renderer
        final private SvgViewerView view;

        SvgViewerViewRenderer(SvgViewerView myView) {

            // keep track of view
            view = myView;
        }

        public void onSurfaceCreated(GL10 gl, EGLConfig config) {

            // clear AmanithSVG log (because we are going to keep track of possible errors
            // and warnings arising from the parsing/rendering of selected file)
            view.logClear();

            // initialize OpenGL ES and load the SVG document
            view.viewerInit((GL11)gl);
        }

        public void onDrawFrame(GL10 gl) {

            view.viewerDraw((GL11)gl);
        }

        public void onSurfaceChanged(GL10 gl, int width, int height) {

            // called when the surface changed size; in the detail, it is called after the surface
            // is created and whenever the OpenGL ES surface size changes, so it is a good
            // place to perform the real SVG draw and the relative GL texture creation
            view.viewerResize((GL11)gl, width, height);

            // output AmanithSVG log content, using Android Studio Logcat window
            view.logOutput("AmanithSVG log buffer", true);
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
        msg += "Vendor: " + AmanithSVG.svgtGetString(SVGTStringID.Vendor) + "\n";
        // version
        msg += "Version: " + AmanithSVG.svgtGetString(SVGTStringID.Version) + "\n";
        messageDialog("About AmanithSVG", msg);
    }

    @Override
    public boolean onTouchEvent(@NonNull MotionEvent event) {

        switch (event.getAction() & MotionEvent.ACTION_MASK) {

            case MotionEvent.ACTION_DOWN:
                viewerTouchDown(event.getX(), event.getY());
                performClick();
                break;

            case MotionEvent.ACTION_UP:
                viewerTouchUp(event.getX(), event.getY());
                break;

            case MotionEvent.ACTION_MOVE:
                viewerTouchMove(event.getX(), event.getY());
                break;
        }

        return true;
    }

    @Override
    public boolean performClick() {

        super.performClick();
        return true;
    }
}
