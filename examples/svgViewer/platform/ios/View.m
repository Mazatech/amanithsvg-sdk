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
#import "View.h"
// header shared between C code here, which executes Metal API commands, and .metal files, which uses these types as inputs to the shaders.
#import "ShaderTypes.h"
#include <math.h>
#include <sys/utsname.h>

// background pattern (dimensions and ARGB colors)
#define BACKGROUND_PATTERN_WIDTH 32
#define BACKGROUND_PATTERN_HEIGHT 32
#define BACKGROUND_PATTERN_COL0 0xFF808080
#define BACKGROUND_PATTERN_COL1 0xFFC0C0C0

@implementation View

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

- (void) didMoveToSuperview {

    // select SVG file
    [self fileChooseDialog];
}

- (void) documentPicker :(UIDocumentPickerViewController *)controller didPickDocumentAtURL:(NSURL *)url {

    if (controller.documentPickerMode == UIDocumentPickerModeImport) {
        // get SVG file content, as a C string
        NSString* xmlStr = [NSString stringWithContentsOfURL:url encoding:NSUTF8StringEncoding error:nil];
        const char* xml = [xmlStr UTF8String];
        // create the SVG document
        if ([self viewerInit :xml]) {
            // resize AmanithSVG surface, then draw the loaded SVG document
            [self sceneResize :viewportSize.x :viewportSize.y];
        }
    }

    // now we can select another file, if we desire
    selectingFile = SVGT_FALSE;
}

- (void) documentPickerWasCancelled :(UIDocumentPickerViewController *)controller {
    
    // now we can select another file, if we desire
    selectingFile = SVGT_FALSE;
}

// get screen dimensions, in pixels
- (SimpleRect) screenDimensionsGet {

    CGRect screenRect = [[UIScreen mainScreen] nativeBounds];
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
    // taken from http://github.com/lmirosevic/GBDeviceInfo/blob/master/GBDeviceInfo/GBDeviceInfo_iOS.m
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
        // 8
        @"iPhone10,1" :@326,
        @"iPhone10,4" :@326,
        // 8 Plus
        @"iPhone10,2" :@401,
        @"iPhone10,5" :@401,
        // X
        @"iPhone10,3" :@458,
        @"iPhone10,6" :@458,
        // XR
        @"iPhone11,8" :@326,
        // XS
        @"iPhone11,2" :@458,
        // XS Max
        @"iPhone11,4" :@458,
        @"iPhone11,6" :@458,
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
        // Pro 12.9-inch, 2017
        @"iPad7,1"   :@264,
        @"iPad7,2"   :@264,
        // Pro 10.5-inch, 2017
        @"iPad7,3"   :@264,
        @"iPad7,4"   :@264,
        // iPad 6th Gen, 2018
        @"iPad7,5"   :@264,
        @"iPad7,6"   :@264,
        // iPad Pro 3rd Gen 11-inch, 2018
        @"iPad8,1"   :@264,
        @"iPad8,3"   :@264,
        // iPad Pro 3rd Gen 11-inch 1TB, 2018
        @"iPad8,2"   :@264,
        @"iPad8,4"   :@264,
        // iPad Pro 3rd Gen 12.9-inch, 2018
        @"iPad8,5"   :@264,
        @"iPad8,7"   :@264,
        // iPad Pro 3rd Gen 12.9-inch 1TB, 2018
        @"iPad8,6"   :@264,
        @"iPad8,8"   :@264,
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
        @"iPod7,1"   :@326,
        // 7th Gen
        @"iPod9,1"   :@326
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

- (void) initView {

    __autoreleasing NSError* error = nil;

    self.clearColor = MTLClearColorMake(0.0, 0.0, 0.0, 1.0);
    viewportSize.x = (SVGTuint)[self drawableSize].width;
    viewportSize.y = (SVGTuint)[self drawableSize].height;

    // keep track of Metal command queue
    mtlCommandQueue = [[self device] newCommandQueue];
    mtlLibrary = [[self device] newDefaultLibrary];

    // configure a pipeline descriptor that is used to draw the background texture
    id<MTLFunction> backgroundVertexFunction = [mtlLibrary newFunctionWithName:@"texturedVertexShader"];
    id<MTLFunction> backgroundFragmentFunction = [mtlLibrary newFunctionWithName:@"texturedRepeatFragmentShader"];
    MTLRenderPipelineDescriptor *backgroundPipelineStateDescriptor = [[MTLRenderPipelineDescriptor alloc] init];
    backgroundPipelineStateDescriptor.label = @"Draw background texture pipeline";
    backgroundPipelineStateDescriptor.vertexFunction = backgroundVertexFunction;
    backgroundPipelineStateDescriptor.fragmentFunction = backgroundFragmentFunction;
    backgroundPipelineStateDescriptor.colorAttachments[0].pixelFormat = [self colorPixelFormat];
    mtlBackgroundPipelineState = [[self device] newRenderPipelineStateWithDescriptor:backgroundPipelineStateDescriptor error:&error];
    NSAssert(mtlBackgroundPipelineState, @"initView: failed to create background pipeline state: %@", error);

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
    mtlSurfacePipelineState = [[self device] newRenderPipelineStateWithDescriptor:surfacePipelineStateDescriptor error:&error];
    NSAssert(mtlSurfacePipelineState, @"initView: failed to create surface pipeline state: %@", error);

    // configure a pipeline descriptor that is used to draw a solid black frame surrounding the SVG document
    id<MTLFunction> frameVertexFunction = [mtlLibrary newFunctionWithName:@"geometricVertexShader"];
    id<MTLFunction> frameFragmentFunction = [mtlLibrary newFunctionWithName:@"coloredFragmentShader"];
    MTLRenderPipelineDescriptor *framePipelineStateDescriptor = [[MTLRenderPipelineDescriptor alloc] init];
    framePipelineStateDescriptor.label = @"Draw SVG document frame pipeline";
    framePipelineStateDescriptor.vertexFunction = frameVertexFunction;
    framePipelineStateDescriptor.fragmentFunction = frameFragmentFunction;
    framePipelineStateDescriptor.colorAttachments[0].pixelFormat = [self colorPixelFormat];
    mtlFramePipelineState = [[self device] newRenderPipelineStateWithDescriptor:framePipelineStateDescriptor error:&error];
    NSAssert(mtlFramePipelineState, @"initView: failed to create frame pipeline state: %@", error);

    // generate pattern texture, used to draw the background
    [self genPatternTexture];
    // setup gestures
    [self setMultipleTouchEnabled:YES];
    [self gesturesSetup];

    // initialize private members
    selectingFile = SVGT_FALSE;
    oldTouchX = 0.0f;
    oldTouchY = 0.0f;
    svgSurface = SVGT_INVALID_HANDLE;
    svgDoc = SVGT_INVALID_HANDLE;
    surfaceTranslation[0] = 0.0f;
    surfaceTranslation[1] = 0.0f;
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

// MTKView by default has its own loop continuously calling the delegate's drawInMTKView method
- (void) drawInMTKView :(nonnull MTKView *)view {

    (void)view;

    // draw scene
    [self sceneDraw];
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

    CGPoint movedPoint = [(UIPanGestureRecognizer*)sender locationInView:self];
    movedPoint.x *= [[UIScreen mainScreen] nativeScale];
    movedPoint.y *= [[UIScreen mainScreen] nativeScale];
 
    if ([(UIPanGestureRecognizer*)sender state] == UIGestureRecognizerStateBegan) {
        // keep track of touch position
        oldTouchX = movedPoint.x;
        oldTouchY = movedPoint.y;
    }
    else
    if ([(UIPanGestureRecognizer*)sender state] == UIGestureRecognizerStateEnded) {
        // nothing to do
    }
    else {
        // update translation
        [self deltaTranslation :(movedPoint.x - oldTouchX) :(movedPoint.y - oldTouchY)];
        oldTouchX = movedPoint.x;
        oldTouchY = movedPoint.y;
    }
}

- (void) doubleTap :(id)sender {

    if ([(UITapGestureRecognizer*)sender state] == UIGestureRecognizerStateEnded) {
        // select SVG file
        [self fileChooseDialog];
    }
}

// textures setup
- (void) genPatternTexture {

    SVGTuint col0 = BACKGROUND_PATTERN_COL0;
    SVGTuint col1 = BACKGROUND_PATTERN_COL1;
    MTLRegion region = {
        { 0, 0, 0 },                                                // MTLOrigin
        { BACKGROUND_PATTERN_WIDTH, BACKGROUND_PATTERN_HEIGHT, 1 }  // MTLSize
    };
    MTLTextureDescriptor* textureDescriptor = [[MTLTextureDescriptor alloc] init];
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

        // indicate that each pixel has a blue, green, red, and alpha channel, where each channel is
        // an 8-bit unsigned normalized value (i.e. 0 maps to 0.0 and 255 maps to 1.0)
        textureDescriptor.pixelFormat = MTLPixelFormatBGRA8Unorm;
        // set the pixel dimensions of the texture
        textureDescriptor.width = BACKGROUND_PATTERN_WIDTH;
        textureDescriptor.height = BACKGROUND_PATTERN_HEIGHT;
        // create the texture from the device by using the descriptor
        patternTexture = [[self device] newTextureWithDescriptor:textureDescriptor];
        // upload pixels
        [patternTexture replaceRegion:region mipmapLevel:0 withBytes:pixels bytesPerRow:BACKGROUND_PATTERN_WIDTH*4];

        free(pixels);
    }
}

- (void) genSurfaceTexture {

    // get AmanithSVG surface dimensions and pixels pointer
    SVGTint surfaceWidth = svgtSurfaceWidth(svgSurface);
    SVGTint surfaceHeight = svgtSurfaceHeight(svgSurface);
    const void* surfacePixels = svgtSurfacePixels(svgSurface);
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
    surfaceTexture = [[self device] newTextureWithDescriptor:textureDescriptor];
    // upload pixels
    [surfaceTexture replaceRegion:region mipmapLevel:0 withBytes:surfacePixels bytesPerRow:surfaceWidth*4];
}

- (void) drawBackgroundTexture :(id<MTLRenderCommandEncoder>)commandEncoder {

    SVGTfloat width = (SVGTfloat)viewportSize.x;
    SVGTfloat height = (SVGTfloat)viewportSize.y;
    SVGTfloat u = width / BACKGROUND_PATTERN_WIDTH;
    SVGTfloat v = height / BACKGROUND_PATTERN_HEIGHT;

    [commandEncoder setRenderPipelineState:mtlBackgroundPipelineState];
    [commandEncoder setFragmentTexture:patternTexture atIndex:0];
    [self texturedRectangleDraw :commandEncoder :0.0f :0.0f :width :height :u :v];
}

- (void) drawSurfaceTexture : (id<MTLRenderCommandEncoder>)commandEncoder {

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

- (void) deleteTextures {

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
}

// resize AmanithSVG surface to the given dimensions, then draw the loaded SVG document
- (void) svgDraw :(SVGTuint)width :(SVGTuint)height {

    if (svgSurface != SVGT_INVALID_HANDLE) {
        // destroy current surface texture
        if (surfaceTexture != nil) {
            [surfaceTexture setPurgeableState:MTLPurgeableStateEmpty];
            [surfaceTexture release];
            surfaceTexture = nil;
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

// viewer functions
- (void) sceneDraw {

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
}

- (void) dealloc {

    // destroy used textures
    [self deleteTextures];
    // destroy Metal command queue
    [mtlCommandQueue release];
    mtlCommandQueue = nil;

    // destroy SVG resources allocated by the viewer
    [self viewerDestroy];
    [super dealloc];
}

- (void) deltaTranslation :(SVGTfloat)deltaX :(SVGTfloat)deltaY {

    surfaceTranslation[0] += deltaX;
    surfaceTranslation[1] -= deltaY;
}

- (SVGTboolean) viewerInit :(const char*)xmlText {

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

// destroy SVG resources allocated by the viewer
- (void) viewerDestroy {

    // destroy the SVG document
    svgtDocDestroy(svgDoc);
    // destroy the drawing surface
    svgtSurfaceDestroy(svgSurface);
    // deinitialize AmanithSVG library
    svgtDone();
}

@end
