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
#import "View.h"
#include <math.h>
#include <sys/utsname.h>

// background pattern (dimensions and ARGB colors)
#define BACKGROUND_PATTERN_WIDTH 32
#define BACKGROUND_PATTERN_HEIGHT 32
#define BACKGROUND_PATTERN_COL0 0xFF808080
#define BACKGROUND_PATTERN_COL1 0xFFC0C0C0

@implementation View

/*****************************************************************
                               EGL
*****************************************************************/
+ (Class)layerClass {
    return [CAEAGLLayer class];
}

- (void) eaglLayerSetup {

    eaglLayer = (CAEAGLLayer*)self.layer;
    eaglLayer.opaque = YES;

    if ([[UIScreen mainScreen] respondsToSelector:@selector(displayLinkWithTarget:selector:)]) {
        // take care of Retina display
        if ([UIScreen mainScreen].scale > 0.0f) {
            self.contentScaleFactor = [UIScreen mainScreen].scale;
            eaglLayer.contentsScale = [UIScreen mainScreen].scale;
        }
    }
}

- (SVGTboolean) eaglContextSetup {

    // OpenGL ES 1.1 context
    eaglContext = [[EAGLContext alloc] initWithAPI:kEAGLRenderingAPIOpenGLES1];
    if (!eaglContext) {
        NSLog(@"Failed to initialize OpenGL ES 1.1 context");
        return SVGT_FALSE;
    }
    
    if (![EAGLContext setCurrentContext:eaglContext]) {
        NSLog(@"Failed to set current OpenGL context");
        return SVGT_FALSE;
    }

    return SVGT_TRUE;
}

/*****************************************************************
                            OpenGL ES
*****************************************************************/
- (SVGTboolean) glesFrameBufferCreate {

    GLint width, height;
    GLenum status;

    // generate framebuffer object
    glGenFramebuffersOES(1, &frameBuffer);
    glBindFramebufferOES(GL_FRAMEBUFFER_OES, frameBuffer);
    // create and attach color buffer
    glGenRenderbuffersOES(1, &colorRenderBuffer);
    glBindRenderbufferOES(GL_RENDERBUFFER_OES, colorRenderBuffer);
    [eaglContext renderbufferStorage:GL_RENDERBUFFER_OES fromDrawable:eaglLayer];
    glFramebufferRenderbufferOES(GL_FRAMEBUFFER_OES, GL_COLOR_ATTACHMENT0_OES, GL_RENDERBUFFER_OES, colorRenderBuffer);
    // retrieve the height and width of the color renderbuffer (see https://developer.apple.com/library/ios/documentation/3ddrawing/conceptual/opengles_programmingguide/WorkingwithEAGLContexts/WorkingwithEAGLContexts.html)
    // "Here, the code retrieves the width and height from the color renderbuffer after its storage is allocated.
    // Your app does this because the actual dimensions of the color renderbuffer are calculated based on the layer's bounds and scale factor.
    // Other renderbuffers attached to the framebuffer must have the same dimensions. In addition to using the height and width to allocate
    // the depth buffer, use them to assign the OpenGL ES viewport and to help determine the level of detail required in your app’s textures and models"
    glGetRenderbufferParameterivOES(GL_RENDERBUFFER_OES, GL_RENDERBUFFER_WIDTH_OES, &width);
    glGetRenderbufferParameterivOES(GL_RENDERBUFFER_OES, GL_RENDERBUFFER_HEIGHT_OES, &height);
    // final checkup
    status = glCheckFramebufferStatusOES(GL_FRAMEBUFFER_OES);
    if (status != GL_FRAMEBUFFER_COMPLETE_OES) {
        NSLog(@"glesFrameBufferCreate: failed to build a complete framebuffer object; status code: %x", status);
        return SVGT_FALSE;
    }

    // set viewport
    glViewport(0, 0, width, height);
    // keep track of color buffer dimensions
    colorRenderBufferWidth = width;
    colorRenderBufferHeight = height;
    return SVGT_TRUE;
}

- (void) glesFrameBufferDestroy {

    if (frameBuffer) {
        glDeleteFramebuffersOES(1, &frameBuffer);
        frameBuffer = 0;
    }
    if (colorRenderBuffer) {
        glDeleteRenderbuffersOES(1, &colorRenderBuffer);
        colorRenderBuffer = 0;
    }
}

- (void) glesInit {

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
}

/*****************************************************************
                            OpenVG
*****************************************************************/
- (void) fileChooseDialog {

    if (!selectingFile) {

        selectingFile = SVGT_TRUE;

        // open only SVG files
        NSArray* types = @[(NSString*)kUTTypeFolder,(NSString*)kUTTypeScalableVectorGraphics];
        UIDocumentPickerViewController* documentPicker = [[UIDocumentPickerViewController alloc] initWithDocumentTypes:types inMode:UIDocumentPickerModeImport];
        documentPicker.delegate = self;

        // find top view controller
        UIViewController* topViewController = [[[[UIApplication sharedApplication] delegate] window] rootViewController];
        while ((topViewController != NULL) && topViewController.presentedViewController) {
            topViewController = topViewController.presentedViewController;
        }
        // if found, present the document picker
        if (topViewController != NULL) {
            [topViewController presentViewController:documentPicker animated:NO completion:nil];
        }
    }
}

- (void)didMoveToSuperview {

    // select SVG file
    [self fileChooseDialog];
}

- (void) documentPicker:(UIDocumentPickerViewController *)controller didPickDocumentAtURL:(NSURL *)url {

    if (controller.documentPickerMode == UIDocumentPickerModeImport) {
        // get SVG file content, as a C string
        NSString* xmlStr = [NSString stringWithContentsOfURL:url encoding:NSUTF8StringEncoding error:nil];
        const char* xml = [xmlStr UTF8String];
        // create the SVG document
        if ([self playerInit :xml]) {
            // resize AmanithSVG surface, then draw the loaded SVG document
            [self sceneResize :colorRenderBufferWidth :colorRenderBufferHeight];
        }
    }

    // now we can select another file, if we desire
    selectingFile = SVGT_FALSE;
}

- (void) documentPickerWasCancelled:(UIDocumentPickerViewController *)controller {
    
    // now we can select another file, if we desire
    selectingFile = SVGT_FALSE;
}

// get screen dimensions, in pixels
- (SimpleRect) screenDimensionsGet {

    CGRect screenRect = [[UIScreen mainScreen] bounds];
    SimpleRect result = {
        screenRect.size.width,
        screenRect.size.height
    };
    return result;
}

// get screen dpi
- (SVGTfloat) screenDpiGet {

    struct utsname systemInfo;
    uname(&systemInfo);
    NSString* code = [NSString stringWithCString:systemInfo.machine encoding:NSUTF8StringEncoding];
    // taken from https://github.com/lmirosevic/GBDeviceInfo/blob/master/GBDeviceInfo/GBDeviceInfo_iOS.m
    NSDictionary* deviceNamesByCode = @{
        // 1st Gen
        @"iPhone1,1" :@163,
        // 3G
        @"iPhone1,2" :@163,
        // 3GS
        @"iPhone2,1" :@163,
        // 4
        @"iPhone3,1" :@326,
        @"iPhone3,2" :@326,
        @"iPhone3,3" :@326,
        // 4S
        @"iPhone4,1" :@326,
        // 5
        @"iPhone5,1" :@326,
        @"iPhone5,2" :@326,
        // 5c
        @"iPhone5,3" :@326,
        @"iPhone5,4" :@326,
        // 5s
        @"iPhone6,1" :@326,
        @"iPhone6,2" :@326,
        // 6 Plus
        @"iPhone7,1" :@401,
        // 6
        @"iPhone7,2" :@326,
        // 6s
        @"iPhone8,1" :@326,
        // 6s Plus
        @"iPhone8,2" :@401,
        // SE
        @"iPhone8,4" :@326,
        // 7
        @"iPhone9,1" :@326,
        @"iPhone9,3" :@326,
        // 7 Plus
        @"iPhone9,2" :@401,
        @"iPhone9,4" :@401,
        // 1
        @"iPad1,1"   :@132,
        // 2
        @"iPad2,1"   :@132,
        @"iPad2,2"   :@132,
        @"iPad2,3"   :@132,
        @"iPad2,4"   :@132,
        // Mini
        @"iPad2,5"   :@163,
        @"iPad2,6"   :@163,
        @"iPad2,7"   :@163,
        // 3
        @"iPad3,1"   :@264,
        @"iPad3,2"   :@264,
        @"iPad3,3"   :@264,
        // 4
        @"iPad3,4"   :@264,
        @"iPad3,5"   :@264,
        @"iPad3,6"   :@264,
        // Air
        @"iPad4,1"   :@264,
        @"iPad4,2"   :@264,
        @"iPad4,3"   :@264,
        // Mini 2
        @"iPad4,4"   :@326,
        @"iPad4,5"   :@326,
        @"iPad4,6"   :@326,
        // Mini 3
        @"iPad4,7"   :@326,
        @"iPad4,8"   :@326,
        @"iPad4,9"   :@326,
        // Mini 4
        @"iPad5,1"   :@326,
        @"iPad5,2"   :@326,
        // Air 2
        @"iPad5,3"   :@264,
        @"iPad5,4"   :@264,
        // Pro 12.9-inch
        @"iPad6,7"   :@264,
        @"iPad6,8"   :@264,
        // Pro 9.7-inch
        @"iPad6,3"   :@264,
        @"iPad6,4"   :@264,
        // iPad 5th Gen, 2017
        @"iPad6,11"  :@264,
        @"iPad6,12"  :@264,
        // 1st Gen
        @"iPod1,1"   :@163,
        // 2nd Gen
        @"iPod2,1"   :@163,
        // 3rd Gen
        @"iPod3,1"   :@163,
        // 4th Gen
        @"iPod4,1"   :@326,
        // 5th Gen
        @"iPod5,1"   :@326,
        // 6th Gen
        @"iPod7,1"   :@326
    };

    SVGTfloat result;
    NSInteger dpi = [[deviceNamesByCode objectForKey:code] integerValue];

    if (!dpi) {
        SVGTfloat scale = 1.0f;
        if ([[UIScreen mainScreen] respondsToSelector:@selector(scale)]) {
            scale = [[UIScreen mainScreen] scale];
        }
        // dpi estimation (not accurate)
        if (UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPad) {
            result = 132.0f * scale;
        }
        else
        if (UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPhone) {
            result = 163.0f * scale;
        }
        else {
            result = 160.0f * scale;
        }
    }
    else {
        result = (SVGTfloat)dpi;
    }

    return result;
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

- (void) render :(CADisplayLink*)displayLink {

    // draw scene
    [self sceneDraw];

    // present color buffer
    [eaglContext presentRenderbuffer:GL_RENDERBUFFER_OES];
}

- (SVGTboolean)resizeFromLayer:(CAEAGLLayer *)layer {

    GLenum status;
    GLint glWidth, glHeight;

    glBindFramebufferOES(GL_FRAMEBUFFER_OES, frameBuffer);
    glBindRenderbufferOES(GL_RENDERBUFFER_OES, colorRenderBuffer);
    [eaglContext renderbufferStorage:GL_RENDERBUFFER_OES fromDrawable:layer];
    // retrieve the height and width of the color renderbuffer
    glGetRenderbufferParameterivOES(GL_RENDERBUFFER_OES, GL_RENDERBUFFER_WIDTH_OES, &glWidth);
    glGetRenderbufferParameterivOES(GL_RENDERBUFFER_OES, GL_RENDERBUFFER_HEIGHT_OES, &glHeight);
    status = glCheckFramebufferStatusOES(GL_FRAMEBUFFER_OES);
    if (status != GL_FRAMEBUFFER_COMPLETE_OES) {
        NSLog(@"resizeFromLayer: failed to resize the framebuffer object; status code: %x", status);
        return SVGT_FALSE;
    }

    // resize AmanithSVG surface (in order to match the new view dimensions), then draw the loaded SVG document
    [self sceneResize :glWidth :glHeight];

    // keep track of color buffer dimensions
    colorRenderBufferWidth = glWidth;
    colorRenderBufferHeight = glHeight;
    return SVGT_TRUE;
}

// if our view is resized, we'll be asked to layout subviews: this is the perfect opportunity to also update
// the framebuffer so that it is the same size as our display area
-(void)layoutSubviews {

    [EAGLContext setCurrentContext:eaglContext];
    [self resizeFromLayer:(CAEAGLLayer*)self.layer];
}

- (void) displayLinkCreate {

    displayLink = [CADisplayLink displayLinkWithTarget:self selector:@selector(render:)];
}

- (void) displayLinkStart {

    [displayLink addToRunLoop:[NSRunLoop currentRunLoop] forMode:NSDefaultRunLoopMode];
}

- (void) displayLinkStop {

    [displayLink invalidate];
}

- (void) gesturesSetup {

    // pan gesture
    UIPanGestureRecognizer* panRecognizer = [[UIPanGestureRecognizer alloc] initWithTarget:self action:@selector(move:)];
    [panRecognizer setMinimumNumberOfTouches:1];
    [panRecognizer setMaximumNumberOfTouches:1];
    [panRecognizer setDelegate:self];
    [self addGestureRecognizer:panRecognizer];

    // tap gesture
    UITapGestureRecognizer* tapRecognizer = [[UITapGestureRecognizer alloc] initWithTarget:self action:@selector(doubleTap:)];
    [tapRecognizer setNumberOfTapsRequired:2];
    [tapRecognizer setDelegate:self];
    [self addGestureRecognizer:tapRecognizer];
}

- (void) move :(id)sender {

    CGPoint translatedPoint = [(UIPanGestureRecognizer*)sender locationInView:self];
    translatedPoint.x *= [[UIScreen mainScreen] scale];
    translatedPoint.y *= [[UIScreen mainScreen] scale];
 
    if ([(UIPanGestureRecognizer*)sender state] == UIGestureRecognizerStateBegan) {
        // keep track of touch position
        oldTouchX = translatedPoint.x;
        oldTouchY = translatedPoint.y;
    }
    else
    if ([(UIPanGestureRecognizer*)sender state] == UIGestureRecognizerStateEnded) {
        // nothing to do
    }
    else {
        // update translation
        [self deltaTranslation :(translatedPoint.x - oldTouchX) :(translatedPoint.y - oldTouchY)];
        oldTouchX = translatedPoint.x;
        oldTouchY = translatedPoint.y;
    }
}

- (void) doubleTap :(id)sender {

    if ([(UITapGestureRecognizer*)sender state] == UIGestureRecognizerStateEnded) {
        // select SVG file
        [self fileChooseDialog];
    }
}

- (id) initWithFrame :(CGRect)frame {

    self = [super initWithFrame:frame];

    if (self) {
        // setup EAGL layer
        [self eaglLayerSetup];
        // setup EAGL context
        if ([self eaglContextSetup]) {
            // setup framebuffer
            if ([self glesFrameBufferCreate]) {

                // initialize private members
                selectingFile = SVGT_FALSE;
                oldTouchX = 0.0f;
                oldTouchY = 0.0f;
                svgSurface = SVGT_INVALID_HANDLE;
                svgDoc = SVGT_INVALID_HANDLE;
                surfaceTexture = 0;
                surfaceTranslation[0] = 0.0f;
                surfaceTranslation[1] = 0.0f;
                patternTexture = 0;

                // set basic OpenGL states and viewport
                [self glesInit];
                // setup gestures
                [self setMultipleTouchEnabled:YES];
                [self gesturesSetup];
            }
            else {
                exit(EXIT_FAILURE);
            }
        }
        else {
            exit(EXIT_FAILURE);
        }

        // create and activate the display link (i.e. render loop)
        [self displayLinkCreate];
        [self displayLinkStart];
    }

    return self;
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
        glEnable(GL_TEXTURE_2D);
        glBindTexture(GL_TEXTURE_2D, patternTexture);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
        // select best format, in order to avoid swizzling
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, BACKGROUND_PATTERN_WIDTH, BACKGROUND_PATTERN_HEIGHT, 0, GL_BGRA, GL_UNSIGNED_BYTE, pixels);
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
    glEnable(GL_TEXTURE_2D);
    glBindTexture(GL_TEXTURE_2D, surfaceTexture);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
    // select best format, in order to avoid swizzling
    glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, surfaceWidth, surfaceHeight, 0, GL_BGRA, GL_UNSIGNED_BYTE, surfacePixels);
}

- (void) drawBackgroundTexture {

    // get frame dimensions
    SVGTfloat u = (SVGTfloat)colorRenderBufferWidth / BACKGROUND_PATTERN_WIDTH;
    SVGTfloat v = (SVGTfloat)colorRenderBufferHeight / BACKGROUND_PATTERN_HEIGHT;

    glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
    glDisable(GL_BLEND);
    glEnable(GL_TEXTURE_2D);
    glBindTexture(GL_TEXTURE_2D, patternTexture);
    // simply put a quad, covering the whole window
    [self texturedRectangleDraw :0.0f :0.0f :(SVGTfloat)colorRenderBufferWidth :(SVGTfloat)colorRenderBufferHeight :u :v];
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

    glEnable(GL_TEXTURE_2D);
    glBindTexture(GL_TEXTURE_2D, surfaceTexture);
    // simply put a quad
    [self texturedRectangleDraw :tx :ty :surfaceWidth :surfaceHeight :1.0f :1.0f];

    // draw a solid black frame surrounding the SVG document
    glColor4f(0.0f, 0.0f, 0.0f, 1.0f);
    glDisable(GL_BLEND);
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
}

- (void) dealloc {

    // stop the display link BEFORE releasing anything in the view
    [self displayLinkStop];
    displayLink = nil;

    // destroy SVG resources allocated by the player
    [self playerDestroy];

    // destroy OpenGL ES buffers
    [self glesFrameBufferDestroy];

    // unbind OpenGL context
    if ([EAGLContext currentContext] == eaglContext) {
        [EAGLContext setCurrentContext:nil];
    }
    eaglContext = nil;

    [super dealloc];
}

- (void) deltaTranslation :(SVGTfloat)deltaX :(SVGTfloat)deltaY {

    surfaceTranslation[0] += deltaX;
    surfaceTranslation[1] -= deltaY;
}

- (SVGTboolean) playerInit :(const char*)xmlText {

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
        svgDoc = svgtDocCreate(xmlText);
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

@end
