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
#import <QuartzCore/QuartzCore.h>
#import <MobileCoreServices/MobileCoreServices.h>
#import <Metal/Metal.h>
#import <MetalKit/MetalKit.h>
#include <simd/simd.h>
#include "svg_viewer.h"

@interface View : MTKView <MTKViewDelegate, UIGestureRecognizerDelegate, UIDocumentPickerDelegate> {

    // Metal command queue
    id<MTLCommandQueue> mtlCommandQueue;
    id<MTLLibrary> mtlLibrary;
    // Metal rendering pipelines
    id<MTLRenderPipelineState> mtlBackgroundPipelineState;
    id<MTLRenderPipelineState> mtlSurfacePipelineState;
    id<MTLRenderPipelineState> mtlFramePipelineState;
    // Metal texture used to draw the pattern background
    id<MTLTexture> patternTexture;
    // Metal texture used to draw the AmanithSVG surface
    id<MTLTexture> surfaceTexture;
    // The current size of the view, used as an input to the vertex shader
    vector_uint2 viewportSize;

    // keep track if we are selecting/opening an SVG file
    SVGTboolean selectingFile;
    // keep track of touch position
    SVGTfloat oldTouchX;
    SVGTfloat oldTouchY;
    // SVG surface and document
    SVGTHandle svgSurface;
    SVGTHandle svgDoc;
    SVGTfloat surfaceTranslation[2];
}

- (void) initView;

@end
