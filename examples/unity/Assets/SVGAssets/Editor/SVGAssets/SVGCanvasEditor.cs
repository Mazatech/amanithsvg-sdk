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
#if UNITY_2_6
    #define UNITY_2_X
    #define UNITY_2_PLUS
#elif UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5
    #define UNITY_3_X
    #define UNITY_2_PLUS
    #define UNITY_3_PLUS
#elif UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
    #define UNITY_4_X
    #define UNITY_2_PLUS
    #define UNITY_3_PLUS
    #define UNITY_4_PLUS
#elif UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_4_OR_NEWER
    #define UNITY_5_X
    #define UNITY_2_PLUS
    #define UNITY_3_PLUS
    #define UNITY_4_PLUS
    #define UNITY_5_PLUS
#endif

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace SVGAssets
{
    [CustomEditor(typeof(SVGCanvasBehaviour))]
    public class SVGCanvasEditor : SVGBasicAtlasEditor
    {
        // border editing callback
        private void OnSpriteEdited(PivotEditingResult result, SVGSpriteAssetFile spriteAsset, Vector2 editedPivot, Vector4 editedBorder)
        {
            SVGCanvasBehaviour svgCanvas = target as SVGCanvasBehaviour;
            if ((svgCanvas != null) && (result == PivotEditingResult.Ok))
            {
                SVGUIAtlas uiAtlas = svgCanvas.UIAtlas;
                if (uiAtlas != null)
                {
                    // assign the new border
                    uiAtlas.UpdateBorder(spriteAsset, editedBorder);
                    SVGUtils.MarkObjectDirty(uiAtlas);
                    SVGUtils.MarkSceneDirty();

                    // get the list of instantiated SVG sprites that reference the edited one
                    List<GameObject> spritesInstances = new List<GameObject>();
                    uiAtlas.GetSpritesInstances(spritesInstances, spriteAsset.SpriteRef);
                    foreach (GameObject gameObj in spritesInstances)
                    {
                        Image uiImage = gameObj.GetComponent<Image>();
                        if (uiImage != null)
                        {
                            // the Image component won't recognize the change of sprite border, so in order to refresh
                            // instantiated objects we have to unset-set the sprite property
                            uiImage.sprite = null;
                            uiImage.sprite = spriteAsset.SpriteData.Sprite;
                        }
                    }
                }
            }
        }

        private void SpritePreview(SVGUIAtlas uiAtlas, Canvas canvas, SVGSpriteAssetFile spriteAsset)
        {
            SVGUIWidgetType widgetType;
            Sprite sprite = spriteAsset.SpriteData.Sprite;
            Texture2D texture = sprite.texture;
            Rect spriteRect = sprite.textureRect;
            Rect uv = new Rect(spriteRect.x / texture.width, spriteRect.y / texture.height, spriteRect.width / texture.width, spriteRect.height / texture.height);
            GUILayoutOption[] spriteTextureOptions = new GUILayoutOption[2] { GUILayout.Width(uiAtlas.SpritesPreviewSize), GUILayout.Height(uiAtlas.SpritesPreviewSize) };

            EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(uiAtlas.SpritesPreviewSize + 5));
            {
                EditorGUILayout.LabelField(sprite.name, GUILayout.MinWidth(10));
                // reserve space for drawing sprite
                EditorGUILayout.LabelField("", spriteTextureOptions);
                Rect guiRect = GUILayoutUtility.GetLastRect();
                float maxSpriteDim = Math.Max(spriteRect.width, spriteRect.height);
                float previewWidth = (spriteRect.width / maxSpriteDim) * uiAtlas.SpritesPreviewSize;
                float previewHeight = (spriteRect.height / maxSpriteDim) * uiAtlas.SpritesPreviewSize;
                float previewX = (uiAtlas.SpritesPreviewSize - previewWidth) / 2;
                float previewY = 0;
                Rect previewRect = new Rect(guiRect.xMin + previewX, guiRect.yMin + previewY, previewWidth, previewHeight);
                GUI.DrawTextureWithTexCoords(previewRect, texture, uv, true);
                EditorGUILayout.Space();
                // sprite dimensions
                EditorGUILayout.LabelField("[" + spriteRect.width + " x " + spriteRect.height + "]", GUILayout.MaxWidth(120));

                if (GUILayout.Button("Edit", EditorStyles.miniButton))
                {
                    // show pivot editor
                    SVGPivotEditor.Show(spriteAsset, OnSpriteEdited);
                }

                widgetType = (SVGUIWidgetType)EditorGUILayout.EnumPopup("", spriteAsset.InstantiatedWidgetType, new GUILayoutOption[] { GUILayout.Width(80) });

                // instantiate
                if (GUILayout.Button("Instantiate", EditorStyles.miniButton))
                {
                    GameObject gameObj = uiAtlas.InstantiateWidget(canvas, spriteAsset.SpriteRef, widgetType);
                    // set the created instance as selected
                    if (gameObj != null)
                    {
                        Selection.objects = new UnityEngine.Object[1] { gameObj as UnityEngine.Object };
                    }
                }

                EditorGUILayout.Space();
            }
            EditorGUILayout.EndHorizontal();

            if (widgetType != spriteAsset.InstantiatedWidgetType)
            {
                spriteAsset.InstantiatedWidgetType = widgetType;
            }
        }

        protected override bool SvgInputAssetDrawImplementation(SVGBasicAtlas basicAtlas, SVGAssetInput svgAsset, int svgAssetIndex)
        {
            SVGUIAtlas uiAtlas = basicAtlas as SVGUIAtlas;
            bool isDirty = false;

            // scale adjustment for this SVG
            EditorGUILayout.LabelField(new GUIContent("Scale adjustment", "An additional scale factor used to adjust this SVG content only"), GUILayout.Width(105));
            float offsetScale = EditorGUILayout.FloatField(svgAsset.Scale, GUILayout.Width(45));
            EditorGUILayout.LabelField("", GUILayout.Width(5));
            if (offsetScale != svgAsset.Scale) {
                uiAtlas.SvgAssetScaleAdjustmentSet(svgAsset, Math.Abs(offsetScale));
                isDirty = true;
            }

            // 'explode groups' flag
            bool separateGroups = EditorGUILayout.Toggle("", svgAsset.SeparateGroups, GUILayout.Width(14));
            EditorGUILayout.LabelField("Separate groups", GUILayout.Width(105));
            // if group explosion flag has been changed, update it
            if (separateGroups != svgAsset.SeparateGroups) {
                uiAtlas.SvgAssetSeparateGroupsSet(svgAsset, separateGroups);
                isDirty = true;
            }

            // if 'Remove' button is clicked, remove the SVG entry
            if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(70)))
            {
                uiAtlas.SvgAssetRemove(svgAssetIndex);
                isDirty = true;
            }
            return isDirty;
        }

        private bool DrawInspector(SVGUIAtlas uiAtlas, Canvas canvas)
        {
            bool isDirty = false;
            // get current event
            Event currentEvent = Event.current;
            // show current options
            EditorGUILayout.LabelField("Canvas scale factor", uiAtlas.CanvasScaleFactor.ToString());
            float offsetScale = EditorGUILayout.FloatField(BasicStyles.offsetScale, uiAtlas.OffsetScale);
            bool pow2Textures = EditorGUILayout.Toggle(BasicStyles.Pow2Textures, uiAtlas.Pow2Textures);
            int maxTexturesDimension = EditorGUILayout.IntField(BasicStyles.MaxTexturesDimension, uiAtlas.MaxTexturesDimension);
            int border = EditorGUILayout.IntField(BasicStyles.SpritesPadding, uiAtlas.SpritesBorder);
            Color clearColor = EditorGUILayout.ColorField(BasicStyles.ClearColor, uiAtlas.ClearColor);
            bool fastUpload = EditorGUILayout.Toggle(BasicStyles.FastUpload, uiAtlas.FastUpload);
            float spritesPreviewSize = EditorGUILayout.IntField(BasicStyles.SpritesPreviewSize, (int)uiAtlas.SpritesPreviewSize);

            // output folder
            OutputFolderDraw(uiAtlas);

            // draw the list of input SVG files / assets
            isDirty |= SvgInputAssetsDraw(uiAtlas, currentEvent, out Rect scollRect);

            // update button
            if (UpdateButtonDraw(uiAtlas))
            {
                // regenerate/update sprites
                uiAtlas.UpdateEditorSprites();
                isDirty = true;
            }
            
            GUILayout.Space(10);

            if (uiAtlas.SvgAssetsCount() > 0)
            {
                // list of sprites, grouped by SVG document
                //Vector2 spritesScrollPos = EditorGUILayout.BeginScrollView(m_SvgSpritesScrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                Vector2 spritesScrollPos = EditorGUILayout.BeginScrollView(_svgSpritesScrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinHeight(uiAtlas.SpritesPreviewSize * 5));
                bool separatorNeeded = false;

                for (int i = 0; i < uiAtlas.SvgAssetsCount(); ++i)
                {
                    SVGAssetInput svgAsset = uiAtlas.SvgAsset(i);
                    List<SVGSpriteAssetFile> spritesAssets = uiAtlas.GetGeneratedSpritesByDocument(svgAsset.TxtAsset);
                    if ((spritesAssets != null) && (spritesAssets.Count > 0))
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
                            SpritePreview(uiAtlas, canvas, spriteAsset);
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
            isDirty |= HandleDragEvents(uiAtlas, currentEvent, scollRect);

            // a negative value is not allowed for texture max dimension
            maxTexturesDimension = (maxTexturesDimension < 0) ? 1024 : maxTexturesDimension;
            // a negative value is not allowed for border
            border = (border < 0) ? 0 : border;
            // if offset additional scale has been changed, update it
            if (uiAtlas.OffsetScale != offsetScale)
            {
                uiAtlas.OffsetScale = Math.Abs(offsetScale);
                isDirty = true;
            }
            // if power-of-two forcing flag has been changed, update it
            if (uiAtlas.Pow2Textures != pow2Textures)
            {
                uiAtlas.Pow2Textures = pow2Textures;
                isDirty = true;
            }
            // if desired maximum texture dimension has been changed, update it
            if (uiAtlas.MaxTexturesDimension != maxTexturesDimension)
            {
                uiAtlas.MaxTexturesDimension = maxTexturesDimension;
                isDirty = true;
            }
            // if border between each packed SVG has been changed, update it
            if (uiAtlas.SpritesBorder != border)
            {
                uiAtlas.SpritesBorder = border;
                isDirty = true;
            }
            // if surface clear color has been changed, update it
            if (uiAtlas.ClearColor != clearColor)
            {
                uiAtlas.ClearColor = clearColor;
                isDirty = true;
            }
            // if "fast upload" flag has been changed, update it
            if (uiAtlas.FastUpload != fastUpload)
            {
                uiAtlas.FastUpload = fastUpload;
                isDirty = true;
            }
            // if sprites preview size has been changed, update it
            if (uiAtlas.SpritesPreviewSize != spritesPreviewSize)
            {
                uiAtlas.SpritesPreviewSize = spritesPreviewSize;
                isDirty = true;
            }
        
            return isDirty;
        }

        public override void OnInspectorGUI()
        {
            // get the target object
            SVGCanvasBehaviour canvasBehaviour = target as SVGCanvasBehaviour;

            if ((canvasBehaviour != null) && (canvasBehaviour.UIAtlas != null))
            {
                Canvas canvas = canvasBehaviour.GetComponent<Canvas>();
                if (canvas != null)
                {
                    base.OnInspectorGUI();
                    // maintain the canvas scale factor synched between Canvas an SVGUIAtlas!
                    canvasBehaviour.EnsureCanvasAssigned();
                    bool isDirty = DrawInspector(canvasBehaviour.UIAtlas, canvas);
                    if (isDirty)
                    {
                        SVGUtils.MarkObjectDirty(canvasBehaviour.UIAtlas);
                        SVGUtils.MarkSceneDirty();
                    }
                }
            }
        }
    }
}
