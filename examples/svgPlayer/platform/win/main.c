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
#include <windows.h>
#include <stdio.h>
#include <time.h>
#include "svg_player.h"
#include "resource.h"
#if defined(_MSC_VER)
    // take care of Visual Studio .Net 2005+ C deprecations
    #if _MSC_VER >= 1400
        #if !defined(_CRT_SECURE_NO_DEPRECATE)
            #define _CRT_SECURE_NO_DEPRECATE 1
        #endif
        #if !defined(_CRT_NONSTDC_NO_DEPRECATE)
            #define _CRT_NONSTDC_NO_DEPRECATE 1
        #endif
        #pragma warning (disable:4996)
    #endif
#endif
#define CLASS_NAME "SVGPlayer"
#define WINDOW_TITLE "SVG Player"

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
HDC deviceContext = NULL;
HWND nativeWindow = NULL;
HINSTANCE applicationInstance;

// GDI back buffer
HDC backBufferContext = NULL;
HBITMAP backBufferBmp = NULL;
// GDI bitmap relative to AmanithSVG surface
Bitmap surfaceBitmap = { NULL, NULL, 0, 0, NULL };
// GDI pattern bitmap
Bitmap patternBitmap = { NULL, NULL, 0, 0, NULL };
// GDI pattern brush, used to draw the background
HBRUSH patternBrush = NULL;
// GDI pen, used to draw SVG document frame
HPEN blackPen = NULL;

SVGTfloat surfaceTranslation[2] = { 0.0f };
// SVG surface and document
SVGTHandle svgSurface = SVGT_INVALID_HANDLE;
SVGTHandle svgDoc = SVGT_INVALID_HANDLE;

// pressed keys buffer
SVGTboolean keysPressed[256] = { SVGT_FALSE };
// keep track if we are resizing the window
SVGTboolean resizing = SVGT_FALSE;
// force the view to redraw
SVGTboolean forceRedraw = SVGT_FALSE;
// keep track of mouse state
SVGTint oldMouseX = 0;
SVGTint oldMouseY = 0;
SVGTint mouseButtonState = MOUSE_BUTTON_NONE;

/*****************************************************************
                       Utility functions
*****************************************************************/
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
    openFile.lpstrTitle      = "Select svg file.";
    return (GetOpenFileName(&openFile) == FALSE) ? SVGT_FALSE : SVGT_TRUE;
}

/*****************************************************************
                     GDI bitmaps and brushes
*****************************************************************/
static SVGTboolean bitmapCreate(Bitmap* dst,
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

static void bitmapDestroy(Bitmap* src) {

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

static void patternBrushCreate(void) {

    SVGTuint i, j;

    // create background bitmap
    bitmapCreate(&patternBitmap, BACKGROUND_PATTERN_WIDTH, BACKGROUND_PATTERN_HEIGHT);

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

/*****************************************************************
                            SVG player
*****************************************************************/

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
        bitmapDestroy(&surfaceBitmap);
        svgtSurfaceResize(svgSurface, width, height);
    }
    else {
        // first time, we must create AmanithSVG surface
        svgSurface = svgtSurfaceCreate(width, height);
        // clear the drawing surface (full transparent white) at every svgtDocDraw call
        svgtClearColor(1.0f, 1.0f, 1.0f, 0.0f);
        svgtClearPerform(SVGT_TRUE);
    }
    // create GDI bitmap that will store AmanithSVG surface content
    bitmapCreate(&surfaceBitmap, svgtSurfaceWidth(svgSurface), svgtSurfaceHeight(svgSurface));
    // draw the SVG document (upon AmanithSVG surface)
    svgtDocDraw(svgDoc, svgSurface, SVGT_RENDERING_QUALITY_BETTER);
    // copy pixels from AmanithSVG surface to GDI bitmap: we must premultiply pixels because GDI AlphaBlend
    // function (see sceneDraw) uses alpha-premultiplied values, while AmanithSVG uses non-premultiplied pixels
    srcPixels = (const SVGTuint*)svgtSurfacePixels(svgSurface);
    dstPixels = surfaceBitmap.pixels;
    for (i = surfaceBitmap.width * surfaceBitmap.height; i != 0; --i) {
        const SVGTuint argb = *srcPixels++;
        *dstPixels++ = pixelPremultiply(argb);
    }
}

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

    // we have finished with the resizing, now we can update the window content (i.e. draw)
    resizing = SVGT_FALSE;
    forceRedraw = SVGT_TRUE;
}

// draw pattern background and blit AmanithSVG surface (per-pixel alpha blend)
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
    // destroy GDI bitmaps
    bitmapDestroy(&surfaceBitmap);
    bitmapDestroy(&patternBitmap);
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

/*****************************************************************
                         Event handlers
*****************************************************************/
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

/*****************************************************************
                       Windowing system
*****************************************************************/
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
    if ((rect.right - rect.left == (SVGTint)desiredRect->width) && (rect.bottom - rect.top == (SVGTint)desiredRect->height)) {
        sceneResize(desiredRect->width, desiredRect->height);
    }
}

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
                        // load the SVG file
                        if (playerInit(fileName)) {
                            // calculate AmanithSVG surface dimensions
                            SimpleRect screenRect = screenDimensionsGet();
                            SimpleRect desiredRect = surfaceDimensionsCalc(svgDoc, screenRect.width, screenRect.height);
                            // resize the window in order to match the desired surface dimensions
                            windowResize(&desiredRect);
                        }
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
    wc.hIcon = LoadIcon(applicationInstance, MAKEINTRESOURCE(IDI_ICON1));
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

int WINAPI WinMain(HINSTANCE hInstance,
                   HINSTANCE hPrevInstance,
                   LPSTR lpCmdLine,
                   int nCmdShow) {

    MSG msg;
    SVGTboolean done = SVGT_FALSE;

    (void)hInstance;
    (void)hPrevInstance;
    (void)lpCmdLine;
    (void)nCmdShow;

    // create window
    if (!windowCreate(WINDOW_TITLE, INITIAL_WINDOW_WIDTH, INITIAL_WINDOW_HEIGHT)) {
        return 0;
    }

    // create pattern brush, used to draw the window background
    patternBrushCreate();

    // show window
    ShowWindow(nativeWindow, SW_NORMAL);
    SetForegroundWindow(nativeWindow);
    SetFocus(nativeWindow);

    // enter main loop
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

    // destroy SVG resources allocated by the player
    playerDestroy();
    // close window
    windowDestroy();
    return (int)(msg.wParam);
}
