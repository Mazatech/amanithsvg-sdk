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
#import "AppDelegate.h"
#include <sys/utsname.h>

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

@implementation AppDelegate

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
        @"iPhone1,1"  :@163,
        // 3G
        @"iPhone1,2"  :@163,
        // 3GS
        @"iPhone2,1"  :@163,
        // 4
        @"iPhone3,1"  :@326,
        @"iPhone3,2"  :@326,
        @"iPhone3,3"  :@326,
        // 4S
        @"iPhone4,1"  :@326,
        // 5
        @"iPhone5,1"  :@326,
        @"iPhone5,2"  :@326,
        // 5c
        @"iPhone5,3"  :@326,
        @"iPhone5,4"  :@326,
        // 5s
        @"iPhone6,1"  :@326,
        @"iPhone6,2"  :@326,
        // 6 Plus
        @"iPhone7,1"  :@401,
        // 6
        @"iPhone7,2"  :@326,
        // 6s
        @"iPhone8,1"  :@326,
        // 6s Plus
        @"iPhone8,2"  :@401,
        // SE
        @"iPhone8,4"  :@326,
        // 7
        @"iPhone9,1"  :@326,
        @"iPhone9,3"  :@326,
        // 7 Plus
        @"iPhone9,2"  :@401,
        @"iPhone9,4"  :@401,
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
        // 11
        @"iPhone12,1" :@326,
        // 11 Pro
        @"iPhone12,3" :@458,
        // 11 Pro Max
        @"iPhone12,5" :@458,
        // SE 2
        @"iPhone12,8" :@326,
        // 12 mini
        @"iPhone13,1" :@476,
        // 12
        @"iPhone13,2" :@460,
        // 12 Pro
        @"iPhone13,3" :@460,
        // 12 Pro Max
        @"iPhone13,4" :@458,
        // 13 mini
        @"iPhone14,4" :@476,
        // 13
        @"iPhone14,5" :@460,
        // 13 Pro
        @"iPhone14,2" :@460,
        // 13 Pro Max
        @"iPhone14,3" :@458,
        // SE 3rd Gen
        @"iPhone14,6" :@326,
        // 14
        @"iPhone14,7" :@460,
        // 14 Plus
        @"iPhone14,8" :@458,
        // 14 Pro
        @"iPhone15,2" :@460,
        // 14 Pro Max
        @"iPhone15,3" :@460,
        // 1
        @"iPad1,1"    :@132,
        // 2
        @"iPad2,1"    :@132,
        @"iPad2,2"    :@132,
        @"iPad2,3"    :@132,
        @"iPad2,4"    :@132,
        // Mini
        @"iPad2,5"    :@163,
        @"iPad2,6"    :@163,
        @"iPad2,7"    :@163,
        // 3
        @"iPad3,1"    :@264,
        @"iPad3,2"    :@264,
        @"iPad3,3"    :@264,
        // 4
        @"iPad3,4"    :@264,
        @"iPad3,5"    :@264,
        @"iPad3,6"    :@264,
        // Air
        @"iPad4,1"    :@264,
        @"iPad4,2"    :@264,
        @"iPad4,3"    :@264,
        // Mini 2
        @"iPad4,4"    :@326,
        @"iPad4,5"    :@326,
        @"iPad4,6"    :@326,
        // Mini 3
        @"iPad4,7"    :@326,
        @"iPad4,8"    :@326,
        @"iPad4,9"    :@326,
        // Mini 4
        @"iPad5,1"    :@326,
        @"iPad5,2"    :@326,
        // Air 2
        @"iPad5,3"    :@264,
        @"iPad5,4"    :@264,
        // Pro 12.9-inch
        @"iPad6,7"    :@264,
        @"iPad6,8"    :@264,
        // Pro 9.7-inch
        @"iPad6,3"    :@264,
        @"iPad6,4"    :@264,
        // iPad 5th Gen, 2017
        @"iPad6,11"   :@264,
        @"iPad6,12"   :@264,
        // Pro 12.9-inch, 2017
        @"iPad7,1"    :@264,
        @"iPad7,2"    :@264,
        // Pro 10.5-inch, 2017
        @"iPad7,3"    :@264,
        @"iPad7,4"    :@264,
        // iPad 6th Gen, 2018
        @"iPad7,5"    :@264,
        @"iPad7,6"    :@264,
        // iPad Pro 3rd Gen 11-inch, 2018
        @"iPad8,1"    :@264,
        @"iPad8,3"    :@264,
        // iPad Pro 3rd Gen 11-inch 1TB, 2018
        @"iPad8,2"    :@264,
        @"iPad8,4"    :@264,
        // iPad Pro 3rd Gen 12.9-inch, 2018
        @"iPad8,5"    :@264,
        @"iPad8,7"    :@264,
        // iPad Pro 3rd Gen 12.9-inch 1TB, 2018
        @"iPad8,6"    :@264,
        @"iPad8,8"    :@264,
        // iPad Pro 4rd Gen 11-inch, 2020
        @"iPad8,9"    :@264,
        @"iPad8,10"   :@264,
        // iPad Pro 4rd Gen 12.9-inch, 2020
        @"iPad8,11"   :@264,
        @"iPad8,12"   :@264,
        // Mini 5
        @"iPad11,1"   :@326,
        @"iPad11,2"   :@326,
        // Air 3
        @"iPad11,3"   :@264,
        @"iPad11,4"   :@264,
        // iPad 8th Gen, 2020
        @"iPad11,6"   :@264,
        @"iPad11,7"   :@264,
        // Air 4
        @"iPad13,1"   :@264,
        @"iPad13,2"   :@264,
        // iPad Pro 3rd Gen 11-inch, 2021
        @"iPad13,4"   :@264,
        @"iPad13,5"   :@264,
        @"iPad13,6"   :@264,
        @"iPad13,7"   :@264,
        // iPad Pro 5th Gen 12.9-inch, 2021
        @"iPad13,8"   :@264,
        @"iPad13,9"   :@264,
        @"iPad13,10"  :@264,
        @"iPad13,11"  :@264,
        // Air 5, 2022
        @"iPad13,16"  :@264,
        @"iPad13,17"  :@264,
        // mini 6
        @"iPad14,1"   :@326,
        @"iPad14,2"   :@326,
        // iPad 9th Gen, 2021
        @"iPad12,1"   :@264,
        @"iPad12,2"   :@264,
        // iPad 10th Gen, 2022
        @"iPad13,18"  :@264,
        @"iPad13,19"  :@264,
        // iPad Pro 4th Gen 11-inch, 2022
        @"iPad14,3"   :@264,
        @"iPad14,4"   :@264,
        // iPad Pro 6th Gen 12.9-inch, 2022
        @"iPad14,5"   :@264,
        @"iPad14,6"   :@264,
        // 1st Gen
        @"iPod1,1"    :@163,
        // 2nd Gen
        @"iPod2,1"    :@163,
        // 3rd Gen
        @"iPod3,1"    :@163,
        // 4th Gen
        @"iPod4,1"    :@326,
        // 5th Gen
        @"iPod5,1"    :@326,
        // 6th Gen
        @"iPod7,1"    :@326,
        // 7th Gen
        @"iPod9,1"    :@326
    };

    SVGTfloat result;
    NSInteger dpi = [[deviceNamesByCode objectForKey:code] integerValue];

    if (!dpi) {

        SVGTfloat scale = 1.0f;

        // get the natural scale factor associated with the screen
        // for Retina displays, the scale factor may be 3.0 or 2.0
        // for standard-resolution displays, the scale factor is 1.0
        // and one point equals one pixel
        if ([[UIScreen mainScreen] respondsToSelector:@selector(scale)]) {
            scale = [[UIScreen mainScreen] scale];
        }

    #if (__IPHONE_OS_VERSION_MIN_REQUIRED < __IPHONE_13_0)
        // UI_USER_INTERFACE_IDIOM() macro is deprecated from iOS 13
        UIUserInterfaceIdiom uiIdiom = UI_USER_INTERFACE_IDIOM();
    #else
        // iOS 13+
        UIDevice* device = [UIDevice currentDevice];
        UIUserInterfaceIdiom uiIdiom = device.userInterfaceIdiom;
    #endif
        // dpi estimation (not accurate)
        if (uiIdiom == UIUserInterfaceIdiomPad) {
            result = 132.0f * scale;
        }
        else
        if (uiIdiom == UIUserInterfaceIdiomPhone) {
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
        [self amanithsvgResourceAdd :&resources[i]];
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

    // terminate AmanithSVG library, freeing its all allocated resources
    // NB: after this call, all AmanithSVG rendering functions will have no effect
    svgtDone();

    // release in-memory external resources (fonts/images)
    // provided to AmanithSVG at initialization time
    [self amanithsvgResourcesRelease];
}

- (BOOL)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)launchOptions {

    (void)launchOptions;

    // initialize AmanithSVG and load external resources (fonts/images)
    [self amanithsvgInit];

    return YES;
}

- (void) applicationWillResignActive :(UIApplication *)application {
    
    // Sent when the application is about to move from active to inactive state. This can occur for certain types of temporary interruptions (such as an incoming phone call or SMS message) or
    // when the user quits the application and it begins the transition to the background state. Use this method to pause ongoing tasks, disable timers, and throttle down OpenGL ES frame rates.
    // Games should use this method to pause the game.
}

- (void) applicationDidEnterBackground :(UIApplication *)application {
    
    // Use this method to release shared resources, save user data, invalidate timers, and store enough application state information to restore your application to its current state in case
    // it is terminated later. If your application supports background execution, this method is called instead of applicationWillTerminate: when the user quits.
}

- (void) applicationWillEnterForeground :(UIApplication *)application {
    
    // Called as part of the transition from the background to the inactive state; here you can undo many of the changes made on entering the background.
}

- (void) applicationDidBecomeActive :(UIApplication *)application {
    
    // Restart any tasks that were paused (or not yet started) while the application was inactive. If the application was previously in the background, optionally refresh the user interface.
}

- (void) applicationWillTerminate :(UIApplication *)application {

    // called when the application is about to terminate
    (void)application;

    // terminate AmanithSVG library
    [self amanithsvgRelease];
}

@end
