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

package com.mazatech.svgt;

import java.io.IOException;
import java.io.InputStream;
import java.nio.Buffer;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.EnumSet;

public abstract class SVGAssetsConfig {

    protected abstract static class SVGResource {

        protected SVGResource(String id,
                              SVGTResourceType type,
                              final EnumSet<SVGTResourceHint> hints) {

            _id = id;
            _type = type;
            _hints = EnumSet.copyOf(hints);
        }

        /* Get input/read stream for the resource. */
        protected abstract InputStream getStream();

        /* Load data from the given stream into memory. */
        private static ByteBuffer loadFromStream(InputStream in) throws IOException {

            int read = 0;
            byte[] readBuffer = new byte[4096];
            // this method is supposed to return an estimated number of bytes that can
            // be read, not necessarily the real file size, and the documentation clearly
            // discourages its use (see https://developer.android.com/reference/java/io/InputStream#available()
            // BUT the documentation refers to generic streams that may take a while to
            // retrieve data, like network streams. In a resource, the exact size is always known and available.
            int size = in.available();
            // create the direct buffer
            ByteBuffer resultBuffer = ByteBuffer.allocateDirect(size);
            resultBuffer.order(ByteOrder.nativeOrder());

            while (read != -1) {
                // read a chunk of data
                read = in.read(readBuffer);
                if (read != -1) {
                    resultBuffer.put(readBuffer, 0, read);
                }
            }
            // close stream
            in.close();
            // rewind buffer; NB: the cast is necessary, because Java 9 introduces overridden
            // methods with covariant return types for the some methods in java.nio.ByteBuffer
            // that are used by the driver. By casting to base Buffer we are always safe. See also
            // http://github.com/libgdx/libgdx/pull/6331 and http://stackoverflow.com/a/58435689/7912520
            ((Buffer)resultBuffer).rewind();
            return resultBuffer;
        }

        /* Get resource identifier. */
        public String getId() {

            return _id;
        }

        /* Get resource type. */
        public SVGTResourceType getType() {

            return _type;
        }

        /* Get resource hints. */
        public EnumSet<SVGTResourceHint> getHints() {

            return EnumSet.copyOf(_hints);
        }

        /* Get in-memory binary data for the resource. */
        public ByteBuffer getBytes() {

            ByteBuffer buffer;

            try {
                // load data from the stream into memory
                buffer = loadFromStream(getStream());
            }
            catch (java.io.IOException e) {
                buffer = null;
            }

            return buffer;
        }

        /* Resource identifier. */
        private final String _id;
        /* Resource type. */
        private final SVGTResourceType _type;
        /* Resource hints. */
        private final EnumSet<SVGTResourceHint> _hints;
    }

    /*
        Constructor, device screen properties must be supplied
        (with/height in pixels and dpi).
    */
    public SVGAssetsConfig(int screenWidth,
                           int screenHeight,
                           float dpi) {

        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        _screenDpi = dpi;

        // a zero or negative value means "keep the default one"
        _curvesQuality = 0;
        // user-agent language settings, start with standard English
        _languages = DefaultLanguage;

        // log settings
        _logLevel = DefaultLogLevel;
        _logCapacity = DefaultLogCapacity;
    }

    /* Get screen resolution width, in pixels. */
    public int getScreenWidth() {

        return _screenWidth;
    }

    /* Get screen resolution height, in pixels. */
    public int getScreenHeight() {

        return _screenHeight;
    }

    /* Get screen dpi. */
    public float getScreenDpi() {

        return _screenDpi;
    }

    /*
        Get curves quality, used by AmanithSVG geometric kernel to approximate curves
        with straight line segments (flattening). Valid range is [1; 100], where 100
        represents the best quality.

        A zero or negative value means "keep the default one".
    */
    public float getCurvesQuality() {

        return _curvesQuality;
    }

    /*
        Set curves quality, used by AmanithSVG geometric kernel to approximate curves
        with straight line segments (flattening). Valid range is [1; 100], where 100
        represents the best quality.

        A zero or negative value means "keep the default one".
    */
    public void setCurvesQuality(float quality) {

        // a zero or negative value means "keep the default one"
        // else clamp value between 1 and 100
        _curvesQuality = (quality <= 0) ? 0 : Math.max(1.0f, Math.min(100.0f, quality));
    }

    /* Get user-agent language. */
    public String getLanguage() {

        return _languages;
    }

    /*
        Set the user-agent language.

        Specify the system/user-agent language; this setting will affect the conditional rendering of
        <switch> elements and elements with 'systemLanguage' attribute specified. The given argument
        must be a non-empty list of languages separated by semicolon (e.g. "en-US;en-GB;it;es")
    */
    public void setLanguage(String languages) {

        // perform all the needed checks on each part
        if (isLanguageValid(languages)) {
            _languages = languages;
        }
    }

    /* Get log level. */
    public EnumSet<SVGTLogLevel> getLogLevel() {

        return _logLevel;
    }

    /* Get log capacity, in characters. */
    public int getLogCapacity() {

        return _logCapacity;
    }

    /*
        Set log parameters (log level and log capacity).

        If the specified log level is empty, logging is disabled.
        If the specified log capacity (in characters) is less than or equal
        zero, logging is disabled.
    */
    public void setLogParameters(EnumSet<SVGTLogLevel> logLevel,
                                 int logCapacity) {

        _logLevel = logLevel;
        _logCapacity = Math.max(logCapacity, 0);
    }

    /*
        Get the number of (external) resources provided by this configuration.
        It must be implemented by all derived classes.
    */
    public abstract int resourcesCount();

    /*
        Get a resource given an index.
        If the given index is less than zero or greater or equal to the value
        returned by 'resourcesCount', a null resource is returned.

        It must be implemented by all derived classes.
    */
    public abstract SVGResource getResource(int index);

    /* Check if the given language string is valid. */
    static public boolean isLanguageValid(String language) {

        // must not be empty
        return ((language != null) && (!language.isEmpty()));
    }

    // Screen width, in pixels.
    private final int _screenWidth;
    // Screen height, in pixels.
    private final int _screenHeight;
    // Screen dpi.
    private final float _screenDpi;
    // Curves quality; a 0 or negative value means "keep the default one".
    private float _curvesQuality;
    // User-agent language settings; a list of languages separated by semicolon (e.g. "en-US;en-GB;it;es")
    private String _languages;
    // Log level, if SVGLogLevel.None is specified, logging is disabled.
    private EnumSet<SVGTLogLevel> _logLevel;
    // Log capacity, in characters; if zero is specified, logging is disabled.
    private int _logCapacity;

    // Default configuration.
    public static final String DefaultLanguage = "en";

    // Enable all log levels.
    public static final EnumSet<SVGTLogLevel> DefaultLogLevel = EnumSet.allOf(SVGTLogLevel.class);
    // 32k log capacity.
    public static final int DefaultLogCapacity = 32768;
}
