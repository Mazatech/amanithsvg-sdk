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
#include <windows.h>
#include <stdio.h>
#include <time.h>
#include "svg_viewer.h"
#include "resource.h"
#if defined(_DEBUG)
    // uncomment it, if you want to enable Visual Leak Detector
    // #include <vld.h>
#endif

#define CLASS_NAME "SVGViewer"
#define WINDOW_TITLE "SVG Viewer"
// send a string to the debugger for display (Visual Studio output window)
#define DEBUG_LOG OutputDebugString

// default window dimensions
#define INITIAL_WINDOW_WIDTH 600
#define INITIAL_WINDOW_HEIGHT 800

// mouse button state
#define MOUSE_BUTTON_NONE 0
#define MOUSE_BUTTON_LEFT 1
#define MOUSE_BUTTON_RIGHT 2

typedef struct {
    // the GDI bitmap handle
    HBITMAP handle;
    // the device context associated with the bitmap (DIB section)
    HDC dc;
    // bitmap width, in pixels
    SVGTuint width;
    // bitmap height, in pixels
    SVGTuint height;
    // pointer to pixels
    SVGTuint* pixels;
} Bitmap;

// Windows variables
static HDC deviceContext = NULL;
static HWND nativeWindow = NULL;
static HINSTANCE applicationInstance;

// GDI back buffer
static HDC backBufferContext = NULL;
static HBITMAP backBufferBmp = NULL;
// GDI bitmap relative to AmanithSVG surface
static Bitmap surfaceBitmap = { NULL, NULL, 0, 0, NULL };
// GDI pattern bitmap
static Bitmap patternBitmap = { NULL, NULL, 0, 0, NULL };
// GDI pattern brush, used to draw the background
static HBRUSH patternBrush = NULL;
// GDI pen, used to draw SVG document frame
static HPEN blackPen = NULL;

static SVGTfloat surfaceTranslation[2] = { 0.0f };
// SVG surface and document
static SVGTHandle svgSurface = SVGT_INVALID_HANDLE;
static SVGTHandle svgDoc = SVGT_INVALID_HANDLE;
// AmanithSVG log buffer
static char* logBuffer = NULL;

// pressed keys buffer
static SVGTboolean keysPressed[256] = { SVGT_FALSE };
// keep track if we are resizing the window
static SVGTboolean resizing = SVGT_FALSE;
// force the view to redraw
static SVGTboolean forceRedraw = SVGT_FALSE;
// keep track of mouse state
static SVGTint oldMouseX = 0;
static SVGTint oldMouseY = 0;
static SVGTint mouseButtonState = MOUSE_BUTTON_NONE;

// ---------------------------------------------------------------
//              External resources (fonts and images)
// ---------------------------------------------------------------
#define SVG_RESOURCES_COUNT 5
static SVGExternalResource resources[SVG_RESOURCES_COUNT] = {
    // filename,                    type,                    buffer, size/id,                 hints
    { "bebas_neue_regular.ttf",     SVGT_RESOURCE_TYPE_FONT, NULL, IDR_FONT_BEBAS_NEUE,     SVGT_RESOURCE_HINT_DEFAULT_FANTASY_FONT },
    { "dancing_script_regular.ttf", SVGT_RESOURCE_TYPE_FONT, NULL, IDR_FONT_DANCING_SCRIPT, SVGT_RESOURCE_HINT_DEFAULT_CURSIVE_FONT },
    { "noto_mono_regular.ttf",      SVGT_RESOURCE_TYPE_FONT, NULL, IDR_FONT_NOTO_MONO,      SVGT_RESOURCE_HINT_DEFAULT_MONOSPACE_FONT |
                                                                                            SVGT_RESOURCE_HINT_DEFAULT_UI_MONOSPACE_FONT },
    { "noto_sans_regular.ttf",      SVGT_RESOURCE_TYPE_FONT, NULL, IDR_FONT_NOTO_SANS,      SVGT_RESOURCE_HINT_DEFAULT_SANS_SERIF_FONT |
                                                                                            SVGT_RESOURCE_HINT_DEFAULT_UI_SANS_SERIF_FONT |
                                                                                            SVGT_RESOURCE_HINT_DEFAULT_SYSTEM_UI_FONT |
                                                                                            SVGT_RESOURCE_HINT_DEFAULT_UI_ROUNDED_FONT },
    { "noto_serif_regular.ttf",     SVGT_RESOURCE_TYPE_FONT, NULL, IDR_FONT_NOTO_SERIF,     SVGT_RESOURCE_HINT_DEFAULT_SERIF_FONT |
                                                                                            SVGT_RESOURCE_HINT_DEFAULT_UI_SERIF_FONT }
};

// ---------------------------------------------------------------
//                     Utility functions
// ---------------------------------------------------------------
static SimpleRect screenDimensionsGet(void) {

    SimpleRect result = {
        (SVGTuint)GetSystemMetrics(SM_CXSCREEN),
        (SVGTuint)GetSystemMetrics(SM_CYSCREEN)
    };
    return result;
}

static SVGTfloat screenDpiGet(void) {

    return (SVGTfloat)GetDeviceCaps(deviceContext, LOGPIXELSX);
}

static void messageDialog(const char* title,
                          const char* message) {

    MessageBox(NULL, message, title, MB_OK);
}

static void aboutDialog(void) {

    char msg[2048];
    char yearStr[64];
    time_t t = time(NULL);
    struct tm *ltm = localtime(&t);

    strcpy(msg, "AmanithSVG - www.mazatech.com\n");
    strcat(msg, "Copyright 2013-");
    strftime(yearStr, sizeof(yearStr), "%Y", ltm);
    strcat(msg, yearStr);
    strcat(msg, " by Mazatech Srl. All Rights Reserved.\n\n");

    strcat(msg, "AmanithSVG driver informations:\n\n");
    // vendor
    strcat(msg, "Vendor: ");
    strcat(msg, (const char *)svgtGetString(SVGT_VENDOR));
    strcat(msg, "\n");
    // version
    strcat(msg, "Version: ");
    strcat(msg, (const char *)svgtGetString(SVGT_VERSION));
    strcat(msg, "\n\n");
    messageDialog("About AmanithSVG", msg);
}

// ---------------------------------------------------------------
//                    AmanithSVG initialization
// ---------------------------------------------------------------
static void amanithsvgResourceAdd(SVGExternalResource* resource) {

    SVGTint rcId = (SVGTint)resource->bufferSize;
    HRSRC res = FindResource(applicationInstance, MAKEINTRESOURCE(rcId), RT_RCDATA);

    if (res) {
        // retrieve the resource handle
        HGLOBAL resHandle = LoadResource(NULL, res);
        if (resHandle) {
            // retrieve the pointer to the specified resource in memory
            SVGTubyte* buffer = LockResource(resHandle);
            // retrieve the size, in bytes, of the specified resource
            size_t bufferSize = (size_t)SizeofResource(NULL, res);

            if ((buffer != NULL) && (bufferSize > 0)) {
                // fill the result structure
                resource->buffer = buffer;
                resource->bufferSize = bufferSize;
                // provide AmanithSVG with the resource
                (void)svgtResourceSet(resource->fileName, buffer, (SVGTuint)bufferSize, resource->type, resource->hints);
            }
        }
    }
}

// make external resources (fonts/images) available to AmanithSVG
static void amanithsvgResourcesLoad(void) {

    SVGTuint i;

    // load external resources and provide them to AmanithSVG
    for (i = 0U; i < SVG_RESOURCES_COUNT; ++i) {
        amanithsvgResourceAdd(&resources[i]);
    }
}

// release in-memory external resources (fonts/images) provided to AmanithSVG at initialization time
static void amanithsvgResourcesRelease(void) {

    // "The pointer returned by LockResource is valid until the module containing the resource is unloaded.
    // It is not necessary to unlock resources because the system automatically deletes them when the
    // process that created them terminates"
    //
    // see https://learn.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-lockresource
}

// initialize AmanithSVG and load external resources
static SVGTboolean amanithsvgInit(void) {

    SVGTErrorCode err;
    // get screen dimensions
    SimpleRect screenRect = screenDimensionsGet();

    // initialize AmanithSVG
    if ((err = svgtInit(screenRect.width, screenRect.height, screenDpiGet())) == SVGT_NO_ERROR) {
        // set curves quality (used by AmanithSVG geometric kernel to approximate curves with straight
        // line segments (flattening); valid range is [1; 100], where 100 represents the best quality
        (void)svgtConfigSet(SVGT_CONFIG_CURVES_QUALITY, AMANITHSVG_CURVES_QUALITY);
        // specify the system/user-agent language; this setting will affect the conditional rendering
        // of <switch> elements and elements with 'systemLanguage' attribute specified
        (void)svgtLanguageSet(AMANITHSVG_USER_AGENT_LANGUAGE);
        // make external resources available to AmanithSVG; NB: all resources must be specified in
        // advance before to call rendering-related functions, which are by definition tied to a thread
        amanithsvgResourcesLoad();
    }

    return (err == SVGT_NO_ERROR) ? SVGT_TRUE : SVGT_FALSE;
}

// release SVG resources allocated by the viewer
static void amanithsvgRelease(void) {

    if (svgDoc != SVGT_INVALID_HANDLE) {
        // destroy the SVG document
        svgtDocDestroy(svgDoc);
    }
    if (svgSurface != SVGT_INVALID_HANDLE) {
        // destroy the drawing surface
        svgtSurfaceDestroy(svgSurface);
    }
    // terminate AmanithSVG library, freeing its all allocated resources
    // NB: after this call, all AmanithSVG rendering functions will have no effect
    svgtDone();

    // release in-memory external resources (fonts/images)
    // provided to AmanithSVG at initialization time
    amanithsvgResourcesRelease();
}

// ---------------------------------------------------------------
//                        AmanithSVG log
// ---------------------------------------------------------------

// append the message to the AmanithSVG log buffer
static void logPrint(const char* message,
                     SVGTLogLevel level) {

    (void)svgtLogPrint(message, level);
}

// append an informational message to the AmanithSVG log buffer
static void logInfo(const char* message) {

    // push the message to AmanithSVG log buffer
    logPrint(message, SVGT_LOG_LEVEL_INFO);
}

static void logInit(const char* fullFileName) {

    if ((logBuffer = calloc(AMANITHSVG_LOG_BUFFER_SIZE, sizeof(char))) != NULL) {
        // enable all log levels
        if (svgtLogBufferSet(logBuffer, AMANITHSVG_LOG_BUFFER_SIZE, SVGT_LOG_LEVEL_ALL) == SVGT_NO_ERROR) {
            char fname[512] = { '\0' };
            char msg[1024] = { '\0' };
            // extract the filename only from the full path
            extractFileName(fname, fullFileName, SVGT_TRUE);
            // keep track of the SVG filename within AmanithSVG log buffer
            sprintf(msg, "Loading and parsing SVG file %s", fname);
            logInfo(msg);
        }
    }
    else {
        DEBUG_LOG("Error allocating AmanithSVG log buffer");
    }
}

static void logDestroy(void) {

    // make sure AmanithSVG no longer uses a log buffer (i.e. disable logging)
    (void)svgtLogBufferSet(NULL, 0U, 0U);
    // release allocated memory
    if (logBuffer != NULL) {
        free(logBuffer);
        logBuffer = NULL;
    }
}

// output AmanithSVG log content, using the Apple unified logging system
static void logOutput(void) {

    if (logBuffer != NULL) {

        SVGTuint info[4] = { 0U, 0U, 0U, 0U };
        SVGTErrorCode err = svgtLogBufferInfo(info);

        // info[2] = current length, in characters (i.e. the total number of
        // characters written, included the trailing '\0')
        if ((err == SVGT_NO_ERROR) && (info[2] > 1U)) {
            DEBUG_LOG(logBuffer);
        }
    }
}

// ---------------------------------------------------------------
//                   GDI bitmaps and brushes
// ---------------------------------------------------------------
static SVGTboolean gdiBitmapCreate(Bitmap* dst,
                                   const SVGTuint width,
                                   const SVGTuint height) {

    SVGTboolean ok = SVGT_FALSE;

    // initialize destination structure
    dst->handle = 0;
    dst->dc = 0;
    dst->width = 0;
    dst->height = 0;
    dst->pixels = NULL;

    if ((width > 0) && (height > 0)) {

        void* pixels;
        HBITMAP handle;
        // initialize store buffer
        char buffer[sizeof(BITMAPINFO) + 16] = { 0 };
        BITMAPINFO* info = (BITMAPINFO *)&buffer;
        HDC dc = CreateCompatibleDC(deviceContext);

        // generate header
        info->bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
        info->bmiHeader.biPlanes = 1;
        info->bmiHeader.biCompression = BI_BITFIELDS;
        info->bmiHeader.biBitCount = 32;
        ((unsigned long *)info->bmiColors)[0] = 0x00FF0000;
        ((unsigned long *)info->bmiColors)[1] = 0x0000FF00;
        ((unsigned long *)info->bmiColors)[2] = 0x000000FF;
        info->bmiHeader.biWidth = (LONG)width;
        info->bmiHeader.biHeight = (LONG)height;
        // create the GDI bitmap (32bpp)
        handle = CreateDIBSection(dc, info, DIB_RGB_COLORS, &pixels, NULL, 0);
        if (handle && pixels) {
            SelectObject(dc, handle);
            // fill the destination structure
            dst->handle = handle;
            dst->dc = dc;
            dst->width = width;
            dst->height = height;
            dst->pixels = (SVGTuint*)pixels;
            // we have finished
            ok = SVGT_TRUE;
        }
    }

    return ok;
}

// destroy the given GDI bitmap
static void gdiBitmapDestroy(Bitmap* src) {

    if ((src != NULL) && src->handle) {
        DeleteDC(src->dc);
        DeleteObject(src->handle);
        src->handle = 0;
        src->dc = 0;
        src->width = 0;
        src->height = 0;
        src->pixels = NULL;
    }
}

// create the pattern brush used to draw the window background
static void gdiPatternBrushCreate(void) {

    SVGTuint i, j;

    // create background bitmap
    gdiBitmapCreate(&patternBitmap, BACKGROUND_PATTERN_WIDTH, BACKGROUND_PATTERN_HEIGHT);

    for (i = 0; i < BACKGROUND_PATTERN_HEIGHT; ++i) {
        for (j = 0; j < BACKGROUND_PATTERN_WIDTH; ++j) {
            if (i < BACKGROUND_PATTERN_HEIGHT / 2) {
                patternBitmap.pixels[i * BACKGROUND_PATTERN_WIDTH + j] = (j < BACKGROUND_PATTERN_WIDTH / 2) ? BACKGROUND_PATTERN_COL0 : BACKGROUND_PATTERN_COL1;
            }
            else {
                patternBitmap.pixels[i * BACKGROUND_PATTERN_WIDTH + j] = (j < BACKGROUND_PATTERN_WIDTH / 2) ? BACKGROUND_PATTERN_COL1 : BACKGROUND_PATTERN_COL0;
            }
        }
    }

    // create pattern brush
    patternBrush = CreatePatternBrush(patternBitmap.handle);
    // pen used to draw SVG document frame
    blackPen = CreatePen(PS_SOLID, 2, 0);
}

static void gdiInit(void) {

    // create the pattern brush used to draw the window background
    gdiPatternBrushCreate();
}

// release all GDI objects
static void gdiRelease(void) {

    // destroy GDI bitmaps
    gdiBitmapDestroy(&surfaceBitmap);
    gdiBitmapDestroy(&patternBitmap);
    // destroy brushes
    DeleteObject(blackPen);
    DeleteObject(patternBrush);
    // destroy GDI back buffer
    if (backBufferContext) {
        DeleteDC(backBufferContext);
    }
    if (backBufferBmp) {
        DeleteObject(backBufferBmp);
    }
}

// ---------------------------------------------------------------
//                          SVG viewer
// ---------------------------------------------------------------

// alpha premultiply the given ARGB pixel
static __inline SVGTuint pixelPremultiply(const SVGTuint argb) {

    const SVGTuint a = argb >> 24;
    const SVGTuint ag = (argb & 0xFF00FF00) >> 8;
    const SVGTuint rb =  argb & 0x00FF00FF;
    SVGTuint sag = (a * ag) + 0x00800080;
    SVGTuint srb = (a * rb) + 0x00800080;

    sag += ((sag >> 8) & 0x00FF00FF);
    srb += ((srb >> 8) & 0x00FF00FF);
    sag = sag & 0x0000FF00;
    srb = (srb >> 8) & 0x00FF00FF;
    return (argb & 0xFF000000) | sag | srb;
}

// resize AmanithSVG surface to the given dimensions, then draw the loaded SVG document
static void svgDraw(const SVGTuint width,
                    const SVGTuint height) {

    SVGTuint i, *dstPixels;
    const SVGTuint* srcPixels;

    if (svgSurface != SVGT_INVALID_HANDLE) {
        // resize surface
        gdiBitmapDestroy(&surfaceBitmap);
        svgtSurfaceResize(svgSurface, width, height);
    }
    else {
        // first time, we must create AmanithSVG surface
        svgSurface = svgtSurfaceCreate(width, height);
    }
    // create GDI bitmap that will store AmanithSVG surface content
    gdiBitmapCreate(&surfaceBitmap, svgtSurfaceWidth(svgSurface), svgtSurfaceHeight(svgSurface));

    // clear the drawing surface (full transparent white)
    svgtSurfaceClear(svgSurface, 1.0f, 1.0f, 1.0f, 0.0f);
    // draw the SVG document (upon AmanithSVG surface)
    svgtDocDraw(svgDoc, svgSurface, SVGT_RENDERING_QUALITY_BETTER);
    // copy pixels from AmanithSVG surface to GDI bitmap: we must premultiply pixels because GDI AlphaBlend
    // function (see sceneDraw) uses alpha-premultiplied values, while AmanithSVG uses non-premultiplied pixels
    srcPixels = svgtSurfacePixels(svgSurface);
    dstPixels = surfaceBitmap.pixels;
    for (i = surfaceBitmap.width * surfaceBitmap.height; i != 0; --i) {
        const SVGTuint argb = *srcPixels++;
        *dstPixels++ = pixelPremultiply(argb);
    }
}

// draw the scene: background pattern and AmanithSVG surface
static void sceneDraw(void) {

    RECT clientRect;

    // get window (client) dimensions
    GetClientRect(nativeWindow, &clientRect);
    // draw background
    FillRect(backBufferContext, &clientRect, patternBrush);

    // blit AmanithSVG surface
    if (surfaceBitmap.pixels != NULL) {

        BLENDFUNCTION blend;
        const SVGTint tx = (SVGTint)(surfaceTranslation[0] + 0.5f);
        const SVGTint ty = (SVGTint)(surfaceTranslation[1] + 0.5f);

        // put AmanithSVG surface using per-pixel alpha blend
        blend.BlendOp = AC_SRC_OVER;
        blend.BlendFlags = 0;
        blend.SourceConstantAlpha = 0xFF;
        blend.AlphaFormat = AC_SRC_ALPHA;
        AlphaBlend(backBufferContext, tx, ty, surfaceBitmap.width, surfaceBitmap.height, surfaceBitmap.dc, 0, 0, surfaceBitmap.width, surfaceBitmap.height, blend);
        // draw a black frame surrounding the SVG document
        SelectObject(backBufferContext, blackPen);
        SelectObject(backBufferContext, GetStockObject(NULL_BRUSH));
        Rectangle(backBufferContext, tx + 1, ty + 1, tx + surfaceBitmap.width, ty + surfaceBitmap.height);
    }

    // blit the back buffer
    BitBlt(deviceContext, 0, 0, clientRect.right - clientRect.left, clientRect.bottom - clientRect.top, backBufferContext, 0, 0, SRCCOPY);
}

// resize AmanithSVG surface and re-draw the loaded SVG document
static void sceneResize(const SVGTuint width,
                        const SVGTuint height) {

    resizing = SVGT_TRUE;

    // destroy a previously created back buffer
    if (backBufferContext) {
        DeleteDC(backBufferContext);
        backBufferContext = 0;
    }
    if (backBufferBmp) {
        DeleteObject(backBufferBmp);
        backBufferBmp = 0;
    }
    // create the new back buffer (in order to realize double buffering)
    backBufferContext = CreateCompatibleDC(deviceContext);
    backBufferBmp = CreateCompatibleBitmap(deviceContext, width, height);
    SelectObject(backBufferContext, backBufferBmp);

    // create / resize the AmanithSVG surface such that it is centered within the new back buffer
    if (svgDoc != SVGT_INVALID_HANDLE) {
        // calculate AmanithSVG surface dimensions
        SimpleRect srfRect = surfaceDimensionsCalc(svgDoc, width, height);
        // create / resize AmanithSVG surface, then draw the loaded SVG document
        svgDraw(srfRect.width, srfRect.height);
        // center AmanithSVG surface within the back buffer
        surfaceTranslation[0] = (SVGTfloat)((SVGTint)width - (SVGTint)svgtSurfaceWidth(svgSurface)) * 0.5f;
        surfaceTranslation[1] = (SVGTfloat)((SVGTint)height - (SVGTint)svgtSurfaceHeight(svgSurface)) * 0.5f;
    }

    // output AmanithSVG log content, using the Apple unified logging system
    // NB: AmanithSVG log was initialized within the pickedDocumentLoad
    logOutput();
    // release AmanithSVG log buffer
    logDestroy();

    // we have finished with the resizing, now we can update the window content (i.e. draw)
    resizing = SVGT_FALSE;
    forceRedraw = SVGT_TRUE;
}

// ---------------------------------------------------------------
//                       Event handlers
// ---------------------------------------------------------------
static void deltaTranslation(const SVGTfloat deltaX,
                             const SVGTfloat deltaY) {

    surfaceTranslation[0] += deltaX;
    surfaceTranslation[1] += deltaY;
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

// ---------------------------------------------------------------
//                        File picker
// ---------------------------------------------------------------
static SVGTboolean fileChooseDialog(char* fileName,
                                    const SVGTuint bufferSize) {

    OPENFILENAME openFile;

    memset(&openFile, 0, sizeof(OPENFILENAME));
    openFile.lStructSize     = sizeof(OPENFILENAME);
    openFile.hwndOwner       = nativeWindow;
    openFile.lpstrFile       = fileName;
    // set lpstrFile[0] to '\0' so that GetOpenFileName does not use the contents of szFile to initialize itself
    openFile.lpstrFile[0]    = '\0';
    openFile.lpstrFilter     = "Svg\0*.svg\0";
    openFile.nMaxFile        = bufferSize;
    openFile.nFilterIndex    = 1;
    openFile.lpstrFileTitle  = NULL;
    openFile.nMaxFileTitle   = 0;
    openFile.lpstrInitialDir = NULL;
    openFile.lpstrDefExt     = "";
    openFile.Flags           = OFN_PATHMUSTEXIST | OFN_FILEMUSTEXIST;
    openFile.lpstrTitle      = "Select SVG file.";
    return (GetOpenFileName(&openFile) == FALSE) ? SVGT_FALSE : SVGT_TRUE;
}

// resize the window in order to match the desired dimensions
static void windowResize(const SimpleRect* desiredRect) {

    RECT rect;
    SVGTint x, y, w, h;
    // get screen dimensions
    SimpleRect screenRect = screenDimensionsGet();

    // calculate window size, such that client area (i.e. canvas area) matches the desired dimensions
    rect.left = 0;
    rect.top = 0;
    rect.right = desiredRect->width;
    rect.bottom = desiredRect->height;
    AdjustWindowRectEx(&rect, WS_OVERLAPPEDWINDOW, TRUE, 0);

    // clamp window dimensions against screen bounds
    w = MIN(rect.right - rect.left, (SVGTint)screenRect.width);
    h = MIN(rect.bottom - rect.top, (SVGTint)screenRect.height);
    // center window in the middle of the screen
    x = (screenRect.width - w) / 2;
    y = (screenRect.height - h) / 2;

    // get current window dimensions
    GetClientRect(nativeWindow, &rect);
    // we want to be sure that a "resize" event will be fired (MoveWindow won't emit a WM_SIZE event if new dimensions are equal to the current ones)
    MoveWindow(nativeWindow, x, y, w, h, TRUE);
    if (((rect.right - rect.left) == (SVGTint)desiredRect->width) && ((rect.bottom - rect.top) == (SVGTint)desiredRect->height)) {
        sceneResize(desiredRect->width, desiredRect->height);
    }
}

// load and draw the choosen SVG file
static void pickedDocumentLoad(const char* fileName) {

    // initialize AmanithSVG log (in order to keep track of possible errors
    // and warnings arising from the parsing/rendering of selected file)
    logInit(fileName);

    // destroy a previously loaded document, if any
    if (svgDoc != SVGT_INVALID_HANDLE) {
        svgtDocDestroy(svgDoc);
    }
    // load a new SVG file
    if ((svgDoc = loadSvgFile(fileName)) != SVGT_INVALID_HANDLE) {
        // get screen dimensions
        SimpleRect screenRect = screenDimensionsGet();
        // calculate AmanithSVG surface dimensions
        SimpleRect desiredRect = surfaceDimensionsCalc(svgDoc, screenRect.width, screenRect.height);
        // resize the window in order to match the desired surface dimensions
        // NB: this call will trigger the following chain of events:
        // WM_SIZE event (see windowMessagesHandler) --> sceneResize
        windowResize(&desiredRect);
    }
}

// ---------------------------------------------------------------
//                     Windowing system
// ---------------------------------------------------------------
LRESULT CALLBACK windowMessagesHandler(HWND hWnd,
                                       UINT uMsg,
                                       WPARAM wParam,
                                       LPARAM lParam) {

    MINMAXINFO* minmaxInfo;
    char fileName[MAX_PATH];

    switch (uMsg) {

        case WM_SYSCOMMAND:
            // disable screensaver and monitor powersave
            switch (wParam) {
                case SC_SCREENSAVE:
                case SC_MONITORPOWER:
                return 0;
            }
            break;

        case WM_COMMAND:
            switch (LOWORD(wParam)) {

                case ID_FILE_EXIT:
                    PostQuitMessage(0);
                    return 0;

                case ID_FILE_OPEN:
                    memset(fileName, 0, MAX_PATH);
                    // choose an SVG file
                    if (fileChooseDialog(fileName, MAX_PATH)) {
                        // load and draw the choosen SVG file
                        pickedDocumentLoad(fileName);
                    }
                    return 0;

                case ID_OPTIONS_ABOUT:
                    aboutDialog();
                    return 0;
            }
            break;

        case WM_CLOSE:
            PostQuitMessage(0);
            return 0;

        case WM_KEYDOWN:
            keysPressed[wParam] = SVGT_TRUE;
            return 0;

        case WM_KEYUP:
            keysPressed[wParam] = SVGT_FALSE;
            return 0;

        case WM_GETMINMAXINFO:
            // clamp window dimensions according to the maximum surface dimension supported by AmanithSVG
            minmaxInfo = (MINMAXINFO*)lParam;
            minmaxInfo->ptMaxSize.x = svgtSurfaceMaxDimension();
            minmaxInfo->ptMaxSize.y = minmaxInfo->ptMaxSize.x;
            minmaxInfo->ptMaxTrackSize = minmaxInfo->ptMaxSize;
            return 0;

        case WM_SIZE:
            sceneResize(LOWORD(lParam), HIWORD(lParam));
            return 0;

        case WM_LBUTTONDOWN:
            mouseLeftButtonDown(LOWORD(lParam), HIWORD(lParam));
            return 0;

        case WM_LBUTTONUP:
            mouseLeftButtonUp(LOWORD(lParam), HIWORD(lParam));
            return 0;

        case WM_MOUSEMOVE:
            mouseMove(LOWORD(lParam), HIWORD(lParam));
            return 0;
    }

    return DefWindowProc(hWnd, uMsg, wParam, lParam);
}

static HWND windowCreateImpl(const char* title,
                             const SVGTuint width,
                             const SVGTuint height,
                             const DWORD dwExStyle,
                             const DWORD dwStyle) {

    RECT rect;
    WNDCLASS wc;
    SVGTint x, y, w, h;
    HMENU menu, subMenu;

    // register window class
    wc.style = CS_OWNDC | CS_VREDRAW | CS_HREDRAW;
    wc.lpfnWndProc = windowMessagesHandler;
    wc.cbClsExtra = 0;
    wc.cbWndExtra = 0;

    applicationInstance = GetModuleHandle(NULL);
    wc.hInstance = applicationInstance;
    wc.hIcon = LoadIcon(applicationInstance, MAKEINTRESOURCE(IDI_APP_ICON));
    wc.hCursor = LoadCursor(NULL, IDC_ARROW);
    wc.hbrBackground = NULL;
    wc.lpszMenuName = NULL;
    wc.lpszClassName = CLASS_NAME;
    RegisterClass(&wc);
        
    // calculate window size
    rect.left = 0;
    rect.top = 0;
    rect.right = width;
    rect.bottom = height;
    AdjustWindowRect(&rect, dwStyle, 0);
    w = rect.right - rect.left;
    h = rect.bottom - rect.top;

    // center window on the screen
    x = (GetSystemMetrics(SM_CXSCREEN) - w) / 2;
    y = (GetSystemMetrics(SM_CYSCREEN) - h) / 2;

    // create menu
    menu = CreateMenu();
    subMenu = CreatePopupMenu();
    AppendMenu(subMenu, MF_STRING, ID_FILE_OPEN, "O&pen...");
    AppendMenu(subMenu, MF_STRING, ID_FILE_EXIT, "E&xit");
    AppendMenu(menu, MF_STRING | MF_POPUP, (UINT_PTR)subMenu, "&File");

    subMenu = CreatePopupMenu();
    AppendMenu(subMenu, MF_STRING, ID_OPTIONS_ABOUT, "About");
    AppendMenu(menu, MF_STRING | MF_POPUP, (UINT_PTR)subMenu, "&Options");

    // create window
    return CreateWindowEx(dwExStyle, CLASS_NAME, title, dwStyle, x, y, w, h, NULL, menu, applicationInstance, NULL);
}

// create the native window
static SVGTboolean windowCreate(const char* title,
                                const SVGTint width,
                                const SVGTint height) {

    // create window
    nativeWindow = windowCreateImpl(title, width, height, WS_EX_APPWINDOW | WS_EX_WINDOWEDGE, WS_TILEDWINDOW | WS_CLIPSIBLINGS | WS_CLIPCHILDREN);
    if (!nativeWindow) {
        return SVGT_FALSE;
    }

    // get window dc
    deviceContext = GetDC(nativeWindow);
    if (!deviceContext) {
        return SVGT_FALSE;
    }

    return SVGT_TRUE;
}

// close the native window
static void windowDestroy(void) {

    // release device context
    ReleaseDC(nativeWindow, deviceContext);
    // destroy window
    DestroyWindow(nativeWindow);
    // unregister class
    UnregisterClass(CLASS_NAME, applicationInstance);
}

static SVGTboolean processKeysPressure(void) {

    SVGTboolean closeApp = SVGT_FALSE;

    // ESC
    if (keysPressed[VK_ESCAPE]) {
        keysPressed[VK_ESCAPE] = SVGT_FALSE;
        closeApp = SVGT_TRUE;
    }

    return closeApp;
}

// the main events loop
static int windowEventsLoop(void) {

    MSG msg;
    SVGTboolean done = SVGT_FALSE;

    // show window
    ShowWindow(nativeWindow, SW_NORMAL);
    SetForegroundWindow(nativeWindow);
    SetFocus(nativeWindow);

    // enter event loop
    while (!done) {
        // dispatch messages
        if (PeekMessage(&msg, NULL, 0, 0, PM_REMOVE)) {
            if (msg.message == WM_QUIT) {
                done = SVGT_TRUE;
            }
            else {
                TranslateMessage(&msg);
                DispatchMessage(&msg);
            }
        }
        else {
            // handle key pressure
            done = processKeysPressure();
            if (!done) {
                // draw the scene, if needed
                if (forceRedraw && (!resizing)) {
                    sceneDraw();
                    forceRedraw = SVGT_FALSE;
                }
            }
        }
    }
    // we have finished
    return (int)(msg.wParam);
}

// ---------------------------------------------------------------
//                             Main
// ---------------------------------------------------------------
int WINAPI WinMain(_In_ HINSTANCE hInstance,
                   _In_opt_ HINSTANCE hPrevInstance,
                   _In_ LPSTR lpCmdLine,
                   _In_ int nShowCmd) {

    // if the WinMain function terminates before entering the message loop, it should return zero
    int exitCode = 0;

    (void)hInstance;
    (void)hPrevInstance;
    (void)lpCmdLine;
    (void)nShowCmd;

    // create the native window
    if (windowCreate(WINDOW_TITLE, INITIAL_WINDOW_WIDTH, INITIAL_WINDOW_HEIGHT)) {
        // initialize AmanithSVG and load external resources
        if (amanithsvgInit()) {
            // create GDI objects (pattern brush used to draw the window background)
            gdiInit();
            // enter events loop
            exitCode = windowEventsLoop();
            // terminate AmanithSVG library
            amanithsvgRelease();
            // release all GDI objects
            gdiRelease();
        }
        else {
            DEBUG_LOG("AmanithSVG initialization failed!");
        }

        // close the native window
        windowDestroy();
    }
    else {
        DEBUG_LOG("Window creation failed!");
    }

    return exitCode;
}
