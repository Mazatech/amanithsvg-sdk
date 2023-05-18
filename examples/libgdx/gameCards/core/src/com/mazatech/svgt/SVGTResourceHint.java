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

package com.mazatech.svgt;

import java.util.EnumSet;

public enum SVGTResourceHint {

    // the given font resource must be selected when the font-family attribute matches the 'serif' generic family
    DefaultSerif(AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_SERIF_FONT),
    // the given font resource must be selected when the font-family attribute matches the 'sans-serif' generic family
    DefaultSansSerif(AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_SANS_SERIF_FONT),
    // the given font resource must be selected when the font-family attribute matches the 'monospace' generic family
    DefaultMonospace(AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_MONOSPACE_FONT),
    // the given font resource must be selected when the font-family attribute matches the 'cursive' generic family
    DefaultCursive(AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_CURSIVE_FONT),
    // the given font resource must be selected when the font-family attribute matches the 'fantasy' generic family
    DefaultFantasy(AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_FANTASY_FONT),
    // the given font resource must be selected when the font-family attribute matches the 'system-ui' generic family
    DefaultSystemUI(AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_SYSTEM_UI_FONT),
    // the given font resource must be selected when the font-family attribute matches the 'ui-serif' generic family
    DefaultUISerif(AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_UI_SERIF_FONT),
    // the given font resource must be selected when the font-family attribute matches the 'ui-sans-serif' generic family
    DefaultUISansSerif(AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_UI_SANS_SERIF_FONT),
    // the given font resource must be selected when the font-family attribute matches the 'ui-monospace' generic family
    DefaultUIMonospace(AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_UI_MONOSPACE_FONT),
    // the given font resource must be selected when the font-family attribute matches the 'ui-rounded' generic family
    DefaultUIRounded(AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_UI_ROUNDED_FONT),
    // the given font resource must be selected when the font-family attribute matches the 'emoji' generic family
    DefaultEmoji(AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_EMOJI_FONT),
    // the given font resource must be selected when the font-family attribute matches the 'math' generic family
    DefaultMath(AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_MATH_FONT),
    // the given font resource must be selected when the font-family attribute matches the 'fangsong' generic family
    DefaultFangsong(AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_FANGSONG_FONT);

    SVGTResourceHint(int svgtEnum) {

        _svgtEnum = svgtEnum;
    }

    public int getValue() {

        return _svgtEnum;
    }

    public static int getBitfield(final EnumSet<SVGTResourceHint> values) {

        int bitField = 0;

        if (values.contains(SVGTResourceHint.DefaultSerif)) {
            bitField |= AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_SERIF_FONT;
        }
        if (values.contains(SVGTResourceHint.DefaultSansSerif)) {
            bitField |= AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_SANS_SERIF_FONT;
        }
        if (values.contains(SVGTResourceHint.DefaultMonospace)) {
            bitField |= AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_MONOSPACE_FONT;
        }
        if (values.contains(SVGTResourceHint.DefaultCursive)) {
            bitField |= AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_CURSIVE_FONT;
        }
        if (values.contains(SVGTResourceHint.DefaultFantasy)) {
            bitField |= AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_FANTASY_FONT;
        }
        if (values.contains(SVGTResourceHint.DefaultSystemUI)) {
            bitField |= AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_SYSTEM_UI_FONT;
        }
        if (values.contains(SVGTResourceHint.DefaultUISerif)) {
            bitField |= AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_UI_SERIF_FONT;
        }
        if (values.contains(SVGTResourceHint.DefaultUISansSerif)) {
            bitField |= AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_UI_SANS_SERIF_FONT;
        }
        if (values.contains(SVGTResourceHint.DefaultUIMonospace)) {
            bitField |= AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_UI_MONOSPACE_FONT;
        }
        if (values.contains(SVGTResourceHint.DefaultUIRounded)) {
            bitField |= AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_UI_ROUNDED_FONT;
        }
        if (values.contains(SVGTResourceHint.DefaultEmoji)) {
            bitField |= AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_EMOJI_FONT;
        }
        if (values.contains(SVGTResourceHint.DefaultMath)) {
            bitField |= AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_MATH_FONT;
        }
        if (values.contains(SVGTResourceHint.DefaultFangsong)) {
            bitField |= AmanithSVGJNI.SVGT_RESOURCE_HINT_DEFAULT_FANGSONG_FONT;
        }

        return bitField;
    }

    public static SVGTResourceHint fromValue(int svgtEnum) {

        return _allValues[svgtEnum];
    }

    private final int _svgtEnum;
    private static final SVGTResourceHint[] _allValues = values();
}
