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
    public class SVGSpriteSelector : ScriptableWizard
    {
        static private List<SVGSpriteAssetFile> GetSpritesList(SVGBasicAtlas atlas)
        {
            List<SVGSpriteAssetFile> spritesAssets = atlas.SpriteAssets();

            spritesAssets.Sort(delegate(SVGSpriteAssetFile spriteAsset1, SVGSpriteAssetFile spriteAsset2)
            {
                Sprite sprite1 = spriteAsset1.SpriteData.Sprite;
                Sprite sprite2 = spriteAsset2.SpriteData.Sprite;

                return string.Compare(sprite1.name, sprite2.name, StringComparison.OrdinalIgnoreCase);
            });

            return spritesAssets;
        }

        // retrieves a list of all sprite whose names contain the specified match string
        static private List<SVGSpriteAssetFile> FilterSpritesList(List<SVGSpriteAssetFile> wholeList, string match)
        {
            List<SVGSpriteAssetFile> result;

            if (wholeList == null)
            {
                return new List<SVGSpriteAssetFile>();
            }

            if (string.IsNullOrEmpty(match))
            {
                return wholeList;
            }

            // create the output list
            result = new List<SVGSpriteAssetFile>();

            // find an exact match
            foreach (SVGSpriteAssetFile spriteAsset in wholeList)
            {
                Sprite sprite = spriteAsset.SpriteData.Sprite;
                if (!string.IsNullOrEmpty(sprite.name) && string.Equals(match, sprite.name, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(spriteAsset);
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

            // find all sprites whose names contain one ore more keyword
            foreach (SVGSpriteAssetFile spriteAsset in wholeList)
            {
                Sprite sprite = spriteAsset.SpriteData.Sprite;
                if (!string.IsNullOrEmpty(sprite.name))
                {
                    string lowerName = sprite.name.ToLower();
                    int matchesCount = 0;

                    foreach (string key in searchKeys)
                    {
                        if (lowerName.Contains(key))
                        {
                            matchesCount++;
                        }
                    }

                    // (matchesCount == searchKeys.Length) if we were interested in finding all sprites whose names contain ALL the keywords
                    if (matchesCount > 0)
                    {
                        result.Add(spriteAsset);
                    }
                }
            }

            return result;
        }

        private static void SpriteLabel(string spriteName, Rect rect)
        {
            GUI.backgroundColor = SPRITE_NAME_BACKGROUND_COLOR;
            GUI.contentColor = SPRITE_NAME_COLOR;
            // we use the sprite name as tooltip
            GUI.Label(new Rect(rect.x, rect.y + rect.height, rect.width, SPRITE_NAME_HEIGHT), new GUIContent(spriteName, spriteName), "ProgressBarBack");
            GUI.contentColor = Color.white;
            GUI.backgroundColor = Color.white;
        }

        // Draw a sprite preview
        private static void SpritePreview(Sprite sprite, Rect clipRect)
        {
            Texture2D spriteTexture = sprite.texture;
            Rect spriteRect = sprite.rect;
            Rect uv = new Rect(spriteRect.x / spriteTexture.width, spriteRect.y / spriteTexture.height, spriteRect.width / spriteTexture.width, spriteRect.height / spriteTexture.height);
            float maxSpriteDim = Math.Max(spriteRect.width, spriteRect.height);
            float previewWidth = (spriteRect.width / maxSpriteDim) * SPRITE_PREVIEW_DIMENSION;
            float previewHeight = (spriteRect.height / maxSpriteDim) * SPRITE_PREVIEW_DIMENSION;
            float previewX = (SPRITE_PREVIEW_DIMENSION - previewWidth) / 2;
            float previewY = (SPRITE_PREVIEW_DIMENSION - previewHeight) / 2;
            Rect previewRect = new Rect(clipRect.xMin + previewX, clipRect.yMin + previewY, previewWidth, previewHeight);
            GUI.DrawTextureWithTexCoords(previewRect, spriteTexture, uv, true);
            GUI.Label(clipRect, spriteRect.width + " x " + spriteRect.height);
        }

        private List<SVGSpriteAssetFile> Header()
        {
            EditorGUIUtility.labelWidth = SPRITE_PREVIEW_DIMENSION;
            // show atlas name
            GUILayout.Label(_atlas.name + " sprites", "LODLevelNotifyText");
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
            // return the filtered sprites list
            return FilterSpritesList(_spritesList, _searchString);
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
            int columnsPerRow = Math.Max(Mathf.FloorToInt((Screen.width / dpiAdjust) / SPRITE_PREVIEW_DIMENSION_PADDED), 1);
            int rowsCount = 1;
            int spriteIdx = 0;
            Rect rect = new Rect(SPRITE_PREVIEW_BORDER, SPRITE_PREVIEW_BORDER, SPRITE_PREVIEW_DIMENSION, SPRITE_PREVIEW_DIMENSION);
            // draw header, with the name of atlas and the "search by name" toolbox
            List<SVGSpriteAssetFile> spritesList = Header();
        
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            while (spriteIdx < spritesList.Count)
            {
                // start a new row
                GUILayout.BeginHorizontal();
                {
                    int currentColumn = 0;
                    rect.x = SPRITE_PREVIEW_BORDER;
                
                    while (spriteIdx < spritesList.Count)
                    {
                        SVGSpriteAssetFile spriteAsset = spritesList[spriteIdx];
                        Sprite sprite = spriteAsset.SpriteData.Sprite;
                    
                        // buttons are used to implement sprite selection (we use the sprite name as tooltip)
                        if (GUI.Button(rect, new GUIContent("", sprite.name)))
                        {
                            // mouse left button click
                            if (Event.current.button == 0)
                            {
                                _callback?.Invoke(spriteAsset);
                                close = true;
                            }
                        }

                        // show sprite preview, taking care to highlight the currently selected one
                        if (Event.current.type == EventType.Repaint)
                        {
                            SpritePreview(sprite, rect);
                        }
                        // draw sprite name
                        SpriteLabel(sprite.name, rect);
                    
                        // next sprite
                        spriteIdx++;
                        // next column
                        rect.x += SPRITE_PREVIEW_DIMENSION_PADDED;
                        if (++currentColumn >= columnsPerRow)
                        {
                            break;
                        }
                    }
                }
            
                GUILayout.EndHorizontal();
                GUILayout.Space(SPRITE_PREVIEW_DIMENSION_PADDED);
                rect.y += SPRITE_PREVIEW_DIMENSION_PADDED + SPRITE_NAME_HEIGHT;
                rowsCount++;
            }
        
            GUILayout.Space((rowsCount - 1) * SPRITE_NAME_HEIGHT + SPRITE_PREVIEW_BORDER);
            GUILayout.EndScrollView();
        
            return close;
        }

        void OnGUI()
        {
            if ((_atlas != null) && (_spritesList != null))
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

        // show the sprite selector
        static public void Show(SVGBasicAtlas atlas, string spriteName, OnSpriteSelectionCallback callback)
        {
            // close the current selector instance, if any
            CloseAll();

            if (atlas != null)
            {
                SVGSpriteSelector selector = DisplayWizard<SVGSpriteSelector>("Select a sprite");
                selector._atlas = atlas;
                selector._searchString = spriteName;
                selector._callback = callback;
                selector._scrollPos = Vector2.zero;
                selector._spritesList = GetSpritesList(atlas);
            }
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
        public delegate void OnSpriteSelectionCallback(SVGSpriteAssetFile spriteAsset);
        // The current selector instance
        [NonSerialized]
        static private SVGSpriteSelector s_instance;
        // Displayed atlas
        [NonSerialized]
        private SVGBasicAtlas _atlas;
        // The string we are using for filtering names
        [NonSerialized]
        private string _searchString;
        // the whole sprites list relative to the specified atlas
        [NonSerialized]
        List<SVGSpriteAssetFile> _spritesList;
        // The current scroll position
        [NonSerialized]
        private Vector2 _scrollPos;
        // The callback to be invoked when a sprite selection occurs
        [NonSerialized]
        private OnSpriteSelectionCallback _callback;

        // Top/left border of the first sprite preview; such border will be maintained even between sprite previews
        readonly private static float SPRITE_PREVIEW_BORDER = 10;
        // Dimension of each sprite preview
        readonly private static float SPRITE_PREVIEW_DIMENSION = 80;
        // Dimension of each sprite preview plus a border
        readonly private static float SPRITE_PREVIEW_DIMENSION_PADDED = SPRITE_PREVIEW_DIMENSION + SPRITE_PREVIEW_BORDER;
        // Height of sprite labels/names
        readonly private static float SPRITE_NAME_HEIGHT = 32;
        // colors used by sprites names
        readonly private static Color SPRITE_NAME_BACKGROUND_COLOR = new Color(1, 1, 1, 0.5f);
        readonly private static Color SPRITE_NAME_COLOR = new Color(1, 1, 1, 0.75f);
    }
}
