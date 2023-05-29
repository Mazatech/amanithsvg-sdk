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
using System;

namespace SVGAssets
{
    public enum PivotEditingResult
    {
        Ok,
        Cancel
    }

    public class SVGPivotEditor : EditorWindow
    {
        // Create a checker-background texture.
        static private Texture2D CreateCheckerTex(Color c0, Color c1, string name)
        {
            Texture2D tex = new Texture2D(16, 16)
            {
                name = name,
                hideFlags = HideFlags.DontSave
            };
            for (int y = 0; y < 8; ++y) for (int x = 0; x < 8; ++x) tex.SetPixel(x, y, c1);
            for (int y = 8; y < 16; ++y) for (int x = 0; x < 8; ++x) tex.SetPixel(x, y, c0);
            for (int y = 0; y < 8; ++y) for (int x = 8; x < 16; ++x) tex.SetPixel(x, y, c0);
            for (int y = 8; y < 16; ++y) for (int x = 8; x < 16; ++x) tex.SetPixel(x, y, c1);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;
        }

        static private Texture2D CreateContrastTexture()
        {
            return CreateCheckerTex(new Color(0f, 0.0f, 0f, 0.5f), new Color(1f, 1f, 1f, 0.5f), "SVGPivotEditor.m_ContrastTexture");
        }

        static private Texture2D ContrastTexture
        {
            get
            {
                if (_contrastTexture == null)
                {
                    _contrastTexture = CreateContrastTexture();
                }
                return _contrastTexture;
            }
        }

        static private void CreatePivotTexture()
        {
            Color32[] pixels = new Color32[PIVOT_CURSOR_DIMENSION * PIVOT_CURSOR_DIMENSION] {
                new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 67, 194, 42), new Color32(0, 67, 193, 111), new Color32(0, 65, 193, 172), new Color32(0, 66, 192, 166), 
                new Color32(0, 66, 194, 96), new Color32(0, 66, 189, 35), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), 
                new Color32(0, 78, 196, 39), new Color32(0, 74, 197, 144), new Color32(0, 73, 195, 196), new Color32(0, 73, 196, 204), new Color32(0, 74, 195, 201), new Color32(0, 74, 197, 201), new Color32(0, 73, 196, 204), new Color32(0, 73, 197, 192), new Color32(0, 75, 196, 129), new Color32(0, 78, 196, 26), 
                new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 86, 199, 59), new Color32(0, 83, 200, 182), new Color32(0, 82, 201, 198), new Color32(0, 83, 201, 197), new Color32(0, 83, 200, 190), 
                new Color32(0, 83, 201, 176), new Color32(0, 82, 201, 178), new Color32(0, 82, 201, 192), new Color32(0, 83, 201, 197), new Color32(0, 82, 200, 199), new Color32(0, 83, 201, 170), new Color32(0, 83, 204, 40), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), 
                new Color32(0, 92, 205, 36), new Color32(0, 92, 205, 175), new Color32(0, 92, 204, 189), new Color32(0, 93, 205, 184), new Color32(0, 92, 205, 117), new Color32(0, 87, 203, 44), new Color32(0, 96, 207, 16), new Color32(0, 94, 201, 19), new Color32(0, 87, 202, 53), new Color32(0, 91, 205, 132), 
                new Color32(0, 92, 203, 188), new Color32(0, 91, 204, 190), new Color32(0, 92, 204, 160), new Color32(0, 94, 201, 19), new Color32(0, 0, 0, 0), new Color32(0, 128, 255, 2), new Color32(0, 102, 211, 128), new Color32(0, 101, 208, 184), new Color32(0, 100, 208, 178), new Color32(0, 101, 208, 76), 
                new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 128, 255, 2), new Color32(0, 99, 207, 100), new Color32(0, 101, 209, 181), new Color32(0, 101, 208, 185), new Color32(0, 101, 208, 104), 
                new Color32(0, 0, 0, 0), new Color32(0, 116, 216, 33), new Color32(0, 110, 213, 169), new Color32(0, 110, 213, 176), new Color32(0, 109, 213, 108), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), 
                new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 170, 170, 3), new Color32(0, 109, 212, 131), new Color32(0, 110, 213, 176), new Color32(0, 110, 213, 157), new Color32(0, 112, 223, 16), new Color32(0, 120, 218, 89), new Color32(0, 118, 219, 168), new Color32(0, 119, 217, 163), 
                new Color32(0, 117, 214, 37), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 119, 217, 60), 
                new Color32(0, 118, 217, 168), new Color32(0, 119, 217, 167), new Color32(0, 119, 217, 47), new Color32(0, 128, 220, 132), new Color32(0, 128, 221, 159), new Color32(0, 129, 223, 144), new Color32(0, 118, 216, 13), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), 
                new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 128, 221, 30), new Color32(0, 129, 222, 154), new Color32(0, 128, 222, 162), new Color32(0, 128, 222, 70), new Color32(0, 138, 226, 122), 
                new Color32(0, 138, 226, 152), new Color32(0, 138, 227, 139), new Color32(0, 146, 219, 14), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), 
                new Color32(0, 0, 0, 0), new Color32(0, 132, 222, 31), new Color32(0, 138, 226, 148), new Color32(0, 138, 227, 155), new Color32(0, 139, 223, 64), new Color32(0, 146, 229, 68), new Color32(0, 146, 229, 148), new Color32(0, 147, 230, 144), new Color32(0, 150, 229, 39), new Color32(0, 0, 0, 0), 
                new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 147, 233, 59), new Color32(0, 146, 229, 148), new Color32(0, 146, 230, 145), 
                new Color32(0, 148, 228, 38), new Color32(0, 151, 232, 22), new Color32(0, 155, 234, 132), new Color32(0, 156, 233, 141), new Color32(0, 155, 237, 97), new Color32(0, 255, 255, 1), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), 
                new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 159, 223, 8), new Color32(0, 156, 235, 113), new Color32(0, 156, 233, 141), new Color32(0, 155, 234, 122), new Color32(0, 153, 230, 10), new Color32(0, 0, 0, 0), new Color32(0, 164, 237, 84), new Color32(0, 166, 238, 134), 
                new Color32(0, 164, 238, 132), new Color32(0, 166, 237, 72), new Color32(0, 170, 255, 3), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 170, 227, 9), new Color32(0, 168, 240, 85), new Color32(0, 165, 238, 133), 
                new Color32(0, 166, 240, 134), new Color32(0, 162, 236, 66), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 175, 239, 16), new Color32(0, 173, 243, 109), new Color32(0, 174, 243, 126), new Color32(0, 173, 243, 125), new Color32(0, 175, 244, 93), new Color32(0, 177, 244, 46), 
                new Color32(0, 181, 244, 24), new Color32(0, 173, 245, 25), new Color32(0, 180, 245, 51), new Color32(0, 173, 243, 102), new Color32(0, 174, 243, 126), new Color32(0, 175, 243, 127), new Color32(0, 175, 244, 96), new Color32(0, 182, 255, 7), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), 
                new Color32(0, 0, 0, 0), new Color32(0, 184, 245, 25), new Color32(0, 182, 245, 101), new Color32(0, 183, 247, 120), new Color32(0, 181, 244, 120), new Color32(0, 182, 246, 119), new Color32(0, 183, 246, 114), new Color32(0, 182, 246, 115), new Color32(0, 181, 244, 120), new Color32(0, 182, 246, 119), 
                new Color32(0, 183, 247, 120), new Color32(0, 182, 247, 91), new Color32(0, 182, 255, 14), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 191, 255, 12), new Color32(0, 188, 251, 65), 
                new Color32(0, 191, 252, 100), new Color32(0, 191, 250, 111), new Color32(0, 194, 253, 112), new Color32(0, 194, 253, 112), new Color32(0, 192, 250, 110), new Color32(0, 192, 252, 97), new Color32(0, 188, 246, 57), new Color32(0, 170, 255, 6), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), 
                new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 204, 255, 10), new Color32(0, 197, 255, 31), new Color32(0, 200, 255, 46), new Color32(0, 197, 255, 44), 
                new Color32(0, 200, 255, 28), new Color32(0, 191, 255, 8), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0)
            };

            _pivotTexture = new Texture2D(PIVOT_CURSOR_DIMENSION, PIVOT_CURSOR_DIMENSION, TextureFormat.ARGB32, false)
            {
                name = "SVGPivotEditor._pivotTexture",
                hideFlags = HideFlags.DontSave,
                filterMode = FilterMode.Point
            };
            _pivotTexture.SetPixels32(pixels);
            _pivotTexture.Apply(false, true);
        }

        static private Texture2D PivotTexture
        {
            get
            {
                if (_pivotTexture == null)
                {
                    CreatePivotTexture();
                }
                return _pivotTexture;
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

        private void Cancel()
        {
            _callback?.Invoke(PivotEditingResult.Cancel, _spriteAsset, new Vector2(_pivot.x, _pivot.y), _border);
            Close();
        }
    
        private void Ok()
        {
            _callback?.Invoke(PivotEditingResult.Ok, _spriteAsset, new Vector2(_pivot.x, _pivot.y), _border);
            Close();
        }

        // Draws the tiled texture. Like GUI.DrawTexture() but tiled instead of stretched.
        static void DrawTiledTexture(Rect rect, Texture tex)
        {
            float u = rect.width / tex.width;
            float v = rect.height / tex.height;

            Rect texCoords = new Rect(0, 0, u, v);
            TextureWrapMode originalMode = tex.wrapMode;
            tex.wrapMode = TextureWrapMode.Repeat;
            GUI.DrawTextureWithTexCoords(rect, tex, texCoords);
            tex.wrapMode = originalMode;
        }

        // Draw the specified Image.
        private static void DrawSprite(Texture tex, Rect drawArea, Vector4 padding, Rect outer, Rect inner, Rect uv, Color color)
        {
            // Create the texture rectangle that is centered inside rect.
            Rect outerRect = drawArea;
            outerRect.width = outer.width;
            outerRect.height = outer.height;

            if (outerRect.width > 0f)
            {
                float f = drawArea.width / outerRect.width;
                outerRect.width *= f;
                outerRect.height *= f;
            }

            if (drawArea.height > outerRect.height)
            {
                outerRect.y += (drawArea.height - outerRect.height) * 0.5f;
            }
            else
            if (outerRect.height > drawArea.height)
            {
                float f = drawArea.height / outerRect.height;
                outerRect.width *= f;
                outerRect.height *= f;
            }

            if (drawArea.width > outerRect.width)
            {
                outerRect.x += (drawArea.width - outerRect.width) * 0.5f;
            }

            // Draw the background
            EditorGUI.DrawTextureTransparent(outerRect, null, ScaleMode.ScaleToFit, outer.width / outer.height);

            // Draw the Image
            GUI.color = color;

            Rect paddedTexArea = new Rect(outerRect.x + outerRect.width * padding.x,
                                          outerRect.y + outerRect.height * padding.w,
                                          outerRect.width - (outerRect.width * (padding.z + padding.x)),
                                          outerRect.height - (outerRect.height * (padding.w + padding.y)));


            GUI.DrawTextureWithTexCoords(paddedTexArea, tex, uv, true);

            // Draw the border indicator lines
            GUI.BeginGroup(outerRect);
            {
                tex = ContrastTexture;
                GUI.color = Color.white;

                if (inner.xMin != outer.xMin)
                {
                    float x = (inner.xMin - outer.xMin) / outer.width * outerRect.width - 1;
                    DrawTiledTexture(new Rect(x, 0f, 1f, outerRect.height), tex);
                }

                if (inner.xMax != outer.xMax)
                {
                    float x = (inner.xMax - outer.xMin) / outer.width * outerRect.width - 1;
                    DrawTiledTexture(new Rect(x, 0f, 1f, outerRect.height), tex);
                }

                if (inner.yMin != outer.yMin)
                {
                    float y = (inner.yMin - outer.yMin) / outer.height * outerRect.height - 1;
                    DrawTiledTexture(new Rect(0f, outerRect.height - y, outerRect.width, 1f), tex);
                }

                if (inner.yMax != outer.yMax)
                {
                    float y = (inner.yMax - outer.yMin) / outer.height * outerRect.height - 1;
                    DrawTiledTexture(new Rect(0f, outerRect.height - y, outerRect.width, 1f), tex);
                }
            }

            GUI.EndGroup();
        }

        // Draw the specified Image.
        private void DrawSprite(Sprite sprite, Rect drawArea, Color color)
        {
            if ((sprite != null) || (sprite.texture != null))
            {
                Rect outer = sprite.rect;
                Rect inner = outer;

                inner.xMin += _border.x;
                inner.yMin += _border.y;
                inner.xMax -= _border.z;
                inner.yMax -= _border.w;

                Vector4 uv4 = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
                Rect uv = new Rect(uv4.x, uv4.y, uv4.z - uv4.x, uv4.w - uv4.y);
                Vector4 padding = UnityEngine.Sprites.DataUtility.GetPadding(sprite);
                padding.x /= outer.width;
                padding.y /= outer.height;
                padding.z /= outer.width;
                padding.w /= outer.height;

                DrawSprite(sprite.texture, drawArea, padding, outer, inner, uv, color);
            }
        }

        // 3 decimal places float field
        private static float FloatField3DP(string label, float value)
        {
            string floatString = value.ToString("0.000");

            GUILayout.Label(label, GUILayout.Width(15));
            string input = GUILayout.TextField(floatString, GUILayout.Width(40));

            if (input != floatString)
            {
                bool valid = float.TryParse(input, out float inputFloat);
                if (valid)
                {
                    value = inputFloat;
                }
            }

            return value;
        }


        private void Draw(Rect region)
        {
            float mouseX, mouseY, px, py, y, h;
            Rect spritePreviewRect = new Rect();
            GUILayoutOption[] okButtonOptions = new GUILayoutOption[2] { GUILayout.Width(_windowWidth / 2 - 8), GUILayout.Height(BUTTONS_HEIGHT - 3) };
            GUILayoutOption[] cancelButtonOptions = new GUILayoutOption[2] { GUILayout.Width(_windowWidth / 2 - 3), GUILayout.Height(BUTTONS_HEIGHT - 3) };
        
            GUILayout.BeginArea(region);
            {
                // ----------------
                // custom code here
                // ----------------
                if (_spriteAsset != null)
                {
                    Sprite sprite = _spriteAsset.SpriteData.Sprite;
                    float spritePreviewWidth = _spritePreviewWidth;
                    float spritePreviewHeight = _spritePreviewHeight;
                    float spritePreviewX = 0;
                    float spritePreviewY = 0;
                
                    // draw the sprite preview
                    spritePreviewRect = new Rect(spritePreviewX, spritePreviewY, spritePreviewWidth, spritePreviewHeight);
                    DrawSprite(sprite, spritePreviewRect, new Color(1.0f, 1.0f, 1.0f, 1.0f));

                    // draw pivot
                    px = spritePreviewX + _pivot.x * spritePreviewWidth;
                    py = spritePreviewY + (1 - _pivot.y) * spritePreviewHeight;
                    GUI.DrawTexture(new Rect(px - PIVOT_CURSOR_DIMENSION * 0.5f, py - PIVOT_CURSOR_DIMENSION * 0.5f, PIVOT_CURSOR_DIMENSION, PIVOT_CURSOR_DIMENSION), PivotTexture);
                }

                // edit fields relative to sprite border
                y = region.height - BUTTONS_HEIGHT - 3 * EDIT_FIELDS_HEIGHT;
                h = 2 * EDIT_FIELDS_HEIGHT;
                GUILayout.BeginArea(new Rect(3, y, region.width, h));
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(EDIT_FIELDS_HEIGHT));
                    {
                        GUILayout.Label("Border", GUILayout.Width(50));
                        GUILayout.Label("L", GUILayout.Width(15));
                        _border.x = EditorGUILayout.FloatField(_border.x, GUILayout.Width(40));
                        GUILayout.Label("B", GUILayout.Width(15));
                        _border.y = EditorGUILayout.FloatField(_border.y, GUILayout.Width(40));
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(GUILayout.Height(EDIT_FIELDS_HEIGHT));
                    {
                        GUILayout.Label("", GUILayout.Width(50));
                        GUILayout.Label("R", GUILayout.Width(15));
                        _border.z = EditorGUILayout.FloatField(_border.z, GUILayout.Width(40));
                        GUILayout.Label("T", GUILayout.Width(15));
                        _border.w = EditorGUILayout.FloatField(_border.w, GUILayout.Width(40));
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndArea();

                // edit fields relative to sprite pivot
                y = region.height - BUTTONS_HEIGHT - EDIT_FIELDS_HEIGHT;
                h = EDIT_FIELDS_HEIGHT;
                GUILayout.BeginArea(new Rect(3, y, region.width, h));
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(EDIT_FIELDS_HEIGHT));
                    {
                        GUILayout.Label("Pivot", GUILayout.Width(50));
                        _pivot.x = FloatField3DP("X", _pivot.x);
                        _pivot.y = FloatField3DP("Y", _pivot.y);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndArea();

                // button bar
                GUILayout.BeginArea(new Rect(3, region.height - BUTTONS_HEIGHT, region.width, BUTTONS_HEIGHT));
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(BUTTONS_HEIGHT));
                    {
                        if (GUILayout.Button("Cancel", cancelButtonOptions))
                        {
                            Cancel();
                        }
                        if (GUILayout.Button("Ok", okButtonOptions))
                        {
                            Ok();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndArea();
            }
            GUILayout.EndArea();
        
            // events handler
            switch (Event.current.type)
            {
                // keyboard press
                case EventType.KeyDown:
                    if (Event.current.keyCode == KeyCode.Return)
                    {
                        Ok();
                    }
                    if (Event.current.keyCode == KeyCode.Escape)
                    {
                        Cancel();
                    }
                    break;
                // mouse left button
                case EventType.MouseDown:
                    mouseX = Event.current.mousePosition.x;
                    mouseY = Event.current.mousePosition.y;
                    // clamp x coordinate
                    if (mouseX < spritePreviewRect.xMin + SNAP_CORNER_THRESHOLD)
                    {
                        mouseX = spritePreviewRect.xMin;
                    }
                    if (mouseX + SNAP_CORNER_THRESHOLD > spritePreviewRect.xMax)
                    {
                        mouseX = spritePreviewRect.xMax;
                    }
                    // clamp y coordinate
                    if (mouseY < spritePreviewRect.yMin + SNAP_CORNER_THRESHOLD)
                    {
                        mouseY = spritePreviewRect.yMin;
                    }
                    if (mouseY + SNAP_CORNER_THRESHOLD > spritePreviewRect.yMax)
                    {
                        mouseY = spritePreviewRect.yMax;
                    }
                    // assign the new pivot value
                    px = (mouseX - spritePreviewRect.xMin) / spritePreviewRect.width;
                    py = (mouseY - spritePreviewRect.yMin) / spritePreviewRect.height;
                    _pivot.Set(Mathf.Clamp(px, 0, 1), 1 - Mathf.Clamp(py, 0, 1));
                    // force a repaint
                    Repaint();
                    break;
            }
        }

        void OnGUI()
        {
            Rect content = new Rect(0, 0, position.width, position.height);
            Draw(content);
        }

        private static void Init(SVGPivotEditor editor, SVGSpriteAssetFile spriteAsset, Vector2 pivot, Vector4 border, OnPivotEditedCallback callback)
        {
            float v;
            Rect spriteRect = spriteAsset.SpriteData.Sprite.rect;
            float minDim = Math.Min(spriteRect.width, spriteRect.height);
        
            // keep track of the sprite and the input/output pivot
            editor._spriteAsset = spriteAsset;
            editor._pivot.Set(pivot.x, pivot.y);
            editor._border = border;
            editor._spritePreviewWidth = spriteRect.width;
            editor._spritePreviewHeight = spriteRect.height;
            // adapt window dimension
            if (minDim < SPRITE_PREVIEW_MIN_DIMENSION)
            {
                float scl = SPRITE_PREVIEW_MIN_DIMENSION / minDim;
                editor._spritePreviewWidth *= scl;
                editor._spritePreviewHeight *= scl;
            }
            // we must not exceed screen resolution (width)
            v = Screen.currentResolution.width * 0.9f;
            if (editor._spritePreviewWidth > v)
            {
                float scl = v / editor._spritePreviewWidth;
                editor._spritePreviewWidth *= scl;
                editor._spritePreviewHeight *= scl;
            }
            v = Screen.currentResolution.height * 0.9f;
            if (editor._spritePreviewHeight > v)
            {
                float scl = v / editor._spritePreviewHeight;
                editor._spritePreviewWidth *= scl;
                editor._spritePreviewHeight *= scl;
            }
        
            editor._spritePreviewWidth = Mathf.Round(editor._spritePreviewWidth);
            editor._spritePreviewHeight = Mathf.Round(editor._spritePreviewHeight);
            editor._windowWidth = editor._spritePreviewWidth;
            // we must ensure that pivot and border controls are already visible
            if (editor._windowWidth < 190)
            {
                editor._windowWidth = 190;
            }
            editor._windowHeight = editor._spritePreviewHeight + BUTTONS_HEIGHT + (EDIT_FIELDS_HEIGHT * 3);
            // set title
            editor.titleContent = new GUIContent("Sprite editor");
            // set callback
            editor._callback = callback;
        }

        private void ShowEditor()
        {
            // position and size the window
            position = new Rect(Screen.currentResolution.width - (_windowWidth / 2), Screen.currentResolution.height - (_windowHeight / 2), _windowWidth, _windowHeight);
            minSize = new Vector2(_windowWidth, _windowHeight);
            maxSize = new Vector2(_windowWidth, _windowHeight);
            // show window
            ShowUtility();
        }

        // show the sprite selector
        static public void Show(SVGSpriteAssetFile spriteAsset, OnPivotEditedCallback callback)
        {
            // close the current selector instance, if any
            CloseAll();
        
            if (spriteAsset != null)
            {
                SVGPivotEditor pivotEditor = CreateInstance<SVGPivotEditor>();
                Init(pivotEditor, spriteAsset, spriteAsset.SpriteData.Pivot, spriteAsset.SpriteData.Border, callback);
                pivotEditor.ShowEditor();
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
        public delegate void OnPivotEditedCallback(PivotEditingResult result, SVGSpriteAssetFile spriteAsset, Vector2 editedPivot, Vector4 editedBorder);
        // The current selector instance
        [NonSerialized]
        static private SVGPivotEditor s_instance;
        // The callback to be invoked editing is finished (i.e. Cancel or Ok button click)
        [NonSerialized]
        private OnPivotEditedCallback _callback;
        // the currently edited pivot value
        [NonSerialized]
        private Vector2 _pivot;
        // the currently edited border value
        [NonSerialized]
        private Vector4 _border;
    
        [NonSerialized]
        static private Texture2D _pivotTexture;

        [NonSerialized]
        static private Texture2D _contrastTexture;

        // edited sprite asset
        [NonSerialized]
        private SVGSpriteAssetFile _spriteAsset;
        // dimensions of the sprite preview area
        [NonSerialized]
        private float _spritePreviewWidth;
        [NonSerialized]
        private float _spritePreviewHeight;
        // window (canvas) dimensions
        [NonSerialized]
        private float _windowWidth;
        [NonSerialized]
        private float _windowHeight;
    
        private const float SNAP_CORNER_THRESHOLD = 5;
        // pivot cursor
        private const int PIVOT_CURSOR_DIMENSION = 16;
        // height of the buttons bar
        private const int BUTTONS_HEIGHT = 25;
        private const int EDIT_FIELDS_HEIGHT = 25;

        // minimum dimension of the sprite preview area
        private const float SPRITE_PREVIEW_MIN_DIMENSION = 128;
    }
}
