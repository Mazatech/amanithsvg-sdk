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
#import <Cocoa/Cocoa.h>
#import <Metal/Metal.h>
#import <MetalKit/MetalKit.h>
#import <os/log.h>
#if (MAC_OS_X_VERSION_MIN_REQUIRED >= MAC_OS_X_VERSION_12_0)
    // for the definition of UTTypeFolder and UTTypeSVG (macOS 12+)
    #import <UniformTypeIdentifiers/UTCoreTypes.h>
#endif
// header shared between C code here, which executes Metal API commands, and .metal files, which uses these types as inputs to the shaders.
#import "ShaderTypes.h"
#include <stdio.h>
#include "svg_viewer.h"

#define WINDOW_TITLE "SVG Viewer"

// default window dimensions
#define INITIAL_WINDOW_WIDTH 600
#define INITIAL_WINDOW_HEIGHT 800

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
//              External resources (fonts and images)
// ---------------------------------------------------------------
#define SVG_RESOURCES_COUNT 5
static SVGExternalResource resources[SVG_RESOURCES_COUNT] = {
    // filename,                    type,                    buffer, size, hints
    { "bebas_neue_regular.ttf",     SVGT_RESOURCE_TYPE_FONT, NULL,   0U,   SVGT_RESOURCE_HINT_DEFAULT_FANTASY_FONT },
    { "dancing_script_regular.ttf", SVGT_RESOURCE_TYPE_FONT, NULL,   0U,   SVGT_RESOURCE_HINT_DEFAULT_CURSIVE_FONT },
    { "noto_mono_regular.ttf",      SVGT_RESOURCE_TYPE_FONT, NULL,   0U,   SVGT_RESOURCE_HINT_DEFAULT_MONOSPACE_FONT |
                                                                           SVGT_RESOURCE_HINT_DEFAULT_UI_MONOSPACE_FONT },
    { "noto_sans_regular.ttf",      SVGT_RESOURCE_TYPE_FONT, NULL,   0U,   SVGT_RESOURCE_HINT_DEFAULT_SANS_SERIF_FONT |
                                                                           SVGT_RESOURCE_HINT_DEFAULT_UI_SANS_SERIF_FONT |
                                                                           SVGT_RESOURCE_HINT_DEFAULT_SYSTEM_UI_FONT |
                                                                           SVGT_RESOURCE_HINT_DEFAULT_UI_ROUNDED_FONT },
    { "noto_serif_regular.ttf",     SVGT_RESOURCE_TYPE_FONT, NULL,   0U,   SVGT_RESOURCE_HINT_DEFAULT_SERIF_FONT |
                                                                           SVGT_RESOURCE_HINT_DEFAULT_UI_SERIF_FONT }
};

// ---------------------------------------------------------------
//                        View (interface)
// ---------------------------------------------------------------
@interface ViewerView : MTKView <MTKViewDelegate, NSWindowDelegate, NSApplicationDelegate> {

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

    // custom log object (Apple unified logging system)
    os_log_t logger;
    // AmanithSVG log buffer
    char* logBuffer;

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

// ---------------------------------------------------------------
//                    AmanithSVG initialization
// ---------------------------------------------------------------
- (void) amanithsvgResourceAdd :(SVGExternalResource*)resource {

    NSString* resourcePath;
    char fext[64] = { '\0' };
    char fname[128] = { '\0' };

    // extract filename without extension (e.g. myfont.ttf --> myfont)
    extractFileName(fname, resource->fileName, SVGT_FALSE);
    // extract file extension (e.g. myfont.ttf --> ttf)
    extractFileExt(fext, resource->fileName);
    // get resource file path
    if ((resourcePath = [[NSBundle mainBundle] pathForResource:@(fname) ofType:@(fext)]) != NULL) {

        // load the resource file into memory
        size_t bufferSize = 0U;
        SVGTubyte* buffer = loadResourceFile(resourcePath.UTF8String, &bufferSize);

        if ((buffer != NULL) && (bufferSize > 0U)) {
            // fill the result structure
            resource->buffer = buffer;
            resource->bufferSize = bufferSize;
            // provide AmanithSVG with the resource
            (void)svgtResourceSet(resource->fileName, buffer, (SVGTuint)bufferSize, resource->type, resource->hints);
        }
    }
}

// make external resources (fonts/images) available to AmanithSVG
- (void) amanithsvgResourcesLoad {

    SVGTuint i;

    // load external resources and provide them to AmanithSVG
    for (i = 0U; i < SVG_RESOURCES_COUNT; ++i) {
        [self amanithsvgResourceAdd:&resources[i]];
    }
}

// release in-memory external resources (fonts/images) provided to AmanithSVG at initialization time
- (void) amanithsvgResourcesRelease {

    SVGTuint i;

    for (i = 0U; i < SVG_RESOURCES_COUNT; ++i) {
        // release allocated memory
        if (resources[i].buffer != NULL) {
            free(resources[i].buffer);
        }
    }
}

// initialize AmanithSVG and load external resources
- (SVGTboolean) amanithsvgInit {

    SVGTErrorCode err;
    // get screen dimensions
    SimpleRect screenRect = [self screenDimensionsGet];

    // initialize AmanithSVG
    if ((err = svgtInit(screenRect.width, screenRect.height, [self screenDpiGet])) == SVGT_NO_ERROR) {
        // set curves quality (used by AmanithSVG geometric kernel to approximate curves with straight
        // line segments (flattening); valid range is [1; 100], where 100 represents the best quality
        (void)svgtConfigSet(SVGT_CONFIG_CURVES_QUALITY, AMANITHSVG_CURVES_QUALITY);
        // specify the system/user-agent language; this setting will affect the conditional rendering
        // of <switch> elements and elements with 'systemLanguage' attribute specified
        (void)svgtLanguageSet(AMANITHSVG_USER_AGENT_LANGUAGE);
        // make external resources available to AmanithSVG; NB: all resources must be specified in
        // advance before to call rendering-related functions, which are by definition tied to a thread
        [self amanithsvgResourcesLoad];
    }

    return (err == SVGT_NO_ERROR) ? SVGT_TRUE : SVGT_FALSE;
}

// terminate AmanithSVG library
- (void) amanithsvgRelease {

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
    [self amanithsvgResourcesRelease];
}

// ---------------------------------------------------------------
//                        AmanithSVG log
// ---------------------------------------------------------------

// append the message to the AmanithSVG log buffer
- (void) logPrint :(const char*)message :(SVGTLogLevel)level {

    (void)svgtLogPrint(message, level);
}

// append an informational message to the AmanithSVG log buffer
- (void) logInfo :(const char*)message {

    // push the message to AmanithSVG log buffer
    [self logPrint :message :SVGT_LOG_LEVEL_INFO];
}

- (void) logInit :(const char*)fullFileName {

    NSString* bundleIdentifier = [[NSBundle mainBundle] bundleIdentifier];

    // NB: a value is always returned
    logger = os_log_create([bundleIdentifier UTF8String], "AmanithSVG");
    if ((logBuffer = calloc(AMANITHSVG_LOG_BUFFER_SIZE, sizeof(char))) != NULL) {
        // enable all log levels
        if (svgtLogBufferSet(logBuffer, AMANITHSVG_LOG_BUFFER_SIZE, SVGT_LOG_LEVEL_ALL) == SVGT_NO_ERROR) {
            char fname[512] = { '\0' };
            char msg[1024] = { '\0' };
            // extract the filename only from the full path
            extractFileName(fname, fullFileName, SVGT_TRUE);
            // keep track of the SVG filename within AmanithSVG log buffer
            sprintf(msg, "Loading and parsing SVG file %s", fname);
            [self logInfo :msg];
        }
    }
    else {
        NSLog(@"Error allocating AmanithSVG log buffer");
    }
}

- (void) logDestroy {

    // make sure AmanithSVG no longer uses a log buffer (i.e. disable logging)
    (void)svgtLogBufferSet(NULL, 0U, 0U);
    // release allocated memory
    if (logBuffer != NULL) {
        free(logBuffer);
        logBuffer = NULL;
    }
    // release logger object
    logger = nil;
}

// output AmanithSVG log content, using the Apple unified logging system
- (void) logOutput {

    if (logBuffer != NULL) {

        SVGTuint info[4] = { 0U, 0U, 0U, 0U };
        SVGTErrorCode err = svgtLogBufferInfo(info);

        // info[2] = current length, in characters (i.e. the total number of
        // characters written, included the trailing '\0')
        if ((err == SVGT_NO_ERROR) && (info[2] > 1U)) {
            os_log_debug(logger, "%s", logBuffer);
        }
    }
}

// ---------------------------------------------------------------
//                         File picker
// ---------------------------------------------------------------

// handler for "Open" menu option
- (void) fileChooseDialog :(id)sender {

    (void)sender;

    if (!selectingFile) {
        // create an open document panel
        NSOpenPanel* panel = [NSOpenPanel openPanel];
        // open only .svg files
    #if (MAC_OS_X_VERSION_MIN_REQUIRED >= MAC_OS_X_VERSION_12_0)
        // macOS 12+
        NSArray<UTType*>* fileTypes = @[UTTypeFolder, UTTypeSVG];
        [panel setAllowedContentTypes:fileTypes];
    #else
        NSArray* fileTypes = [[NSArray alloc] initWithObjects:@"svg", @"SVG", nil];
        [panel setAllowedFileTypes:fileTypes];
    #endif
        selectingFile = SVGT_TRUE;

        // display the panel
        [panel beginWithCompletionHandler:^(NSInteger result) {
            // lets load the SVG file, if OK button has been clicked
            if (result == NSModalResponseOK) {
                // grab a reference to what has been selected
                NSURL* fileUrl = [[panel URLs]objectAtIndex:0];
                // write our file name to a label
                NSString* filePath = [fileUrl path];
                // load and draw the choosen SVG file
                [self pickedDocumentLoad:[filePath fileSystemRepresentation]];
            }
            // now we can select another file, if we desire
            selectingFile = SVGT_FALSE;
        }];
    }
}

// resize the window in order to match the given dimensions
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
    // we want to be sure that a "resize" event will be fired (see drawableSizeWillChange)
    // NB: the setContentSize won't emit a reshape event if new dimensions are equal to the current ones
    [window setContentSize:r.size];
    [window center];
    if (((SVGTuint)bounds.size.width == desiredRect->width) && ((SVGTuint)bounds.size.height == desiredRect->height)) {
        [self sceneResize :desiredRect->width :desiredRect->height];
    }
}

// load and draw the choosen SVG file
- (void) pickedDocumentLoad :(const char*)fileName {

    // initialize AmanithSVG log (in order to keep track of possible errors
    // and warnings arising from the parsing/rendering of selected file)
    [self logInit:fileName];

    // destroy a previously loaded document, if any
    if (svgDoc != SVGT_INVALID_HANDLE) {
        svgtDocDestroy(svgDoc);
    }
    // load a new SVG file
    if ((svgDoc = loadSvgFile(fileName)) != SVGT_INVALID_HANDLE) {
        // get screen dimensions
        SimpleRect screenRect = [self screenDimensionsGet];
        // calculate AmanithSVG surface dimensions
        SimpleRect desiredRect = surfaceDimensionsCalc(svgDoc, screenRect.width, screenRect.height);
        // resize the window in order to match the desired surface dimensions
        // NB: this call will trigger the following chain of events:
        // mtkView:drawableSizeWillChange --> self:sceneResize
        [self windowResize:&desiredRect];
    }
}

// ---------------------------------------------------------------
//                          MTKView
// ---------------------------------------------------------------

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

// called whenever view changes orientation or layout is changed
// NB: this method is NOT called when the view/window is first opened
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

// ---------------------------------------------------------------
//                         Metal textures
// ---------------------------------------------------------------
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
        patternTexture = [mtlDevice newTextureWithDescriptor:textureDescriptor];
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
    surfaceTexture = [mtlDevice newTextureWithDescriptor:textureDescriptor];
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

- (void) deleteTextures {

    if (patternTexture != nil) {
        [patternTexture setPurgeableState:MTLPurgeableStateEmpty];
        patternTexture = nil;
    }
    if (surfaceTexture != nil) {
        [surfaceTexture setPurgeableState:MTLPurgeableStateEmpty];
        surfaceTexture = nil;
    }
}

// ---------------------------------------------------------------
//                       Event handlers
// ---------------------------------------------------------------
- (void) deltaTranslation :(SVGTfloat)deltaX :(SVGTfloat)deltaY {

    surfaceTranslation[0] += deltaX;
    surfaceTranslation[1] += deltaY;
    forceRedraw = SVGT_TRUE;
}

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

// ---------------------------------------------------------------
//                          SVG viewer
// ---------------------------------------------------------------

// resize AmanithSVG surface to the given dimensions, then draw the loaded SVG document
- (void) svgDraw :(SVGTuint)width :(SVGTuint)height {

    if (svgSurface != SVGT_INVALID_HANDLE) {
        // destroy current surface texture
        if (surfaceTexture != nil) {
            [surfaceTexture setPurgeableState:MTLPurgeableStateEmpty];
            surfaceTexture = nil;
        }
        // resize AmanithSVG surface
        svgtSurfaceResize(svgSurface, width, height);
    }
    else {
        // first time, we must create AmanithSVG surface
        svgSurface = svgtSurfaceCreate(width, height);
    }
    // clear the drawing surface (full transparent white)
    svgtSurfaceClear(svgSurface, 1.0f, 1.0f, 1.0f, 0.0f);
    // draw the SVG document (upon AmanithSVG surface)
    svgtDocDraw(svgDoc, svgSurface, SVGT_RENDERING_QUALITY_BETTER);
    // create surface texture
    [self genSurfaceTexture];
}

// draw the scene: background pattern and AmanithSVG surface (texture)
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
    [commandBuffer waitUntilCompleted];
}

// resize AmanithSVG surface and re-draw the loaded SVG document
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

    // output AmanithSVG log content, using the Apple unified logging system
    // NB: AmanithSVG log was initialized within the pickedDocumentLoad
    [self logOutput];
    // release AmanithSVG log buffer
    [self logDestroy];

    // we have finished with the resizing, now we can update the window content (i.e. draw)
    resizing = SVGT_FALSE;
    forceRedraw = SVGT_TRUE;
}

// from NSResponder
- (BOOL) acceptsFirstResponder {
    // as first responder, the receiver is the first object in the responder chain to be sent key events and action messages
    return YES;
}

// from NSResponder
- (BOOL) becomeFirstResponder {

    return YES;
}

// from NSResponder
- (BOOL) resignFirstResponder {

    return YES;
}

// from NSView
- (BOOL) isFlipped {

    return NO;
}

// handler for "Quit" menu option
- (void) applicationTerminate :(id)sender {

    (void)sender;

    // terminate the application
    [NSApp terminate:self];
}

// from NSWindowDelegate
- (void)windowWillClose:(NSNotification *)note {

    (void)note;

    // terminate the application
    [NSApp terminate:self];
}

// from NSApplicationDelegate
- (void)applicationDidFinishLaunching:(NSNotification *)notification {

    (void)notification;

    // initialize AmanithSVG and load external resources (fonts/images)
    [self amanithsvgInit];
}

// from NSApplicationDelegate
- (void)applicationWillTerminate:(NSNotification *)notification {

    (void)notification;

    // destroy used textures
    [self deleteTextures];
    // destroy Metal command queue
    mtlCommandQueue = nil;

    // terminate AmanithSVG library
    [self amanithsvgRelease];
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

    // create main menu = menu bar
    NSMenu* mainMenu = [[NSMenu alloc] initWithTitle:@"MainMenu"];
    // the titles of the menu items are for identification purposes only and shouldn't be localized; the strings in the menu bar come
    // from the submenu titles, except for the application menu, whose title is ignored at runtime
    NSMenuItem* menuItem = [mainMenu addItemWithTitle:@"Apple" action:NULL keyEquivalent:@""];
    NSMenu* subMenu = [[NSMenu alloc] initWithTitle:@"Apple"];

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

    NSWindow* window;
    ViewerView* view;
    NSRect frame, maxRect;
    // create the application
    NSScreen* screen = [NSScreen mainScreen];
    NSApplication* app = [NSApplication sharedApplication];
    // get the device instance Metal selects as the default
    id<MTLDevice> mtlDevice = MTLCreateSystemDefaultDevice();

    (void)argc;
    (void)argv;

    // take care of Retina display
    frame = NSMakeRect(0, 0, INITIAL_WINDOW_WIDTH, INITIAL_WINDOW_HEIGHT);
    frame = [screen convertRectFromBacking :frame];

    // create the window
    window = [[NSWindow alloc] initWithContentRect:frame styleMask:NSWindowStyleMaskTitled | NSWindowStyleMaskClosable | NSWindowStyleMaskMiniaturizable | NSWindowStyleMaskResizable backing:NSBackingStoreBuffered defer: TRUE];
    [window setAcceptsMouseMovedEvents:YES];
    [window setTitle: @WINDOW_TITLE];

    // create the Metal view
    view = [[ViewerView alloc] initWithFrame:frame device:mtlDevice];

    // link the view to the application
    [app setActivationPolicy:NSApplicationActivationPolicyRegular];
    [app setDelegate: view];

    // link the view to the window
    [window setDelegate: view];
    [window setContentView: view];
    [window makeFirstResponder: view];
    // do not allow a content size bigger than the maximum surface dimension that AmanithSVG can handle
    maxRect = NSMakeRect(0, 0, svgtSurfaceMaxDimension(), svgtSurfaceMaxDimension());
    maxRect = [screen convertRectFromBacking :maxRect];
    [window setContentMaxSize: maxRect.size];

    // center the window
    [window center];
    [window makeKeyAndOrderFront: nil];

    // create and populate the menu
    applicationMenuCreate(view);
    // starts the main event loop
    [app finishLaunching];
    [app run];

    return EXIT_SUCCESS;
}
