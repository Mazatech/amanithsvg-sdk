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
    SVGTAlign

    Alignment indicates whether to force uniform scaling and, if so, the alignment method to use in case the aspect ratio of the source
    viewport doesn't match the aspect ratio of the destination (drawing surface) viewport.
*/
public enum SVGTAlign {

    /*
        Do not force uniform scaling.
        Scale the graphic content of the given element non-uniformly if necessary such that
        the element's bounding box exactly matches the viewport rectangle.
        NB: in this case, the <meetOrSlice> value is ignored.
    */
    None(AmanithSVG.SVGT_ASPECT_RATIO_ALIGN_NONE),

    /*
        Force uniform scaling.
        Align the <min-x> of the source viewport with the smallest x value of the destination (drawing surface) viewport.
        Align the <min-y> of the source viewport with the smallest y value of the destination (drawing surface) viewport.
    */
    XMinYMin(AmanithSVG.SVGT_ASPECT_RATIO_ALIGN_XMINYMIN),

    /*
        Force uniform scaling.
        Align the <mid-x> of the source viewport with the midpoint x value of the destination (drawing surface) viewport.
        Align the <min-y> of the source viewport with the smallest y value of the destination (drawing surface) viewport.
    */
    XMidYMin(AmanithSVG.SVGT_ASPECT_RATIO_ALIGN_XMIDYMIN),

    /*
        Force uniform scaling.
        Align the <max-x> of the source viewport with the maximum x value of the destination (drawing surface) viewport.
        Align the <min-y> of the source viewport with the smallest y value of the destination (drawing surface) viewport.
    */
    XMaxYMin(AmanithSVG.SVGT_ASPECT_RATIO_ALIGN_XMAXYMIN),

    /*
        Force uniform scaling.
        Align the <min-x> of the source viewport with the smallest x value of the destination (drawing surface) viewport.
        Align the <mid-y> of the source viewport with the midpoint y value of the destination (drawing surface) viewport.
    */
    XMinYMid(AmanithSVG.SVGT_ASPECT_RATIO_ALIGN_XMINYMID),

    /*
        Force uniform scaling.
        Align the <mid-x> of the source viewport with the midpoint x value of the destination (drawing surface) viewport.
        Align the <mid-y> of the source viewport with the midpoint y value of the destination (drawing surface) viewport.
    */
    XMidYMid(AmanithSVG.SVGT_ASPECT_RATIO_ALIGN_XMIDYMID),

    /*
        Force uniform scaling.
        Align the <max-x> of the source viewport with the maximum x value of the destination (drawing surface) viewport.
        Align the <mid-y> of the source viewport with the midpoint y value of the destination (drawing surface) viewport.
    */
    XMaxYMid(AmanithSVG.SVGT_ASPECT_RATIO_ALIGN_XMAXYMID),

    /*
        Force uniform scaling.
        Align the <min-x> of the source viewport with the smallest x value of the destination (drawing surface) viewport.
        Align the <max-y> of the source viewport with the maximum y value of the destination (drawing surface) viewport.
    */
    XMinYMax(AmanithSVG.SVGT_ASPECT_RATIO_ALIGN_XMINYMAX),

    /*
        Force uniform scaling.
        Align the <mid-x> of the source viewport with the midpoint x value of the destination (drawing surface) viewport.
        Align the <max-y> of the source viewport with the maximum y value of the destination (drawing surface) viewport.
    */
    XMidYMax(AmanithSVG.SVGT_ASPECT_RATIO_ALIGN_XMIDYMAX),

    /*
        Force uniform scaling.
        Align the <max-x> of the source viewport with the maximum x value of the destination (drawing surface) viewport.
        Align the <max-y> of the source viewport with the maximum y value of the destination (drawing surface) viewport.
    */
    XMaxYMax(AmanithSVG.SVGT_ASPECT_RATIO_ALIGN_XMAXYMAX);

    SVGTAlign(int svgtEnum) {
        
        _svgtEnum = svgtEnum;
    }

    public int getValue() {

        return _svgtEnum;
    }

    public static SVGTAlign fromValue(int svgtEnum) {

        return _allValues[svgtEnum];
    }

    private final int _svgtEnum;
    private static SVGTAlign[] _allValues = values();
}
