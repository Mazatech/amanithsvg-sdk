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
#include <X11/Xlib.h>
#include <X11/Xutil.h>
#include <X11/Xatom.h>
#include <X11/keysym.h>
#include <GL/glx.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <unistd.h>
#include <sys/stat.h>
#include "svg_player.h"

#define WINDOW_TITLE "SVG Player - Press F1 for about info"

// default window dimensions
#define INITIAL_WINDOW_WIDTH 600
#define INITIAL_WINDOW_HEIGHT 800

// mouse button state
#define MOUSE_BUTTON_NONE 0
#define MOUSE_BUTTON_LEFT 1
#define MOUSE_BUTTON_RIGHT 2

// background pattern (dimensions and ARGB colors)
#define BACKGROUND_PATTERN_WIDTH 32
#define BACKGROUND_PATTERN_HEIGHT 32
#define BACKGROUND_PATTERN_COL0 0xFF808080
#define BACKGROUND_PATTERN_COL1 0xFFC0C0C0

// GL_EXT_bgra extension
#if !defined(GL_BGRA_EXT)
    #define GL_BGRA_EXT 0x80E1
#endif
// GL_EXT_texture_rectangle extension
#if !defined(GL_TEXTURE_RECTANGLE_EXT)
    #define GL_TEXTURE_RECTANGLE_EXT 0x84F5
#endif

// GLX_SGI_swap_control
typedef int (*MY_PFNGLXSWAPINTERVALSGIPROC)(int interval);
// GLX_EXT_swap_control
typedef void (*MY_PFNGLXSWAPINTERVALEXTPROC)(Display* dpy,
                                             GLXDrawable drawable,
                                             int interval);

// X11 variables
Display* display = NULL;
Window window;
GC windowGfxContext;
Atom atom_DELWIN;
Atom atom_PROTOCOLS;
XFontStruct* fontInfo = NULL;

// OpenGL context and surface/drawable
GLXContext glContext = 0;
GLXWindow glWindow = 0;
// keep track of useful OpenGL extensions
SVGTboolean bgraSupport = SVGT_FALSE;
SVGTboolean textureRectangleSupport = SVGT_FALSE;
// OpenGL texture used to draw the pattern background
GLuint patternTexture = 0;
// OpenGL texture used to blit the AmanithSVG surface
GLuint surfaceTexture = 0;
SVGTfloat surfaceTranslation[2] = { 0.0f };

// SVG surface and document
SVGTHandle svgSurface = SVGT_INVALID_HANDLE;
SVGTHandle svgDoc = SVGT_INVALID_HANDLE;

// keep track if we are resizing the window
SVGTboolean resizing = SVGT_FALSE;
// force the view to redraw
SVGTboolean forceRedraw = SVGT_FALSE;

// keep track of mouse state
SVGTint oldMouseX = 0;
SVGTint oldMouseY = 0;
SVGTint mouseButtonState = MOUSE_BUTTON_NONE;

// info message
char infoMessage[2048] = { 0 };
SVGTboolean displayAbout = SVGT_FALSE;
SVGTboolean done = SVGT_FALSE;

/*****************************************************************
                       Utility functions
*****************************************************************/
static SimpleRect screenDimensionsGet(void) {

    // get the default screen associated with the display
    SVGTint screen = DefaultScreen(display);
    // get screen dimensions, in pixels
    SimpleRect result = {
        DisplayWidth(display, screen),
        DisplayHeight(display, screen)
    };
    return result;
}

static SVGTfloat screenDpiGet(void) {

    // get the default screen associated with the display
    SVGTint screen = DefaultScreen(display);
    // get screen dimensions, in pixels
    SVGTfloat widthPX = (SVGTfloat)DisplayWidth(display, screen);
    // get screen dimensions, in millimetres
    SVGTfloat widthMM = (SVGTfloat)DisplayWidthMM(display, screen);
    // calculate screen dpi (25.4 millimetres in an inch)
    return ((widthPX / widthMM) * 25.4f);
}

void windowFrameDimensionsGet(SimpleRect* dst) {

    Window root;
    SVGTint x, y;
    SVGTuint w, h, borderWidth, depth;

    if (XGetGeometry(display, window, &root, &x, &y, &w, &h, &borderWidth, &depth)) {
        dst->width = w;
        dst->height = h;
    }
    else {
        dst->width = 0;
        dst->height = 0;
    }
}

// return the power of two value greater (or equal) to a given value
static SVGTuint pow2Get(SVGTuint value) {

    SVGTuint v = 1;

    if (value >= (SVGTuint)((SVGTuint)1 << 31)) {
        return (SVGTuint)((SVGTuint)1 << 31);
    }
    
    while (v < value) {
        v <<= 1;
    }

    return v;
}

// swap R and B components (ARGB <--> ABGR and vice versa)
static __inline SVGTuint swapRedBlue(SVGTuint p) {
    // swap R <--> B
    SVGTuint ag = p & 0xFF00FF00;
    SVGTuint rb = p & 0x00FF00FF;
    SVGTuint r = rb >> 16;
    SVGTuint b = rb & 0xFF;
    return ag | (b << 16) | r;
}

// load projection matrix
static void projectionLoad(const SVGTfloat left,
                           const SVGTfloat right,
                           const SVGTfloat bottom,
                           const SVGTfloat top) {

    const SVGTfloat prjMatrix[16] = {
        2.0f / (right - left),            0.0f,                             0.0f, 0.0f,
        0.0f,                             2.0f / (top - bottom),            0.0f, 0.0f,
        0.0f,                             0.0f,                             1.0f, 0.0f,
        -(right + left) / (right - left), -(top + bottom) / (top - bottom), 0.0f, 1.0f
    };
    glMatrixMode(GL_PROJECTION);
    glLoadMatrixf(prjMatrix);
}

static void texturedRectangleDraw(const SVGTfloat x,
                                  const SVGTfloat y,
                                  const SVGTfloat width,
                                  const SVGTfloat height,
                                  const SVGTfloat u,
                                  const SVGTfloat v) {

    // 4 vertices
    const GLfloat xy[] = {
        x, y,
        x + width, y,
        x, y + height,
        x + width, y + height
    };
    const GLfloat uv[] = {
        0.0f, 0.0f,
        u, 0.0f,
        0.0f, v,
        u, v
    };

    glVertexPointer(2, GL_FLOAT, 0, xy);
    glEnableClientState(GL_VERTEX_ARRAY);
    glTexCoordPointer(2, GL_FLOAT, 0, uv);
    glEnableClientState(GL_TEXTURE_COORD_ARRAY);
    glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);
}

static void rectangleDraw(const SVGTfloat x,
                          const SVGTfloat y,
                          const SVGTfloat width,
                          const SVGTfloat height) {

    // 4 vertices
    const GLfloat xy[] = {
        x, y,
        x + width, y,
        x, y + height,
        x + width, y + height
    };

    glVertexPointer(2, GL_FLOAT, 0, xy);
    glEnableClientState(GL_VERTEX_ARRAY);
    glDisableClientState(GL_TEXTURE_COORD_ARRAY);
    glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);
}

// check if a string can be found within the given extensions
static SVGTboolean extensionFind(const char* string,
                                 const char* extensions) {

    char *position, *terminator;
    const char* start = extensions;

    while (1) {
        position = (char *)strstr(start, string);
        if (!position) {
            return SVGT_FALSE;
        }
        terminator = position + strlen(string);
        if ((position == start) || (*(position - 1) == ' ')) {
            if ((*terminator == ' ') || (*terminator == '\0')) {
                break;
            }
        }
        start = terminator;
    }

    return SVGT_TRUE;
}

static void stringDraw(const char* msg,
                       const SVGTint x,
                       const SVGTint y) {
    
    XDrawImageString(display, window, windowGfxContext, x, y, msg, strlen(msg));
}

static void textDraw(const char* msg,
                     SVGTint x,
                     SVGTint y) {

    SVGTint font_height = fontInfo->ascent + fontInfo->descent;
    SVGTint i = 0, j = 0;
    char str[255];

    y += font_height;   
    while (msg[j] != '\0') {
        if (msg[j] != '\n') {
            str[i] = msg[j];
            i++;
        }
        else {
            str[i] = '\0';
            stringDraw(str, x, y);
            y += font_height;
            i = 0;
        }
        j++;
    }
    if (i > 0) {
        stringDraw(str, x, y);
    }
}

static void aboutDialog(void) {

    char yearStr[64];
    time_t t = time(NULL);
    struct tm *ltm = localtime(&t);

    strcpy(infoMessage, "AmanithSVG - www.mazatech.com\n");
    strcat(infoMessage, "Copyright 2013-");
    strftime(yearStr, sizeof(yearStr), "%Y", ltm);
    strcat(infoMessage, yearStr);
    strcat(infoMessage, " by Mazatech Srl. All Rights Reserved.\n\n");

    strcat(infoMessage, "AmanithSVG driver informations:\n\n");
    // vendor
    strcat(infoMessage, "Vendor: ");
    strcat(infoMessage, (const char *)svgtGetString(SVGT_VENDOR));
    strcat(infoMessage, "\n");
    // version
    strcat(infoMessage, "Version: ");
    strcat(infoMessage, (const char *)svgtGetString(SVGT_VERSION));
    strcat(infoMessage, "\n\n");

    displayAbout = SVGT_TRUE;
}

static SVGTboolean fileStatusCheck(const char* absolutePath,
                                   const short fileMode) {

    char path[512];
    size_t l = strlen(absolutePath);

    // remove a possible trailer path delimiter at the end
    strcpy(path, absolutePath);
    if (path[l - 1] == '/') {
        path[l - 1] = 0;
    }

    if (access(path, 0) == 0) {
        struct stat status;
        stat(path, &status);
        return ((status.st_mode & fileMode) != 0) ? SVGT_TRUE : SVGT_FALSE;
    }
    else {
        return SVGT_FALSE;
    }
}

// check if the specified file exists
static SVGTboolean fileExists(const char* absoluteFileName) {

    return fileStatusCheck(absoluteFileName, S_IFREG);
}

/*****************************************************************
                            SVG player
*****************************************************************/
static void genPatternTexture(void) {

    // allocate pixels
    SVGTuint* pixels = (SVGTuint *)malloc(BACKGROUND_PATTERN_WIDTH * BACKGROUND_PATTERN_HEIGHT * 4);

    if (pixels != NULL) {
        SVGTint i, j;
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

        glGenTextures(1, &patternTexture);
        glDisable(GL_TEXTURE_RECTANGLE_EXT);
        glEnable(GL_TEXTURE_2D);
        glBindTexture(GL_TEXTURE_2D, patternTexture);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
        // select best format, in order to avoid swizzling
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, BACKGROUND_PATTERN_WIDTH, BACKGROUND_PATTERN_HEIGHT, 0, bgraSupport ? GL_BGRA_EXT : GL_RGBA, GL_UNSIGNED_BYTE, pixels);
        free(pixels);
    }
}

static void genSurfaceTexture(void) {

    SVGTint maxTextureSize;
    // get AmanithSVG surface dimensions and pixels pointer
    SVGTint surfaceWidth = svgtSurfaceWidth(svgSurface);
    SVGTint surfaceHeight = svgtSurfaceHeight(svgSurface);
    void* surfacePixels = (void*)svgtSurfacePixels(svgSurface);

    // get maximum texture size
    glGetIntegerv(GL_MAX_TEXTURE_SIZE, &maxTextureSize);
    if ((surfaceWidth <= maxTextureSize) && (surfaceHeight <= maxTextureSize)) {
        // generate OpenGL texture
        glGenTextures(1, &surfaceTexture);
        // rectangle texture support
        if (textureRectangleSupport) {
            // generate a 2D rectangular texture
            glEnable(GL_TEXTURE_RECTANGLE_EXT);
            glDisable(GL_TEXTURE_2D);
            glBindTexture(GL_TEXTURE_RECTANGLE_EXT, surfaceTexture);
            glTexParameteri(GL_TEXTURE_RECTANGLE_EXT, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
            glTexParameteri(GL_TEXTURE_RECTANGLE_EXT, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
            glTexParameteri(GL_TEXTURE_RECTANGLE_EXT, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
            glTexParameteri(GL_TEXTURE_RECTANGLE_EXT, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
            if (bgraSupport) {
                // direct copy
                glTexImage2D(GL_TEXTURE_RECTANGLE_EXT, 0, GL_RGBA, surfaceWidth, surfaceHeight, 0, GL_BGRA_EXT, GL_UNSIGNED_BYTE, surfacePixels);
            }
            else {
                // convert on the fly pixels from ARGB to ABGR
                SVGTuint i = (SVGTuint)(surfaceWidth * surfaceHeight);
                SVGTuint* pixels = (SVGTuint*)surfacePixels;
                for (; i != 0; --i) {
                    *pixels = swapRedBlue(*pixels);
                    pixels++;
                }
                glTexImage2D(GL_TEXTURE_RECTANGLE_EXT, 0, GL_RGBA, surfaceWidth, surfaceHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, surfacePixels);
            }

        }
        else {
            // greater (or equal) power of two values
            SVGTuint texWidth = pow2Get(surfaceWidth);
            SVGTuint texHeight = pow2Get(surfaceHeight);

            // power of two texture
            glDisable(GL_TEXTURE_RECTANGLE_EXT);
            glEnable(GL_TEXTURE_2D);
            glBindTexture(GL_TEXTURE_2D, surfaceTexture);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
            // upload pixels
            if (bgraSupport) {
                glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, texWidth, texHeight, 0, GL_BGRA_EXT, GL_UNSIGNED_BYTE, NULL);
                glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, surfaceWidth, surfaceHeight, GL_BGRA_EXT, GL_UNSIGNED_BYTE, surfacePixels);
            }
            else {
                glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, texWidth, texHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, NULL);   
                // convert on the fly pixels from ARGB to ABGR
                SVGTuint i = (SVGTuint)(surfaceWidth * surfaceHeight);
                SVGTuint* pixels = (SVGTuint *)surfacePixels;
                for (; i != 0; --i) {
                    *pixels = swapRedBlue(*pixels);
                    pixels++;
                }
                glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, surfaceWidth, surfaceHeight, GL_RGBA, GL_UNSIGNED_BYTE, surfacePixels);
            }
        }
    }
}

static void drawBackgroundTexture(void) {

    SVGTfloat u, v;
    SimpleRect frameRect;

    // get frame dimensions
    windowFrameDimensionsGet(&frameRect);
    u = (SVGTfloat)frameRect.width / BACKGROUND_PATTERN_WIDTH;
    v = (SVGTfloat)frameRect.height / BACKGROUND_PATTERN_HEIGHT;

    glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
    glDisable(GL_BLEND);
    glDisable(GL_TEXTURE_RECTANGLE_EXT);
    glEnable(GL_TEXTURE_2D);
    glBindTexture(GL_TEXTURE_2D, patternTexture);
    // simply put a quad, covering the whole window
    texturedRectangleDraw(0.0f, 0.0f, (SVGTfloat)frameRect.width, (SVGTfloat)frameRect.height, u, v);
}

static void drawSurfaceTexture(void) {

    // get AmanithSVG surface dimensions
    const SVGTfloat surfaceWidth = (SVGTfloat)svgtSurfaceWidth(svgSurface);
    const SVGTfloat surfaceHeight = (SVGTfloat)svgtSurfaceHeight(svgSurface);
    const SVGTfloat tx = (SVGTfloat)((SVGTint)(surfaceTranslation[0] + 0.5f));
    const SVGTfloat ty = (SVGTfloat)((SVGTint)(surfaceTranslation[1] + 0.5f));
    
    // enable per-pixel alpha blending, using surface texture as source
    glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
    glEnable(GL_BLEND);
    if (textureRectangleSupport) {
        glEnable(GL_TEXTURE_RECTANGLE_EXT);
        glDisable(GL_TEXTURE_2D);
        glBindTexture(GL_TEXTURE_RECTANGLE_EXT, surfaceTexture);
        // simply put a quad
        texturedRectangleDraw(tx, ty, surfaceWidth, surfaceHeight, surfaceWidth, surfaceHeight);
    }
    else {
        // greater (or equal) power of two values
        const SVGTfloat texWidth = (SVGTfloat)pow2Get(svgtSurfaceWidth(svgSurface));
        const SVGTfloat texHeight = (SVGTfloat)pow2Get(svgtSurfaceHeight(svgSurface));
        const SVGTfloat u = (surfaceWidth - 0.5f) / texWidth;
        const SVGTfloat v = (surfaceHeight - 0.5f) / texHeight;

        glEnable(GL_TEXTURE_2D);
        glDisable(GL_TEXTURE_RECTANGLE_EXT);
        glBindTexture(GL_TEXTURE_2D, surfaceTexture);
        // simply put a quad
        texturedRectangleDraw(tx, ty, surfaceWidth, surfaceHeight, u, v);
    }

    // draw a solid black frame surrounding the SVG document
    glColor4f(0.0f, 0.0f, 0.0f, 1.0f);
    glDisable(GL_BLEND);
    glDisable(GL_TEXTURE_RECTANGLE_EXT);
    glDisable(GL_TEXTURE_2D);
    rectangleDraw(tx, ty, surfaceWidth, 2.0f);
    rectangleDraw(tx, ty, 2.0f, surfaceHeight);
    rectangleDraw(tx, ty + surfaceHeight - 2.0f, surfaceWidth, 2.0f);
    rectangleDraw(tx + surfaceWidth - 2.0f, ty, 2.0f, surfaceHeight);
}

static void deleteTextures(void) {

    if (patternTexture != 0) {
        glDeleteTextures(1, &patternTexture);
        patternTexture = 0;
    }

    if (surfaceTexture != 0) {
        glDeleteTextures(1, &surfaceTexture);
        surfaceTexture = 0;
    }
}

// resize AmanithSVG surface to the given dimensions, then draw the loaded SVG document
static void svgDraw(const SVGTuint width,
                    const SVGTuint height) {

    if (svgSurface != SVGT_INVALID_HANDLE) {
        // destroy current surface texture
        if (surfaceTexture != 0) {
            glDeleteTextures(1, &surfaceTexture);
            surfaceTexture = 0;
        }
        // resize AmanithSVG surface
        svgtSurfaceResize(svgSurface, width, height);
    }
    else {
        // first time, we must create AmanithSVG surface
        svgSurface = svgtSurfaceCreate(width, height);
        // clear the drawing surface (full transparent white) at every svgtDocDraw call
        svgtClearColor(1.0f, 1.0f, 1.0f, 0.0f);
        svgtClearPerform(SVGT_TRUE);
    }
    // draw the SVG document (upon AmanithSVG surface)
    svgtDocDraw(svgDoc, svgSurface, SVGT_RENDERING_QUALITY_BETTER);
    // create surface texture
    genSurfaceTexture();
}

// draw pattern background and blit AmanithSVG surface (per-pixel alpha blend)
static void sceneDraw(void) {

    // clear OpenGL buffer
    glClear(GL_COLOR_BUFFER_BIT);
    // draw pattern background
    drawBackgroundTexture();
    // put AmanithSVG surface using per-pixel alpha blend
    if ((svgDoc != SVGT_INVALID_HANDLE) && (svgSurface != SVGT_INVALID_HANDLE)) {
        drawSurfaceTexture();
    }
}

static void sceneResize(const SVGTuint width,
                        const SVGTuint height) {

    resizing = SVGT_TRUE;

    // create / resize the AmanithSVG surface such that it is centered within the OpenGL view
    if (svgDoc != SVGT_INVALID_HANDLE) {
        // calculate AmanithSVG surface dimensions
        SimpleRect srfRect = surfaceDimensionsCalc(svgDoc, width, height);
        // create / resize AmanithSVG surface, then draw the loaded SVG document
        svgDraw(srfRect.width, srfRect.height);
        // center AmanithSVG surface within the OpenGL view
        surfaceTranslation[0] = (SVGTfloat)((SVGTint)width - (SVGTint)svgtSurfaceWidth(svgSurface)) * 0.5f;
        surfaceTranslation[1] = (SVGTfloat)((SVGTint)height - (SVGTint)svgtSurfaceHeight(svgSurface)) * 0.5f;
    }
    // set OpenGL viewport and projection
    glViewport(0, 0, width, height);
    projectionLoad(0.0f, width, 0.0f, height);

    // we have finished with the resizing, now we can update the window content (i.e. draw)
    resizing = SVGT_FALSE;
    forceRedraw = SVGT_TRUE;
}

// initialize the SVG player, loading the given SVG file
static SVGTboolean playerInit(const char* fileName) {

    SVGTboolean ok = SVGT_FALSE;
    // get screen dimensions
    SimpleRect screenRect = screenDimensionsGet();

    // initialize AmanithSVG library (actually just the first call performs the real initialization)
    if (svgtInit(screenRect.width, screenRect.height, screenDpiGet()) == SVGT_NO_ERROR) {
        // destroy a previously loaded document, if any
        if (svgDoc != SVGT_INVALID_HANDLE) {
            svgtDocDestroy(svgDoc);
        }
        // load a new SVG file
        svgDoc = loadSvg(fileName);
        ok = (svgDoc != SVGT_INVALID_HANDLE) ? SVGT_TRUE : SVGT_FALSE;
    }
    // we have finished
    mouseButtonState = MOUSE_BUTTON_NONE;
    return ok;
}

// destroy SVG resources allocated by the player
static void playerDestroy(void) {

    // destroy the SVG document
    svgtDocDestroy(svgDoc);
    // destroy the drawing surface
    svgtSurfaceDestroy(svgSurface);
    // deinitialize AmanithSVG library
    svgtDone();
    // destroy used textures
    deleteTextures();
}

/*****************************************************************
                         Event handlers
*****************************************************************/
static void deltaTranslation(const SVGTfloat deltaX,
                             const SVGTfloat deltaY) {

    surfaceTranslation[0] += deltaX;
    surfaceTranslation[1] -= deltaY;
    forceRedraw = SVGT_TRUE;
}

static void mouseLeftButtonDown(const SVGTint x,
                                const SVGTint y) {

    oldMouseX = x;
    oldMouseY = y;
    mouseButtonState = MOUSE_BUTTON_LEFT;
}

static void mouseLeftButtonUp(const SVGTint x,
                              const SVGTint y) {

    (void)x;
    (void)y;
    mouseButtonState = MOUSE_BUTTON_NONE;
}

static void mouseMove(const SVGTint x,
                      const SVGTint y) {

    if (mouseButtonState == MOUSE_BUTTON_LEFT) {
        // update translation
        deltaTranslation((SVGTfloat)(x - oldMouseX), (SVGTfloat)(y - oldMouseY));
        oldMouseX = x;
        oldMouseY = y;
    }
}

/*****************************************************************
                       Windowing system
*****************************************************************/
static void windowResize(const SimpleRect* desiredRect) {

    // get screen dimensions
    SimpleRect screenRect = screenDimensionsGet();
    // clamp window dimensions against screen bounds
    SVGTint w = MIN(desiredRect->width, screenRect.width);
    SVGTint h = MIN(desiredRect->height, screenRect.height);
    // center window in the middle of the screen
    SVGTint x = ((SVGTint)screenRect.width - w) / 2;
    SVGTint y = ((SVGTint)screenRect.height - h) / 2;

    // move / resize the window
    XMoveResizeWindow(display, window, x, MAX(y, 32), (SVGTuint)w, (SVGTuint)h);
}

static void windowFontLoad(void) {

    char fontName[] = "9x15";

    // load font and get font information structure
    if ((fontInfo = XLoadQueryFont(display, fontName)) == NULL) {
        fprintf(stderr, "Cannot open %s font.\n", fontName);
    }
}

static SVGTboolean windowCreate(const char* title,
                                const SVGTuint width,
                                const SVGTuint height) {

    SVGTint screen, screenWidth, screenHeight, screenDepth;
    XSetWindowAttributes windowAttributes;
    XSizeHints windowSizeHints;
    XVisualInfo* visualInfo;
    char* glExtensions;
    MY_PFNGLXSWAPINTERVALSGIPROC sgiSwapInterval;
    MY_PFNGLXSWAPINTERVALEXTPROC extSwapInterval;
    // OpenGL surface configuration
    GLXFBConfig* fbConfigs;
    SVGTint fbConfigsCount = 0;
    SVGTint glAttributes[] = {
        GLX_DRAWABLE_TYPE, GLX_WINDOW_BIT,
        GLX_RENDER_TYPE, GLX_RGBA_BIT,
        // request a double-buffered color buffer with the maximum number of bits per component
        GLX_DOUBLEBUFFER, True,
        GLX_RED_SIZE, 1,
        GLX_GREEN_SIZE, 1,
        GLX_BLUE_SIZE, 1,
        GLX_ALPHA_SIZE, 1,
        GLX_SAMPLE_BUFFERS, 0,
        None
    };

    // open a display on the current root window
    display = XOpenDisplay(NULL);
    if (display == NULL) {
        fprintf(stderr, "Unable to open display.\n");
        return SVGT_FALSE;
    }

    // get the default screen associated with the previously opened display
    screen = DefaultScreen(display);
    // get screen bitdepth
    screenDepth = DefaultDepth(display, screen);
    
    // run only on a 32bpp display
    if ((screenDepth != 24) && (screenDepth != 32)) {
        fprintf(stderr, "Cannot find 32bit pixel format on the current display.\n");
        XCloseDisplay(display);
        return SVGT_FALSE;
    }

    // get screen dimensions
    screenWidth = DisplayWidth(display, screen);
    screenHeight = DisplayHeight(display, screen);

    // request a suitable framebuffer configuration
    fbConfigs = glXChooseFBConfig(display, screen, glAttributes, &fbConfigsCount);
    if ((!fbConfigs) || (fbConfigsCount < 1)) {
        fprintf(stderr, "Cannot find a suitable framebuffer configuration.\n");
        return SVGT_FALSE;
    }

    // create an X colormap and window with a visual matching the first returned framebuffer config
    visualInfo = glXGetVisualFromFBConfig(display, fbConfigs[0]);
    windowAttributes.colormap = XCreateColormap(display, RootWindow(display, visualInfo->screen), visualInfo->visual, AllocNone);

    // initialize window's attribute structure
    windowAttributes.border_pixel = BlackPixel(display, screen);
    windowAttributes.background_pixel = 0xCCCCCCCC;
    windowAttributes.backing_store = NotUseful;

    // create the window centered on the screen
    window = XCreateWindow(display, RootWindow(display, visualInfo->screen), (screenWidth - width) / 2, (screenHeight - height) / 2, width, height, 0, visualInfo->depth, InputOutput, visualInfo->visual, CWColormap | CWBorderPixel | CWBackPixel | CWBackingStore, &windowAttributes);
    if (window == None) {
        fprintf(stderr, "Unable to create the window.\n");
        XCloseDisplay(display);
        return SVGT_FALSE;
    }
    // set the window's name
    XStoreName(display, window, title);
    // tell the server to report mouse and key-related events
    XSelectInput(display, window, KeyPressMask | KeyReleaseMask | ButtonPressMask | Button1MotionMask | Button2MotionMask | Button3MotionMask | StructureNotifyMask | ExposureMask);
    // initialize window's sizehint definition structure
    windowSizeHints.flags = PPosition | PMinSize | PMaxSize;
    windowSizeHints.x = 0;
    windowSizeHints.y = 0;
    windowSizeHints.min_width = 1;
    // clamp window dimensions according to the maximum surface dimension supported by the AmanithSVG
    windowSizeHints.max_width = svgtSurfaceMaxDimension();
    if (screenWidth < windowSizeHints.max_width) {
        windowSizeHints.max_width = screenWidth;
    }
    windowSizeHints.min_height = 1;
    windowSizeHints.max_height = windowSizeHints.max_width;
    if (screenHeight < windowSizeHints.max_height) {
        windowSizeHints.max_height = screenHeight;
    }
    // set the window's sizehint
    XSetWMNormalHints(display, window, &windowSizeHints);
    // clear the window
    XClearWindow(display, window);

    // create a GLX context for OpenGL rendering
    glContext = glXCreateNewContext(display, fbConfigs[0], GLX_RGBA_TYPE, NULL, True);
    if (!glContext) {
        fprintf(stderr, "Unable to create the GLX context.\n");
        XDestroyWindow(display, window);
        XCloseDisplay(display);
        return SVGT_FALSE;
    }
    // create a GLX window to associate the frame buffer configuration with the created X window
    glWindow = glXCreateWindow(display, fbConfigs[0], window, NULL);
    // bind the GLX context to the Window
    glXMakeContextCurrent(display, glWindow, glWindow, glContext);

    // GLX_EXT_swap_control
    extSwapInterval = (MY_PFNGLXSWAPINTERVALEXTPROC)glXGetProcAddressARB((const GLubyte *)"glXSwapIntervalEXT");
    if (extSwapInterval) {
        extSwapInterval(display, glWindow, 0);
    }
    else {
        // GLX_SGI_swap_control
        sgiSwapInterval = (MY_PFNGLXSWAPINTERVALSGIPROC)glXGetProcAddressARB((const GLubyte *)"glXSwapIntervalSGI");
        if (sgiSwapInterval) {
            sgiSwapInterval(0);
        }
    }

    // get the list of supported OpenGL extensions
    glExtensions = (char*)glGetString(GL_EXTENSIONS);
    // check GL_EXT_bgra extension support
    bgraSupport = extensionFind("GL_EXT_bgra", glExtensions);
    // check GL_EXT_texture_rectangle extension support
    textureRectangleSupport = extensionFind("GL_EXT_texture_rectangle", glExtensions) | extensionFind("GL_ARB_texture_rectangle", glExtensions);

    // put the window on top of the others
    XMapRaised(display, window);
    // clear event queue
    XFlush(display);
    
    // get the default graphic context
    windowGfxContext = DefaultGC(display, visualInfo->screen);
    XSetForeground(display, windowGfxContext, BlackPixel(display, screen));
    XSetBackground(display, windowGfxContext, 0xCCCCCCCC);
    XFree(visualInfo);

    atom_DELWIN = XInternAtom(display, "WM_DELETE_WINDOW", False);
    atom_PROTOCOLS = XInternAtom(display, "WM_PROTOCOLS", False);
    XChangeProperty(display, window, atom_PROTOCOLS, XA_ATOM, 32, PropModeReplace, (unsigned char *)&atom_DELWIN, 1);

    // set basic OpenGL states
    glDisable(GL_LIGHTING);
    glShadeModel(GL_FLAT);
    glHint(GL_PERSPECTIVE_CORRECTION_HINT, GL_NICEST);
    glDisable(GL_CULL_FACE);
    glDisable(GL_ALPHA_TEST);
    glDisable(GL_SCISSOR_TEST);
    glDisable(GL_DEPTH_TEST);
    glDisable(GL_STENCIL_TEST);
    glDisable(GL_BLEND);
    glDepthMask(GL_FALSE);
    glColorMask(GL_TRUE, GL_TRUE, GL_TRUE, GL_TRUE);
    glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
    glMatrixMode(GL_PROJECTION);
    glLoadIdentity();
    glMatrixMode(GL_MODELVIEW);
    glLoadIdentity();
    glClearColor(0.0f, 0.0f, 0.0f, 1.0f);

    // generate pattern texture, used to draw the background
    genPatternTexture();
    // load font
    windowFontLoad();

    resizing = SVGT_TRUE;
    forceRedraw = SVGT_FALSE;
    return SVGT_TRUE;
}

static void windowDestroy(void) {

    // delete used font
    XFreeFont(display, fontInfo);
    // unbind OpenGL context / surface
    glXMakeContextCurrent(display, 0, 0, 0);
    // destroy OpenGL surface/drawable
    glXDestroyWindow(display, glWindow);
    // destroy OpenGL context
    glXDestroyContext(display, glContext);
    // close the window
    XDestroyWindow(display, window);
    // close the display
    XCloseDisplay(display);
}

static void windowBuffersSwap(void) {

    glXSwapBuffers(display, glWindow);
}

static void processKeyPressure(KeySym key) {

    // ESC
    if (key == XK_Escape) {
        done = SVGT_TRUE;
    }
    else
    // F1
    if (key == XK_F1) {
        if (!displayAbout) {
            XClearWindow(display, window);
            aboutDialog();
        }
        else {
            displayAbout = SVGT_FALSE;
            forceRedraw = SVGT_TRUE;
        }
    }
    else {
        if (displayAbout) {
            displayAbout = SVGT_FALSE;
            forceRedraw = SVGT_TRUE;
        }
    }
}

static void processEvent(XEvent *ev) {

    switch (ev->type) {
        
        case KeyPress:
            processKeyPressure(XLookupKeysym(&ev->xkey, 0));
            break;

        case ButtonPress:
            if (displayAbout) {
                displayAbout = SVGT_FALSE;
                forceRedraw = SVGT_TRUE;
            }
            if (ev->xbutton.button == Button1) {
                mouseLeftButtonDown(ev->xbutton.x, ev->xbutton.y);
            }
            break;
                
        case ButtonRelease:
            if (displayAbout) {
                displayAbout = SVGT_FALSE;
                forceRedraw = SVGT_TRUE;
            }
            if (ev->xbutton.button == Button1) {
                mouseLeftButtonUp(ev->xbutton.x, ev->xbutton.y);
            }
            break;
                
        case MotionNotify:
            mouseMove(ev->xmotion.x, ev->xmotion.y);
            break;
                
        case ClientMessage:
            if ((((XClientMessageEvent *)ev)->message_type == atom_PROTOCOLS) &&
                (((XClientMessageEvent *)ev)->data.l[0] == (long)atom_DELWIN)) {
                done = SVGT_TRUE;
            }
            break;

        default:
            break;
    }
}

static void processEvents(void) {

    XEvent event;
    XWindowAttributes windowAttributes;

    if (XCheckWindowEvent(display, window, ExposureMask, &event)) {
        // resizing a window may generate more than one event, and we are only interested into the final size of the window
        while (XCheckWindowEvent(display, window, ExposureMask, &event));
        // get window dimensions
        XGetWindowAttributes(display, window, &windowAttributes);
        // resize AmanithSVG surface, in order to match the new window dimensions
        displayAbout = SVGT_FALSE;
        sceneResize(windowAttributes.width, windowAttributes.height);
    }
    else {
        SVGTint i = XPending(display);
    
        if (i > 0) {
            // get the next event in queue
            XNextEvent(display, &event);
            processEvent(&event);
            i--;
            for (; i > 0; --i) {
                XNextEvent(display, &event);
            }
        }
    }
}

int main(int argc,
         char *argv[]) {

    if (argc < 2) {
        fprintf(stderr, "\nUsage is: %s <svg file>\n\n", argv[0]);
        return EXIT_FAILURE;
    }
    // check input SVG file
    if (!fileExists(argv[1])) {
        fprintf(stderr, "\nInput file %s does not exist or it is not readable!\n\n", argv[1]);
        return EXIT_FAILURE;
    }

    // create the window
    if (!windowCreate(WINDOW_TITLE, INITIAL_WINDOW_WIDTH, INITIAL_WINDOW_HEIGHT)) {
        return EXIT_FAILURE;
    }

    // load the SVG file
    if (playerInit(argv[1])) {
        SimpleRect screenRect = screenDimensionsGet();
        // calculate AmanithSVG surface dimensions
        SimpleRect desiredRect = surfaceDimensionsCalc(svgDoc, screenRect.width, screenRect.height);
        // resize the window in order to match the desired surface dimensions
        windowResize(&desiredRect);
    }
    else {
        windowDestroy();
        return EXIT_FAILURE;
    }

    // main loop
    while (!done) {
        // dispatch events, taking care to process keyboard and mouse
        processEvents();
        if (displayAbout) {
            textDraw(infoMessage, 10, 10);
        }
        else {
            if (forceRedraw && (!resizing)) {
                sceneDraw();
                windowBuffersSwap();
                forceRedraw = SVGT_FALSE;
            }
        }
    }

    // destroy SVG resources allocated by the player
    playerDestroy();
    // close window
    windowDestroy();
    return EXIT_SUCCESS;
}
