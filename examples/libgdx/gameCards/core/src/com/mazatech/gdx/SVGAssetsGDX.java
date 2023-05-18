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

import java.io.IOException;
import java.io.File;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.OutputStream;

// libGDX
import com.badlogic.gdx.Gdx;
import com.badlogic.gdx.files.FileHandle;

// AmanithSVG java binding (high level layer)
import com.mazatech.svgt.SVGAssets;
import com.mazatech.svgt.SVGColor;
import com.mazatech.svgt.SVGDocument;
import com.mazatech.svgt.SVGSurface;

public class SVGAssetsGDX extends SVGAssets {

    // Get a temporary directory, ensuring a trailing file separator
    static private String getTempDirectory() {

        String tmpDir = System.getProperty("java.io.tmpdir", "");
        return tmpDir.endsWith(File.separator) ? tmpDir : tmpDir + File.separator;
    }

    // Copy the given input stream into the provided output stream
    static private boolean streamsCopy(InputStream input,
                                       OutputStream output) throws IOException {

        boolean ok = false;

        if ((input != null) && (output != null)) {
            try {
                byte[] buffer = new byte[4096]; 
                while (true) {
                    int length = input.read(buffer);
                    if (length == -1) {
                        break;
                    }
                    output.write(buffer, 0, length);
                }
                ok = true;
            }
            catch (IOException e) {
                throw new RuntimeException(e);
            }
            finally {
                // close streams
                input.close();
                output.close();
            }
        }

        return ok;
    }

    // Load a dynamic/shared library (provided by the given input stream); before the actual load
    // the library is written/copied into the given destination output path.
    static private boolean sharedLibraryLoad(InputStream input,
                                             String outputFileName) {

        boolean ok = false;
        File outputFile = new File(outputFileName);

        // ensure the existence of destination directory
        outputFile.getParentFile().mkdirs();

        try {
            FileOutputStream output = new FileOutputStream(outputFile);
            // copy the native library to a safe (executable) location
            if (streamsCopy(input, output)) {
                // perform the real load of dynamic code
                System.load(outputFile.getAbsolutePath());
                // we have finished
                ok = true;
            }
        }
        catch (FileNotFoundException e) {
            Gdx.app.error(s_LOG_TAG, "File " + outputFile.getAbsolutePath() + " not found");
        }
        catch (UnsatisfiedLinkError e) {
            Gdx.app.error(s_LOG_TAG, "Failed to load " + outputFile.getAbsolutePath() + " native library, the file does not exist");
        }
        catch (SecurityException e) {
            Gdx.app.error(s_LOG_TAG, "Failed to load " + outputFile.getAbsolutePath() + " native library, the security manager doesn't allow loading of the specified dynamic library");
        }
        catch (IOException e) {
            Gdx.app.error(s_LOG_TAG, "I/O error copying native library to " + outputFile.getAbsolutePath());
        }

        return ok;
    }

    static private boolean sharedLibraryLoad(String srcPath,
                                             String outputFileName) {

        InputStream input = null;
        // Maven's local repository is a directory on the local machine that stores all the
        // project artifacts. When we execute a Maven build, Maven automatically downloads all the
        // dependency jars into the local repository. Usually, this directory is named .m2
        // Here's where the default local repository is located based on OS:
        //
        // - Windows: C:\Users\<User_Name>\.m2
        // - Linux: /home/<User_Name>/.m2
        // - Mac: /Users/<user_name>/.m2
        //
        // mavenLocal() can be accessed via Gdx.files.internal, so shared libraries located
        // in mavenLocal and in local project assets ("Internal files are relative to the
        // application’s root or working directory on desktops, relative to the assets directory
        // on Android, and relative to the core/assets directory of your GWT project") have
        // the highest priority
        FileHandle handle = Gdx.files.internal(srcPath);

        try {
            input = handle.read();
        }
        catch (Throwable t) {
            // if the file handle represents a directory, doesn't exist, or could not be read
            // then try with SVGAssets resource
            input = SVGAssets.class.getResourceAsStream(srcPath);
        }

        return (input != null) && sharedLibraryLoad(input, outputFileName);
    }

    // Load a dynamic/shared library for desktop platforms (Windows, MacOS X, Linux)
    static private boolean desktopSharedLibraryLoad(String jarSubDir,
                                                    String nativeFileName) {

        boolean ok = true;
        //String srcPath = "/" + jarSubDir + "/" + nativeFileName;
        String srcPath = jarSubDir + "/" + nativeFileName;
        // build destination path
        String tmpDir = getTempDirectory();
        String userName = System.getProperty("user.name", "");
        String tmpPath = tmpDir + s_LIB_BASIC_NAME + File.separator + userName + File.separator + nativeFileName;

        // try temp directory with username in path
        if (!sharedLibraryLoad(srcPath, tmpPath)) {
            // try user home
            String homePath = System.getProperty("user.home", "") + File.separator + "." + s_LIB_BASIC_NAME + File.separator;
            File file = new File(homePath, nativeFileName);
            if (!sharedLibraryLoad(srcPath, file.getAbsolutePath())) {
                // try relative directory
                file = new File(".temp" + File.separator, nativeFileName);
                ok = sharedLibraryLoad(srcPath, file.getAbsolutePath());
            }
        }

        return ok;
    }

    // Load AmanithSVG (and its JNI counterpart) native library
    static public boolean jniLibraryLoad() {

        boolean ok = true;
        String os = System.getProperty("os.name", "").toLowerCase();
        String runtime = System.getProperty("java.runtime.name", "").toLowerCase();
        boolean isIOS = os.contains("ios") || runtime.contains("robovm");
        
        // in case of iOS, things have been linked statically to the executable
        if (!isIOS) {

            boolean isWindows = os.contains("windows");
            boolean isLinux = os.contains("linux");
            boolean isMac = os.contains("mac");
            boolean isAndroid = false;
            String arch = System.getProperty("os.arch", "").toLowerCase();
            boolean is64Bit = arch.contains("64") || arch.startsWith("armv8");
            boolean isArm = arch.startsWith("arm") || arch.startsWith("aarch64");
            String vm = System.getProperty("java.vm.name", "").toLowerCase();

            // detect Android
            if (vm.contains("dalvik") || runtime.contains("android runtime")) {
                isAndroid = true;
                isWindows = false;
                isLinux = false;
                isMac = false;
                is64Bit = false;
            }

            if (isAndroid) {
                try {
                    System.loadLibrary(s_LIB_BASIC_NAME);
                    System.loadLibrary(s_JNI_BASIC_NAME);
                }
                catch (UnsatisfiedLinkError | SecurityException e) {
                    ok = false;
                }
            }
            else {
                // desktop platforms
                java.lang.String path = "";
                java.lang.String libName = "";
                java.lang.String jniName = "";

                if (isWindows) {
                    // Windows desktop
                    path = is64Bit ? "windows/x86_64" : "windows/x86";
                    libName = "lib" + s_LIB_BASIC_NAME + ".dll";
                    jniName = "lib" + s_JNI_BASIC_NAME + ".dll";
                }
                else
                if (isLinux) {
                    if (isArm) {
                        // Linux ARM
                        path = is64Bit ? "linux/arm64-v8a" : "linux/arm-v7a";
                    }
                    else {
                        // Linux desktop
                        path = is64Bit ? "linux/x86_64" : "linux/x86";
                    }
                    libName = "lib" + s_LIB_BASIC_NAME + ".so";
                    jniName = "lib" + s_JNI_BASIC_NAME + ".so";
                }
                else
                if (isMac) {
                    // MacOS X universal binary
                    path = "macosx/ub";
                    libName = "lib" + s_LIB_BASIC_NAME + ".dylib";
                    jniName = "lib" + s_JNI_BASIC_NAME + ".dylib";
                }

                // load AmanithSVG native library
                ok = desktopSharedLibraryLoad(path, libName);
                if (ok) {
                    // load JNI binding for the native library; NB: this second library
                    // must be loaded as second, because it depends on the first
                    ok = desktopSharedLibraryLoad(path, jniName);
                }
            }
        }

        return ok;
    }

    /* Constructor, the given configuration must provide resources too. */
    public SVGAssetsGDX(final SVGAssetsConfigGDX config) {

        super();

        // load AmanithSVG (and its JNI counterpart) native library
        if (jniLibraryLoad()) {
            // initialize the AmanithSVG library using the given configuration
            super.init(config);
            // make sure the thread that initialized AmanithSVG for libGDX
            // has access to the logbuffer (if enabled by the configuration)
            logBufferGet(true);
        }
    }

    /* Create and load an SVG document, specifying a file handle. */
    public SVGDocument createDocument(final FileHandle file) {

        SVGDocument result = null;

        if (isInitialized()) {
            // check for illegal arguments
            if (file == null) {
                throw new IllegalArgumentException("file == null");
            }
            else {
                result = createDocument(file.readString("UTF-8"));
            }
        }

        return result;
    }

    /*
        Create and load an SVG document, specifying an internal filename.
        It is a simple shortcut to the previous method:

        createDocument(Gdx.files.internal(internalFileName))
    */
    public SVGDocument createDocumentFromFile(String internalFileName) {

        SVGDocument result = null;

        if (isInitialized()) {
            // check for illegal arguments
            if ((internalFileName == null) || (internalFileName.isEmpty())) {
                throw new IllegalArgumentException("null or empty filename");
            }
            else {
                result = createDocument(Gdx.files.internal(internalFileName));
            }
        }

        return result;
    }

    /*
        Generate a texture from the given "internal" SVG filename.

        With the term "internal", it's intended those read-only files located on the internal storage.
        For more details about libGDX file handling, please refer to the official documentation (http://github.com/libgdx/libgdx/wiki/File-handling)

        Size of the texture is derived from the information available within the SVG file:

        - if the outermost <svg> element has 'width' and 'height' attributes, such values are used to size the texture
        - if the outermost <svg> element does not have 'width' and 'height' attributes, the size of texture is determined by the width and height values of the 'viewBox' attribute
    */
    public SVGTexture createTexture(String internalPath) {

        return (isInitialized()) ? new SVGTexture(this, internalPath) : null;
    }

    /*
        Generate a texture from the given "internal" SVG filename.
        Size of texture is specified by the given 'width' and 'height'.
        Before the SVG rendering, pixels are initialized with a transparent black.
    */
    public SVGTexture createTexture(String internalPath,
                                    int width,
                                    int height) {

        return createTexture(internalPath, width, height, SVGColor.Clear);
    }

    /*
        Generate a texture from the given "internal" SVG filename.
        Size of texture is specified by the given 'width' and 'height'.
        Before the SVG rendering, pixels are initialized with the given 'clearColor'.
    */
    public SVGTexture createTexture(String internalPath,
                                    int width,
                                    int height,
                                    final SVGColor clearColor) {

        return createTexture(internalPath, width, height, clearColor, false);
    }

    /*
        Generate a texture from the given "internal" SVG filename.
        Size of texture is specified by the given 'width' and 'height'.
        Before the SVG rendering, pixels are initialized with the given 'clearColor'.

        If the 'dilateEdgesFix' flag is set to true, the rendering process will also
        perform a 1-pixel dilate post-filter; this dilate filter is useful when the
        texture has some transparent parts (i.e. pixels with alpha component = 0): such
        flag will induce TextureFilter.Linear minification/magnification filtering.

        If the 'dilateEdgesFix' flag is set to false, no additional dilate post-filter
        is applied, and the texture minification/magnification filtering is set to
        TextureFilter.Nearest.

        Before the SVG rendering, pixels are initialized with the given 'clearColor'.
    */
    public SVGTexture createTexture(String internalPath,
                                    int width,
                                    int height,
                                    final SVGColor clearColor,
                                    boolean dilateEdgesFix) {

        SVGTexture result = null;

        if (isInitialized()) {
            // check for illegal arguments
            if ((width <= 0) || (height <= 0)) {
                throw new IllegalArgumentException("Invalid (negative or zero) texture dimensions");
            }
            else {
                result = new SVGTexture(this, Gdx.files.internal(internalPath), width, height, clearColor, dilateEdgesFix);
            }
        }

        return result;
    }

    public SVGTexture createTexture(final FileHandle file) {

        SVGTexture result = null;

        if (isInitialized()) {
            // check for illegal arguments
            if (file == null) {
                throw new IllegalArgumentException("file == null");
            }
            else {
                result = new SVGTexture(this, file);
            }
        }

        return result;
    }

    public SVGTexture createTexture(final FileHandle file,
                                    int width,
                                    int height) {

        return createTexture(file, width, height, SVGColor.Clear);
    }

    public SVGTexture createTexture(final FileHandle file,
                                    int width,
                                    int height,
                                    SVGColor clearColor) {

        return createTexture(file, width, height, clearColor, false);
    }

    /*
        Generate a texture from the given SVG file handle.
        Size of texture is specified by the given 'width' and 'height'.
        Before the SVG rendering, pixels are initialized with the given 'clearColor'.

        If the 'dilateEdgesFix' flag is set to true, the rendering process will also
        perform a 1-pixel dilate post-filter; this dilate filter is useful when the
        texture has some transparent parts (i.e. pixels with alpha component = 0): such
        flag will induce TextureFilter.Linear minification/magnification filtering.

        If the 'dilateEdgesFix' flag is set to false, no additional dilate post-filter
        is applied, and the texture minification/magnification filtering is set to
        TextureFilter.Nearest.
    */
    public SVGTexture createTexture(final FileHandle file,
                                    int width,
                                    int height,
                                    final SVGColor clearColor,
                                    boolean dilateEdgesFix) {

        SVGTexture result = null;

        if (isInitialized()) {
            // check for illegal arguments
            if (file == null) {
                throw new IllegalArgumentException("file == null");
            }
            else
            if ((width <= 0) || (height <= 0)) {
                throw new IllegalArgumentException("Invalid (negative or zero) texture dimensions");
            }
            else {
                result = new SVGTexture(this, file, width, height, clearColor, dilateEdgesFix);
            }
        }

        return result;
    }

    // Create a texture out of an SVG document (already instantiated externally)
    public SVGTexture createTexture(final SVGDocument doc,
                                    int width,
                                    int height) {

        return createTexture(doc, width, height, SVGColor.Clear);
    }

    public SVGTexture createTexture(final SVGDocument doc,
                                    int width,
                                    int height,
                                    SVGColor clearColor) {

        return createTexture(doc, width, height, clearColor, false);
    }

    // Create a texture out of an SVG document (already instantiated externally)
    public SVGTexture createTexture(final SVGDocument doc,
                                    int width,
                                    int height,
                                    final SVGColor clearColor,
                                    boolean dilateEdgesFix) {

        SVGTexture result = null;

        if (isInitialized()) {
            // check for illegal arguments
            if (doc == null) {
                throw new IllegalArgumentException("doc == null");
            }
            else
            if ((width <= 0) || (height <= 0)) {
                throw new IllegalArgumentException("Invalid (negative or zero) texture dimensions");
            }
            else {
                result = new SVGTexture(this, doc, width, height, clearColor, dilateEdgesFix);
            }
        }

        return result;
    }

    /*
        Instantiate a texture atlas generator.

        The specified `scale` factor will be applied to all collected SVG
        documents/elements, in order to realize resolution-independent atlas.
        Every collected SVG document/element will be packed into rectangular
        textures, whose dimensions is auto-detected taking care of OpenGL and
        AmanithSVG limitations.

        If true, 'pow2Textures' will force textures to have power-of-two dimensions.

        Each packed element will be separated from the others by the specified 'border',
        in pixels.

        If the 'dilateEdgesFix' flag is set to true, the rendering process will also
        perform a 1-pixel dilate post-filter; this dilate filter is useful when the
        texture has some transparent parts (i.e. pixels with alpha component = 0): such
        flag will induce TextureFilter.Linear minification/magnification filtering.

        If the 'dilateEdgesFix' flag is set to false, no additional dilate post-filter
        is applied, and the texture minification/magnification filtering is set to
        TextureFilter.Nearest.

        Before the SVG rendering, pixels are initialized with the given 'clearColor'.
    */
    public SVGTextureAtlasGenerator createAtlasGenerator(float scale,
                                                         int border,
                                                         boolean dilateEdgesFix,
                                                         final SVGColor clearColor) {

        // auto-detect the maximum texture dimension (taking care of OpenGL and AmanithSVG limitations)
        int maxTexturesDimension = Math.min(SVGTextureUtils.getGlMaxTextureDimension(), SVGSurface.getMaxDimension());
        return createAtlasGenerator(scale, maxTexturesDimension, border, false, dilateEdgesFix, clearColor);
    }

    /*
        Instantiate a texture atlas generator.

        The specified `scale` factor will be applied to all collected SVG
        documents/elements, in order to realize resolution-independent atlas.
        Every collected SVG document/element will be packed into rectangular
        textures, whose dimensions is auto-detected taking care of OpenGL and
        AmanithSVG limitations.

        Each packed element will be separated from the others by the specified 'border',
        in pixels.

        If the 'dilateEdgesFix' flag is set to true, the rendering process will also
        perform a 1-pixel dilate post-filter; this dilate filter is useful when the
        texture has some transparent parts (i.e. pixels with alpha component = 0): such
        flag will induce TextureFilter.Linear minification/magnification filtering.

        If the 'dilateEdgesFix' flag is set to false, no additional dilate post-filter
        is applied, and the texture minification/magnification filtering is set to
        TextureFilter.Nearest.

        Before the SVG rendering, pixels are initialized with a transparent black color.
    */
    public SVGTextureAtlasGenerator createAtlasGenerator(float scale,
                                                         int border,
                                                         boolean dilateEdgesFix) {

        return createAtlasGenerator(scale, border, dilateEdgesFix, SVGColor.Clear);
    }

    /*
        Instantiate a texture atlas generator.

        The specified `scale` factor will be applied to all collected SVG
        documents/elements, in order to realize resolution-independent atlas.
        Every collected SVG document/element will be packed into rectangular
        textures, whose dimensions won't exceed the specified 'maxTexturesDimension',
        in pixels.

        If true, 'pow2Textures' will force textures to have power-of-two dimensions.

        Each packed element will be separated from the others by the specified 'border',
        in pixels.

        If the 'dilateEdgesFix' flag is set to true, the rendering process will also
        perform a 1-pixel dilate post-filter; this dilate filter is useful when the
        texture has some transparent parts (i.e. pixels with alpha component = 0): such
        flag will induce TextureFilter.Linear minification/magnification filtering.

        If the 'dilateEdgesFix' flag is set to false, no additional dilate post-filter
        is applied, and the texture minification/magnification filtering is set to
        TextureFilter.Nearest.

        Before the SVG rendering, pixels are initialized with the given 'clearColor'.
    */
    public SVGTextureAtlasGenerator createAtlasGenerator(float scale,
                                                         int maxTexturesDimension,
                                                         int border,
                                                         boolean pow2Textures,
                                                         boolean dilateEdgesFix,
                                                         final SVGColor clearColor) {

        return new SVGTextureAtlasGenerator(this, scale, maxTexturesDimension, border, pow2Textures, dilateEdgesFix, clearColor);
    }

    private static final String s_LIB_BASIC_NAME = "AmanithSVG";
    private static final String s_JNI_BASIC_NAME = "AmanithSVG-JNI";

    private static final String s_LOG_TAG = "SVGAssetsGDX";
}
