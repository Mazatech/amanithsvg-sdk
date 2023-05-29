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
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SVGAssets
{
    public class SVGAtlasSelector : ScriptableWizard
    {
        static private List<SVGAtlas> GetAtlasesList()
        {
            string[] guids = AssetDatabase.FindAssets("t:SVGAtlas");
            List<SVGAtlas> atlasesList = new List<SVGAtlas>();

            foreach (string assetGUID in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(SVGAtlas));
                SVGAtlas atlas = asset as SVGAtlas;
            
                if (atlas != null)
                {
                    atlasesList.Add(atlas);
                }
            }

            // sort atlases by name
            atlasesList.Sort(delegate(SVGAtlas atlas1, SVGAtlas atlas2) {
                return string.Compare(atlas1.name, atlas2.name, StringComparison.OrdinalIgnoreCase);
            });

            return atlasesList;
        }

        // retrieves a list of all atlas whose names contain the specified match string
        static private List<SVGAtlas> FilterAtlasesList(List<SVGAtlas> wholeList, string match)
        {
            List<SVGAtlas> result;

            if (wholeList == null)
            {
                return new List<SVGAtlas>();
            }

            if (string.IsNullOrEmpty(match))
            {
                return wholeList;
            }

            // create the output list
            result = new List<SVGAtlas>();

            // find an exact match
            foreach (SVGAtlas atlas in wholeList)
            {
                if (!string.IsNullOrEmpty(atlas.name) && string.Equals(match, atlas.name, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(atlas);
                }
            }
            // if an exact match has been found, simply return the result
            if (result.Count > 0)
            {
                return result;
            }

            // search for (space) separated components
            string[] searchKeys = match.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < searchKeys.Length; ++i)
            {
                searchKeys[i] = searchKeys[i].ToLower();
            }

            // find all atlases whose names contain one ore more keyword
            foreach (SVGAtlas atlas in wholeList)
            {
                if (!string.IsNullOrEmpty(atlas.name))
                {
                    string lowerName = atlas.name.ToLower();
                    int matchesCount = 0;

                    foreach (string key in searchKeys)
                    {
                        if (lowerName.Contains(key))
                        {
                            matchesCount++;
                        }
                    }

                    // (matchesCount == searchKeys.Length) if we were interested in finding all atlases whose names contain ALL the keywords
                    if (matchesCount > 0)
                    {
                        result.Add(atlas);
                    }
                }
            }

            return result;
        }

        private static void AtlasLabel(string atlasName, Rect rect)
        {
            GUI.backgroundColor = ATLAS_NAME_BACKGROUND_COLOR;
            GUI.contentColor = ATLAS_NAME_COLOR;
            // we use the atlas name as tooltip
            GUI.Label(new Rect(rect.x, rect.y + rect.height, rect.width, ATLAS_NAME_HEIGHT), new GUIContent(atlasName, atlasName), "ProgressBarBack");
            GUI.contentColor = Color.white;
            GUI.backgroundColor = Color.white;
        }

        // Draw an atlas preview
        private static void AtlasPreview(SVGAtlas atlas, Rect clipRect, int textureIndex)
        {
            int idx = textureIndex % atlas.TextureAssetsCount();
            AssetFile textureAsset = atlas.TextureAsset(idx);
            Texture2D atlasTexture = textureAsset.Object as Texture2D;

            if (atlasTexture != null)
            {
                Rect uv = new Rect(0, 0, 1, 1);
                float maxAtlasDim = Math.Max(atlasTexture.width, atlasTexture.height);
                float previewWidth = (atlasTexture.width / maxAtlasDim) * ATLAS_PREVIEW_DIMENSION;
                float previewHeight = (atlasTexture.height / maxAtlasDim) * ATLAS_PREVIEW_DIMENSION;
                float previewX = (ATLAS_PREVIEW_DIMENSION - previewWidth) / 2;
                float previewY = (ATLAS_PREVIEW_DIMENSION - previewHeight) / 2;
                Rect previewRect = new Rect(clipRect.xMin + previewX, clipRect.yMin + previewY, previewWidth, previewHeight);
                GUI.DrawTextureWithTexCoords(previewRect, atlasTexture, uv, true);
            }
        }

        private List<SVGAtlas> Header()
        {
            EditorGUIUtility.labelWidth = ATLAS_PREVIEW_DIMENSION;
            // show atlas name
            GUILayout.Label("Available atlases", "LODLevelNotifyText");
            // the search toolbox
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(85);
                _searchString = EditorGUILayout.TextField("", _searchString, "SearchTextField");
                if (GUILayout.Button("", "SearchCancelButton", GUILayout.Width(18)))
                {
                    _searchString = "";
                    GUIUtility.keyboardControl = 0;
                }
                GUILayout.Space(85);
            }
            GUILayout.EndHorizontal();
            // return the filtered atlases list
            return FilterAtlasesList(_atlasesList, _searchString);
        }

        private bool DrawGUI()
        {
            bool close = false;
        #if UNITY_EDITOR_WIN
            // take care of dpi scaling factor on Windows (Display Settings --> Advanced scaling settings)
            float dpi = Screen.dpi;
            // dpi  96 == 1.00
            // dpi 120 == 1.25
            // dpi 144 == 1.50
            // dpi 168 == 1.75
            // ... and so on
            float dpiAdjust = (((dpi - 96.0f) / 24.0f) * 0.25f) + 1.0f;
        #else
            float dpiAdjust = 1.0f;
        #endif
            int columnsPerRow = Math.Max(Mathf.FloorToInt((Screen.width / dpiAdjust) / ATLAS_PREVIEW_DIMENSION_PADDED), 1);
            int rowsCount = 1;
            int atlasIdx = 0;
            Rect rect = new Rect(ATLAS_PREVIEW_BORDER, ATLAS_PREVIEW_BORDER,
                                 ATLAS_PREVIEW_DIMENSION, ATLAS_PREVIEW_DIMENSION);
            // draw header, with the name of atlas and the "search by name" toolbox
            List<SVGAtlas> atlasesList = Header();

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            while (atlasIdx < atlasesList.Count)
            {
                // start a new row
                GUILayout.BeginHorizontal();
                {
                    int currentColumn = 0;
                    rect.x = ATLAS_PREVIEW_BORDER;
                
                    while (atlasIdx < atlasesList.Count)
                    {
                        SVGAtlas atlas = atlasesList[atlasIdx];

                        // buttons are used to implement atlas selection (we use the atlas name as tooltip)
                        if (GUI.Button(rect, new GUIContent("", atlas.name)))
                        {
                            // mouse left button click
                            if (Event.current.button == 0)
                            {
                                _callback?.Invoke(atlas);
                                close = true;
                            }
                        }
                        // draw atlas preview
                        if (Event.current.type == EventType.Repaint)
                        {
                            AtlasPreview(atlas, rect, _textureIndex);
                        }
                        // draw atlas name
                        AtlasLabel(atlas.name, rect);
                    
                        // next atlas
                        atlasIdx++;
                        // next column
                        rect.x += ATLAS_PREVIEW_DIMENSION_PADDED;
                        if (++currentColumn >= columnsPerRow)
                        {
                            break;
                        }
                    }
                }
            
                GUILayout.EndHorizontal();
                GUILayout.Space(ATLAS_PREVIEW_DIMENSION_PADDED);
                rect.y += ATLAS_PREVIEW_DIMENSION_PADDED + ATLAS_NAME_HEIGHT;
                rowsCount++;
            }
        
            GUILayout.Space((rowsCount - 1) * ATLAS_NAME_HEIGHT + ATLAS_PREVIEW_BORDER);
            GUILayout.EndScrollView();

            return close;
        }

        void OnGUI()
        {
            if (_atlasesList != null)
            {
                // draw the actual wizard content
                if (DrawGUI())
                {
                    Close();
                }
            }
        }

        void OnEnable()
        {
            s_instance = this;
        }

        void OnDisable()
        {
            s_instance = null;
        }

        void Update()
        {
            // check time
            float currentTime = Time.realtimeSinceStartup;
            // advance to the next texture every 3 seconds
            if (currentTime > (_time + 3))
            {
                _time = currentTime;
                _textureIndex++;
                Repaint();
            }
        }

        // show the atlas selector
        static public void Show(string atlasName, OnAtlasSelectionCallback callback)
        {
            // close the current selector instance, if any
            CloseAll();

            SVGAtlasSelector selector = DisplayWizard<SVGAtlasSelector>("Select an atlas");
            selector._searchString = atlasName;
            selector._callback = callback;
            selector._scrollPos = Vector2.zero;
            selector._atlasesList = GetAtlasesList();
            selector._time = Time.realtimeSinceStartup;
            selector._textureIndex = 0;
        }

        static public void CloseAll()
        {
            // close the current selector instance, if any
            if (s_instance != null)
            {
                s_instance.Close();
                s_instance = null;
            }
        }

        // Selection callback
        public delegate void OnAtlasSelectionCallback(SVGAtlas atlas);
        // The current selector instance
        [NonSerialized]
        static private SVGAtlasSelector s_instance;
        // The string we are using for filtering names
        [NonSerialized]
        private string _searchString;
        // the whole atlases list
        [NonSerialized]
        List<SVGAtlas> _atlasesList;
        // The current scroll position
        [NonSerialized]
        private Vector2 _scrollPos;
        // The callback to be invoked when an atlas selection occurs
        [NonSerialized]
        private OnAtlasSelectionCallback _callback;
        // Keep track of time, used to switch between atlas texures
        [NonSerialized]
        private float _time;
        // Fo each atlas, the current texture index (used to switch between atlas texures)
        [NonSerialized]
        private int _textureIndex;

        // Top/left border of the first atlas preview; such border will be maintained even between atlas previews
        readonly private static float ATLAS_PREVIEW_BORDER = 10;
        // Dimension of each atlas preview
        readonly private static float ATLAS_PREVIEW_DIMENSION = 120;
        // Dimension of each atlas preview plus a border
        readonly private static float ATLAS_PREVIEW_DIMENSION_PADDED = ATLAS_PREVIEW_DIMENSION + ATLAS_PREVIEW_BORDER;
        // Height of atlas labels/names
        readonly private static float ATLAS_NAME_HEIGHT = 32;
        // colors used by atlases names
        readonly private static Color ATLAS_NAME_BACKGROUND_COLOR = new Color(1, 1, 1, 0.5f);
        readonly private static Color ATLAS_NAME_COLOR = new Color(1, 1, 1, 0.75f);
    }
}
