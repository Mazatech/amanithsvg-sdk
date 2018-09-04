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

/*
    In AmanithSVG, a SVGTHandle is a 32bit unsigned integer.
    We wrap the handle type in a class.
*/
public class SVGTHandle {

    public SVGTHandle(int nativeHandle) {

        _nativeHandle = nativeHandle;
    }

    public SVGTHandle() {

        _nativeHandle = 0;
    }

    public int getNativeHandle() {

        return _nativeHandle;
    }

    private int _nativeHandle;
}
