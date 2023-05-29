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
#if UNITY_EDITOR
    using UnityEditor;
    using System.IO;
#endif
using UnityEngine;
using UnityEngine.UIElements;

namespace SVGAssets
{
    // Create SVGAssetsSettingsProvider by deriving from SettingsProvider
    class SVGAssetsConfigProvider : SettingsProvider
    {
        private SerializedObject m_SerializedConfig;

        public SVGAssetsConfigProvider(string path, SettingsScope scope = SettingsScope.User) : base(path, scope)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            // this function is called when the user clicks on the SVGAsstes element in the Settings window.
            m_SerializedConfig = SVGAssetsConfigUnityScriptable.GetSerializedConfig();
        }

        private bool IsValidResourceAsset(object obj)
        {
            bool ok = obj is TextAsset;

        #if UNITY_EDITOR
            if (ok)
            {
                TextAsset txtAsset = obj as TextAsset;
                // check file extension
                string assetPath = AssetDatabase.GetAssetPath(txtAsset);
                string fileExt = Path.GetExtension(assetPath);
                // if must be .bytes (see SVGRenamerImporter.cs)
                ok = fileExt.Equals(".bytes", System.StringComparison.OrdinalIgnoreCase);
            }
        #endif

            return ok;
        }

        // disable all the hints for image resources
        private static bool DisableHints(Enum enumValue)
        {
            return ((SVGResourceHint)enumValue == SVGResourceHint.None) ? true : false;
        }

        private bool DrawResource(SVGAssetsConfigUnity config,
                                  int index,
                                  out Rect rowRect)
        {
            bool isDirty = false;
            SVGAssetsConfigUnity.SVGResourceUnity resource = config.GetResource(index) as SVGAssetsConfigUnity.SVGResourceUnity;
            bool highlight = (_dragInfo.Dragging && (_dragInfo.DraggedObject == resource)) ? true : false;

            if ((_dragInfo.InsertIdx == index) && _dragInfo.InsertBefore)
            {
                // draw a separator before the row
                GUILayout.Box(GUIContent.none, Styles.BlueLine, GUILayout.ExpandWidth(true), GUILayout.Height(2));
            }

            // if the font row is the dragged one, change colors
            if (highlight)
            {
                EditorGUILayout.BeginHorizontal(Styles.HighlightRow);
                GUILayout.Space(10);
                EditorGUILayout.LabelField(resource.Asset.name, Styles.HighlightRow, GUILayout.MaxWidth(200));
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);
                EditorGUILayout.LabelField(resource.Asset.name, GUILayout.MaxWidth(200));
            }

            // resource type
            SVGResourceType type = (SVGResourceType)EditorGUILayout.EnumPopup(resource.Type, GUILayout.Width(70));
            // resource hints
            GUILayout.Space(10);
            EditorGUIUtility.labelWidth = 40;
            // NB: we disable all the hints for image resources
            SVGResourceHint hints = (type == SVGResourceType.Font) ? (SVGResourceHint)EditorGUILayout.EnumFlagsField(Styles.ResourceHints, resource.HintsEditor)
                                                                   : (SVGResourceHint)EditorGUILayout.EnumPopup(Styles.ResourceHints, resource.HintsEditor, DisableHints, false);
            EditorGUIUtility.labelWidth = 0;

            // if 'Remove' button is clicked, remove the font entry
            if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(70)))
            {
                config.ResourceRemove(index);
                isDirty = true;
            }
            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();

            rowRect = GUILayoutUtility.GetLastRect();

            if ((_dragInfo.InsertIdx == index) && (!_dragInfo.InsertBefore))
            {
                // draw a separator after the row
                GUILayout.Box(GUIContent.none, Styles.BlueLine, GUILayout.ExpandWidth(true), GUILayout.Height(2));
            }

            // update font parameters
            if (type != resource.Type)
            {
                resource.Type = type;
                isDirty = true;
            }
            if (hints != resource.HintsEditor)
            {
                resource.HintsEditor = hints;
                isDirty = true;
            }

            return isDirty;
        }

        private bool DrawResources(SVGAssetsConfigUnity config,
                                   Event currentEvent,
                                   out Rect scollRect)
        {
            bool isDirty = false;

            // keep track of drawn rows
            if (currentEvent.type != EventType.Layout)
            {
                _resourcesAssetsRects = new List<Rect>();
            }

            Vector2 scrollPos = EditorGUILayout.BeginScrollView(_resourcesListScrollPos, GUILayout.ExpandWidth(true));
            for (int i = 0; i < config.ResourcesCount(); ++i)
            {
                isDirty |= DrawResource(config, i, out Rect rowRect);
                // keep track of row rectangle
                if (currentEvent.type != EventType.Layout)
                {
                    _resourcesAssetsRects.Add(rowRect);
                }
            }
            EditorGUILayout.EndScrollView();

            // keep track of the scrollview area
            scollRect = GUILayoutUtility.GetLastRect();

            if (_resourcesListScrollPos != scrollPos)
            {
                _resourcesListScrollPos = scrollPos;
            }

            return isDirty;
        }

        private bool HandleDragEvents(SVGAssetsConfigUnity config,
                                      Event currentEvent,
                                      Rect scrollRect)
        {
            int i;
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

                    for (i = 0; i < config.ResourcesCount(); ++i)
                    {
                        // get the row rectangle relative to i-thm font
                        Rect rowRect = _resourcesAssetsRects[i];
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
                                    if (!IsValidResourceAsset(_dragInfo.DraggedObject))
                                    {
                                        if (_dragInfo.DraggedObject == config.GetResource(i))
                                        {
                                            ok = false;
                                        }
                                        else
                                        {
                                            if (dragBefore)
                                            {
                                                if ((i > 0) && (_dragInfo.DraggedObject == config.GetResource(i - 1)))
                                                {
                                                    ok = false;
                                                }
                                            }
                                            else
                                            {
                                                if (i < (config.ResourcesCount() - 1) && (_dragInfo.DraggedObject == config.GetResource(i + 1)))
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
                                    _dragInfo.StartDrag(config.GetResource(i));
                                    needRepaint = true;
                                }
                            }
                        }
                    }

                    // mouse is dragging inside the drop box, but not under an already present row; insertion point is inside the last element
                    if (_dragInfo.Dragging && (!separatorInserted) && (config.ResourcesCount() > 0) && (mousePos.y > _resourcesAssetsRects[config.ResourcesCount() - 1].yMax))
                    {
                        bool ok = true;

                        if (!IsValidResourceAsset(_dragInfo.DraggedObject))
                        {
                            if (_dragInfo.DraggedObject == config.GetResource(config.ResourcesCount() - 1))
                            {
                                ok = false;
                            }
                        }

                        if (ok)
                        {
                            _dragInfo.InsertIdx = config.ResourcesCount() - 1;
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
                switch (currentEvent.type)
                {
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
                                    if (IsValidResourceAsset(_dragInfo.DraggedObject))
                                    {
                                        // if a valid inter-position has not been selected, append the new asset at the end of list
                                        if (_dragInfo.InsertIdx < 0)
                                        {
                                            index = config.ResourcesCount();
                                        }
                                        else
                                        {
                                            index = _dragInfo.InsertBefore ? _dragInfo.InsertIdx : (_dragInfo.InsertIdx + 1);
                                        }
                                        // add the text asset to the SVG list
                                        if (config.ResourceAdd(_dragInfo.DraggedObject as TextAsset, index))
                                        {
                                            isDirty = true;
                                        }
                                    }
                                    else
                                    {
                                        // we are dropping an already present SVG row
                                        index = _dragInfo.InsertBefore ? _dragInfo.InsertIdx : (_dragInfo.InsertIdx + 1);
                                        if (config.ResourceMove(_dragInfo.DraggedObject as SVGAssetsConfigUnity.SVGResourceUnity, index))
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
                                    if (IsValidResourceAsset(draggedObject))
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

        private bool DrawInspector(SVGAssetsConfigUnity config)
        {
            bool isDirty = false;
            // get current event
            Event currentEvent = Event.current;

            // general settings
            GUILayout.Space(10);
            EditorGUILayout.LabelField("General settings", EditorStyles.boldLabel);
            GUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            EditorGUIUtility.labelWidth = 140;
            // user-agent language settings
            string language = EditorGUILayout.TextField(Styles.Language, config.Language, GUILayout.MaxWidth(250));
            GUILayout.Space(10);
            EditorGUIUtility.labelWidth = 100;
            // curves quality
            float curvesQuality = EditorGUILayout.Slider(Styles.CurvesQuality, config.CurvesQuality, 1.0f, 100.0f);
            GUILayout.Space(7);
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = 0;
            GUILayout.Space(20);

            // log facility
            EditorGUILayout.LabelField("Log facility", EditorStyles.boldLabel);
            GUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            EditorGUIUtility.labelWidth = 70;
            SVGLogLevel logLevel = (SVGLogLevel)EditorGUILayout.EnumFlagsField(Styles.LogLevel, config.LogLevelEditor, GUILayout.MaxWidth(250));
            GUILayout.Space(10);
            // max 1M log capacity
            EditorGUIUtility.labelWidth = 130;
            int logCapacity = EditorGUILayout.IntSlider(Styles.LogCapacity, (int)config.LogCapacity, 0, 999999);
            GUILayout.Space(7);
            EditorGUILayout.EndHorizontal();

            // draw the list of resources
            GUILayout.Space(20);
            EditorGUILayout.LabelField("Resources (drag JPEG/PNG/OTF/TTF/WOFF files here)", EditorStyles.boldLabel);
            isDirty |= DrawResources(config, currentEvent, out Rect scrollRect);

            // events handler
            isDirty |= HandleDragEvents(config, currentEvent, scrollRect);

            // update user-agent language
            if (language != config.Language)
            {
                // language
                if (SVGAssetsConfig.IsLanguageValid(language))
                {
                    config.Language = language;
                    isDirty = true;
                }
            }
            // update curves quality
            if (curvesQuality != config.CurvesQuality)
            {
                config.CurvesQuality = curvesQuality;
                isDirty = true;
            }
            // update log facility
            if (logLevel != config.LogLevelEditor)
            {
                config.LogLevelEditor = logLevel;
                isDirty = true;
            }
            if (logCapacity != config.LogCapacity)
            {
                config.LogCapacity = (uint)logCapacity;
                isDirty = true;
            }

            return isDirty;
        }

        public override void OnGUI(string searchContext)
        {
            // get the real config parameters
            SVGAssetsConfigUnityScriptable configSciptable = m_SerializedConfig.targetObject as SVGAssetsConfigUnityScriptable;

            if (configSciptable != null)
            {
                // enable / disable the GUI
                GUI.enabled = !Application.isPlaying;

                // draw the GUI, and check if the edited configuration is "dirty"
                if (DrawInspector(configSciptable.Config))
                {
                    SVGUtils.MarkObjectDirty(configSciptable);
                    m_SerializedConfig.ApplyModifiedProperties();
                }
            }
        }

        // Register the SettingsProvider
        [SettingsProvider]
        public static SettingsProvider CreateSVGAssetsConfigProvider()
        {
            return new SVGAssetsConfigProvider("Project/SVGAssets", SettingsScope.Project);
        }

        static private class Styles
        {
            static Styles()
            {
                // blue line separator
                BlueLine = new GUIStyle();
                BlueLine.normal.background = SVGUtils.ColorTexture(new Color32(51, 81, 226, 255));
                // blue highlighted background
                HighlightRow = new GUIStyle();
                HighlightRow.normal.background = SVGUtils.ColorTexture(new Color32(65, 92, 150, 255));
                HighlightRow.normal.textColor = Color.white;
            }

            // Blue line separator
            public static GUIStyle BlueLine;
            // Blue highlighted background
            public static GUIStyle HighlightRow;

            // Curves quality.
            public static GUIContent CurvesQuality = new GUIContent("Curves quality", "Used by AmanithSVG geometric kernel to approximate curves with straight line segments (flattening). Valid range is [1; 100], where 100 represents the best quality");
            // User-agent language settings.
            public static GUIContent Language = new GUIContent("User-agent language", "a list of languages separated by semicolon (e.g. en-US;en-GB;it;es)");
            // Log facility.
            public static GUIContent LogLevel = new GUIContent("Log level", "Log level");
            public static GUIContent LogCapacity = new GUIContent("Log capacity (chars)", "Size of log buffer, in characters; if zero is specified, logging is disabled");
            // Resource hints.
            public static GUIContent ResourceHints = new GUIContent("Hints", "Hints");
        }

        // Keep track of drawn rows (each resource file is displayed as a row consisting of asset name, type, hints, remove button
        [NonSerialized]
        private List<Rect> _resourcesAssetsRects = null;
        // Drag and drop information.
        [NonSerialized]
        private DragInfo _dragInfo = new DragInfo();
        // Current scroll position inside the list of input fonts
        [NonSerialized]
        private Vector2 _resourcesListScrollPos = new Vector2(0, 0);
    }
}
