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

// Java
import java.nio.Buffer;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.nio.CharBuffer;
import java.nio.charset.CharacterCodingException;
import java.nio.charset.Charset;
import java.nio.charset.CharsetDecoder;
import java.util.ArrayList;
import java.util.EnumSet;
import java.util.concurrent.ConcurrentHashMap;

public class SVGAssets {

    // Constructor
    protected SVGAssets() {

        _initialized = false;
        // array of buffers
        _resourcesBuffers = null;
        // list of log buffers, one for each calling thread.
        _logBuffers = null;
        // the configuration with which the class was initialized
        _initConfig = null;
    }

    /* Has the class already been initialized? */
    protected boolean isInitialized() {

        return _initialized;
    }

    // Load the given configuration
    private void configurationLoad(SVGAssetsConfig config) {

        SVGTError err = SVGTError.None;
        float curvesQuality = config.getCurvesQuality();
        int resourcesCount = config.resourcesCount();

        // set curves quality, if needed
        // NB: a 0 or negative value means "keep the default one"
        if (curvesQuality > 0) {
            err = AmanithSVG.svgtConfigSet(SVGTConfig.CurvesQuality, curvesQuality);
        }

        // set user-agent language
        if (SVGAssetsConfig.isLanguageValid(config.getLanguage())) {
            err = AmanithSVG.svgtLanguageSet(config.getLanguage());
        }

        if (resourcesCount > 0) {

            // create the array that will keep track of external resources
            _resourcesBuffers = new ArrayList<>(resourcesCount);

            // load external resources (fonts and images)
            for (int i = 0; i < resourcesCount; ++i) {
                // get a resource
                SVGAssetsConfig.SVGResource resource = config.getResource(i);
                // get in-memory binary data for the resource
                ByteBuffer buffer = resource.getBytes();
                if (buffer != null) {
                    // provide AmanithSVG with the font resource
                    err = AmanithSVG.svgtResourceSet(resource.getId(), buffer, resource.getType(), resource.getHints());
                    if (err == SVGTError.None) {
                        // keep track of buffer (i.e. avoid to be released by GC)
                        _resourcesBuffers.add(buffer);
                    }
                }
            }
        }

        // NB: we don't return an error code, because even if there was an error in the
        // load of configuration, it would not affect the functioning of AmanithSVG
    }

    // Release allocated resources during AmanithSVG initialization.
    private void configurationUnload() {

        // unload external resources (i.e. release buffers containing TTF/OTF/JPG/PNG files)
        if (_resourcesBuffers != null) {
            // empty the list
            _resourcesBuffers.clear();
            _resourcesBuffers = null;
        }
    }

    private void logBuffersInit() {

        // create the dictionary that links each calling thread to a log buffer
        _logBuffers = new ConcurrentHashMap<>();
    }

    // Get AmanithSVG log buffer for the current thread.
    protected ByteBuffer logBufferGet(boolean createIfNotExist) {

        ByteBuffer buffer = null;

        // library must have been initialized, with a given configuration that enable log facility
        if ((isInitialized() && (_initConfig != null)) && ((_initConfig.getLogCapacity() > 0) && (!_initConfig.getLogLevel().isEmpty()))) {
            // get current thread identifier
            long threadId = Thread.currentThread().getId();
            // try to get the buffer associated with the thread identifier
            buffer = _logBuffers.get(threadId);
            if (buffer == null)
            {
                if (createIfNotExist)
                {
                    // create a direct buffer
                    buffer = ByteBuffer.allocateDirect(_initConfig.getLogCapacity());
                    buffer.order(ByteOrder.nativeOrder());
                    // set AmanithSVG log for the calling thread
                    // NB: after calling this function, the buffer will be initialized as empty
                    // (i.e. a '\0' character will be written at the beginning)
                    SVGTError err = AmanithSVG.svgtLogBufferSet(buffer, _initConfig.getLogLevel());
                    if (err == SVGTError.None) {
                        _logBuffers.put(threadId, buffer);
                    }
                }
            }
        }

        return buffer;
    }

    // Release all created log buffers.
    private void logBuffersDestroy() {

        if (_logBuffers != null) {
            // empty the whole dictionary
            _logBuffers.clear();
            _logBuffers = null;
        }
    }

    // Ensure that per-thread data structures needed by AmanithSVG are assigned.
    private void threadDataEnsure() {

        ByteBuffer buffer = logBufferGet(true);
    }

    /*
        Initialize the AmanithSVG library as well as the JNI wrapper mechanism.
        
        NB: before using any other method of this class (e.g. createSurface, createDocument, createPacker), it
        is MANDATORY to call this function as a first thing to do.
    */
    protected SVGTError init(SVGAssetsConfig config) {

        SVGTError err = SVGTError.None;

        if (!isInitialized()) {
            // initialize AmanithSVG library
            if ((err = AmanithSVG.svgtInit(config.getScreenWidth(), config.getScreenHeight(), config.getScreenDpi())) == SVGTError.None) {
                // load configuration
                if (config != null) {
                    configurationLoad(config);
                }
                // create the dictionary that links each calling thread to a log buffer
                logBuffersInit();
                // keep track of the configuration with which the class was initialized
                _initConfig = config;
                // now the class is initialized
                _initialized = true;
            }
        }

        return err;
    }

    public void dispose() {

        if (isInitialized()) {
            // uninitialize AmanithSVG native library
            AmanithSVG.svgtDone();
            // release buffers containing TTF/OTF files
            configurationUnload();
            // release log buffers
            logBuffersDestroy();
            // we have finished
            _initConfig = null;
            _initialized = false;
        }
    }

    // Is AmanithSVG logging enabled for the current thread?
    private boolean isLoggingEnabled() {

        return logBufferGet(false) != null;
    }

    // Get AmanithSVG log content as a string.
    public String getLog() {

        String result = "";
        ByteBuffer logBuffer = logBufferGet(false);

        if (logBuffer != null) {

            // rewind buffer
            ((Buffer)logBuffer).rewind();

            // get information about the current thread log buffer
            int[] info = new int[] { 0, 0, 0, 0 };
            SVGTError err = AmanithSVG.svgtLogBufferInfo(info);

            // info[2] = current length, in characters (i.e. the total number of characters
            // written, included the trailing '\0')
            if ((err == SVGTError.None) && (info[2] > 1)) {

                Charset charset = Charset.forName("UTF-8");
                CharsetDecoder decoder = charset.newDecoder();

                try {
                    // decode AmanithSVG log buffer into a String
                    CharBuffer decodedBuffer = decoder.decode(logBuffer);
                    result = new String(decodedBuffer.array(), 0, info[2] - 1);
                }
                catch (CharacterCodingException e) {
                    // nothing to do
                }
            }
        }

        return result;
    }

    // Append the given message to the AmanithSVG log buffer set for the current thread.
    private SVGTError logPrint(String message,
                               SVGTLogLevel level) {

        SVGTError err = SVGTError.None;

        if (isLoggingEnabled()) {
            err = AmanithSVG.svgtLogPrint(message, level);
        }

        return err;
    }

    /*
        Append the given message to the AmanithSVG log buffer set for the current thread.
        The given tag is appended to clarify the source of the print operation.
    */
    private SVGTError logPrint(String message,
                               SVGTLogLevel level,
                               String tag) {

        SVGTError err = SVGTError.None;

        if (isLoggingEnabled()) {
            // append a tag which tells us the source of the print operation
            String msg = tag + "\n" + message;
            err = AmanithSVG.svgtLogPrint(msg, level);
        }

        return err;
    }

    /*
        Clear the AmanithSVG log buffer set for the current thread.

        If specified, make sure AmanithSVG no longer uses a log buffer for the current thread.
        In this case, logging can be reactivated (for the calling thread) by calling this method
        with a 'false' parameter.
    */
    public SVGTError logClear(boolean stopLogging) {

        SVGTError err = SVGTError.None;

        if (isInitialized()) {

            if (stopLogging) {
                // switch off logging at AmanithSVG side
                if ((err = AmanithSVG.svgtLogBufferSet(null, EnumSet.noneOf(SVGTLogLevel.class))) == SVGTError.None) {
                    // NB: we don't remove the ByteBuffer from dictionary, we still keep
                    // track of it; next time this function will be called with a 'false'
                    // parameter, logging will be activated again
                }
            }
            else {
                // get log buffer (create it if it does not exist)
                ByteBuffer buffer = logBufferGet(true);

                if (buffer != null) {
                    // set AmanithSVG log for the calling thread
                    // NB: after calling this function, the buffer will be initialized as empty
                    // (i.e. a '\0' character will be written at the beginning)
                    err = AmanithSVG.svgtLogBufferSet(buffer, _initConfig.getLogLevel());
                }
            }
        }

        return err;
    }

    // Append an informational message to the AmanithSVG log buffer set for the current thread.
    public SVGTError logInfo(String message) {

        return logPrint(message, SVGTLogLevel.Info);
    }

    /*
        Append an informational message to the AmanithSVG log buffer set for the current thread.
        The given tag is appended to clarify the source of the print operation.
    */
    public SVGTError logInfo(String message,
                             String tag) {

        return logPrint(message, SVGTLogLevel.Info, tag);
    }

    // Append a warning message to the AmanithSVG log buffer set for the current thread.
    public SVGTError logWarning(String message) {

        return logPrint(message, SVGTLogLevel.Warning);
    }

    /*
        Append a warning message to the AmanithSVG log buffer set for the current thread.
        The given tag is appended to clarify the source of the print operation.
    */
    public SVGTError logWarning(String message,
                                String tag) {

        return logPrint(message, SVGTLogLevel.Warning, tag);
    }

    // Append an error message to the AmanithSVG log buffer set for the current thread.
    public SVGTError logError(String message) {

        return logPrint(message, SVGTLogLevel.Error);
    }

    /*
        Append an error message to the AmanithSVG log buffer set for the current thread.
        The given tag is appended to clarify the source of the print operation.
    */
    public SVGTError logError(String message,
                              String tag) {

        return logPrint(message, SVGTLogLevel.Error, tag);
    }

    /*
        Create a drawing surface, specifying its dimensions in pixels.

        Supplied dimensions should be positive numbers greater than zero, else
        a null instance will be returned.
    */
    public SVGSurface createSurface(int width,
                                    int height) {

        SVGSurface result = null;

        if (isInitialized()) {
            // check for illegal arguments
            if ((width <= 0) || (height <= 0)) {
                throw new IllegalArgumentException("Invalid (negative or zero) surface dimensions");
            }
            else {
                // ensure that per-thread data structures needed by AmanithSVG are assigned
                threadDataEnsure();

                // create AmanithSVG surface handle
                SVGTHandle surface = AmanithSVG.svgtSurfaceCreate(width, height);
                // create SVGSurface instance out of handle
                if (surface != null) {
                    result = new SVGSurface(surface.getNativeHandle());
                }
            }
        }

        return result;
    }

    /*
        Create and load an SVG document, specifying the whole XML string.
        If supplied XML string is null or empty, a null instance will be returned.
    */
    public SVGDocument createDocument(String xmlText) {

        SVGDocument result = null;

        if (isInitialized()) {
            // check for illegal arguments
            if ((xmlText == null) || (xmlText.length() == 0)) {
                throw new IllegalArgumentException("xmlText == null or empty");
            }
            else {
                // ensure that per-thread data structures needed by AmanithSVG are assigned
                threadDataEnsure();

                // create AmanithSVG document handle
                SVGTHandle document = AmanithSVG.svgtDocCreate(xmlText);
                // create SVGDocument instance out of handle
                if (document != null) {
                    result = new SVGDocument(document.getNativeHandle());
                }
            }
        }

        return result;
    }

    /*
        Create an SVG packer, specifying a scale factor.

        Every collected SVG document/element will be packed into rectangular bins, whose
        dimensions won't exceed the specified 'maxTexturesDimension' in pixels.

        If true, 'pow2Textures' will force bins to have power-of-two dimensions.

        Each rectangle will be separated from the others by the specified 'border' in pixels.

        The specified 'scale' factor will be applied to all collected SVG documents/elements,
        in order to realize resolution-independent atlases.
    */
    public SVGPacker createPacker(float scale,
                                  int maxTexturesDimension,
                                  int border,
                                  boolean pow2Textures) {

        SVGPacker result = null;

        if (isInitialized()) {

            // ensure that per-thread data structures needed by AmanithSVG are assigned
            threadDataEnsure();

            // create the packer
            result = new SVGPacker(scale, maxTexturesDimension, border, pow2Textures);
        }

        return result;
    }

    // Get library version.
    public static String getVersion() {

        return AmanithSVG.svgtGetString(SVGTStringID.Version);
    }

    // Get library vendor.
    public static String getVendor() {

        return AmanithSVG.svgtGetString(SVGTStringID.Vendor);
    }

    // Keep track if AmanithSVG library has been initialized.
    private boolean _initialized;
    // Array of external resources (fonts and images).
    private ArrayList<ByteBuffer> _resourcesBuffers;
    /*
        Log buffers.

        It is important to not specify the same buffer memory for different
        threads, because AmanithSVG does not synchronize write operations to
        the provided log buffer: in other words, each thread must provide its
        own log buffer.
    */
    private ConcurrentHashMap<Long, ByteBuffer> _logBuffers;
    // The configuration with which the class was initialized.
    private SVGAssetsConfig _initConfig;
}
