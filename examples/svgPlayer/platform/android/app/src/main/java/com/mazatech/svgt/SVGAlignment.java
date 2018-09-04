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

class SVGAlignment {

    private final SVGTAlign _align;
    private final SVGTMeetOrSlice _meetOrSlice;

    SVGAlignment(SVGTAlign align, SVGTMeetOrSlice meetOrSlice) {
        _align = align;
        _meetOrSlice = meetOrSlice;
    }

    SVGTAlign getAlign() {
        return _align;
    }

    SVGTMeetOrSlice getMeetOrSlice() {
        return _meetOrSlice;
    }

    @Override
    public int hashCode() {
        return _align.hashCode() ^ _meetOrSlice.hashCode();
    }

    @Override
    public boolean equals(Object o) {
        if (!(o instanceof SVGAlignment)) {
            return false;
        }
        return (_align.equals(((SVGAlignment)o).getAlign()) && _meetOrSlice.equals(((SVGAlignment)o).getMeetOrSlice()));
    }
}
