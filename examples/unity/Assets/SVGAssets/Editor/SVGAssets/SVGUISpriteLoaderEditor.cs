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
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace SVGAssets
{
    [ CustomEditor(typeof(SVGUISpriteLoaderBehaviour)) ]
    public class SVGUISpriteLoaderEditor : Editor {

        private void OnSpriteSelect(SVGSpriteAssetFile spriteAsset)
        {
            if (!_editedLoader.SpriteReference.Equals(spriteAsset.SpriteRef))
            {
                // set the selected sprite (reference)
                _editedLoader.SpriteReference.TxtAsset = spriteAsset.SpriteRef.TxtAsset;
                _editedLoader.SpriteReference.ElemIdx = spriteAsset.SpriteRef.ElemIdx;
                // set the selected sprite into the Image component
                Image uiImage = _editedLoader.GetComponent<Image>();
                if (uiImage != null)
                {
                    RectTransform rectTransform = uiImage.gameObject.GetComponent<RectTransform>();
                    Sprite sprite = spriteAsset.SpriteData.Sprite;
                    uiImage.sprite = sprite;
                    // Unity editor won't display immediately the new sprite if it has the same dimensions of the previous one
                    // so in order to refresh the game view, we set a temporary value and restore the previous one (or a new one)
                    if ((rectTransform.anchorMin == Vector2.zero) && (rectTransform.anchorMax == Vector2.one))
                    {
                        Vector2 tmp = rectTransform.sizeDelta;
                        rectTransform.sizeDelta = tmp + Vector2.one;
                        rectTransform.sizeDelta = tmp;
                    }
                    else
                    {
                        rectTransform.sizeDelta = Vector2.zero;
                        rectTransform.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height);
                    }
                }
            }
        }

        // border editing callback
        private void OnSpriteEdited(PivotEditingResult result, SVGSpriteAssetFile spriteAsset, Vector2 editedPivot, Vector4 editedBorder)
        {
            SVGUISpriteLoaderBehaviour spriteLoader = target as SVGUISpriteLoaderBehaviour;
            if ((spriteLoader != null) && (result == PivotEditingResult.Ok)) {
                SVGUIAtlas uiAtlas = spriteLoader.UIAtlas;
                if (uiAtlas != null)
                {
                    // assign the new border
                    uiAtlas.UpdateBorder(spriteAsset, editedPivot, editedBorder);
                    SVGUtils.MarkObjectDirty(uiAtlas);
                    Image uiImage = spriteLoader.GetComponent<Image>();
                    if (uiImage != null)
                    {
                        // the Image component does not recognize the change of sprite border, so in order to refresh
                        // instantiated objects we have to unset-set the sprite property
                        uiImage.sprite = null;
                        uiImage.sprite = spriteAsset.SpriteData.Sprite;
                    }
                }
            }
        }

        private void DrawInspector()
        {
            bool resizeOnStart;
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel("Resize on Start()");
                resizeOnStart = EditorGUILayout.Toggle(_editedLoader.ResizeOnStart);
            }
            EditorGUILayout.EndHorizontal();

            SVGUIAtlas uiAtlas = _editedLoader.UIAtlas;

            if ((uiAtlas != null) && (_editedLoader.SpriteReference != null))
            {
                SVGSpriteAssetFile spriteAsset = uiAtlas.GetGeneratedSprite(_editedLoader.SpriteReference);
                string buttonText = (spriteAsset != null) ? spriteAsset.SpriteData.Sprite.name : "<select>";

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PrefixLabel("Sprite");
                    if (GUILayout.Button(buttonText, "DropDown"))
                    {
                        SVGSpriteSelector.Show(uiAtlas, "", OnSpriteSelect);
                    }
                    if (GUILayout.Button("Edit", GUILayout.Width(80)))
                    {
                        // show pivot editor
                        SVGPivotEditor.Show(spriteAsset, OnSpriteEdited);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            if (_editedLoader.ResizeOnStart != resizeOnStart)
            {
                _editedLoader.ResizeOnStart = resizeOnStart;
                SVGUtils.MarkObjectDirty(_editedLoader);
            }
        }

        public override void OnInspectorGUI()
        {
            // get the target object
            _editedLoader = target as SVGUISpriteLoaderBehaviour;

            if (_editedLoader != null)
            {
                GUI.enabled = !Application.isPlaying;
                DrawInspector();
            }
        }

        void OnDestroy()
        {
        }

        [NonSerialized]
        private SVGUISpriteLoaderBehaviour _editedLoader = null;
    }
}
