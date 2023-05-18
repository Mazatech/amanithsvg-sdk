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
package com.mazatech.amanithsvg.svgviewer;

import android.app.Activity;
import android.content.Intent;
import android.content.res.Resources;
import android.os.Build;
import android.support.annotation.NonNull;
import android.support.annotation.RequiresApi;
import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.util.DisplayMetrics;
import android.util.TypedValue;
import android.view.Menu;
import android.view.MenuItem;

// AmanithSVG for Android
import com.mazatech.android.SVGAssetsAndroid;
import com.mazatech.android.SVGAssetsConfigAndroid;

// AmanithSVG (low level layer)
import com.mazatech.svgt.SVGTResourceHint;
import com.mazatech.svgt.SVGTResourceType;

import static com.mazatech.amanithsvg.svgviewer.SvgViewerView.*;

import java.util.EnumSet;

public class SvgViewerActivity extends AppCompatActivity {

    private static final String LOG_TAG = "SvgViewerActivity";

    // keep track of loaded AmanithSVG native libraries
    private static boolean nativeLibsLoaded = false;
    // AmanithSVG for Android instance
    private static SVGAssetsAndroid svg = null;

    // view
    private SvgViewerView view = null;
    // SVG file path
    private String filePath = null;
    // menu
    static final int OPEN_MENU_ITEM = 0;
    static final int QUIT_MENU_ITEM = 99;
    // file dialog
    static final int REQUEST_LOAD = 0;

    private void resourceAdd(SVGAssetsConfigAndroid config,
                             // the unique integer id as generate by aapt tool
                             int aaptId,
                             final EnumSet<SVGTResourceHint> hints) {

        boolean supported;
        SVGTResourceType resourceType;
        Resources resources = getResources();
        TypedValue value = new TypedValue();
        // get resource value (e.g. raw/myfont.ttf)
        resources.getValue(aaptId, value, true);
        // extract filename (e.g. myfont.ttf)
        String[] parts = value.string.toString().split("/");
        String fileName = parts[parts.length - 1];
        String fileExt = fileName.substring(fileName.lastIndexOf(".") + 1);

        switch (fileExt.toLowerCase()) {
            case "otf":
            case "ttf":
            case "woff":
            case "woff2":
                supported = true;
                resourceType = SVGTResourceType.Font;
                break;
            case "jpg":
            case "jpeg":
            case "png":
                supported = true;
                resourceType = SVGTResourceType.Image;
                break;
            default:
                supported = false;
                resourceType = SVGTResourceType.Font;
                break;
        }

        if (supported) {
            if (resourceType == SVGTResourceType.Font) {
                config.addFont(resources.openRawResource(aaptId), aaptId, fileName, hints);
            }
            else {
                config.addImage(resources.openRawResource(aaptId), aaptId, fileName);
            }
        }
    }
    // initialize AmanithSVG and load external resources
    private void amanithsvgInit() {

        DisplayMetrics metrics = getResources().getDisplayMetrics();
        SVGAssetsConfigAndroid config = new SVGAssetsConfigAndroid(metrics.widthPixels, metrics.heightPixels, metrics.xdpi);

        // set curves quality (used by AmanithSVG geometric kernel to approximate curves with straight
        // line segments (flattening); valid range is [1; 100], where 100 represents the best quality
        config.setCurvesQuality(75);

        // specify the system/user-agent language; this setting will affect the conditional rendering
        // of <switch> elements and elements with 'systemLanguage' attribute specified
        config.setLanguage("en");

        // make external resources available to AmanithSVG; NB: all resources must be specified in
        // advance before to call rendering-related functions, which are by definition tied to a thread
        resourceAdd(config, R.raw.bebas_neue_regular, EnumSet.of(SVGTResourceHint.DefaultFantasy));
        resourceAdd(config, R.raw.dancing_script_regular, EnumSet.of(SVGTResourceHint.DefaultCursive));
        resourceAdd(config, R.raw.noto_mono_regular, EnumSet.of(SVGTResourceHint.DefaultMonospace,
                                                                SVGTResourceHint.DefaultUIMonospace));
        resourceAdd(config, R.raw.noto_sans_regular, EnumSet.of(SVGTResourceHint.DefaultSansSerif,
                                                                SVGTResourceHint.DefaultUISansSerif,
                                                                SVGTResourceHint.DefaultSystemUI,
                                                                SVGTResourceHint.DefaultUIRounded));
        resourceAdd(config, R.raw.noto_serif_regular, EnumSet.of(SVGTResourceHint.DefaultSerif,
                                                                 SVGTResourceHint.DefaultUISerif));

        // initialize AmanithSVG library
        svg = new SVGAssetsAndroid(config);
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
                view = new SvgViewerView(this, svg, filePath);
                super.setContentView(view);
            }
        }
        else {
            finish();
        }
    }

    @RequiresApi(api = Build.VERSION_CODES.LOLLIPOP)
    @Override
    protected void onCreate(Bundle savedInstanceState) {

        super.onCreate(savedInstanceState);
        // load AmanithSVG native libraries, if needed
        if (!nativeLibsLoaded) {
            nativeLibsLoaded = SVGAssetsAndroid.jniLibraryLoad(getAssets());
            if (nativeLibsLoaded) {
                // instantiate AmanithSVG for Android
                amanithsvgInit();
            }
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
    protected void onDestroy() {

        super.onDestroy();
        // NB: we don't call AmanithSVG.svgtDone because the application may only have moved in the
        // background; by not terminating AmanithSVG here, we are sure that once moved back in
        // foreground, the application (i.e. calls to AmanithSVG) will work without problems.
        //
        // AmanithSVG.svgtDone should be called when the application is really killed, but this
        // implies the use of additional mechanisms to intercept the real termination
        // (i.e. process killing) and is therefore left out of this example.
        // However, it must be taken into account that AmanithSVG shared library implements a
        // library destructor (i.e. __attribute__ ((destructor)) static void SoDestructor)
        // that will call the svgtDone function by itself; so that, no matter what the application
        // does, all allocated resources will be released.
    }

    @Override
    public boolean onCreateOptionsMenu(@NonNull Menu menu) {

        // build the menu
        menu.add(0, OPEN_MENU_ITEM, 0, "Open...");
        menu.add(0, SVGVIEWER_ABOUT_CMD, 0, "About...");
        menu.add(0, QUIT_MENU_ITEM, 0, "Quit");
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(@NonNull MenuItem item) {

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
