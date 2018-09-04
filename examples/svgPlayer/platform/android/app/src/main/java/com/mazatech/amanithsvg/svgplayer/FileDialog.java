// A modified version of http://code.google.com/p/android-file-dialog by Alexander Ponomarev
package com.mazatech.amanithsvg.svgplayer;

import java.io.File;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.TreeMap;

import android.app.AlertDialog;
import android.app.ListActivity;
import android.content.DialogInterface;
import android.content.pm.PackageManager;
import android.os.Build;
import android.os.Bundle;
import android.support.v4.app.ActivityCompat;
import android.support.v4.content.ContextCompat;
import android.view.KeyEvent;
import android.view.View;
import android.view.View.OnClickListener;
import android.view.inputmethod.InputMethodManager;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.ListView;
import android.widget.SimpleAdapter;
import android.widget.TextView;

import static android.Manifest.permission.READ_EXTERNAL_STORAGE;

public class FileDialog extends ListActivity {

    private static final int REQUEST_SD_READ_PERMISSION = 0;

    private static final String ITEM_KEY = "key";
    private static final String ITEM_IMAGE = "image";
    private static final String ROOT = "/";
    public static final String START_PATH = "START_PATH";
    public static final String FORMAT_FILTER = "FORMAT_FILTER";
    public static final String RESULT_PATH = "RESULT_PATH";

    private String startPath;
    private List<String> path = null;
    private Button selectButton;
    private String parentPath;
    private String currentPath = ROOT;
    private File selectedFile;
    private HashMap<String, Integer> lastPositions = new HashMap<String, Integer>();

    @Override
    public void onRequestPermissionsResult(int requestCode, String permissions[], int[] grantResults) {

        switch (requestCode) {
            case REQUEST_SD_READ_PERMISSION:
                // if request is cancelled, the result arrays are empty
                if ((grantResults.length <= 0) || (grantResults[0] != PackageManager.PERMISSION_GRANTED)) {
                    // permission denied
                    startPath = ROOT;
                }
                getDir(startPath);
                break;
            default:
                break;
        }
    }

    @Override
    public void onCreate(Bundle savedInstanceState) {

        super.onCreate(savedInstanceState);
        setResult(RESULT_CANCELED, getIntent());
        setContentView(R.layout.file_dialog_main);

        // 'Select' button
        selectButton = (Button)findViewById(R.id.fdButtonSelect);
        selectButton.setEnabled(false);
        selectButton.setOnClickListener(new OnClickListener() {
            // "select" button, click handler
            @Override public void onClick(View v) {
                if (selectedFile != null) {
                    // we have finished
                    getIntent().putExtra(RESULT_PATH, selectedFile.getPath());
                    setResult(RESULT_OK, getIntent());
                    finish();
                }
            }
        });

        // start browsing path
        startPath = getIntent().getStringExtra(START_PATH);
        startPath = startPath != null ? startPath : ROOT;

        // ask for storage read permission, if needed
        if (Build.VERSION.SDK_INT >= 23) {
            boolean sdReadGranted = ContextCompat.checkSelfPermission(this, READ_EXTERNAL_STORAGE) == PackageManager.PERMISSION_GRANTED;
            if (!sdReadGranted) {
                ActivityCompat.requestPermissions(this, new String[] { READ_EXTERNAL_STORAGE }, REQUEST_SD_READ_PERMISSION);
            }
            else {
                getDir(startPath);
            }
        }
        else {
            getDir(startPath);
        }
    }

    private void getDir(String dirPath) {

        boolean useAutoSelection = dirPath.length() < currentPath.length();
        Integer position = lastPositions.get(parentPath);

        // get the actual list of files
        getDirImpl(dirPath);
        if ((position != null) && useAutoSelection) {
            getListView().setSelection(position);
        }
    }

    private void getDirImpl(final String dirPath) {


        ArrayList<HashMap<String, Object>> list = new ArrayList<HashMap<String, Object>>();
        final List<String> item = new ArrayList<String>();
        TextView locationLabel = (TextView)findViewById(R.id.path);

        path = new ArrayList<String>();
        currentPath = dirPath;

        // if given path is not readable, try with the root "/" path
        File f = new File(currentPath);
        File[] files = f.listFiles();
        if (files == null) {
            currentPath = ROOT;
            f = new File(currentPath);
            files = f.listFiles();
        }

        // set head label, showing current path
        locationLabel.setText(getText(R.string.fd_location) + ": " + currentPath);

        if (!currentPath.equals(ROOT)) {

            item.add(ROOT);
            addItem(list, ROOT, R.drawable.fd_folder_icon);
            path.add(ROOT);

            item.add("../");
            addItem(list, "../", R.drawable.fd_folder_icon);
            path.add(f.getParent());
            parentPath = f.getParent();
        }

        TreeMap<String, String> dirsMap = new TreeMap<String, String>();
        TreeMap<String, String> dirsPathMap = new TreeMap<String, String>();
        TreeMap<String, String> filesMap = new TreeMap<String, String>();
        TreeMap<String, String> filesPathMap = new TreeMap<String, String>();
        for (File file : files) {
            if (file.isDirectory()) {
                String dirName = file.getName();
                dirsMap.put(dirName, dirName);
                dirsPathMap.put(dirName, file.getPath());
            }
            else {
                final String fileName = file.getName();
                final String fileNameLwr = fileName.toLowerCase();
                final String[] formatFilter = getIntent().getStringArrayExtra(FORMAT_FILTER);

                if (formatFilter != null) {
                    boolean contains = false;
                    for (int i = 0; i < formatFilter.length; i++) {
                        final String formatLwr = formatFilter[i].toLowerCase();
                        if (fileNameLwr.endsWith(formatLwr)) {
                            contains = true;
                            break;
                        }
                    }
                    if (contains) {
                        filesMap.put(fileName, fileName);
                        filesPathMap.put(fileName, file.getPath());
                    }
                }
                else {
                    filesMap.put(fileName, fileName);
                    filesPathMap.put(fileName, file.getPath());
                }
            }
        }
        item.addAll(dirsMap.tailMap("").values());
        item.addAll(filesMap.tailMap("").values());
        path.addAll(dirsPathMap.tailMap("").values());
        path.addAll(filesPathMap.tailMap("").values());

        SimpleAdapter fileList = new SimpleAdapter(this, list, R.layout.file_dialog_row, new String[] { ITEM_KEY, ITEM_IMAGE }, new int[] { R.id.fd_row_text, R.id.fd_row_image });

        // add folders
        for (String dir : dirsMap.tailMap("").values()) {
            addItem(list, dir, R.drawable.fd_folder_icon);
        }

        // add files
        for (String file : filesMap.tailMap("").values()) {
            addItem(list, file, R.drawable.fd_file_icon);
        }

        fileList.notifyDataSetChanged();
        setListAdapter(fileList);
    }

    private void addItem(ArrayList<HashMap<String, Object>> list, String fileName, int imageId) {

        HashMap<String, Object> item = new HashMap<String, Object>();
        item.put(ITEM_KEY, fileName);
        item.put(ITEM_IMAGE, imageId);
        list.add(item);
    }

    @Override
    protected void onListItemClick(ListView l, View v, int position, long id) {

        File file = new File(path.get(position));

        setSelectVisible(v);

        if (file.isDirectory()) {
            selectButton.setEnabled(false);
            if (file.canRead()) {
                lastPositions.put(currentPath, position);
                getDir(path.get(position));
            }
            else {
                AlertDialog.Builder dialogBuilder = new AlertDialog.Builder(this);
                dialogBuilder.setIcon(R.drawable.fd_alert_icon);
                dialogBuilder.setTitle("[" + file.getName() + "] " + getText(R.string.fd_cant_read_folder));
                dialogBuilder.setPositiveButton("OK", new DialogInterface.OnClickListener() {
                    @Override public void onClick(DialogInterface dialog, int which) {
                    }
                });
                dialogBuilder.show();
            }
        }
        else {
            selectedFile = file;
            v.setSelected(true);
            selectButton.setEnabled(true);
        }
    }

    @Override
    public boolean onKeyDown(int keyCode, KeyEvent event) {

        if ((keyCode == KeyEvent.KEYCODE_BACK)) {
            selectButton.setEnabled(false);
            if (!currentPath.equals(ROOT)) {
                getDir(parentPath);
            }
            else {
                finish();
            }
            return true;
        }
        else {
            return super.onKeyDown(keyCode, event);
        }
    }

    private void setSelectVisible(View v) {

        LinearLayout layoutSelect = (LinearLayout)findViewById(R.id.fdLinearLayoutSelect);
        InputMethodManager inputManager = (InputMethodManager)getSystemService(INPUT_METHOD_SERVICE);

        layoutSelect.setVisibility(View.VISIBLE);
        inputManager.hideSoftInputFromWindow(v.getWindowToken(), 0);
        selectButton.setEnabled(false);
    }
}
