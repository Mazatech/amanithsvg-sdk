/****************************************************************************
** Copyright (c) 2013-2019 Mazatech S.r.l.
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
#import <Cocoa/Cocoa.h>
#if defined(USE_OPENGL)
    #import <QuartzCore/CVDisplayLink.h>
    #import <OpenGL/OpenGL.h>
    #include <OpenGL/gl.h>
#else
    #import <Metal/Metal.h>
    #import <MetalKit/MetalKit.h>
    // header shared between C code here, which executes Metal API commands, and .metal files, which uses these types as inputs to the shaders.
    #import "ShaderTypes.h"
#endif
#include <stdio.h>
#include "svg_viewer.h"

#define WINDOW_TITLE "SVG Viewer"

// default window dimensions
#define INITIAL_WINDOW_WIDTH 600
#define INITIAL_WINDOW_HEIGHT 800

// background pattern (dimensions and ARGB colors)
#define BACKGROUND_PATTERN_WIDTH 32
#define BACKGROUND_PATTERN_HEIGHT 32
#define BACKGROUND_PATTERN_COL0 0xFF808080
#define BACKGROUND_PATTERN_COL1 0xFFC0C0C0

#if defined(USE_OPENGL)
    #if !defined(GL_RED)
        #define GL_RED 0x1903
    #endif
    #if !defined(GL_GREEN)
        #define GL_GREEN 0x1904
    #endif
    #if !defined(GL_BLUE)
        #define GL_BLUE 0x1905
    #endif
    #if !defined(GL_ALPHA)
        #define GL_ALPHA 0x1906
    #endif
    // GL_EXT_bgra extension
    #if !defined(GL_BGRA)
        #define GL_BGRA 0x80E1
    #endif
    // GL_EXT_texture_swizzle extension
    #if !defined(GL_TEXTURE_SWIZZLE_R)
        #define GL_TEXTURE_SWIZZLE_R 0x8E42
    #endif
    #if !defined(GL_TEXTURE_SWIZZLE_G)
        #define GL_TEXTURE_SWIZZLE_G 0x8E43
    #endif
    #if !defined(GL_TEXTURE_SWIZZLE_B)
        #define GL_TEXTURE_SWIZZLE_B 0x8E44
    #endif
    #if !defined(GL_TEXTURE_SWIZZLE_A)
        #define GL_TEXTURE_SWIZZLE_A 0x8E45
    #endif
#endif

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
#if defined(USE_OPENGL)
@interface ViewerView : NSOpenGLView <NSWindowDelegate> {
    // a Core Video display link 
    CVDisplayLinkRef displayLink;
    // Texture capabilities
    SVGTboolean bgraSupport;
    SVGTboolean npotSupport;
    SVGTboolean swizzleSupport;
    GLint internalFormat;
    GLenum externalFormat;
    // OpenGL texture used to draw the pattern background
    GLuint patternTexture;
    // OpenGL texture used to draw the AmanithSVG surface
    GLuint surfaceTexture;
#else
@interface ViewerView : MTKView <MTKViewDelegate, NSWindowDelegate> {
    id<MTLDevice> mtlDevice;
    id<MTLCommandQueue> mtlCommandQueue;
    id<MTLLibrary> mtlLibrary;
    id<MTLRenderPipelineState> mtlBackgroundPipelineState;
    id<MTLRenderPipelineState> mtlSurfacePipelineState;
    id<MTLRenderPipelineState> mtlFramePipelineState;
    // Metal texture used to draw the pattern background
    id<MTLTexture> patternTexture;
    // Metal texture used to draw the AmanithSVG surface
    id<MTLTexture> surfaceTexture;
    // The current size of the view, used as an input to the vertex shader.
    vector_uint2 viewportSize;
#endif
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
    SVGTfloat surfaceTranslation[2];
}

@end

// ---------------------------------------------------------------
//                     View (implementation)
// ---------------------------------------------------------------
@implementation ViewerView

// get screen dimensions, in pixels
- (SimpleRect) screenDimensionsGet {

    NSRect r;
    SimpleRect result;
    NSScreen* screen = [NSScreen mainScreen];
    NSDictionary* description = [screen deviceDescription];
    NSSize displayPixelSize = [[description objectForKey:NSDeviceSize] sizeValue];

    r.origin.x = 0;
    r.origin.y = 0;
    r.size.width = displayPixelSize.width;
    r.size.height = displayPixelSize.height;
    // take care of Retina display
    r = [screen convertRectToBacking :r];

    result.width = r.size.width;
    result.height = r.size.height;
    return result;
}

// get screen dpi
- (SVGTfloat) screenDpiGet {

    NSRect r;
    NSScreen* screen = [NSScreen mainScreen];
    NSDictionary* description = [screen deviceDescription];
    NSSize displayPixelSize = [[description objectForKey:NSDeviceSize] sizeValue];
    CGSize displayPhysicalSize = CGDisplayScreenSize([[description objectForKey:@"NSScreenNumber"] unsignedIntValue]);

    r.origin.x = 0;
    r.origin.y = 0;
    r.size.width = displayPixelSize.width;
    r.size.height = displayPixelSize.height;
    // take care of Retina display
    r = [screen convertRectToBacking :r];

    // calculate screen dpi (25.4 millimetres in an inch)
    return ((r.size.width / displayPhysicalSize.width) * 25.4f);
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

#if defined(USE_OPENGL)

// swap R and B components (ARGB <--> ABGR and vice versa)
- (SVGTuint) swapRedBlue :(SVGTuint)p {

    // swap R <--> B
    SVGTuint ag = p & 0xFF00FF00;
    SVGTuint rb = p & 0x00FF00FF;
    SVGTuint r = rb >> 16;
    SVGTuint b = rb & 0xFF;
    return ag | (b << 16) | r;
}

// return the power of two value greater (or equal) to a given value
- (SVGTuint) pow2Get :(SVGTuint)value {

    SVGTuint v = 1;

    if (value >= ((SVGTuint)1 << 31)) {
        return ((SVGTuint)1 << 31);
    }
    
    while (v < value) {
        v <<= 1;
    }

    return v;
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
            [self drawRect];
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
    
    CVReturn result = [(__bridge ViewerView*)displayLinkContext getFrameForTime:outputTime];
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
        NSLog(@"initWithFrame: unable to create pixel format");
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

    // take care of Retina display
    [self setWantsBestResolutionOpenGLSurface:YES];
    [super prepareOpenGL];

    // the reshape function may have changed the thread to which our OpenGL
    // context is attached before prepareOpenGL and initGL are called. So call
    // makeCurrentContext to ensure that our OpenGL context current to this 
    // thread (i.e. makeCurrentContext directs all OpenGL calls on this thread
    // to [self openGLContext])
    [[self openGLContext] makeCurrentContext];

    // do not synchronize buffer swaps with vertical refresh rate
    GLint swapInt = 0;
    [[self openGLContext] setValues:&swapInt forParameter:NSOpenGLCPSwapInterval];

    // get texture capabilities
    NSString* extensionString = [NSString stringWithUTF8String:(char *)glGetString(GL_EXTENSIONS)];
    NSArray* extensions = [extensionString componentsSeparatedByCharactersInSet:[NSCharacterSet whitespaceCharacterSet]];
    if ([extensions containsObject: @"GL_APPLE_texture_format_BGRA8888"] || [extensions containsObject: @"GL_EXT_bgra"]) {
        bgraSupport = SVGT_TRUE;
        internalFormat = GL_RGBA;
        externalFormat = GL_BGRA;
    }
    else
    if ([extensions containsObject: @"GL_IMG_texture_format_BGRA8888"] || [extensions containsObject: @"GL_EXT_texture_format_BGRA8888"]) {
        bgraSupport = SVGT_TRUE;
        internalFormat = GL_BGRA;
        externalFormat = GL_BGRA;
    }
    else {
        bgraSupport = SVGT_FALSE;
        internalFormat = GL_RGBA;
        externalFormat = GL_RGBA;
    }

    npotSupport = ([extensions containsObject: @"GL_OES_texture_npot"] ||
                   [extensions containsObject: @"GL_APPLE_texture_2D_limited_npot"] ||
                   [extensions containsObject: @"GL_ARB_texture_non_power_of_two"]) ? SVGT_TRUE : SVGT_FALSE;
    swizzleSupport = ([extensions containsObject: @"GL_EXT_texture_swizzle"] ||
                      [extensions containsObject: @"GL_ARB_texture_swizzle"]) ? SVGT_TRUE : SVGT_FALSE;

    // get OpenGL version
    const char* glVersionStr = (const char *)glGetString(GL_VERSION);
    // parse string
    if (glVersionStr != NULL) {

        int minor = 0;
        int major = 0;
        int revision = 0;
        const char* ptr = glVersionStr;

        // find first digit
        while ((*ptr != '\0') && (!((*ptr >= '0') && (*ptr <= '9')))) {
            ptr++;
        }
        // parse major number
        for (; (*ptr != '\0') && ((*ptr >= '0') && (*ptr <= '9')); ptr++) {
            major = (10 * major) + (*ptr - '0');
        }
        if (*ptr == '.') {
            ptr++;
            // parse minor number
            for (; (*ptr != '\0') && ((*ptr >= '0') && (*ptr <= '9')); ptr++) {
                minor = (10 * minor) + (*ptr - '0');
            }
            if (*ptr == '.') {
                ptr++;
                // parse revision number
                for (; (*ptr != '\0') && ((*ptr >= '0') && (*ptr <= '9')); ptr++) {
                    revision = (10 * revision) + (*ptr - '0');
                }
            }
        }
        // on OpenGL 2.0+ npot textures are supported as a core functionality
        if (major >= 2) {
            npotSupport = SVGT_TRUE;
        }
        // on OpenGL 3.3+ texture swizzling is supported as a core functionality
        if ((major >= 3) && (minor >= 3)) {
            swizzleSupport = SVGT_TRUE;
        }
    }

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

- (void) drawRect {

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

        // update the receiver's drawable object
        [[self openGLContext] update];

        // get view dimensions in pixels, taking care of Retina display
        // see https://developer.apple.com/library/archive/documentation/GraphicsImaging/Conceptual/OpenGL-MacProgGuide/EnablingOpenGLforHighResolution/EnablingOpenGLforHighResolution.html
        NSRect backingBounds = [self convertRectToBacking:[self bounds]];
        SVGTuint backingPixelWidth = (SVGTuint)backingBounds.size.width;
        SVGTuint backingPixelHeight = (SVGTuint)backingBounds.size.height;

        // resize AmanithSVG surface (in order to match the new window dimensions), then draw the loaded SVG document
        [self sceneResize :backingPixelWidth :backingPixelHeight];
        
        // unlock the context
        CGLUnlockContext([[self openGLContext] CGLContextObj]);
        [self unlockFocus];
    }
}

#else

- (void) texturedRectangleDraw :(id<MTLRenderCommandEncoder>)commandEncoder :(SVGTfloat)x :(SVGTfloat)y :(SVGTfloat)width :(SVGTfloat)height :(SVGTfloat)u :(SVGTfloat)v {

    // triangle strip
    const TexturedVertex rectVertices[4] = {
        // pixel positions           texture coordinates
        { { x, y },                  { 0.0f, 0.0f } },
        { { x + width, y },          { u, 0.0f } },
        { { x, y + height },         { 0.0f, v } },
        { { x + width, y + height }, { u, v } }
    };
    [commandEncoder setVertexBytes:rectVertices length:sizeof(rectVertices) atIndex:0];
    [commandEncoder setVertexBytes:&viewportSize length:sizeof(viewportSize) atIndex:1];
    [commandEncoder drawPrimitives:MTLPrimitiveTypeTriangleStrip vertexStart:0 vertexCount:4];
}

- (void) rectangleDraw :(id<MTLRenderCommandEncoder>)commandEncoder :(SVGTfloat)x :(SVGTfloat)y :(SVGTfloat)width :(SVGTfloat)height {

    // triangle strip
    const GeometricVertex rectVertices[4] = {
        // pixel positions
        { { x, y } },
        { { x + width, y } },
        { { x, y + height } },
        { { x + width, y + height } }
    };
    [commandEncoder setVertexBytes:rectVertices length:sizeof(rectVertices) atIndex:0];
    [commandEncoder setVertexBytes:&viewportSize length:sizeof(viewportSize) atIndex:1];
    [commandEncoder drawPrimitives:MTLPrimitiveTypeTriangleStrip vertexStart:0 vertexCount:4];
}

// implementation of MTKView and MTKViewDelegate methods
- (id) initWithFrame :(CGRect)frameRect device:(id<MTLDevice>)device {

    self = [super initWithFrame:frameRect device:device];
    if (self != nil) {

        __autoreleasing NSError* error = nil;

        self.device = device;
        self.colorPixelFormat = MTLPixelFormatBGRA8Unorm;
        self.clearColor = MTLClearColorMake(0.0, 0.0, 0.0, 1.0);
        self.framebufferOnly = YES;
        self.autoResizeDrawable = YES;
        self.delegate = self;
        // MTKView by default has its own loop continuously calling the delegate's drawInMTKView method
        // by using 'enableSetNeedsDisplay' and 'paused', we can disable that loop and rather have the view
        // render only when 'needsDisplay' is set
        //
        // anyway, in order to keep the SVG viewer application simple, we enable the default MTKView loop and force
        // the drawing using the private variable 'forceRedraw': in this way we avoid to setup the CVDisplayLink
        self.paused = NO;
        self.enableSetNeedsDisplay = NO;

        // NB: drawableSizeWillChange event is NOT called when the view/window is first opened
        // so we must initialize 'viewportSize' variable here
        NSRect backedRect = [self convertRectToBacking:frameRect];
        viewportSize.x = backedRect.size.width;
        viewportSize.y = backedRect.size.height;

        // keep track of Metal device and command queue
        mtlDevice = device;
        mtlCommandQueue = [mtlDevice newCommandQueue];
        mtlLibrary = [mtlDevice newDefaultLibrary];

        // configure a pipeline descriptor that is used to draw the background texture
        id<MTLFunction> backgroundVertexFunction = [mtlLibrary newFunctionWithName:@"texturedVertexShader"];
        id<MTLFunction> backgroundFragmentFunction = [mtlLibrary newFunctionWithName:@"texturedRepeatFragmentShader"];
        MTLRenderPipelineDescriptor *backgroundPipelineStateDescriptor = [[MTLRenderPipelineDescriptor alloc] init];
        backgroundPipelineStateDescriptor.label = @"Draw background texture pipeline";
        backgroundPipelineStateDescriptor.vertexFunction = backgroundVertexFunction;
        backgroundPipelineStateDescriptor.fragmentFunction = backgroundFragmentFunction;
        backgroundPipelineStateDescriptor.colorAttachments[0].pixelFormat = [self colorPixelFormat];
        mtlBackgroundPipelineState = [mtlDevice newRenderPipelineStateWithDescriptor:backgroundPipelineStateDescriptor error:&error];
        NSAssert(mtlBackgroundPipelineState, @"Failed to create background pipeline state: %@", error);

        // configure a pipeline descriptor that is used to draw AmanithSVG surface texture
        id<MTLFunction> surfaceVertexFunction = [mtlLibrary newFunctionWithName:@"texturedVertexShader"];
        id<MTLFunction> surfaceFragmentFunction = [mtlLibrary newFunctionWithName:@"texturedClampFragmentShader"];
        MTLRenderPipelineDescriptor *surfacePipelineStateDescriptor = [[MTLRenderPipelineDescriptor alloc] init];
        surfacePipelineStateDescriptor.label = @"Draw surface texture pipeline";
        surfacePipelineStateDescriptor.vertexFunction = surfaceVertexFunction;
        surfacePipelineStateDescriptor.fragmentFunction = surfaceFragmentFunction;
        surfacePipelineStateDescriptor.colorAttachments[0].pixelFormat = [self colorPixelFormat];
        surfacePipelineStateDescriptor.colorAttachments[0].blendingEnabled = YES;
        surfacePipelineStateDescriptor.colorAttachments[0].rgbBlendOperation = MTLBlendOperationAdd;
        surfacePipelineStateDescriptor.colorAttachments[0].alphaBlendOperation = MTLBlendOperationAdd;
        surfacePipelineStateDescriptor.colorAttachments[0].sourceRGBBlendFactor = MTLBlendFactorSourceAlpha;
        surfacePipelineStateDescriptor.colorAttachments[0].sourceAlphaBlendFactor = MTLBlendFactorSourceAlpha;
        surfacePipelineStateDescriptor.colorAttachments[0].destinationRGBBlendFactor = MTLBlendFactorOneMinusSourceAlpha;
        surfacePipelineStateDescriptor.colorAttachments[0].destinationAlphaBlendFactor = MTLBlendFactorOneMinusSourceAlpha;
        mtlSurfacePipelineState = [mtlDevice newRenderPipelineStateWithDescriptor:surfacePipelineStateDescriptor error:&error];
        NSAssert(mtlSurfacePipelineState, @"Failed to create surface pipeline state: %@", error);

        // configure a pipeline descriptor that is used to draw a solid black frame surrounding the SVG document
        id<MTLFunction> frameVertexFunction = [mtlLibrary newFunctionWithName:@"geometricVertexShader"];
        id<MTLFunction> frameFragmentFunction = [mtlLibrary newFunctionWithName:@"coloredFragmentShader"];
        MTLRenderPipelineDescriptor *framePipelineStateDescriptor = [[MTLRenderPipelineDescriptor alloc] init];
        framePipelineStateDescriptor.label = @"Draw SVG document frame pipeline";
        framePipelineStateDescriptor.vertexFunction = frameVertexFunction;
        framePipelineStateDescriptor.fragmentFunction = frameFragmentFunction;
        framePipelineStateDescriptor.colorAttachments[0].pixelFormat = [self colorPixelFormat];
        mtlFramePipelineState = [mtlDevice newRenderPipelineStateWithDescriptor:framePipelineStateDescriptor error:&error];
        NSAssert(mtlFramePipelineState, @"Failed to create frame pipeline state: %@", error);

        // generate pattern texture, used to draw the background
        [self genPatternTexture];

        // initialize private members
        selectingFile = SVGT_FALSE;
        forceRedraw = SVGT_TRUE;
        resizing = SVGT_FALSE;
        oldMouseX = 0.0f;
        oldMouseY = 0.0f;
        svgSurface = SVGT_INVALID_HANDLE;
        svgDoc = SVGT_INVALID_HANDLE;
        surfaceTranslation[0] = 0.0f;
        surfaceTranslation[1] = 0.0f;
    }

    return self;
}

// called whenever view changes orientation or layout is changed; NB: this method is NOT called when the view/window is first opened
- (void) mtkView:(nonnull MTKView *)view drawableSizeWillChange:(CGSize)size {

    (void)view;
    // save the size of the drawable to pass to the vertex shader
    viewportSize.x = size.width;
    viewportSize.y = size.height;
    // resize AmanithSVG surface (in order to match the new window dimensions), then draw the loaded SVG document
    [self sceneResize :viewportSize.x :viewportSize.y];
}

- (void) drawInMTKView :(nonnull MTKView *)view {

    (void)view;

    // update the scene, if we are not resizing the window and a redraw is needed
    if (forceRedraw && (!resizing)) {
        forceRedraw = SVGT_FALSE;
        // draw scene
        [self sceneDraw];
    }
}

#endif

- (void) dealloc {

#if defined(USE_OPENGL)
    // stop the display link BEFORE releasing anything in the view
    // otherwise the display link thread may call into the view and
    // crash when it encounters something that has been release
    CVDisplayLinkStop(displayLink);
    // release the display link
    CVDisplayLinkRelease(displayLink);
    // destroy used textures
    [self deleteTextures];
#else
    // destroy used textures
    [self deleteTextures];
    // destroy Metal command queue
    [mtlCommandQueue release];
    mtlCommandQueue = nil;
#endif

    // destroy SVG resources allocated by the viewer
    [self viewerDestroy];
    [super dealloc];
}

// textures setup
- (void) genPatternTexture {

#if defined(USE_OPENGL)
    SVGTuint col0 = bgraSupport ? BACKGROUND_PATTERN_COL0 : [self swapRedBlue :BACKGROUND_PATTERN_COL0];
    SVGTuint col1 = bgraSupport ? BACKGROUND_PATTERN_COL1 : [self swapRedBlue :BACKGROUND_PATTERN_COL1];
#else
    SVGTuint col0 = BACKGROUND_PATTERN_COL0;
    SVGTuint col1 = BACKGROUND_PATTERN_COL1;
    MTLRegion region = {
        { 0, 0, 0 },                                                // MTLOrigin
        { BACKGROUND_PATTERN_WIDTH, BACKGROUND_PATTERN_HEIGHT, 1 }  // MTLSize
    };
    MTLTextureDescriptor* textureDescriptor = [[MTLTextureDescriptor alloc] init];
#endif
    // allocate pixels
    SVGTuint* pixels = malloc(BACKGROUND_PATTERN_WIDTH * BACKGROUND_PATTERN_HEIGHT * 4);

    if (pixels != NULL) {
        SVGTint i, j;
        for (i = 0; i < BACKGROUND_PATTERN_HEIGHT; ++i) {
            // use ARGB pixel format
            for (j = 0; j < BACKGROUND_PATTERN_WIDTH; ++j) {
                if (i < BACKGROUND_PATTERN_HEIGHT / 2) {
                    pixels[i * BACKGROUND_PATTERN_WIDTH + j] = (j < BACKGROUND_PATTERN_WIDTH / 2) ? col0 : col1;
                }
                else {
                    pixels[i * BACKGROUND_PATTERN_WIDTH + j] = (j < BACKGROUND_PATTERN_WIDTH / 2) ? col1 : col0;
                }
            }
        }

    #if defined(USE_OPENGL)
        glGenTextures(1, &patternTexture);
        glEnable(GL_TEXTURE_2D);
        glBindTexture(GL_TEXTURE_2D, patternTexture);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
        glTexImage2D(GL_TEXTURE_2D, 0, internalFormat, BACKGROUND_PATTERN_WIDTH, BACKGROUND_PATTERN_HEIGHT, 0, externalFormat, GL_UNSIGNED_BYTE, pixels);
    #else
        // indicate that each pixel has a blue, green, red, and alpha channel, where each channel is
        // an 8-bit unsigned normalized value (i.e. 0 maps to 0.0 and 255 maps to 1.0)
        textureDescriptor.pixelFormat = MTLPixelFormatBGRA8Unorm;
        // set the pixel dimensions of the texture
        textureDescriptor.width = BACKGROUND_PATTERN_WIDTH;
        textureDescriptor.height = BACKGROUND_PATTERN_HEIGHT;
        // create the texture from the device by using the descriptor
        patternTexture = [mtlDevice newTextureWithDescriptor:textureDescriptor];
        // upload pixels
        [patternTexture replaceRegion:region mipmapLevel:0 withBytes:pixels bytesPerRow:BACKGROUND_PATTERN_WIDTH*4];
    #endif
        free(pixels);
    }
}

- (void) genSurfaceTexture {

    // get AmanithSVG surface dimensions and pixels pointer
    SVGTint surfaceWidth = svgtSurfaceWidth(svgSurface);
    SVGTint surfaceHeight = svgtSurfaceHeight(svgSurface);
    const void* surfacePixels = svgtSurfacePixels(svgSurface);

#if defined(USE_OPENGL)
    glGenTextures(1, &surfaceTexture);
    glEnable(GL_TEXTURE_2D);
    glBindTexture(GL_TEXTURE_2D, surfaceTexture);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);

    if (bgraSupport) {
        if (npotSupport) {
            // allocate texture memory and upload pixels
            glTexImage2D(GL_TEXTURE_2D, 0, internalFormat, surfaceWidth, surfaceHeight, 0, externalFormat, GL_UNSIGNED_BYTE, surfacePixels);
        }
        else {
            // allocate texture memory
            glTexImage2D(GL_TEXTURE_2D, 0, internalFormat, [self pow2Get :surfaceWidth], [self pow2Get :surfaceHeight], 0, externalFormat, GL_UNSIGNED_BYTE, NULL);
            // upload pixels
            glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, surfaceWidth, surfaceHeight, externalFormat, GL_UNSIGNED_BYTE, surfacePixels);
        }
    }
    else {
        if (swizzleSupport) {
            // set swizzle
            const GLint bgraSwizzle[] = { GL_BLUE, GL_GREEN, GL_RED, GL_ALPHA };
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_SWIZZLE_R, bgraSwizzle[0]);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_SWIZZLE_G, bgraSwizzle[1]);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_SWIZZLE_B, bgraSwizzle[2]);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_SWIZZLE_A, bgraSwizzle[3]);
            if (npotSupport) {
                // allocate texture memory and upload pixels
                glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, surfaceWidth, surfaceHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, surfacePixels);
            }
            else {
                // allocate texture memory
                glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, [self pow2Get :surfaceWidth], [self pow2Get :surfaceHeight], 0, GL_RGBA, GL_UNSIGNED_BYTE, NULL);
                // upload pixels
                glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, surfaceWidth, surfaceHeight, GL_RGBA, GL_UNSIGNED_BYTE, surfacePixels);
            }
        }
        else {
            // we must pass through a temporary buffer
            SVGTuint* rgbaPixels = malloc(surfaceWidth * surfaceHeight * sizeof(SVGTuint));
            if (rgbaPixels != NULL) {
                // copy AmanithSVG drawing surface content into the specified pixels buffer, taking care to swap red <--> blue channels
                if (svgtSurfaceCopy(svgSurface, rgbaPixels, SVGT_TRUE, SVGT_FALSE) == SVGT_NO_ERROR) {
                    if (npotSupport) {
                        // allocate texture memory and upload pixels
                        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, surfaceWidth, surfaceHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, rgbaPixels);
                    }
                    else {
                        // allocate texture memory
                        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, [self pow2Get :surfaceWidth], [self pow2Get :surfaceHeight], 0, GL_RGBA, GL_UNSIGNED_BYTE, NULL);
                        // upload pixels
                        glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, surfaceWidth, surfaceHeight, GL_RGBA, GL_UNSIGNED_BYTE, rgbaPixels);
                    }
                }
                free(rgbaPixels);
            }
        }
    }
#else
    MTLRegion region = {
        { 0, 0, 0 },                        // MTLOrigin
        { surfaceWidth, surfaceHeight, 1 }  // MTLSize
    };
    MTLTextureDescriptor* textureDescriptor = [[MTLTextureDescriptor alloc] init];

    // indicate that each pixel has a blue, green, red, and alpha channel, where each channel is
    // an 8-bit unsigned normalized value (i.e. 0 maps to 0.0 and 255 maps to 1.0)
    textureDescriptor.pixelFormat = MTLPixelFormatBGRA8Unorm;
    // set the pixel dimensions of the texture
    textureDescriptor.width = surfaceWidth;
    textureDescriptor.height = surfaceHeight;
    // create the texture from the device by using the descriptor
    surfaceTexture = [mtlDevice newTextureWithDescriptor:textureDescriptor];
    // upload pixels
    [surfaceTexture replaceRegion:region mipmapLevel:0 withBytes:surfacePixels bytesPerRow:surfaceWidth*4];
#endif
}

#if defined(USE_OPENGL)
- (void) drawBackgroundTexture {

    // get view dimensions in pixels, taking care of Retina display
    // see https://developer.apple.com/library/archive/documentation/GraphicsImaging/Conceptual/OpenGL-MacProgGuide/EnablingOpenGLforHighResolution/EnablingOpenGLforHighResolution.html
    NSRect backingBounds = [self convertRectToBacking:[self bounds]];
    NSSize backingSize = backingBounds.size;
    SVGTfloat u = backingSize.width / BACKGROUND_PATTERN_WIDTH;
    SVGTfloat v = backingSize.height / BACKGROUND_PATTERN_HEIGHT;

    glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
    glDisable(GL_BLEND);
    glEnable(GL_TEXTURE_2D);
    glBindTexture(GL_TEXTURE_2D, patternTexture);
    // simply put a quad, covering the whole window
    [self texturedRectangleDraw :0.0f :0.0f :backingSize.width :backingSize.height :u :v];
}

- (void) drawSurfaceTexture {

    // get AmanithSVG surface dimensions
    SVGTfloat surfaceWidth = (SVGTfloat)svgtSurfaceWidth(svgSurface);
    SVGTfloat surfaceHeight = (SVGTfloat)svgtSurfaceHeight(svgSurface);
    SVGTfloat tx = (SVGTfloat)((SVGTint)(surfaceTranslation[0] + 0.5f));
    SVGTfloat ty = (SVGTfloat)((SVGTint)(surfaceTranslation[1] + 0.5f));
    SVGTfloat u, v;

    if (npotSupport) {
        u = 1.0f;
        v = 1.0f;
    }
    else {
        // greater (or equal) power of two values
        SVGTfloat texWidth = (SVGTfloat)[self pow2Get :svgtSurfaceWidth(svgSurface)];
        SVGTfloat texHeight = (SVGTfloat)[self pow2Get :svgtSurfaceHeight(svgSurface)];
        u = (surfaceWidth - 0.5f) / texWidth;
        v = (surfaceHeight - 0.5f) / texHeight;
    }

    // enable per-pixel alpha blending, using surface texture as source
    glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
    glEnable(GL_BLEND);
    glEnable(GL_TEXTURE_2D);
    glBindTexture(GL_TEXTURE_2D, surfaceTexture);
    // simply put a quad
    [self texturedRectangleDraw :tx :ty :surfaceWidth :surfaceHeight :u :v];

    // draw a solid black frame surrounding the SVG document
    glColor4f(0.0f, 0.0f, 0.0f, 1.0f);
    glDisable(GL_BLEND);
    glDisable(GL_TEXTURE_2D);
    [self rectangleDraw :tx :ty :surfaceWidth :2.0f];
    [self rectangleDraw :tx :ty :2.0f :surfaceHeight];
    [self rectangleDraw :tx :ty + surfaceHeight - 2.0f :surfaceWidth :2.0f];
    [self rectangleDraw :tx + surfaceWidth - 2.0f :ty :2.0f :surfaceHeight];
}
#else
- (void) drawBackgroundTexture :(id<MTLRenderCommandEncoder>)commandEncoder {

    SVGTfloat width = (SVGTfloat)viewportSize.x;
    SVGTfloat height = (SVGTfloat)viewportSize.y;
    SVGTfloat u = width / BACKGROUND_PATTERN_WIDTH;
    SVGTfloat v = height / BACKGROUND_PATTERN_HEIGHT;

    [commandEncoder setRenderPipelineState:mtlBackgroundPipelineState];
    [commandEncoder setFragmentTexture:patternTexture atIndex:0];
    [self texturedRectangleDraw :commandEncoder :0.0f :0.0f :width :height :u :v];
}

- (void) drawSurfaceTexture :(id<MTLRenderCommandEncoder>)commandEncoder {

    vector_float4 blackCol = { 0.0f, 0.0f, 0.0f, 1.0f };
    // get AmanithSVG surface dimensions
    SVGTfloat surfaceWidth = (SVGTfloat)svgtSurfaceWidth(svgSurface);
    SVGTfloat surfaceHeight = (SVGTfloat)svgtSurfaceHeight(svgSurface);
    SVGTfloat tx = (SVGTfloat)((SVGTint)(surfaceTranslation[0] + 0.5f));
    SVGTfloat ty = (SVGTfloat)((SVGTint)(surfaceTranslation[1] + 0.5f));

    [commandEncoder setRenderPipelineState:mtlSurfacePipelineState];
    [commandEncoder setFragmentTexture:surfaceTexture atIndex:0];
    [self texturedRectangleDraw :commandEncoder :tx :ty :surfaceWidth :surfaceHeight :1.0f :1.0f];

    // draw a solid black frame surrounding the SVG document
    [commandEncoder setRenderPipelineState:mtlFramePipelineState];
    [commandEncoder setFragmentBytes:&blackCol length:sizeof(blackCol) atIndex:0];
    [self rectangleDraw :commandEncoder :tx :ty :surfaceWidth :2.0f];
    [self rectangleDraw :commandEncoder :tx :ty :2.0f :surfaceHeight];
    [self rectangleDraw :commandEncoder :tx :ty + surfaceHeight - 2.0f :surfaceWidth :2.0f];
    [self rectangleDraw :commandEncoder :tx + surfaceWidth - 2.0f :ty :2.0f :surfaceHeight];
}
#endif

- (void) deleteTextures {

#if defined(USE_OPENGL)
    if (patternTexture != 0) {
        glDeleteTextures(1, &patternTexture);
        patternTexture = 0;
    }

    if (surfaceTexture != 0) {
        glDeleteTextures(1, &surfaceTexture);
        surfaceTexture = 0;
    }
#else
    if (patternTexture != nil) {
        [patternTexture setPurgeableState:MTLPurgeableStateEmpty];
        [patternTexture release];
        patternTexture = nil;
    }
    if (surfaceTexture != nil) {
        [surfaceTexture setPurgeableState:MTLPurgeableStateEmpty];
        [surfaceTexture release];
        surfaceTexture = nil;
    }
#endif
}

// resize AmanithSVG surface to the given dimensions, then draw the loaded SVG document
- (void) svgDraw :(SVGTuint)width :(SVGTuint)height {

    if (svgSurface != SVGT_INVALID_HANDLE) {
        // destroy current surface texture
    #if defined(USE_OPENGL)
        if (surfaceTexture != 0) {
            glDeleteTextures(1, &surfaceTexture);
            surfaceTexture = 0;
        }
    #else
        if (surfaceTexture != nil) {
            [surfaceTexture setPurgeableState:MTLPurgeableStateEmpty];
            [surfaceTexture release];
            surfaceTexture = nil;
        }
    #endif
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

// viewer functions
- (void) sceneDraw {
#if defined(USE_OPENGL)
    // get view dimensions in pixels, taking care of Retina display
    // see https://developer.apple.com/library/archive/documentation/GraphicsImaging/Conceptual/OpenGL-MacProgGuide/EnablingOpenGLforHighResolution/EnablingOpenGLforHighResolution.html
    NSRect backingBounds = [self convertRectToBacking:[self bounds]];
    SVGTuint backingPixelWidth = (SVGTuint)backingBounds.size.width;
    SVGTuint backingPixelHeight = (SVGTuint)backingBounds.size.height;

    // set OpenGL viewport and projection
    glViewport(0, 0, backingPixelWidth, backingPixelHeight);
    [self projectionLoad :0.0f :backingPixelWidth :0.0f :backingPixelHeight];
    // clear OpenGL buffer
    glClear(GL_COLOR_BUFFER_BIT);
    // draw pattern background
    [self drawBackgroundTexture];
    // put AmanithSVG surface using per-pixel alpha blend
    if ((svgDoc != SVGT_INVALID_HANDLE) && (svgSurface != SVGT_INVALID_HANDLE)) {
        [self drawSurfaceTexture];
    }
#else
    id<MTLCommandBuffer> commandBuffer = [mtlCommandQueue commandBuffer];
    // obtain a render pass descriptor generated from the view's drawable textures
    MTLRenderPassDescriptor* passDescriptor = [self currentRenderPassDescriptor];

    if (passDescriptor != nil) {
        id<MTLRenderCommandEncoder> commandEncoder = [commandBuffer renderCommandEncoderWithDescriptor:passDescriptor];
        // set the region of the drawable to draw into
        [commandEncoder setViewport:(MTLViewport){ 0.0, 0.0, viewportSize.x, viewportSize.y, 0.0, 1.0 }];
        // draw pattern background
        [self drawBackgroundTexture:commandEncoder];
        // put AmanithSVG surface using per-pixel alpha blend
        if ((svgDoc != SVGT_INVALID_HANDLE) && (svgSurface != SVGT_INVALID_HANDLE)) {
            [self drawSurfaceTexture:commandEncoder];
        }
        [commandEncoder endEncoding];
        [commandBuffer presentDrawable:[self currentDrawable]];
    }

    // finalize rendering and push the command buffer to the GPU
    [commandBuffer commit];
#endif    
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

    // we have finished with the resizing, now we can update the window content (i.e. draw)
    resizing = SVGT_FALSE;
    forceRedraw = SVGT_TRUE;
}

- (void) windowResize :(const SimpleRect*)desiredRect {

    NSRect r;
    // get current window dimensions
    NSWindow* window = [self window];
    // get screen dimensions
    SimpleRect screenRect = [self screenDimensionsGet];
    // clamp window dimensions against screen bounds
    SVGTfloat w = MIN(desiredRect->width, screenRect.width);
    SVGTfloat h = MIN(desiredRect->height, screenRect.height);
    NSRect bounds = [self convertRectToBacking:[self bounds]];

    // move / resize the window
    r.origin.x = 0;
    r.origin.y = 0;
    r.size.width = w;
    r.size.height = h;
    // take care of Retina display
    r = [self convertRectFromBacking :r];
    // we want to be sure that a "resize" event will be fired (the setContentSize won't emit a reshape event if new dimensions are equal to the current ones)
    [window setContentSize:r.size];
    [window center];
    if (((SVGTuint)bounds.size.width == desiredRect->width) && ((SVGTuint)bounds.size.height == desiredRect->height)) {
        [self sceneResize :desiredRect->width :desiredRect->height];
    }
}

- (void) deltaTranslation :(SVGTfloat)deltaX :(SVGTfloat)deltaY {

    surfaceTranslation[0] += deltaX;
    surfaceTranslation[1] += deltaY;
    forceRedraw = SVGT_TRUE;
}

- (SVGTboolean) viewerInit :(const char*)fileName {

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

// destroy SVG resources allocated by the viewer
- (void) viewerDestroy {

    // destroy the SVG document
    svgtDocDestroy(svgDoc);
    // destroy the drawing surface
    svgtSurfaceDestroy(svgSurface);
    // deinitialize AmanithSVG library
    svgtDone();
}

// mouse and keyboard events
- (void) mouseDown: (NSEvent *)theEvent {

    NSPoint p;

    // convert window location into view location
    p = [theEvent locationInWindow];
    p = [self convertPoint: p fromView: nil];
    p = [self convertPointToBacking:p];

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
    p = [self convertPointToBacking:p];
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

// menu handlers
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
                if ([self viewerInit :fileName]) {
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

// from NSWindowDelegate
- (void)windowWillClose:(NSNotification *)note {

    (void)note;

#if defined(USE_OPENGL)
    // Stop the display link when the window is closing because default
    // OpenGL render buffers will be destroyed. If display link continues to
    // fire without renderbuffers, OpenGL draw calls will set errors.
    CVDisplayLinkStop(displayLink);
#endif    
    done = SVGT_TRUE;
}
@end

// ---------------------------------------------------------------
//                             Main
// ---------------------------------------------------------------
static void applicationMenuPopulate(NSMenu* subMenu,
                                    const ViewerView* view) {

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

static void mainMenuPopulate(const ViewerView* view) {

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

static void applicationMenuCreate(const ViewerView* view) {

    mainMenuPopulate(view);
}

int main(int argc,
         char *argv[]) {

    (void)argc;
    (void)argv;

    @autoreleasepool {

        NSScreen* screen = [NSScreen mainScreen];
        NSRect frame = NSMakeRect(0, 0, INITIAL_WINDOW_WIDTH, INITIAL_WINDOW_HEIGHT);

        // take care of Retina display
        frame = [screen convertRectFromBacking :frame];

        // get application
        NSApplication* app = [NSApplication sharedApplication];
        [NSApp setActivationPolicy: NSApplicationActivationPolicyRegular];

        // create the window
        NSWindow* window = [[NSWindow alloc] initWithContentRect:frame styleMask:NSWindowStyleMaskTitled | NSWindowStyleMaskClosable | NSWindowStyleMaskMiniaturizable | NSWindowStyleMaskResizable backing:NSBackingStoreBuffered defer: TRUE];
        [window setAcceptsMouseMovedEvents:YES];
        [window setTitle: @ WINDOW_TITLE];

    #if defined(USE_OPENGL)
        // create the OpenGL view
        ViewerView* view = [[ViewerView alloc] initWithFrame: frame];
    #else
        // create the Metal view
        id<MTLDevice> mtlDevice = MTLCreateSystemDefaultDevice();
        ViewerView* view = [[ViewerView alloc] initWithFrame:frame device:mtlDevice];
    #endif

        // link the view to the window
        [window setDelegate: view];
        [window setContentView: view];
        [window makeFirstResponder: view];
        // do not allow a content size bigger than the maximum surface dimension that AmanithVG can handle
        NSRect maxRect = NSMakeRect(0.0f, 0.0f, svgtSurfaceMaxDimension(), svgtSurfaceMaxDimension());
        maxRect = [screen convertRectFromBacking :maxRect];
        [window setContentMaxSize: maxRect.size];
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
