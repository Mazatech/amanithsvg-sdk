/****************************************************************************
** Copyright (c) 2013-2023 Mazatech S.r.l.
** All rights reserved.
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
package com.mazatech.gdx;

// libGDX
import com.badlogic.gdx.utils.Disposable;

// AmanithSVG java binding (high level layer)
import com.mazatech.svgt.SVGColor;
import com.mazatech.svgt.SVGPacker;

public class SVGTextureAtlas implements Disposable {

    SVGTextureAtlas(final SVGAssetsGDX svg,
                    final SVGPacker.SVGPackedBin[] packerResult,
                    boolean dilateEdgesFix,
                    final SVGColor clearColor) {

        if (packerResult == null) {
            throw new IllegalArgumentException("packerResult == null");
        }
        if (clearColor == null) {
            throw new IllegalArgumentException("clearColor == null");
        }
        // do the real atlas construction
        build(svg, packerResult, dilateEdgesFix, clearColor);
    }

    private void build(final SVGAssetsGDX svg,
                       final SVGPacker.SVGPackedBin[] packerResult,
                       boolean dilateEdgesFix,
                       final SVGColor clearColor) {

        int pagesCount = packerResult.length;

        _pages = new SVGTextureAtlasPage[pagesCount];
        for (int i = 0; i < pagesCount; ++i) {
            _pages[i] = new SVGTextureAtlasPage(svg, packerResult[i], dilateEdgesFix, clearColor);
        }
    }

    /* get the generated textures */
    public SVGTextureAtlasPage[] getPages() {

        return _pages;
    }

    @Override
    public void dispose() {

        int pagesCount = _pages.length;

        for (int i = 0; i < pagesCount; ++i) {
            _pages[i].dispose();
            _pages[i] = null;
        }
        _pages = null;
    }

    private SVGTextureAtlasPage[] _pages = null;
}
