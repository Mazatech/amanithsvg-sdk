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

package com.mazatech.svgt;

public enum SVGTRenderingQuality {

    /* Disables antialiasing */
    NonAntialiased(AmanithSVG.SVGT_RENDERING_QUALITY_NONANTIALIASED),
    /* Causes rendering to be done at the highest available speed */
    Faster(AmanithSVG.SVGT_RENDERING_QUALITY_FASTER),
    /* Causes rendering to be done with the highest available quality */
    Better(AmanithSVG.SVGT_RENDERING_QUALITY_BETTER);

    SVGTRenderingQuality(int svgtEnum) {
        
        _svgtEnum = svgtEnum;
    }

    public int getValue() {

        return _svgtEnum;
    }

    public static SVGTRenderingQuality fromValue(int svgtEnum) {

        return _allValues[svgtEnum];
    }

    private final int _svgtEnum;
    private static SVGTRenderingQuality[] _allValues = values();
}
