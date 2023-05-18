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

package com.mazatech.android;

import java.io.InputStream;
import java.util.ArrayList;
import java.util.EnumSet;

// AmanithSVG (high level layer)
import com.mazatech.svgt.SVGAssetsConfig;

// AmanithSVG (low level layer)
import com.mazatech.svgt.SVGTResourceHint;
import com.mazatech.svgt.SVGTResourceType;

public class SVGAssetsConfigAndroid extends SVGAssetsConfig {

    static class SVGResourceAndroid extends SVGAssetsConfig.SVGResource {

        SVGResourceAndroid(InputStream resourceStream,
                           // the unique integer id as generate by aapt tool
                           // see https://developer.android.com/reference/android/content/res/Resources#openRawResource(int,%20android.util.TypedValue)
                           int aaptId,
                           // a unique not-empty string identifier, provided by the application
                           final String strId,
                           SVGTResourceType type,
                           final EnumSet<SVGTResourceHint> hints) {

            super(strId, type, hints);
            _resourceStream = resourceStream;
            _aaptId = aaptId;
        }

        // Get input/read stream for the resource.
        @Override
        protected InputStream getStream() {

            return _resourceStream;
        }

        /*
            Get a unique identifier for the resource; if two resources point to the same
            file, they must return the same resource identifier.
        */
        int getResourceId() {

             return _aaptId;
        }

        private final InputStream _resourceStream;
        // the unique integer id as generate by aapt tool
        // see https://developer.android.com/reference/android/content/res/Resources#openRawResource(int,%20android.util.TypedValue)
        private final int _aaptId;
    }

    /* Constructor, device screen properties must be supplied (with/height in pixels and dpi). */
    public SVGAssetsConfigAndroid(int screenWidth,
                                  int screenHeight,
                                  float screenDpi) {

        super(screenWidth, screenHeight, screenDpi);
        // list of external resources (fonts and images)
        _resources = new ArrayList<>();
    }

    /*
        Get the number of resources provided by this configuration.
        Mandatory implementation from SVGAssetsConfig.
    */
    @Override
    public int resourcesCount() {

        return (_resources == null) ? 0 : _resources.size();
    }

    /*
        Get a resource given an index.
        If the given index is less than zero or greater or equal to the value
        returned by 'resourcesCount', a null resource is returned.

        Mandatory implementation from SVGAssetsConfig.
    */
    @Override
    public SVGResource getResource(int index) {

        return (_resources == null) ? null : _resources.get(index);
    }

    /* Add an external resource to the configuration, by providing an input stream. */
    private boolean addResource(InputStream resourceStream,
                                // the unique integer id as generate by aapt tool
                                int aaptId,
                                // a unique not-empty string identifier, provided by the application (e.g. the file name)
                                final String strId,
                                SVGTResourceType type,
                                final EnumSet<SVGTResourceHint> hints) {

        boolean alreadyExist = false;

        for (SVGResourceAndroid resource : _resources) {
            if (resource.getResourceId() == aaptId) {
                alreadyExist = true;
                break;
            }
        }

        if (!alreadyExist) {
            _resources.add(new SVGResourceAndroid(resourceStream, aaptId, strId, type, hints));
        }

        return (!alreadyExist);
    }

    /*
        Add a font resource to the configuration, by providing an input stream.
        Return true if the resource has been added to the internal list, else
        false (i.e. the resource was already present within the internal list).
    */
    public boolean addFont(InputStream resourceStream,
                           // the unique integer id as generate by aapt tool
                           int aaptId,
                           // a unique not-empty string identifier, provided by the
                           // application (typically the file name, without the path)
                           final String strId,
                           final EnumSet<SVGTResourceHint> hints) {

        return addResource(resourceStream, aaptId, strId, SVGTResourceType.Font, hints);
    }

    public boolean addFont(InputStream resourceStream,
                           // the unique integer id as generate by aapt tool
                           int aaptId,
                           // a unique not-empty string identifier, provided by the
                           // application (typically the file name, without the path)
                           final String strId) {

        return addFont(resourceStream, aaptId, strId, EnumSet.noneOf(SVGTResourceHint.class));
    }

    /*
        Add an image resource to the configuration, by providing an input stream.
        Return true if the resource has been added to the internal list, else
        false (i.e. the resource was already present within the internal list).
    */
    public boolean addImage(InputStream resourceStream,
                            // the unique integer id as generate by aapt tool
                            int aaptId,
                            // a unique not-empty string identifier, provided by the
                            // application (typically the file name, without the path)
                            final String strId) {

        return addResource(resourceStream, aaptId, strId, SVGTResourceType.Image, EnumSet.noneOf(SVGTResourceHint.class));
    }

    // List of external resources (fonts and images).
    private final ArrayList<SVGResourceAndroid> _resources;
}
