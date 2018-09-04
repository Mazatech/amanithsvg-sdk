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
#import <UIKit/UIKit.h>
#import <QuartzCore/QuartzCore.h>
#import <MobileCoreServices/MobileCoreServices.h>
#include <OpenGLES/ES1/gl.h>
#include <OpenGLES/ES1/glext.h>
#include "svg_player.h"

@interface View : UIView <UIGestureRecognizerDelegate, UIDocumentPickerDelegate> {

    CAEAGLLayer* eaglLayer;
    // OpenGL ES 1.1 context
    EAGLContext* eaglContext;
    // DisplayLink
    CADisplayLink* displayLink;
    // framebuffer
    GLuint frameBuffer;
    // OpenGL ES color buffer
    GLuint colorRenderBuffer;
    SVGTint colorRenderBufferWidth;
    SVGTint colorRenderBufferHeight;
    // keep track if we are selecting/opening an SVG file
    SVGTboolean selectingFile;
    // keep track of touch position
    SVGTfloat oldTouchX;
    SVGTfloat oldTouchY;
    // SVG surface and document
    SVGTHandle svgSurface;
    SVGTHandle svgDoc;
    // OpenGL texture used to draw the pattern background
    GLuint patternTexture;
    // OpenGL texture used to draw the AmanithSVG surface
    GLuint surfaceTexture;
    SVGTfloat surfaceTranslation[2];
}

@end
