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
using UnityEditor;

namespace SVGAssets
{
    [ CustomEditor(typeof(SVGSpriteLoaderBehaviour)) ]
    public class SVGSpriteLoaderEditor : Editor {

        private void OnSpriteSelect(SVGSpriteAssetFile spriteAsset)
        {
            if (_editedLoader.SpriteReference.TxtAsset != spriteAsset.SpriteRef.TxtAsset ||
                _editedLoader.SpriteReference.ElemIdx != spriteAsset.SpriteRef.ElemIdx)
            {
                // set the selected sprite (reference)
                _editedLoader.SpriteReference.TxtAsset = spriteAsset.SpriteRef.TxtAsset;
                _editedLoader.SpriteReference.ElemIdx = spriteAsset.SpriteRef.ElemIdx;
                // set the selected sprite into the renderer
                SpriteRenderer renderer = _editedLoader.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.sprite = spriteAsset.SpriteData.Sprite;
                }
            }
        }

        private void OnAtlasSelect(SVGAtlas atlas)
        {
            if (_editedLoader.Atlas != atlas)
            {
                _editedLoader.Atlas = atlas;
                _editedLoader.SpriteReference = null;

                SpriteRenderer renderer = _editedLoader.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.sprite = null;
                }
            }
        }

        private void DrawInspector()
        {
            bool resizeOnStart = EditorGUILayout.Toggle("Resize on Start()", _editedLoader.ResizeOnStart);
            bool updateTransform = EditorGUILayout.Toggle("Update transform", _editedLoader.UpdateTransform);

            string atlasName = (_editedLoader.Atlas != null) ? _editedLoader.Atlas.name : "<select>";
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel("Atlas");
                if (GUILayout.Button(atlasName, "DropDown"))
                {
                    SVGAtlasSelector.Show("", OnAtlasSelect);
                }
            }
            EditorGUILayout.EndHorizontal();

            if ((_editedLoader.Atlas != null) && (_editedLoader.SpriteReference != null))
            {
                SVGSpriteAssetFile spriteAsset = _editedLoader.Atlas.GetGeneratedSprite(_editedLoader.SpriteReference);
                string buttonText = (spriteAsset != null) ? spriteAsset.SpriteData.Sprite.name : "<select>";

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PrefixLabel("Sprite");
                    if (GUILayout.Button(buttonText, "DropDown"))
                    {
                        SVGSpriteSelector.Show(_editedLoader.Atlas, "", OnSpriteSelect);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            if (_editedLoader.ResizeOnStart != resizeOnStart)
            {
                _editedLoader.ResizeOnStart = resizeOnStart;
                SVGUtils.MarkObjectDirty(_editedLoader);
            }

            if (_editedLoader.UpdateTransform != updateTransform)
            {
                _editedLoader.UpdateTransform = updateTransform;
                SVGUtils.MarkObjectDirty(_editedLoader);
            }
        }

        public override void OnInspectorGUI()
        {
            // get the target object
            _editedLoader = target as SVGSpriteLoaderBehaviour;

            if (_editedLoader != null)
            {
                GUI.enabled = (Application.isPlaying) ? false : true;
                DrawInspector();
            }
        }

        void OnDestroy()
        {
        }

        [NonSerialized]
        private SVGSpriteLoaderBehaviour _editedLoader = null;
    }
}
