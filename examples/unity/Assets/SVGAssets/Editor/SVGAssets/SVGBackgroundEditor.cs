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
    [ CustomEditor(typeof(SVGBackgroundBehaviour)) ]
    public class SVGBackgroundEditor : Editor
    {
        private void DrawInspector(SVGBackgroundBehaviour svgBackground)
        {
            SVGBackgroundScaleType scaleAdaption = SVGBackgroundScaleType.Vertical;
            int size = 256;
            int slicedWidth = 256;
            int slicedHeight = 256;
            bool needUpdate = false;
            bool fullUpdate = false;
            TextAsset svgFile = EditorGUILayout.ObjectField("SVG file", svgBackground.SVGFile, typeof(TextAsset), true) as TextAsset;
            bool sliced = EditorGUILayout.Toggle(new GUIContent("Sliced", "Check if you want to slice the background in order to fit specified width/height"), svgBackground.Sliced);
            if (sliced)
            {
                slicedWidth = EditorGUILayout.IntField(new GUIContent("Width", "Sliced width, in pixels"), svgBackground.SlicedWidth);
                slicedHeight = EditorGUILayout.IntField(new GUIContent("Height", "Sliced height, in pixels"), svgBackground.SlicedHeight);
            }
            else
            {
                scaleAdaption = (SVGBackgroundScaleType)EditorGUILayout.EnumPopup("Scale adaption", svgBackground.ScaleAdaption);
                size = EditorGUILayout.IntField(new GUIContent("Size", "Size in pixels"), svgBackground.Size);
            }
            Color clearColor = EditorGUILayout.ColorField("Clear color", svgBackground.ClearColor);
            bool generateOnStart = EditorGUILayout.Toggle(new GUIContent("Generate on Start()", "Generate the background texture/sprite on Start()"), svgBackground.GenerateOnStart);
            bool fastUpload = EditorGUILayout.Toggle(new GUIContent("Fast upload", "Use the fast native method (OpenGL/DirectX) to upload the texture to the GPU"), svgBackground.FastUpload);
            // show dimensions in pixel and world units
            EditorGUILayout.LabelField("Width (pixel unit)", svgBackground.PixelWidth.ToString());
            EditorGUILayout.LabelField("Height (pixel units)", svgBackground.PixelHeight.ToString());
            EditorGUILayout.LabelField("Width (world units)", svgBackground.WorldWidth.ToString());
            EditorGUILayout.LabelField("Height (world units)", svgBackground.WorldHeight.ToString());

            // update svg file, if needed
            if (svgFile != svgBackground.SVGFile)
            {
                svgBackground.SVGFile = svgFile;
                SVGUtils.MarkSceneDirty();
                needUpdate = true;
                // in this case we must destroy the current document and load the new one
                fullUpdate = true;
            }

            if (sliced != svgBackground.Sliced)
            {
                svgBackground.Sliced = sliced;
                SVGUtils.MarkSceneDirty();
                needUpdate = true;
            }

            if (sliced)
            {
                // update sliced width (in pixels), if needed
                if (slicedWidth != svgBackground.SlicedWidth)
                {
                    svgBackground.SlicedWidth = slicedWidth;
                    SVGUtils.MarkSceneDirty();
                    needUpdate = true;
                }
                // update sliced height (in pixels), if needed
                if (slicedHeight != svgBackground.SlicedHeight)
                {
                    svgBackground.SlicedHeight = slicedHeight;
                    SVGUtils.MarkSceneDirty();
                    needUpdate = true;
                }
            }
            else
            {
                // update scale adaption, if needed
                if (scaleAdaption != svgBackground.ScaleAdaption)
                {
                    svgBackground.ScaleAdaption = scaleAdaption;
                    SVGUtils.MarkSceneDirty();
                    needUpdate = true;
                }
                // update size (in pixels), if needed
                if (size != svgBackground.Size)
                {
                    svgBackground.Size = size;
                    SVGUtils.MarkSceneDirty();
                    needUpdate = true;
                }
            }
            // update clear color, if needed
            if (clearColor != svgBackground.ClearColor)
            {
                svgBackground.ClearColor = clearColor;
                SVGUtils.MarkSceneDirty();
                needUpdate = true;
            }
            // update "update on start" flag, if needed
            if (generateOnStart != svgBackground.GenerateOnStart)
            {
                svgBackground.GenerateOnStart = generateOnStart;
                SVGUtils.MarkSceneDirty();
            }
            // update "fast upload" flag, if needed
            if (fastUpload != svgBackground.FastUpload)
            {
                svgBackground.FastUpload = fastUpload;
                SVGUtils.MarkSceneDirty();
            }
            // update the background, if needed
            if (needUpdate)
            {
                svgBackground.UpdateBackground(fullUpdate);
            }
        }

        public override void OnInspectorGUI()
        {
            // get the target object
            SVGBackgroundBehaviour svgBackground = target as SVGBackgroundBehaviour;
        
            if (svgBackground != null)
            {
                DrawInspector(svgBackground);
            }
        }
    }
}
