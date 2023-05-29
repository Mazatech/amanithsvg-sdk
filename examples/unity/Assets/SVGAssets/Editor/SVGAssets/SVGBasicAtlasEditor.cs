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
using UnityEngine; 
using UnityEditor;
using UnityEditor.Callbacks;
using System;
using System.Collections.Generic;

namespace SVGAssets
{
    // Used to keep track of dragged object inside the list of input SVG rows
    public class DragInfo
    {
        public DragInfo()
        {
            Dragging = false;
            DraggedObject = null;
            InsertIdx = -1;
            InsertBefore = false;
        }

        // Start a drag operation.
        public void StartDrag(object obj)
        {
            Dragging = true;
            DraggedObject = obj;
            InsertIdx = -1;
            InsertBefore = false;
        }

        // Stop a drag operation.
        public void StopDrag()
        {
            Dragging = false;
            DraggedObject = null;
            InsertIdx = -1;
            InsertBefore = false;
        }

        // True if the user is dragging a text asset or an already present SVG row, else false.
        public bool Dragging { get; private set; }

        // Target insertion position.
        public int InsertIdx { get; set; }

        // True/False if the dragged object must be inserted before/after the selected position.
        public bool InsertBefore { get; set; }

        // The dragged object.
        public object DraggedObject { get; private set; }
    }

    public static class SVGBuildProcessor
    {
        private static void ProcessAtlas(SVGBasicAtlas atlas)
        {
            Texture2D tmpTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            List<SVGSpriteAssetFile> spritesAssets = atlas.SpriteAssets();

            foreach (SVGSpriteAssetFile spriteAsset in spritesAssets)
            {
                SVGSpriteData spriteData = spriteAsset.SpriteData;
                Sprite original = spriteData.Sprite;
                // we must reference the original texture, because we want to keep the file reference (rd->texture.IsValid())
                Sprite tmpSprite = SVGAssetsUnity.CreateSprite(original.texture, new Rect(0, 0, 1, 1), Vector2.zero);
                // now we change the (sprite) asset content: actually we have just reduced its rectangle to a 1x1 pixel
                EditorUtility.CopySerialized(tmpSprite, original);
            }

            for (int i = 0; i < atlas.TextureAssetsCount(); i++)
            {
                AssetFile file = atlas.TextureAsset(i);
                Texture2D original = file.Object as Texture2D;

                // copy the 1x1 texture inside the original texture
                EditorUtility.CopySerialized(tmpTexture, original);
            }
        }

        private static void ProcessScene()
        {
            GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            List<SVGBasicAtlas> unexportedAtlases = new List<SVGBasicAtlas>();

            // scan all game objects in the current scene, and keep track of used atlas generators
            foreach (GameObject gameObj in allObjects)
            {
                if (gameObj.activeInHierarchy)
                {
                    SVGSpriteLoaderBehaviour loader = gameObj.GetComponent<SVGSpriteLoaderBehaviour>();
                    if (loader != null)
                    {
                        // if this atlas has not been already flagged, lets keep track of it
                        if (!loader.Atlas.Exporting)
                        {
                            unexportedAtlases.Add(loader.Atlas);
                            loader.Atlas.Exporting = true;
                        }
                    }

                    SVGUISpriteLoaderBehaviour uiLoader = gameObj.GetComponent<SVGUISpriteLoaderBehaviour>();
                    if (uiLoader != null)
                    {
                        // if this atlas has not been already flagged, lets keep track of it
                        if (!uiLoader.UIAtlas.Exporting)
                        {
                            unexportedAtlases.Add(uiLoader.UIAtlas);
                            uiLoader.UIAtlas.Exporting = true;
                        }
                    }

                    SVGCanvasBehaviour uiCanvas = gameObj.GetComponent<SVGCanvasBehaviour>();
                    if (uiCanvas != null)
                    {
                        // if this atlas has not been already flagged, lets keep track of it
                        if (!uiCanvas.UIAtlas.Exporting)
                        {
                            unexportedAtlases.Add(uiCanvas.UIAtlas);
                            uiCanvas.UIAtlas.Exporting = true;
                        }
                    }
                }
            }

            foreach (SVGBasicAtlas baseAtlas in unexportedAtlases)
            {
                ProcessAtlas(baseAtlas);
                // keep track of this atlas in the global list
                s_atlases.Add(baseAtlas);
            }
        }

        [PostProcessScene]
        public static void OnPostprocessScene()
        {
            if (!Application.isPlaying)
            {
                ProcessScene();
            }
        }

        [PostProcessBuild]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (!Application.isPlaying)
            {
                // unflag processed atlases
                foreach (SVGAtlas atlas in s_atlases)
                {
                    // update sprites using the last used scale factor
                    atlas.UpdateEditorSprites(false);
                    atlas.Exporting = false;
                }
                // clear the list
                s_atlases.Clear();
            }
        }

        [NonSerialized]
        private static List<SVGBasicAtlas> s_atlases = new List<SVGBasicAtlas>();
    }

    public abstract class SVGBasicAtlasEditor : Editor
    {
        protected void OutputFolderDraw(SVGBasicAtlas atlas)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel("Output folder");
                if (GUILayout.Button(atlas.OutputFolder))
                {
                    string absFolderPath = EditorUtility.OpenFolderPanel("Select output folder", atlas.OutputFolder, "");
                    // selected directory must be a 'Application.dataPath' sub-directory; in other words, if a directory
                    // was selected outside the 'Application.dataPath', it is discarded
                    if ((absFolderPath != "") && (absFolderPath.StartsWith(Application.dataPath)))
                    {
                        atlas.OutputFolder = "Assets" + absFolderPath.Substring(Application.dataPath.Length);
                    }
                    // "Puts the GUI in a state that will prevent all subsequent immediate mode GUI functions from evaluating
                    // for the remainder of the GUI loop by throwing an ExitGUIException"
                    // see also https://forum.unity.com/threads/endlayoutgroup-beginlayoutgroup-must-be-called-first.523209
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();
            // textures output subfolder
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel("Textures subfolder");
                EditorGUILayout.LabelField(atlas.TexturesSubFolder);
            }
            EditorGUILayout.EndHorizontal();
            // sprites output subfolder
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel("Sprites subfolder");
                EditorGUILayout.LabelField(atlas.SpritesSubFolder);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(18);
            EditorGUILayout.LabelField("Drag & drop SVG assets here", EditorStyles.boldLabel);
        }

        protected bool UpdateButtonDraw(SVGBasicAtlas atlas)
        {
            bool pushed = false;
            // update button
            string updateStr = (atlas.NeedsUpdate()) ? "Update *" : "Update";

            if (GUILayout.Button(updateStr))
            {
                // close all modal popup editors
                SVGPivotEditor.CloseAll();
                SVGSpriteSelector.CloseAll();
                SVGAtlasSelector.CloseAll();
                pushed = true;
            }

            return pushed;
        }

        protected abstract bool SvgInputAssetDrawImplementation(SVGBasicAtlas atlas, SVGAssetInput svgAsset, int svgAssetIndex);

        private bool SvgInputAssetDraw(SVGBasicAtlas atlas, int index, out Rect rowRect)
        {
            bool isDirty;
            SVGAssetInput svgAsset = atlas.SvgAsset(index);
            bool highlight = (_dragInfo.Dragging && (_dragInfo.DraggedObject == svgAsset)) ? true : false;

            if ((_dragInfo.InsertIdx == index) && _dragInfo.InsertBefore)
            {
                // draw a separator before the row
                GUILayout.Box(GUIContent.none, BasicStyles.BlueLine, GUILayout.ExpandWidth(true), GUILayout.Height(2));
            }

            // if the SVG row is the dragged one, change colors
            if (highlight)
            {
                EditorGUILayout.BeginHorizontal(BasicStyles.HighlightRow);
                // a row: asset name, separate groups checkbox, remove button, instantiate button
                EditorGUILayout.LabelField(svgAsset.TxtAsset.name, BasicStyles.HighlightRow, GUILayout.MinWidth(10));
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                // a row: asset name, separate groups checkbox, remove button, instantiate button
                EditorGUILayout.LabelField(svgAsset.TxtAsset.name, GUILayout.MinWidth(10));
            }

            isDirty = SvgInputAssetDrawImplementation(atlas, svgAsset, index);

            EditorGUILayout.EndHorizontal();
            rowRect = GUILayoutUtility.GetLastRect();

            if ((_dragInfo.InsertIdx == index) && (!_dragInfo.InsertBefore))
            {
                // draw a separator after the row
                GUILayout.Box(GUIContent.none, BasicStyles.BlueLine, GUILayout.ExpandWidth(true), GUILayout.Height(2));
            }

            return isDirty;
        }

        protected bool SvgInputAssetsDraw(SVGBasicAtlas atlas, Event currentEvent, out Rect scollRect)
        {
            bool isDirty = false;

            // keep track of drawn rows
            if (currentEvent.type != EventType.Layout)
            {
                _inputAssetsRects = new List<Rect>();
            }

            Vector2 scrollPos = EditorGUILayout.BeginScrollView(_svgListScrollPos, GUILayout.ExpandWidth(true), GUILayout.MaxHeight(102), GUILayout.Height(102));
            // perform backward loop because we could even remove entries without issues
            for (int i = 0; i < atlas.SvgAssetsCount(); ++i)
            {
                isDirty |= SvgInputAssetDraw(atlas, i, out Rect rowRect);
                // keep track of row rectangle
                if (currentEvent.type != EventType.Layout)
                {
                    _inputAssetsRects.Add(rowRect);
                }
            }
            EditorGUILayout.EndScrollView();
            // keep track of the scrollview area
            scollRect = GUILayoutUtility.GetLastRect();
            if (_svgListScrollPos != scrollPos)
            {
                _svgListScrollPos = scrollPos;
            }

            return isDirty;
        }

        protected bool HandleDragEvents(SVGBasicAtlas atlas, Event currentEvent, Rect scrollRect)
        {
            bool isDirty = false;

            // events handler
            if (currentEvent.type != EventType.Layout)
            {
                bool needRepaint = false;
                // get mouse position relative to scollRect
                Vector2 mousePos = currentEvent.mousePosition - new Vector2(scrollRect.xMin, scrollRect.yMin);

                if (scrollRect.Contains(currentEvent.mousePosition))
                {
                    bool separatorInserted = false;

                    for (int i = 0; i < atlas.SvgAssetsCount(); ++i)
                    {
                        // get the row rectangle relative to atlas.SvgList[i]
                        Rect rowRect = _inputAssetsRects[i];
                        // expand the rectangle height
                        rowRect.yMin -= 3;
                        rowRect.yMax += 3;

                        if (rowRect.Contains(mousePos))
                        {
                            // a mousedown on a row, will stop an already started drag operation
                            if (currentEvent.type == EventType.MouseDown)
                            {
                                _dragInfo.StopDrag();
                            }
                            // check if we are already dragging an object
                            if (_dragInfo.Dragging)
                            {
                                if (!separatorInserted)
                                {
                                    bool ok = true;
                                    bool dragBefore = (mousePos.y <= (rowRect.yMin + (rowRect.height / 2))) ? true : false;
                                    // if we are dragging a text (asset) file, all positions are ok
                                    // if we are dragging an already present SVG row, we must perform additional checks
                                    if (!(_dragInfo.DraggedObject is TextAsset))
                                    {
                                        if (_dragInfo.DraggedObject == atlas.SvgAsset(i))
                                        {
                                            ok = false;
                                        }
                                        else
                                        {
                                            if (dragBefore)
                                            {
                                                if ((i > 0) && (_dragInfo.DraggedObject == atlas.SvgAsset(i - 1)))
                                                {
                                                    ok = false;
                                                }
                                            }
                                            else
                                            {
                                                if ((i < (atlas.SvgAssetsCount() - 1)) && (_dragInfo.DraggedObject == atlas.SvgAsset(i + 1)))
                                                {
                                                    ok = false;
                                                }
                                            }
                                        }
                                    }

                                    if (ok)
                                    {
                                        if (dragBefore)
                                        {
                                            _dragInfo.InsertIdx = i;
                                            _dragInfo.InsertBefore = true;
                                            separatorInserted = true;
                                        }
                                        else
                                        {
                                            _dragInfo.InsertIdx = i;
                                            _dragInfo.InsertBefore = false;
                                            separatorInserted = true;
                                        }
                                        needRepaint = true;
                                    }
                                }
                            }
                            else
                            {
                                // initialize the drag of an already present SVG document
                                if (currentEvent.type == EventType.MouseDrag)
                                {
                                    DragAndDrop.PrepareStartDrag();
                                    DragAndDrop.StartDrag("Start drag");
                                    _dragInfo.StartDrag(atlas.SvgAsset(i));
                                    needRepaint = true;
                                }
                            }
                        }
                    }

                    // mouse is dragging inside the drop box, but not under an already present row; insertion point is inside the last element
                    if (_dragInfo.Dragging && (!separatorInserted) && (atlas.SvgAssetsCount() > 0) && (mousePos.y > _inputAssetsRects[atlas.SvgAssetsCount() - 1].yMax))
                    {
                        bool ok = true;

                        if (!(_dragInfo.DraggedObject is TextAsset))
                        {
                            if (_dragInfo.DraggedObject == atlas.SvgAsset(atlas.SvgAssetsCount() - 1))
                            {
                                ok = false;
                            }
                        }

                        if (ok)
                        {
                            _dragInfo.InsertIdx = atlas.SvgAssetsCount() - 1;
                            _dragInfo.InsertBefore = false;
                            needRepaint = true;
                        }
                    }
                }
                else
                {
                    _dragInfo.InsertIdx = -1;
                }

                if (needRepaint)
                {
                    Repaint();
                }
            }

            if (currentEvent.type == EventType.DragExited)
            {
                _dragInfo.StopDrag();
                DragAndDrop.objectReferences = Array.Empty<UnityEngine.Object>();
            }
            else
            {
                switch (currentEvent.type) {

                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (_dragInfo.Dragging)
                    {
                        bool dragValid = true;

                        if (scrollRect.Contains(currentEvent.mousePosition) && dragValid)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                            if (currentEvent.type == EventType.DragPerform)
                            {
                                int index;

                                // accept drag&drop operation
                                DragAndDrop.AcceptDrag();
                                // check if we are dropping a text asset
                                if (_dragInfo.DraggedObject is TextAsset)
                                {
                                    // if a valid inter-position has not been selected, append the new asset at the end of list
                                    if (_dragInfo.InsertIdx < 0)
                                    {
                                        index = atlas.SvgAssetsCount();
                                    }
                                    else
                                    {
                                        index = (_dragInfo.InsertBefore) ? _dragInfo.InsertIdx : (_dragInfo.InsertIdx + 1);
                                    }
                                    // add the text asset to the SVG list
                                    if (atlas.SvgAssetAdd(_dragInfo.DraggedObject as TextAsset, index))
                                    {
                                        isDirty = true;
                                    }
                                }
                                else
                                {
                                    // we are dropping an already present SVG row
                                    index = (_dragInfo.InsertBefore) ? _dragInfo.InsertIdx : (_dragInfo.InsertIdx + 1);
                                    if (atlas.SvgAssetMove(_dragInfo.DraggedObject as SVGAssetInput, index))
                                    {
                                        isDirty = true;
                                    }
                                }
                                // now we can close the drag operation
                                _dragInfo.StopDrag();
                            }
                        }
                        else
                        {
                            // if we are dragging outside of the allowed drop region, simply reject the drag&drop
                            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                        }
                    }
                    else
                    {
                        if (scrollRect.Contains(currentEvent.mousePosition))
                        {
                            if ((DragAndDrop.objectReferences != null) && (DragAndDrop.objectReferences.Length > 0))
                            {
                                UnityEngine.Object draggedObject = DragAndDrop.objectReferences[0];
                                // check object type, only TextAssets are allowed
                                if (draggedObject is TextAsset)
                                {
                                    _dragInfo.StartDrag(DragAndDrop.objectReferences[0]);
                                    Repaint();
                                }
                                else
                                {
                                    // acceptance is not confirmed (e.g. we are dragging a binary file)
                                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                                }
                            }
                        }
                    }
                    break;

                default:
                    break;
                }
            }

            return isDirty;
        }

        public override void OnInspectorGUI()
        {
            GUI.enabled = (Application.isPlaying) ? false : true;
        }

        void OnDestroy()
        {
            // nothing to do
        }

        static protected class BasicStyles
        {
            static BasicStyles()
            {
                // blue line separator
                BlueLine = new GUIStyle();
                BlueLine.normal.background = SVGUtils.ColorTexture(new Color32(51, 81, 226, 255));
                // grey line separator
                GreyLine = new GUIStyle();
                GreyLine.normal.background = SVGUtils.ColorTexture(new Color32(128, 128, 128, 255));
                GreyLine.padding.bottom = 0;
                GreyLine.padding.top = 0;
                GreyLine.border.top = 0;
                GreyLine.border.bottom = 0;
                // blue highlighted background
                HighlightRow = new GUIStyle();
                HighlightRow.normal.background = SVGUtils.ColorTexture(new Color32(65, 92, 150, 255));
                HighlightRow.normal.textColor = Color.white;
            }

            // blue line separator
            public static GUIStyle BlueLine;
            // grey line separator
            public static GUIStyle GreyLine;
            // blue highlighted background
            public static GUIStyle HighlightRow;

            public static GUIContent offsetScale = new GUIContent("Offset scale", "An additional scale factor used to adjust SVG contents globally (i.e. applied to all SVG files belonging to this atlas)");
            public static GUIContent Pow2Textures = new GUIContent("Force pow2 textures", "Force generated textures to have power-of-two dimensions");
            public static GUIContent MaxTexturesDimension = new GUIContent("Max textures dimension", "The maximum dimension (in pixels) of generated textures");
            public static GUIContent SpritesPadding = new GUIContent("Sprites padding", "Each sprite will be separated from the others by the given number of pixels (i.e. padding frame)");
            public static GUIContent ClearColor = new GUIContent("Clear color", "The background color used for generated textures");
            public static GUIContent FastUpload = new GUIContent("Fast upload", "Use the fast native method (OpenGL/DirectX/Metal) to upload the texture to the GPU");
            public static GUIContent SpritesPreviewSize = new GUIContent("Sprites preview size", "Dimension of sprite preview (in pixels)");
        }

        // Keep track of drawn rows (each SVG file is displayed as a row consisting of
        // asset name, separate groups checkbox, remove button, instantiate button)
        protected List<Rect> _inputAssetsRects = null;
        // Drag and drop information
        protected DragInfo _dragInfo = new DragInfo();
        // Current scroll position inside the list of input SVG
        protected Vector2 _svgListScrollPos = new Vector2(0, 0);
        // Current scroll position inside the list of generated sprites
        protected Vector2 _svgSpritesScrollPos = new Vector2(0, 0);

        static public string LastOutputFolder = "Assets";
    }
}
