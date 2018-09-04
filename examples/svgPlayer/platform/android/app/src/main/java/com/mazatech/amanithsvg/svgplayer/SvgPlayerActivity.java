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
package com.mazatech.amanithsvg.svgplayer;

import android.app.Activity;
import android.content.Intent;
import android.os.Environment;
import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.os.Build;
import android.view.Menu;
import android.view.MenuItem;

import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.OutputStream;

import static com.mazatech.amanithsvg.svgplayer.SvgPlayerView.*;

public class SvgPlayerActivity extends AppCompatActivity {

    // keep track of loaded AmanithVG native libraries
    static boolean nativeLibsLoaded = false;
    // view
    private SvgPlayerView view = null;
    // SVG file path
    private String filePath = null;
    // menu
    static final int QUIT_MENU_ITEM = 99;
    // file dialog
    static final int REQUEST_LOAD = 0;

    private boolean copyFile(InputStream input, OutputStream output) {

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

        // set start path
        intent.putExtra(FileDialog.START_PATH, Environment.getExternalStorageDirectory().getAbsolutePath());
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
                view = new SvgPlayerView(this, filePath);
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
        menu.add(0, SVGPLAYER_ABOUT_CMD, 0, "About...");
        menu.add(0, QUIT_MENU_ITEM, 0, "Quit");
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {

        boolean result = view.playerMenuOption(item.getItemId());

        if (!result) {
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
