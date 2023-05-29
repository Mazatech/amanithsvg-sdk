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
    public static class ScriptableObjectUtility
    {
        public static void CreateSpriteAtlasAsset()
        {
            // create the new atlas
            SVGAtlas atlas = ScriptableObject.CreateInstance<SVGAtlas>();

            if (atlas != null)
            {
                // start with the default, always valid, directory
                string outputPath = "Assets";

                // check the best fitting directory; NB: outputPath must be relative to the project folder
                if (AssetDatabase.IsValidFolder("Assets/SVGAssets"))
                {
                    outputPath = "Assets/SVGAssets";
                }
                if (AssetDatabase.IsValidFolder("Assets/SVGAssets/Atlases"))
                {
                    outputPath = "Assets/SVGAssets/Atlases";
                }
                // assign output directory
                atlas.OutputFolder = outputPath;

                // create the asset file containing the new atlas
                string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(outputPath + "/New " + typeof(SVGAtlas).ToString() + ".asset");
                AssetDatabase.CreateAsset(atlas, assetPathAndName);
                AssetDatabase.SaveAssets();

                // select and highlight the new asset
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = atlas;
                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(atlas);
            }
        }
    }

    // Menu item used to create a new atlas generator
    public class SVGAtlasAsset
    {
        [MenuItem("Assets/SVGAssets/Create SVG sprites atlas", false, 0)]
        public static void CreateAsset()
        {
            ScriptableObjectUtility.CreateSpriteAtlasAsset();
        }
    }

    [ CustomEditor(typeof(SVGAtlas)) ]
    public class SVGAtlasEditor : SVGBasicAtlasEditor
    {
        // pivot editing callback
        private void OnPivotEdited(PivotEditingResult result, SVGSpriteAssetFile spriteAsset, Vector2 editedPivot, Vector4 editedBorder)
        {
            SVGAtlas atlas = target as SVGAtlas;
            if ((atlas != null) && (result == PivotEditingResult.Ok))
            {
                // assign the new pivot
                atlas.UpdatePivot(spriteAsset, editedPivot);
                SVGUtils.MarkObjectDirty(atlas);
            }
        }

        private void SpritePreview(SVGAtlas atlas, SVGSpriteAssetFile spriteAsset)
        {
            Sprite sprite = spriteAsset.SpriteData.Sprite;
            Texture2D texture = sprite.texture;
            Rect spriteRect = sprite.textureRect;
            Rect uv = new Rect(spriteRect.x / texture.width, spriteRect.y / texture.height, spriteRect.width / texture.width, spriteRect.height / texture.height);
            GUILayoutOption[] spriteTextureOptions = new GUILayoutOption[2] { GUILayout.Width(atlas.SpritesPreviewSize), GUILayout.Height(atlas.SpritesPreviewSize) };

            EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(atlas.SpritesPreviewSize + 5));
            {
                EditorGUILayout.LabelField(sprite.name, GUILayout.MinWidth(10));
                // reserve space for drawing sprite
                EditorGUILayout.LabelField("", spriteTextureOptions);
                Rect guiRect = GUILayoutUtility.GetLastRect();
                float maxSpriteDim = Math.Max(spriteRect.width, spriteRect.height);
                float previewWidth = (spriteRect.width / maxSpriteDim) * atlas.SpritesPreviewSize;
                float previewHeight = (spriteRect.height / maxSpriteDim) * atlas.SpritesPreviewSize;
                float previewX = (atlas.SpritesPreviewSize - previewWidth) / 2;
                //float previewY = (SVGAtlasEditor.SPRITE_PREVIEW_DIMENSION - previewHeight) / 2;
                //float previewY = (previewWidth > previewHeight) ? 0 : ((SVGAtlasEditor.SPRITE_PREVIEW_DIMENSION - previewHeight) / 2);
                float previewY = 0;
                Rect previewRect = new Rect(guiRect.xMin + previewX, guiRect.yMin + previewY, previewWidth, previewHeight);
                GUI.DrawTextureWithTexCoords(previewRect, texture, uv, true);
                EditorGUILayout.Space();
                // sprite dimensions
                EditorGUILayout.LabelField("[" + spriteRect.width + " x " + spriteRect.height + "]", GUILayout.MaxWidth(100));
                EditorGUILayout.Space();
                // current pivot
                EditorGUILayout.LabelField("Pivot [" + string.Format("{0:0.00}", spriteAsset.SpriteData.Pivot.x) + " , " + string.Format("{0:0.00}", spriteAsset.SpriteData.Pivot.y) + "]", GUILayout.Width(120));
                EditorGUILayout.Space();
                // edit pivot
                if (GUILayout.Button("Edit", EditorStyles.miniButton))
                {
                    // show pivot editor
                    SVGPivotEditor.Show(spriteAsset, OnPivotEdited);
                }
                // instantiate
                if (GUILayout.Button("Instantiate", EditorStyles.miniButton, GUILayout.Width(80)))
                {
                    GameObject gameObj = atlas.InstantiateSprite(spriteAsset.SpriteRef);
                    // set the created instance as selected
                    if (gameObj != null)
                    {
                        Selection.objects = new UnityEngine.Object[1] { gameObj as UnityEngine.Object };
                    }
                }
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndHorizontal();
        }

        protected override bool SvgInputAssetDrawImplementation(SVGBasicAtlas basicAtlas, SVGAssetInput svgAsset, int svgAssetIndex)
        {
            SVGAtlas atlas = basicAtlas as SVGAtlas;
            bool isDirty = false;

            // scale adjustment for this SVG
            EditorGUILayout.LabelField(Styles.ScaleAdjustment, GUILayout.Width(105));
            float offsetScale = EditorGUILayout.FloatField(svgAsset.Scale, GUILayout.Width(45));
            EditorGUILayout.LabelField("", GUILayout.Width(5));
            if (offsetScale != svgAsset.Scale) {
                atlas.SvgAssetScaleAdjustmentSet(svgAsset, Math.Abs(offsetScale));
                isDirty = true;
            }

            // 'explode groups' flag
            bool separateGroups = EditorGUILayout.Toggle("", svgAsset.SeparateGroups, GUILayout.Width(14));
            EditorGUILayout.LabelField("Separate groups", GUILayout.Width(105));
            // if group explosion flag has been changed, update it
            if (separateGroups != svgAsset.SeparateGroups) {
                atlas.SvgAssetSeparateGroupsSet(svgAsset, separateGroups);
                isDirty = true;
            }
            // if 'Remove' button is clicked, remove the SVG entry
            if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(70)))
            {
                atlas.SvgAssetRemove(svgAssetIndex);
                isDirty = true;
            }
            // instantiate all groups
            GUI.enabled = ((svgAsset.Instantiable && (!Application.isPlaying)) ? true : false);
            if (GUILayout.Button("Instantiate", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                GameObject[] gameObjs = atlas.InstantiateGroups(svgAsset);
                // set the created instances as selected
                if (gameObjs != null)
                {
                    Selection.objects = gameObjs;
                }
            }
            GUI.enabled = !Application.isPlaying;
            return isDirty;
        }

        private float MatchSlider(Rect position, float value)
        {
            position.height = EditorGUIUtility.singleLineHeight;
            Rect pos = position;
            position.y += 12;
            position.xMin += EditorGUIUtility.labelWidth;
            position.xMax -= EditorGUIUtility.fieldWidth;
            GUI.Label(position, "Width", Styles.LeftAlignedLabel);
            GUI.Label(position, "Height", Styles.RightAlignedLabel);
            return EditorGUI.Slider(pos, Styles.MatchSlider, value, 0.0f, 1.0f);
        }

        private bool DrawInspector(SVGAtlas atlas)
        {
            float match = atlas.Match;
            bool isDirty = false;
            // get current event
            Event currentEvent = Event.current;
            // show current options
            int refWidth = EditorGUILayout.IntField(Styles.ReferenceWidth, atlas.ReferenceWidth);
            int refHeight = EditorGUILayout.IntField(Styles.ReferenceHeight, atlas.ReferenceHeight);
            int deviceTestWidth = EditorGUILayout.IntField("Device test width", atlas.DeviceTestWidth);
            int deviceTestHeight = EditorGUILayout.IntField("Device test height", atlas.DeviceTestHeight);
            SVGScalerMatchMode scaleType = (SVGScalerMatchMode)EditorGUILayout.EnumPopup(Styles.ScalerMatchMode, atlas.ScaleType);
            if (scaleType == SVGScalerMatchMode.MatchWidthOrHeight)
            {
                Rect r = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + 12);
                match = MatchSlider(r, atlas.Match);
            }
            float offsetScale = EditorGUILayout.FloatField(BasicStyles.offsetScale, atlas.OffsetScale);
            bool pow2Textures = EditorGUILayout.Toggle(BasicStyles.Pow2Textures, atlas.Pow2Textures);
            int maxTexturesDimension = EditorGUILayout.IntField(BasicStyles.MaxTexturesDimension, atlas.MaxTexturesDimension);
            int border = EditorGUILayout.IntField(BasicStyles.SpritesPadding, atlas.SpritesBorder);
            Color clearColor = EditorGUILayout.ColorField(BasicStyles.ClearColor, atlas.ClearColor);
            bool fastUpload = EditorGUILayout.Toggle(BasicStyles.FastUpload, atlas.FastUpload);
            float spritesPreviewSize = EditorGUILayout.IntField(BasicStyles.SpritesPreviewSize, (int)atlas.SpritesPreviewSize);

            // output folder
            OutputFolderDraw(atlas);

            // draw the list of input SVG files / assets
            isDirty |= SvgInputAssetsDraw(atlas, currentEvent, out Rect scrollRect);

            // update button
            if (UpdateButtonDraw(atlas))
            {
                // regenerate/update sprites
                atlas.UpdateEditorSprites(true);
                isDirty = true;
            }
            GUILayout.Space(10);

            if (atlas.SvgAssetsCount() > 0)
            {
                // list of sprites, grouped by SVG document
                Vector2 spritesScrollPos = EditorGUILayout.BeginScrollView(_svgSpritesScrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                bool separatorNeeded = false;
                for (int i = 0; i < atlas.SvgAssetsCount(); ++i)
                {
                    SVGAssetInput svgAsset = atlas.SvgAsset(i);
                    List<SVGSpriteAssetFile> spritesAssets = atlas.GetGeneratedSpritesByDocument(svgAsset.TxtAsset);
                    if (spritesAssets != null && spritesAssets.Count > 0)
                    {
                        // line separator
                        if (separatorNeeded)
                        {
                            EditorGUILayout.Separator();
                            GUILayout.Box(GUIContent.none, BasicStyles.GreyLine, GUILayout.ExpandWidth(true), GUILayout.Height(1));
                            EditorGUILayout.Separator();
                        }
                        // display sprites list
                        foreach (SVGSpriteAssetFile spriteAsset in spritesAssets)
                        {
                            SpritePreview(atlas, spriteAsset);
                        }
                        // we have displayed some sprites, next time a line separator is needed
                        separatorNeeded = true;
                    }
                }
                EditorGUILayout.EndScrollView();

                if (_svgSpritesScrollPos != spritesScrollPos)
                {
                    _svgSpritesScrollPos = spritesScrollPos;
                }
            }
        
            // events handler
            isDirty |= HandleDragEvents(atlas, currentEvent, scrollRect);

            // negative values are not allowed for reference resolution
            refWidth = (refWidth <= 0) ? Screen.currentResolution.width : refWidth;
            refHeight = (refHeight <= 0) ? Screen.currentResolution.height : refHeight;
            deviceTestWidth = (deviceTestWidth <= 0) ? refWidth : deviceTestWidth;
            deviceTestHeight = (deviceTestHeight <= 0) ? refHeight : deviceTestHeight;
            // a negative value is not allowed for texture max dimension
            maxTexturesDimension = (maxTexturesDimension < 0) ? 1024 : maxTexturesDimension;
            // a negative value is not allowed for border
            border = (border < 0) ? 0 : border;

            // if reference resolution has been changed, update it
            if (atlas.ReferenceWidth != refWidth)
            {
                atlas.ReferenceWidth = refWidth;
                isDirty = true;
            }
            if (atlas.ReferenceHeight != refHeight)
            {
                atlas.ReferenceHeight = refHeight;
                isDirty = true;
            }
            // if device (test) resolution has been changed, update it
            if (atlas.DeviceTestWidth != deviceTestWidth)
            {
                atlas.DeviceTestWidth = deviceTestWidth;
                isDirty = true;
            }
            if (atlas.DeviceTestHeight != deviceTestHeight)
            {
                atlas.DeviceTestHeight = deviceTestHeight;
                isDirty = true;
            }
            // if scale adaption method has been changed, update it
            if (atlas.ScaleType != scaleType)
            {
                atlas.ScaleType = scaleType;
                isDirty = true;
            }
            if (atlas.Match != match)
            {
                atlas.Match = match;
                isDirty = true;
            }
            // if offset additional scale has been changed, update it
            if (atlas.OffsetScale != offsetScale)
            {
                atlas.OffsetScale = Math.Abs(offsetScale);
                isDirty = true;
            }
            // if power-of-two forcing flag has been changed, update it
            if (atlas.Pow2Textures != pow2Textures)
            {
                atlas.Pow2Textures = pow2Textures;
                isDirty = true;
            }
            // if desired maximum texture dimension has been changed, update it
            if (atlas.MaxTexturesDimension != maxTexturesDimension)
            {
                atlas.MaxTexturesDimension = maxTexturesDimension;
                isDirty = true;
            }
            // if border between each packed SVG has been changed, update it
            if (atlas.SpritesBorder != border)
            {
                atlas.SpritesBorder = border;
                isDirty = true;
            }
            // if surface clear color has been changed, update it
            if (atlas.ClearColor != clearColor)
            {
                atlas.ClearColor = clearColor;
                isDirty = true;
            }
            // if "fast upload" flag has been changed, update it
            if (atlas.FastUpload != fastUpload)
            {
                atlas.FastUpload = fastUpload;
                isDirty = true;
            }
            // if sprites preview size has been changed, update it
            if (atlas.SpritesPreviewSize != spritesPreviewSize)
            {
                atlas.SpritesPreviewSize = spritesPreviewSize;
                isDirty = true;
            }

            return isDirty;
        }

        public override void OnInspectorGUI()
        {
            // get the target object
            SVGAtlas atlas = target as SVGAtlas;

            if (atlas != null)
            {
                base.OnInspectorGUI();
                // we assign the Name property, so we can check if the user has renamed the atlas asset file
                atlas.Name = atlas.name;
                bool isDirty = DrawInspector(atlas);
                if (isDirty)
                {
                    SVGUtils.MarkObjectDirty(atlas);
                    SVGUtils.MarkSceneDirty();
                }
            }
        }

        static private class Styles
        {
            static Styles()
            {
                RightAlignedLabel = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleRight
                };
            }

            public static GUIStyle RightAlignedLabel;
            public static GUIStyle LeftAlignedLabel = new GUIStyle(EditorStyles.label);
            public static GUIContent ScaleAdjustment = new GUIContent("Scale adjustment", "An additional scale factor used to adjust this SVG content only");
            public static GUIContent MatchSlider = new GUIContent("Match", "Determines if the scaling is using the width or height as reference, or a mix in between");
            public static GUIContent ReferenceWidth = new GUIContent("Reference width", "The resolution the SVG files content is designed for. If the screen resolution is larger, SVG contents will be scaled up, and if it’s smaller, it will be scaled down.");
            public static GUIContent ReferenceHeight = new GUIContent("Reference height", "The resolution the SVG files content is designed for. If the screen resolution is larger, SVG contents will be scaled up, and if it’s smaller, it will be scaled down.");
            public static GUIContent ScalerMatchMode = new GUIContent("Screen match mode", "A mode used to scale (i.e. generate) SVG sprites if the aspect ratio of the current resolution doesn’t fit the reference resolution.");
        }
    }
}
