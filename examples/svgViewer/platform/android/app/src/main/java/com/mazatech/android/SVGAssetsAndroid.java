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

import java.io.BufferedReader;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.nio.IntBuffer;
import java.nio.charset.Charset;

import javax.microedition.khronos.opengles.GL10;
import javax.microedition.khronos.opengles.GL11;

import android.annotation.TargetApi;
import android.content.res.AssetManager;
import android.opengl.GLES11Ext;
import android.opengl.GLES30;
import android.os.Build;
import android.support.annotation.NonNull;
import android.support.annotation.Nullable;
import android.support.annotation.RequiresApi;
import android.util.Log;

// AmanithSVG (high level layer)
import com.mazatech.svgt.SVGAssets;
import com.mazatech.svgt.SVGDocument;
import com.mazatech.svgt.SVGSurface;
import com.mazatech.svgt.SVGTError;

public class SVGAssetsAndroid extends SVGAssets {

    /* Constructor, the given configuration must provide resources too. */
    public SVGAssetsAndroid(SVGAssetsConfigAndroid config) {

        super();

        // initialize the AmanithSVG library as well as the JNI wrapper mechanism
        // NB: init() method is from SVGAssets base class
        if (super.init(config) != SVGTError.None) {
            throw new IllegalStateException("Failed SVGAssets initialization");
        }
        _glesQueried = false;
    }

    private static boolean sharedLibraryCopy(@NonNull InputStream input,
                                             OutputStream output) {

        boolean result;

        try {
            byte[] buffer = new byte[4096];
            while (true) {
                int length = input.read(buffer);
                if (length == -1) {
                    break;
                }
                output.write(buffer, 0, length);
            }
            // close streams
            input.close();
            output.close();
            result = true;
        }
        catch (java.io.IOException e) {
            Log.e(s_LOG_TAG, "File copy failed.\n" + e);
            result = false;
        }

        return result;
    }

    private static boolean sharedLibraryLoad(@NonNull AssetManager assets,
                                             String libPath,
                                             String libName) {

        boolean result;
        String tmpPath = System.getProperty("java.io.tmpdir") + "/AmanithSVG/";
        String tmpLib = tmpPath + libName;
        // ensure the existence of destination directory
        File tmpFile = new File(tmpLib);
        tmpFile.getParentFile().mkdirs();

        try {
            InputStream input = assets.open(libPath + libName);
            FileOutputStream output = new FileOutputStream(tmpFile);
            result = sharedLibraryCopy(input, output);
        }
        catch (java.io.IOException e) {
            Log.e(s_LOG_TAG, "Opening file streams failed.\n" + e);
            result = false;
        }

        if (result) {
            try {
                System.load(tmpLib);
            }
            catch (UnsatisfiedLinkError e) {
                Log.e(s_LOG_TAG, "Native code library failed to load, the file does not exist.\n" + e);
                result = false;
            }
        }

        return result;
    }

    // Load AmanithSVG (and its JNI counterpart) native library
    @RequiresApi(api = Build.VERSION_CODES.LOLLIPOP)
    static public boolean jniLibraryLoad(@NonNull AssetManager assets) {

        boolean result = true;

        String vm = System.getProperty("java.vm.name");

        if ((vm != null) && vm.contains("Dalvik")) {

            String abi = (Build.VERSION.SDK_INT < Build.VERSION_CODES.LOLLIPOP) ? Build.CPU_ABI : Build.SUPPORTED_ABIS[0];
            String svgLibsPath = "amanithsvg-natives";

            if (abi.equals("arm64-v8a")) {
                if (System.getProperty("os.arch").startsWith("armv7")) {
                    abi = "armeabi-v7a";
                }
            }

            if (abi.equals("armeabi")) {
                svgLibsPath += "/armeabi";
            }
            else
            if (abi.equals("armeabi-v7a")) {
                svgLibsPath += "/armeabi-v7a";
            }
            else
            if (abi.equals("arm64-v8a")) {
                svgLibsPath += "/arm64-v8a";
            }
            else
            if (abi.equals("mips")) {
                svgLibsPath += "/mips";
            }
            else
            if (abi.equals("mips64")) {
                svgLibsPath += "/mips64";
            }
            else
            if (abi.equals("x86")) {
                svgLibsPath += "/x86";
            }
            else
            if (abi.equals("x86_64")) {
                svgLibsPath += "/x86_64";
            }
            else {
                result = false;
            }
            // select the backend engine (SRE / GLE)
            svgLibsPath += "/sre";
            svgLibsPath += "/standalone/";

            if (result) {
                // load AmanithSVG library (thread-safe version)
                result = sharedLibraryLoad(assets, svgLibsPath, "lib" + s_LIB_BASIC_NAME + ".so");
                if (result) {
                    // load AmanithSVG JNI wrapper
                    result = sharedLibraryLoad(assets, svgLibsPath, "lib" + s_JNI_BASIC_NAME + ".so");
                }
            }
        }

        return result;
    }

    public int createTexture(@NonNull GL11 gl,
                             int width,
                             int height,
                             int[] pixels) {

        int[] glTexture = new int[] { 0 };
        IntBuffer pixelsBuffer = IntBuffer.wrap(pixels);

        gl.glGenTextures(1, glTexture, 0);
        gl.glEnable(GL10.GL_TEXTURE_2D);
        gl.glBindTexture(GL10.GL_TEXTURE_2D, glTexture[0]);
        gl.glTexParameteri(GL10.GL_TEXTURE_2D, GL10.GL_TEXTURE_MIN_FILTER, GL10.GL_NEAREST);
        gl.glTexParameteri(GL10.GL_TEXTURE_2D, GL10.GL_TEXTURE_MAG_FILTER, GL10.GL_NEAREST);
        gl.glTexParameteri(GL10.GL_TEXTURE_2D, GL10.GL_TEXTURE_WRAP_S, GL10.GL_REPEAT);
        gl.glTexParameteri(GL10.GL_TEXTURE_2D, GL10.GL_TEXTURE_WRAP_T, GL10.GL_REPEAT);
        gl.glTexImage2D(GL10.GL_TEXTURE_2D, 0, _internalFormat, width, height, 0, _externalFormat, GL10.GL_UNSIGNED_BYTE, pixelsBuffer);
        return glTexture[0];
    }

    /*
        Create an OpenGL ES texture out of the given drawing surface.

        Internally the method queries OpenGL ES runtime, in order to verify the support
        of helpful extensions that can accelerate the upload of AmanithSVG surface
        pixels to the OpenGL ES texture. Tested GPU features are:

        - BGRA textures: GL_APPLE_texture_format_BGRA8888, GL_EXT_bgra,
          GL_IMG_texture_format_BGRA8888, GL_EXT_texture_format_BGRA8888

        - NPOT (non-power-of-two) textures: GL_OES_texture_npot,
          GL_APPLE_texture_2D_limited_npot, GL_ARB_texture_non_power_of_two

        - texture swizzle: GL_EXT_texture_swizzle, GL_ARB_texture_swizzle

        The function returns the OpenGL ES texture handle.
    */
    @TargetApi(18)
    public int createTexture(@NonNull GL11 gl,
                             @NonNull SVGSurface surface) {

        int[] glTexture = new int[] { 0 };
        int[] maxTextureSize = new int[1];
        // get AmanithSVG surface dimensions
        int surfaceWidth = surface.getWidth();
        int surfaceHeight = surface.getHeight();

        if (!_glesQueried) {
            glesQuery(gl);
        }

        // get maximum texture size
        gl.glGetIntegerv(GL10.GL_MAX_TEXTURE_SIZE, maxTextureSize, 0);

        if ((surfaceWidth <= maxTextureSize[0]) && (surfaceHeight <= maxTextureSize[0])) {

            // generate OpenGL ES texture
            gl.glGenTextures(1, glTexture, 0);
            gl.glEnable(GL10.GL_TEXTURE_2D);
            gl.glBindTexture(GL10.GL_TEXTURE_2D, glTexture[0]);
            gl.glTexParameteri(GL10.GL_TEXTURE_2D, GL10.GL_TEXTURE_MIN_FILTER, GL10.GL_NEAREST);
            gl.glTexParameteri(GL10.GL_TEXTURE_2D, GL10.GL_TEXTURE_MAG_FILTER, GL10.GL_NEAREST);
            gl.glTexParameteri(GL10.GL_TEXTURE_2D, GL10.GL_TEXTURE_WRAP_S, GL10.GL_CLAMP_TO_EDGE);
            gl.glTexParameteri(GL10.GL_TEXTURE_2D, GL10.GL_TEXTURE_WRAP_T, GL10.GL_CLAMP_TO_EDGE);

            if (_bgraSupport) {
                // get AmanithSVG surface pixels
                java.nio.ByteBuffer surfacePixels = surface.getPixels();
                if (_npotSupport) {
                    // allocate texture memory and upload pixels
                    gl.glTexImage2D(GL10.GL_TEXTURE_2D, 0, _internalFormat, surfaceWidth, surfaceHeight, 0, _externalFormat, GL10.GL_UNSIGNED_BYTE, surfacePixels);
                }
                else {
                    // allocate texture memory
                    gl.glTexImage2D(GL10.GL_TEXTURE_2D, 0, _internalFormat, surface.getWidthPow2(), surface.getHeightPow2(), 0, _externalFormat, GL10.GL_UNSIGNED_BYTE, null);
                    // upload pixels
                    gl.glTexSubImage2D(GL10.GL_TEXTURE_2D, 0, 0, 0, surfaceWidth, surfaceHeight, _externalFormat, GL10.GL_UNSIGNED_BYTE, surfacePixels);
                }
            }
            else {
                if (_swizzleSupport) {
                    // get AmanithSVG surface pixels
                    java.nio.ByteBuffer surfacePixels = surface.getPixels();
                    // set swizzle
                    int[] bgraSwizzle = new int[] { GLES30.GL_BLUE, GLES30.GL_GREEN, GLES30.GL_RED, GL10.GL_ALPHA };
                    gl.glTexParameteri(GL10.GL_TEXTURE_2D, GLES30.GL_TEXTURE_SWIZZLE_R, bgraSwizzle[0]);
                    gl.glTexParameteri(GL10.GL_TEXTURE_2D, GLES30.GL_TEXTURE_SWIZZLE_G, bgraSwizzle[1]);
                    gl.glTexParameteri(GL10.GL_TEXTURE_2D, GLES30.GL_TEXTURE_SWIZZLE_B, bgraSwizzle[2]);
                    gl.glTexParameteri(GL10.GL_TEXTURE_2D, GLES30.GL_TEXTURE_SWIZZLE_A, bgraSwizzle[3]);

                    if (_npotSupport) {
                        // allocate texture memory and upload pixels
                        gl.glTexImage2D(GL10.GL_TEXTURE_2D, 0, GL10.GL_RGBA, surfaceWidth, surfaceHeight, 0, GL10.GL_RGBA, GL10.GL_UNSIGNED_BYTE, surfacePixels);
                    }
                    else {
                        // allocate texture memory
                        gl.glTexImage2D(GL10.GL_TEXTURE_2D, 0, GL10.GL_RGBA, surface.getWidthPow2(), surface.getHeightPow2(), 0, GL10.GL_RGBA, GL10.GL_UNSIGNED_BYTE, null);
                        // upload pixels
                        gl.glTexSubImage2D(GL10.GL_TEXTURE_2D, 0, 0, 0, surfaceWidth, surfaceHeight, GL10.GL_RGBA, GL10.GL_UNSIGNED_BYTE, surfacePixels);
                    }
                }
                else {
                    int[] rgbaPixels = new int[surfaceWidth * surfaceHeight];
                    // copy AmanithSVG drawing surface content into the specified pixels buffer, taking care to swap red <--> blue channels
                    if (surface.copy(rgbaPixels, true, false) == SVGTError.None) {
                        if (_npotSupport) {
                            // allocate texture memory and upload pixels
                            gl.glTexImage2D(GL10.GL_TEXTURE_2D, 0, GL10.GL_RGBA, surfaceWidth, surfaceHeight, 0, GL10.GL_RGBA, GL10.GL_UNSIGNED_BYTE, IntBuffer.wrap(rgbaPixels));
                        }
                        else {
                            // allocate texture memory
                            gl.glTexImage2D(GL10.GL_TEXTURE_2D, 0, GL10.GL_RGBA, surface.getWidthPow2(), surface.getHeightPow2(), 0, GL10.GL_RGBA, GL10.GL_UNSIGNED_BYTE, null);
                            // upload pixels
                            gl.glTexSubImage2D(GL10.GL_TEXTURE_2D, 0, 0, 0, surfaceWidth, surfaceHeight, GL10.GL_RGBA, GL10.GL_UNSIGNED_BYTE, IntBuffer.wrap(rgbaPixels));
                        }
                    }
                }
            }
        }

        return glTexture[0];
    }

    // Load a text file.
    private @Nullable String loadXml(String filePath) {

        // read text from file
        StringBuilder stringBuilder = new StringBuilder();

        try {
            Charset charset = Charset.forName("UTF-8");
            BufferedReader reader = new BufferedReader(new InputStreamReader(new FileInputStream(filePath), charset));
            String line;
            while ((line = reader.readLine()) != null) {
                stringBuilder.append(line);
                stringBuilder.append('\n');
            }
            reader.close();
        }
        catch (IOException e) {
            return null;
        }
        return stringBuilder.toString();
    }

    /* Create and load an SVG document, specifying a full file path (path and file name). */
    public SVGDocument createDocumentFromFile(String filePath) {

        SVGDocument result = null;
        // load SVG content
        String xmlText = loadXml(filePath);

        if ((xmlText != null) && (!xmlText.isEmpty())) {
            result = createDocument(xmlText);
        }

        return result;
    }

    // Does the given OpenGL ES instance support non-power-of-two texture?
    public boolean glSupportNPOT(@NonNull GL11 gl) {

        if (!_glesQueried) {
            glesQuery(gl);
        }

        return _npotSupport;
    }

    // Does the given OpenGL ES instance support BGRA texture?
    public boolean glSupportBGRA(@NonNull GL11 gl) {

        if (!_glesQueried) {
            glesQuery(gl);
        }

        return _bgraSupport;
    }

    // Query the given OpenGL ES instance in order to check useful extensions
    private void glesQuery(@NonNull GL11 gl) {

        String extensions = gl.glGetString(GL10.GL_EXTENSIONS);

        // get texture capabilities
        if (extensions.contains("GL_APPLE_texture_format_BGRA8888") || extensions.contains("GL_EXT_bgra")) {
            _bgraSupport = true;
            _internalFormat = GL10.GL_RGBA;
            _externalFormat = GLES11Ext.GL_BGRA;
        }
        else
        if (extensions.contains("GL_IMG_texture_format_BGRA8888") || extensions.contains("GL_EXT_texture_format_BGRA8888")) {
            _bgraSupport = true;
            _internalFormat = GLES11Ext.GL_BGRA;
            _externalFormat = GLES11Ext.GL_BGRA;
        }
        else {
            _bgraSupport = false;
            _internalFormat = GL10.GL_RGBA;
            _externalFormat = GL10.GL_RGBA;
        }
        _npotSupport = (extensions.contains("GL_OES_texture_npot") ||
                        extensions.contains("GL_APPLE_texture_2D_limited_npot") ||
                        extensions.contains("GL_ARB_texture_non_power_of_two"));
        _swizzleSupport = (extensions.contains("GL_EXT_texture_swizzle") ||
                           extensions.contains("GL_ARB_texture_swizzle"));
        _glesQueried = true;
    }

    private boolean _glesQueried;
    // OpenGL ES support of BGRA textures
    private boolean _bgraSupport;
    // OpenGL ES support of npot textures
    private boolean _npotSupport;
    // OpenGL ES support of texture swizzle
    private boolean _swizzleSupport;
    // OpenGL ES texture formats
    private int _internalFormat;
    private int _externalFormat;

    private static final String s_LOG_TAG = "SVGAssetsAndroid";
    private static final String s_LIB_BASIC_NAME = "AmanithSVG";
    private static final String s_JNI_BASIC_NAME = "AmanithSVG-JNI";
}
