/****************************************************************************
** Copyright (c) 2013-2019 Mazatech S.r.l.
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
package com.mazatech.amanithsvg.svgviewer;

import android.app.Activity;
import android.content.Intent;
import android.support.annotation.NonNull;
import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.os.Build;
import android.view.Menu;
import android.view.MenuItem;

import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.OutputStream;

import static com.mazatech.amanithsvg.svgviewer.SvgViewerView.*;

public class SvgViewerActivity extends AppCompatActivity {

    // keep track of loaded AmanithSVG native libraries
    static boolean nativeLibsLoaded = false;
    // view
    private SvgViewerView view = null;
    // SVG file path
    private String filePath = null;
    // menu
    static final int OPEN_MENU_ITEM = 0;
    static final int QUIT_MENU_ITEM = 99;
    // file dialog
    static final int REQUEST_LOAD = 0;

    private boolean copyFile(@NonNull InputStream input, OutputStream output) {

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
            System.err.println("File copy failed.\n" + e);
            result = false;
        }

        return result;
    }

    private boolean loadSharedLibrary(String libPath, String libName) {

        boolean result;
        String tmpPath = System.getProperty("java.io.tmpdir") + "/AmanithSVG/";
        String tmpLib = tmpPath + libName;
        // ensure the existence of destination directory
        File tmpFile = new File(tmpLib);
        tmpFile.getParentFile().mkdirs();

        try {
            InputStream input = getAssets().open(libPath + libName);
            FileOutputStream output = new FileOutputStream(tmpFile);
            result = copyFile(input, output);
        }
        catch (java.io.IOException e) {
            System.err.println("Opening file streams failed.\n" + e);
            result = false;
        }

        if (result) {
            try {
                System.load(tmpLib);
            }
            catch (UnsatisfiedLinkError e) {
                System.err.println("Native code library failed to load, the file does not exist.\n" + e);
                result = false;
            }
        }

        return result;
    }

    private boolean loadAmanithSVG() {

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
                // load AmanithSVG library
                result = loadSharedLibrary(svgLibsPath, "libAmanithSVG.so");
                if (result) {
                    // load AmanithSVG JNI wrapper
                    result = loadSharedLibrary(svgLibsPath, "libAmanithSVGJNI.so");
                }
            }
        }

        return result;
    }

    private void fileChooseDialog() {

        Intent intent = new Intent(getBaseContext(), FileDialog.class);

        // set start path (by setting an empty string, the file dialog will start from
        // the last selected file location, if any; or from the standard "external storage" directory
        intent.putExtra(FileDialog.START_PATH, "");
        // set file filter
        intent.putExtra(FileDialog.FORMAT_FILTER, new String[] { "svg" });
        // start dialog activity
        startActivityForResult(intent, REQUEST_LOAD);
    }

    @Override
    protected synchronized void onActivityResult(final int requestCode, int resultCode, final Intent data) {

        if (resultCode == Activity.RESULT_OK) {
            if (requestCode == REQUEST_LOAD) {
                filePath = new String(data.getStringExtra(FileDialog.RESULT_PATH));
                // create the view
                view = new SvgViewerView(this, filePath);
                super.setContentView(view);
            }
        }
        else {
            finish();
        }
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {

        super.onCreate(savedInstanceState);
        // load AmanithSVG native libraries, if needed
        if (!nativeLibsLoaded) {
            nativeLibsLoaded = loadAmanithSVG();
        }
        if (nativeLibsLoaded) {
            view = null;
            filePath = null;
        }
        else {
            setContentView(R.layout.error_view);
        }
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {

        // build the menu
        menu.add(0, OPEN_MENU_ITEM, 0, "Open...");
        menu.add(0, SVGVIEWER_ABOUT_CMD, 0, "About...");
        menu.add(0, QUIT_MENU_ITEM, 0, "Quit");
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {

        // first check if the action can be handled by the view
        boolean result = view.viewerMenuOption(item.getItemId());

        if (!result) {
            if (item.getItemId() == OPEN_MENU_ITEM) {
                fileChooseDialog();
                result = true;
            }
            else
            if (item.getItemId() == QUIT_MENU_ITEM) {
                finish();
                result = true;
            }
            else {
                result = super.onOptionsItemSelected(item);
            }
        }
        return result;
    }

    @Override
    protected void onPause() {

        super.onPause();

        if (view != null) {
            view.onPause();
        }
    }

    @Override
    protected void onResume() {

        super.onResume();

        if (filePath == null) {
            // select SVG file
            fileChooseDialog();
        }
        else {
            view.onResume();
        }
    }
}
