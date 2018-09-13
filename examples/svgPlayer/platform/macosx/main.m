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
#import <Cocoa/Cocoa.h>
#import <QuartzCore/CVDisplayLink.h>
#import <OpenGL/OpenGL.h>
#include <OpenGL/gl.h>
#include <stdio.h>
#include "svg_player.h"

#define WINDOW_TITLE "SVG Player"

// default window dimensions
#define INITIAL_WINDOW_WIDTH 600
#define INITIAL_WINDOW_HEIGHT 800

// background pattern (dimensions and ARGB colors)
#define BACKGROUND_PATTERN_WIDTH 32
#define BACKGROUND_PATTERN_HEIGHT 32
#define BACKGROUND_PATTERN_COL0 0xFF808080
#define BACKGROUND_PATTERN_COL1 0xFFC0C0C0

// The 10.12 SDK adds new symbols and immediately deprecates the old ones
#if MAC_OS_X_VERSION_MAX_ALLOWED < 101200
    #define NSAlertStyleInformational NSInformationalAlertStyle
    #define NSWindowStyleMaskTitled NSTitledWindowMask
    #define NSWindowStyleMaskClosable NSClosableWindowMask
    #define NSWindowStyleMaskMiniaturizable NSMiniaturizableWindowMask
    #define NSWindowStyleMaskResizable NSResizableWindowMask
    #define NSEventMaskAny NSAnyEventMask
#endif

// ---------------------------------------------------------------
//                       Global variables
// ---------------------------------------------------------------
SVGTboolean done = SVGT_FALSE;

// ---------------------------------------------------------------
//                        View (interface)
// ---------------------------------------------------------------
@interface PlayerView : NSOpenGLView <NSWindowDelegate> {

    // keep track if we are selecting/opening an SVG file
    SVGTboolean selectingFile;
    // force the view to redraw
    SVGTboolean forceRedraw;
    // keep track if we are resizing the window
    SVGTboolean resizing;
    // keep track of mouse position
    SVGTfloat oldMouseX;
    SVGTfloat oldMouseY;
    // SVG surface and document
    SVGTHandle svgSurface;
    SVGTHandle svgDoc;
    // OpenGL texture used to draw the pattern background
    GLuint patternTexture;
    // OpenGL texture used to draw the AmanithSVG surface
    GLuint surfaceTexture;
    SVGTfloat surfaceTranslation[2];
    // a Core Video display link 
    CVDisplayLinkRef displayLink;
}

// utility functions
- (SimpleRect) screenDimensionsGet;
- (SVGTfloat) screenDpiGet;
- (void) messageDialog :(const char *)title :(const char *)message;
- (void) aboutDialog :(id)sender;
- (void) projectionLoad :(SVGTfloat)left :(SVGTfloat)right :(SVGTfloat)bottom :(SVGTfloat)top;
- (void) texturedRectangleDraw :(SVGTfloat)x :(SVGTfloat)y :(SVGTfloat)width :(SVGTfloat)height :(SVGTfloat)u :(SVGTfloat)v;
- (void) rectangleDraw :(SVGTfloat)x :(SVGTfloat)y :(SVGTfloat)width :(SVGTfloat)height;
// menu handlers
- (void) fileChooseDialog :(id)sender;
- (void) applicationTerminate :(id)sender;
// implementation of NSOpenGLView methods
- (id) initWithFrame :(NSRect)frameRect;
- (void) prepareOpenGL;
- (void) drawRect :(NSRect)dirtyRect;
- (void) reshape;
- (void) dealloc;
// Core Video display link
- (CVReturn)getFrameForTime :(const CVTimeStamp *)outputTime;
// textures setup
- (void) genPatternTexture;
- (void) genSurfaceTexture;
- (void) drawBackgroundTexture;
- (void) drawSurfaceTexture;
- (void) deleteTextures;
// player functions
- (void) svgDraw :(SVGTuint)width :(SVGTuint)height;
- (void) sceneDraw;
- (void) sceneResize :(SVGTuint)width :(SVGTuint)height;
- (void) windowResize :(const SimpleRect*)desiredRect;
- (void) deltaTranslation :(SVGTfloat)deltaX :(SVGTfloat)deltaY;
- (SVGTboolean) playerInit :(const char*)fileName;
- (void) playerDestroy;
// mouse and keyboard events
- (void) mouseDown :(NSEvent *)theEvent;
- (void) mouseUp :(NSEvent *)theEvent;
- (void) mouseDragged:(NSEvent *)theEvent;
// repsonder properties
- (BOOL) acceptsFirstResponder;
- (BOOL) becomeFirstResponder;
- (BOOL) resignFirstResponder;
- (BOOL) isFlipped;
@end

// ---------------------------------------------------------------
//                     View (implementation)
// ---------------------------------------------------------------
@implementation PlayerView

// get screen dimensions, in pixels
- (SimpleRect) screenDimensionsGet {

    NSScreen* screen = [NSScreen mainScreen];
    NSDictionary* description = [screen deviceDescription];
    NSSize displayPixelSize = [[description objectForKey:NSDeviceSize] sizeValue];
    SimpleRect result = {
        displayPixelSize.width,
        displayPixelSize.height
    };
    return result;
}

// get screen dpi
- (SVGTfloat) screenDpiGet {

    NSScreen* screen = [NSScreen mainScreen];
    NSDictionary* description = [screen deviceDescription];
    NSSize displayPixelSize = [[description objectForKey:NSDeviceSize] sizeValue];
    CGSize displayPhysicalSize = CGDisplayScreenSize([[description objectForKey:@"NSScreenNumber"] unsignedIntValue]);
    // calculate screen dpi (25.4 millimetres in an inch)
    return ((displayPixelSize.width / displayPhysicalSize.width) * 25.4f);
}

- (void) messageDialog :(const char *)title :(const char *)message {

    NSString* sMessage;
    NSAlert* alert = [[NSAlert alloc] init];
 
    (void)title;
    [alert addButtonWithTitle:@"OK"];
    // set message
    sMessage = [NSString stringWithCString:message encoding:NSASCIIStringEncoding];
    [alert setMessageText:sMessage];
    [alert setAlertStyle:NSAlertStyleInformational];
    // display the modal dialog
    [alert runModal];
    [alert release];
}

- (void) aboutDialog :(id)sender {

    char msg[2048];
    char yearStr[64];
    time_t t = time(NULL);
    struct tm *ltm = localtime(&t);

    (void)sender;
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
    [self messageDialog :"AmanithSVG" :msg];
}

- (void) projectionLoad :(SVGTfloat)left :(SVGTfloat)right :(SVGTfloat)bottom :(SVGTfloat)top {

    const SVGTfloat prjMatrix[16] = {
        2.0f / (right - left),            0.0f,                             0.0f, 0.0f,
        0.0f,                             2.0f / (top - bottom),            0.0f, 0.0f,
        0.0f,                             0.0f,                             1.0f, 0.0f,
        -(right + left) / (right - left), -(top + bottom) / (top - bottom), 0.0f, 1.0f
    };
    glMatrixMode(GL_PROJECTION);
    glLoadMatrixf(prjMatrix);
}

- (void) texturedRectangleDraw :(SVGTfloat)x :(SVGTfloat)y :(SVGTfloat)width :(SVGTfloat)height :(SVGTfloat)u :(SVGTfloat)v {

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

- (void) rectangleDraw :(SVGTfloat)x :(SVGTfloat)y :(SVGTfloat)width :(SVGTfloat)height {

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

- (void) fileChooseDialog :(id)sender {

    (void)sender;

    if (!selectingFile) {
        // create an open document panel
        NSOpenPanel* panel = [NSOpenPanel openPanel];
        NSArray* fileTypes = [[NSArray alloc] initWithObjects:@"svg", @"SVG", nil];

        selectingFile = SVGT_TRUE;
        // open only .svg files
        [panel setAllowedFileTypes:fileTypes];
        // display the panel
        [panel beginWithCompletionHandler:^(NSInteger result) {
            // lets load the SVG file, if OK button has been clicked
            if (result == NSFileHandlingPanelOKButton) {
                // grab a reference to what has been selected
                NSURL* fileUrl = [[panel URLs]objectAtIndex:0];
                // write our file name to a label
                NSString* filePath = [fileUrl path];
                // get standard C string
                const char* fileName = (const char *)[filePath fileSystemRepresentation];

                // load the SVG file
                if ([self playerInit :fileName]) {
                    // get screen dimensions
                    SimpleRect screenRect = [self screenDimensionsGet];
                    // calculate AmanithSVG surface dimensions
                    SimpleRect desiredRect = surfaceDimensionsCalc(svgDoc, screenRect.width, screenRect.height);
                    // resize the window in order to match the desired surface dimensions
                    [self windowResize :&desiredRect];
                }
            }
            // now we can select another file, if we desire
            selectingFile = SVGT_FALSE;
        }];
    }
}

- (void) applicationTerminate :(id)sender {

    (void)sender;
    // exit from main loop
    done = SVGT_TRUE;
}

// Core Video display link
- (CVReturn)getFrameForTime :(const CVTimeStamp *)outputTime {

    // deltaTime is unused in this application, but here's how to calculate it using display link info
    // double deltaTime = 1.0 / (outputTime->rateScalar * (double)outputTime->videoTimeScale / (double)outputTime->videoRefreshPeriod);
    (void)outputTime;

    // there is no autorelease pool when this method is called because it will be called from a background thread
    // it's important to create one or app can leak objects
    @autoreleasepool {
        // update the scene, if we are not resizing the window and a redraw is needed
        if (forceRedraw && (!resizing)) {
            forceRedraw = SVGT_FALSE;
            [self drawRect:[self bounds]];
        }
    }

    return kCVReturnSuccess;
}

static CVReturn displayLinkCallback(CVDisplayLinkRef displayLink,
                                    const CVTimeStamp* now,
                                    const CVTimeStamp* outputTime,
                                    CVOptionFlags flagsIn,
                                    CVOptionFlags* flagsOut,
                                    void* displayLinkContext) {

    (void)displayLink;
    (void)now;
    (void)outputTime;
    (void)flagsIn;
    (void)flagsOut;
    
    CVReturn result = [(__bridge PlayerView*)displayLinkContext getFrameForTime:outputTime];
    return result;
}

// implementation of NSOpenGLView methods
- (id) initWithFrame :(NSRect)frameRect {

    NSOpenGLPixelFormatAttribute attributes [] = {
        NSOpenGLPFAAccelerated,
        NSOpenGLPFANoRecovery,
        NSOpenGLPFADoubleBuffer,
        NSOpenGLPFAOpenGLProfile, NSOpenGLProfileVersionLegacy,
        NSOpenGLPFAColorSize, 32,
        (NSOpenGLPixelFormatAttribute)0
    };
    NSOpenGLPixelFormat* format = [[NSOpenGLPixelFormat alloc] initWithAttributes:attributes];

    if (!format) {
        NSLog(@"Unable to create pixel format.");
        exit(EXIT_FAILURE);
    }
    
    self = [super initWithFrame: frameRect pixelFormat: format];
    [format release];

    // initialize private members
    selectingFile = SVGT_FALSE;
    forceRedraw = SVGT_TRUE;
    resizing = SVGT_TRUE;
    oldMouseX = 0.0f;
    oldMouseY = 0.0f;
    svgSurface = SVGT_INVALID_HANDLE;
    svgDoc = SVGT_INVALID_HANDLE;
    surfaceTexture = 0;
    surfaceTranslation[0] = 0.0f;
    surfaceTranslation[1] = 0.0f;
    patternTexture = 0;
    return self;
}

- (void) prepareOpenGL {

    [super prepareOpenGL];

    // the reshape function may have changed the thread to which our OpenGL
    // context is attached before prepareOpenGL and initGL are called.  So call
    // makeCurrentContext to ensure that our OpenGL context current to this 
    // thread (i.e. makeCurrentContext directs all OpenGL calls on this thread
    // to [self openGLContext])
    [[self openGLContext] makeCurrentContext];

    // do not synchronize buffer swaps with vertical refresh rate
    GLint swapInt = 0;
    [[self openGLContext] setValues:&swapInt forParameter:NSOpenGLCPSwapInterval];

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
    [self genPatternTexture];

    // create a display link capable of being used with all active displays
    CVDisplayLinkCreateWithActiveCGDisplays(&displayLink);
    
    // set the renderer output callback function
    CVDisplayLinkSetOutputCallback(displayLink, &displayLinkCallback, (__bridge void*)self);
    
    // set the display link for the current renderer
    CGLContextObj cglContext = [[self openGLContext] CGLContextObj];
    CGLPixelFormatObj cglPixelFormat = [[self pixelFormat] CGLPixelFormatObj];
    CVDisplayLinkSetCurrentCGDisplayFromOpenGLContext(displayLink, cglContext, cglPixelFormat);
    
    // activate the display link
    CVDisplayLinkStart(displayLink);
}

- (void) drawRect :(NSRect)dirtyRect {

    (void)dirtyRect;

    if ([self lockFocusIfCanDraw]) {
    
        [[self openGLContext] makeCurrentContext];

        // we draw on a secondary thread through the display link when resizing the view, -reshape is called automatically on the main thread
        // add a mutex around to avoid the threads accessing the context simultaneously when resizing
        CGLLockContext([[self openGLContext] CGLContextObj]);

        // draw scene
        [self sceneDraw];

        // copy a double-buffered context’s back buffer to its front buffer
        CGLFlushDrawable([[self openGLContext] CGLContextObj]);

        // unlock the context
        CGLUnlockContext([[self openGLContext] CGLContextObj]);
        [self unlockFocus];
    }
}

// this method is called whenever the window/control is reshaped, it is also called when the control is first opened
- (void) reshape {

    [super reshape];

    if ([self lockFocusIfCanDraw]) {

        [[self openGLContext] makeCurrentContext];

        // we draw on a secondary thread through the display link, however, when resizing the view, -drawRect is called on the main thread
        // add a mutex around to avoid the threads accessing the context simultaneously when resizing
        CGLLockContext([[self openGLContext] CGLContextObj]);

        // get new dimensions
        NSSize bound = [self frame].size;
        // resize AmanithSVG surface (in order to match the new window dimensions), then draw the loaded SVG document
        [self sceneResize :bound.width :bound.height];
        
        // unlock the context
        CGLUnlockContext([[self openGLContext] CGLContextObj]);
        [self unlockFocus];
    }
}

- (void) dealloc {

    // stop the display link BEFORE releasing anything in the view
    // otherwise the display link thread may call into the view and
    // crash when it encounters something that has been release
    CVDisplayLinkStop(displayLink);
    // release the display link
    CVDisplayLinkRelease(displayLink);
    // destroy SVG resources allocated by the player
    [self playerDestroy];
    [super dealloc];
}

// textures setup
- (void) genPatternTexture {

    // allocate pixels
    SVGTuint* pixels = (SVGTuint *)malloc(BACKGROUND_PATTERN_WIDTH * BACKGROUND_PATTERN_HEIGHT * 4);

    if (pixels != NULL) {
        SVGTint i, j;
        for (i = 0; i < BACKGROUND_PATTERN_HEIGHT; ++i) {
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

        glGenTextures(1, &patternTexture);
        glDisable(GL_TEXTURE_RECTANGLE_ARB);
        glEnable(GL_TEXTURE_2D);
        glBindTexture(GL_TEXTURE_2D, patternTexture);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
        // select best format, in order to avoid swizzling
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, BACKGROUND_PATTERN_WIDTH, BACKGROUND_PATTERN_HEIGHT, 0, GL_BGRA, GL_UNSIGNED_INT_8_8_8_8_REV, pixels);
        free(pixels);
    }
}

- (void) genSurfaceTexture {

    // get AmanithSVG surface dimensions and pixels pointer
    SVGTint surfaceWidth = svgtSurfaceWidth(svgSurface);
    SVGTint surfaceHeight = svgtSurfaceHeight(svgSurface);
    void* surfacePixels = (void*)svgtSurfacePixels(svgSurface);

    // generate a 2D rectangular texture
    glGenTextures(1, &surfaceTexture);
    glEnable(GL_TEXTURE_RECTANGLE_ARB);
    glDisable(GL_TEXTURE_2D);
    glBindTexture(GL_TEXTURE_RECTANGLE_ARB, surfaceTexture);
    glTexParameteri(GL_TEXTURE_RECTANGLE_ARB, GL_TEXTURE_STORAGE_HINT_APPLE, GL_STORAGE_CACHED_APPLE);
    glPixelStorei(GL_UNPACK_CLIENT_STORAGE_APPLE, GL_TRUE);
    glTexParameteri(GL_TEXTURE_RECTANGLE_ARB, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
    glTexParameteri(GL_TEXTURE_RECTANGLE_ARB, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
    glTexParameteri(GL_TEXTURE_RECTANGLE_ARB, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
    glTexParameteri(GL_TEXTURE_RECTANGLE_ARB, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
    // select best format, in order to avoid swizzling
    glTexImage2D(GL_TEXTURE_RECTANGLE_ARB, 0, GL_RGBA, surfaceWidth, surfaceHeight, 0, GL_BGRA, GL_UNSIGNED_INT_8_8_8_8_REV, surfacePixels);
}

- (void) drawBackgroundTexture {

    // get frame dimensions
    NSSize bound = [self frame].size;
    SVGTfloat u = bound.width / BACKGROUND_PATTERN_WIDTH;
    SVGTfloat v = bound.height / BACKGROUND_PATTERN_HEIGHT;

    glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
    glDisable(GL_BLEND);
    glDisable(GL_TEXTURE_RECTANGLE_ARB);
    glEnable(GL_TEXTURE_2D);
    glBindTexture(GL_TEXTURE_2D, patternTexture);
    // simply put a quad, covering the whole window
    [self texturedRectangleDraw :0.0f :0.0f :bound.width :bound.height :u :v];
}

- (void) drawSurfaceTexture {

    // get AmanithSVG surface dimensions
    SVGTfloat surfaceWidth = (SVGTfloat)svgtSurfaceWidth(svgSurface);
    SVGTfloat surfaceHeight = (SVGTfloat)svgtSurfaceHeight(svgSurface);
    SVGTfloat tx = (SVGTfloat)((SVGTint)(surfaceTranslation[0] + 0.5f));
    SVGTfloat ty = (SVGTfloat)((SVGTint)(surfaceTranslation[1] + 0.5f));

    // enable per-pixel alpha blending, using surface texture as source
    glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
    glEnable(GL_BLEND);

    glEnable(GL_TEXTURE_RECTANGLE_EXT);
    glDisable(GL_TEXTURE_2D);
    glBindTexture(GL_TEXTURE_RECTANGLE_EXT, surfaceTexture);
    // simply put a quad
    [self texturedRectangleDraw :tx :ty :surfaceWidth :surfaceHeight :surfaceWidth :surfaceHeight];

    // draw a solid black frame surrounding the SVG document
    glColor4f(0.0f, 0.0f, 0.0f, 1.0f);
    glDisable(GL_BLEND);
    glDisable(GL_TEXTURE_RECTANGLE_EXT);
    glDisable(GL_TEXTURE_2D);
    [self rectangleDraw :tx :ty :surfaceWidth :2.0f];
    [self rectangleDraw :tx :ty :2.0f :surfaceHeight];
    [self rectangleDraw :tx :ty + surfaceHeight - 2.0f :surfaceWidth :2.0f];
    [self rectangleDraw :tx + surfaceWidth - 2.0f :ty :2.0f :surfaceHeight];
}

- (void) deleteTextures {

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
- (void) svgDraw :(SVGTuint)width :(SVGTuint)height {

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
    [self genSurfaceTexture];
}

// player functions
- (void) sceneDraw {

    // clear OpenGL buffer
    glClear(GL_COLOR_BUFFER_BIT);
    // draw pattern background
    [self drawBackgroundTexture];
    // put AmanithSVG surface using per-pixel alpha blend
    if ((svgDoc != SVGT_INVALID_HANDLE) && (svgSurface != SVGT_INVALID_HANDLE)) {
        [self drawSurfaceTexture];
    }
}

- (void) sceneResize :(SVGTuint)width :(SVGTuint)height {

    resizing = SVGT_TRUE;

    // create / resize the AmanithSVG surface such that it is centered within the OpenGL view
    if (svgDoc != SVGT_INVALID_HANDLE) {
        // calculate AmanithSVG surface dimensions
        SimpleRect srfRect = surfaceDimensionsCalc(svgDoc, width, height);
        // create / resize AmanithSVG surface, then draw the loaded SVG document
        [self svgDraw :srfRect.width :srfRect.height];
        // center AmanithSVG surface within the OpenGL view
        surfaceTranslation[0] = (SVGTfloat)((SVGTint)width - (SVGTint)svgtSurfaceWidth(svgSurface)) * 0.5f;
        surfaceTranslation[1] = (SVGTfloat)((SVGTint)height - (SVGTint)svgtSurfaceHeight(svgSurface)) * 0.5f;
    }

    // set OpenGL viewport and projection
    glViewport(0, 0, width, height);
    [self projectionLoad :0.0f :width :0.0f :height];

    // we have finished with the resizing, now we can update the window content (i.e. draw)
    resizing = SVGT_FALSE;
    forceRedraw = SVGT_TRUE;
}

- (void) windowResize :(const SimpleRect*)desiredRect {

    NSRect r;
    // get current window dimensions
    const NSWindow* window = [self window];
    const NSSize curSize = window.frame.size;
    // get screen dimensions
    const SimpleRect screenRect = [self screenDimensionsGet];
    // clamp window dimensions against screen bounds
    SVGTfloat w = MIN(desiredRect->width, screenRect.width);
    SVGTfloat h = MIN(desiredRect->height, screenRect.height);
    // center window in the middle of the screen
    SVGTfloat x = ((SVGTfloat)screenRect.width - w) * 0.5f;
    SVGTfloat y = ((SVGTfloat)screenRect.height - h) * 0.5f;

    // move / resize the window
    r.origin.x = x;
    r.origin.y = y;
    r.size.width = w;
    r.size.height = h;
    // we want to be sure that a "resize" event will be fired (the setFrame won't emit a reshape event if new dimensions are equal to the current ones)
    [window setFrame:r display:true];
    if ((curSize.width == (SVGTfloat)desiredRect->width) && (curSize.height == (SVGTfloat)desiredRect->height)) {
        [self reshape];
    }
}

- (void) deltaTranslation :(SVGTfloat)deltaX :(SVGTfloat)deltaY {

    surfaceTranslation[0] += deltaX;
    surfaceTranslation[1] += deltaY;
    forceRedraw = SVGT_TRUE;
}

- (SVGTboolean) playerInit :(const char*)fileName {

    SVGTboolean ok = SVGT_FALSE;
    // get screen dimensions
    SimpleRect screenRect = [self screenDimensionsGet];

    // initialize AmanithSVG library (actually just the first call performs the real initialization)
    if (svgtInit(screenRect.width, screenRect.height, [self screenDpiGet]) == SVGT_NO_ERROR) {
        // destroy a previously loaded document, if any
        if (svgDoc != SVGT_INVALID_HANDLE) {
            svgtDocDestroy(svgDoc);
        }
        // load a new SVG file
        svgDoc = loadSvg(fileName);
        ok = (svgDoc != SVGT_INVALID_HANDLE) ? SVGT_TRUE : SVGT_FALSE;
    }
    // we have finished
    return ok;
}

// destroy SVG resources allocated by the player
- (void) playerDestroy {

    // destroy the SVG document
    svgtDocDestroy(svgDoc);
    // destroy the drawing surface
    svgtSurfaceDestroy(svgSurface);
    // deinitialize AmanithSVG library
    svgtDone();
    // destroy used textures
    [self deleteTextures];
}

// mouse and keyboard events
- (void) mouseDown: (NSEvent *)theEvent {

    NSPoint p;

    // convert window location into view location
    p = [theEvent locationInWindow];
    p = [self convertPoint: p fromView: nil];
    // keep track of mouse position
    oldMouseX = p.x;
    oldMouseY = p.y;
}

- (void) mouseUp: (NSEvent *)theEvent {

    (void)theEvent;
}

- (void) mouseDragged:(NSEvent *)theEvent {

    NSPoint p;

    // convert window location into view location
    p = [theEvent locationInWindow];
    p = [self convertPoint: p fromView: nil];
    // update translation
    [self deltaTranslation :(p.x - oldMouseX) :(p.y - oldMouseY)];
    oldMouseX = p.x;
    oldMouseY = p.y;
}

- (BOOL) acceptsFirstResponder {
    // as first responder, the receiver is the first object in the responder chain to be sent key events and action messages
    return YES;
}

- (BOOL) becomeFirstResponder {

    return YES;
}

- (BOOL) resignFirstResponder {

    return YES;
}

- (BOOL) isFlipped {

    return NO;
}

// from NSWindowDelegate
- (void)windowWillClose:(NSNotification *)note {

    (void)note;

    // Stop the display link when the window is closing because default
    // OpenGL render buffers will be destroyed. If display link continues to
    // fire without renderbuffers, OpenGL draw calls will set errors.
    CVDisplayLinkStop(displayLink);
    done = SVGT_TRUE;
}
@end

// ---------------------------------------------------------------
//                             Main
// ---------------------------------------------------------------
static void applicationMenuPopulate(NSMenu* subMenu,
                                    const PlayerView* view) {

    NSMenuItem* menuItem;

    // open SVG file
    menuItem = [subMenu addItemWithTitle:[NSString stringWithFormat:@"%@", NSLocalizedString(@"Open...", nil)] action:@selector(fileChooseDialog:) keyEquivalent:@"f"];
    [menuItem setTarget:view];
    // about dialog
    menuItem = [subMenu addItemWithTitle:[NSString stringWithFormat:@"%@", NSLocalizedString(@"About", nil)] action:@selector(aboutDialog:) keyEquivalent:@"a"];
    [menuItem setTarget:view];
    // quit application
    menuItem = [subMenu addItemWithTitle:[NSString stringWithFormat:@"%@", NSLocalizedString(@"Quit", nil)] action:@selector(applicationTerminate:) keyEquivalent:@"q"];
    [menuItem setTarget:view];
}

static void mainMenuPopulate(const PlayerView* view) {

    NSMenuItem* menuItem;
    NSMenu* subMenu;
    // create main menu = menu bar
    NSMenu* mainMenu = [[NSMenu alloc] initWithTitle:@"MainMenu"];
    
    // the titles of the menu items are for identification purposes only and shouldn't be localized; the strings in the menu bar come
    // from the submenu titles, except for the application menu, whose title is ignored at runtime
    menuItem = [mainMenu addItemWithTitle:@"Apple" action:NULL keyEquivalent:@""];
    subMenu = [[NSMenu alloc] initWithTitle:@"Apple"];
    [NSApp performSelector:@selector(setAppleMenu:) withObject:subMenu];
    applicationMenuPopulate(subMenu, view);
    [mainMenu setSubmenu:subMenu forItem:menuItem];
    
    [NSApp setMainMenu:mainMenu];
}

static void applicationMenuCreate(const PlayerView* view) {

    mainMenuPopulate(view);
}

int main(int argc,
         char *argv[]) {

    (void)argc;
    (void)argv;

    @autoreleasepool {

        NSRect frame = NSMakeRect(0, 0, INITIAL_WINDOW_WIDTH, INITIAL_WINDOW_HEIGHT);

        // get application
        NSApplication* app = [NSApplication sharedApplication];
        [NSApp setActivationPolicy: NSApplicationActivationPolicyRegular];

        // create the window
        NSWindow* window = [[NSWindow alloc] initWithContentRect:frame styleMask:NSWindowStyleMaskTitled | NSWindowStyleMaskClosable | NSWindowStyleMaskMiniaturizable | NSWindowStyleMaskResizable backing:NSBackingStoreBuffered defer: TRUE];
        [window setAcceptsMouseMovedEvents:YES];
        [window setTitle: @ WINDOW_TITLE];

        // create the OpenGL view
        PlayerView* view = [[PlayerView alloc] initWithFrame: frame];

        // link the view to the window
        [window setDelegate: view];
        [window setContentView: view];
        [window makeFirstResponder: view];
        [window setMaxSize: NSMakeSize(svgtSurfaceMaxDimension(), svgtSurfaceMaxDimension())];
        [view release];

        // center the window
        [window center];
        [window makeKeyAndOrderFront: nil];

        // create and populate the menu
        applicationMenuCreate(view);
        [app finishLaunching];

        // enter main loop
        done = SVGT_FALSE;
        while (!done) {
            // dispatch events
            NSEvent* event = [app nextEventMatchingMask: NSEventMaskAny untilDate: [NSDate dateWithTimeIntervalSinceNow: 0.0] inMode: NSDefaultRunLoopMode dequeue: true];
            if (event != nil) {
                [app sendEvent: event];
                [app updateWindows];
            }
        }

    } // @autoreleasepool

    return EXIT_SUCCESS;
}
