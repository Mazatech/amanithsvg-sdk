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
#if UNITY_EDITOR
    using UnityEditor;
#endif

namespace SVGAssets
{
    public enum SVGBackgroundScaleType {
        // Scale SVG background according to desired width.
        Horizontal          = 0,
        // Scale SVG background according to desired height.
        Vertical            = 1
    };

    [ExecuteInEditMode]
    public class SVGBackgroundBehaviour : MonoBehaviour
    {
        private void LoadSVG()
        {
            // create and load SVG document
            _svgDoc = (SVGFile != null) ? SVGAssetsUnity.CreateDocument(SVGFile.text) : null;
        }

        public void DestroyAll(bool fullDestroy)
        {
            // set an empty sprite
            SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sprite = null;
            }
            // destroy SVG document, if loaded
            if ((_svgDoc != null) && fullDestroy)
            {
                _svgDoc.Dispose();
                _svgDoc = null;
            }
            // destroy sprite
            if (_sprite != null)
            {
            #if UNITY_EDITOR
                DestroyImmediate(_sprite);
            #else
                Destroy(_sprite);
            #endif
                _sprite = null;
            }
            // destroy texture
            if (_texture != null)
            {
            #if UNITY_EDITOR
                DestroyImmediate(_texture);
            #else
                Destroy(_texture);
            #endif
                _texture = null;
            }
        }
    
        private Texture2D DrawSVG(uint texWidth, uint texHeight, SVGColor clearColor, bool fastUpload)
        {
            SVGSurfaceUnity surface;
            Texture2D texture = null;

            // create a drawing surface, with the same texture dimensions
            if ((surface = SVGAssetsUnity.CreateSurface(texWidth, texHeight)) != null)
            {
                // draw the SVG document onto the surface
                if (surface.Draw(_svgDoc, clearColor, SVGRenderingQuality.Better) == SVGError.None)
                {
                    // create a 2D texture compatible with the drawing surface
                    if ((texture = surface.CreateCompatibleTexture(true, false)) != null)
                    {
                        // copy the surface content into the texture
                        if (fastUpload && Application.isPlaying)
                        {
                            _ = surface.CopyAndDestroy(texture);
                        }
                        else
                        {
                            if (surface.Copy(texture) == SVGError.None)
                            {
                                // call Apply() so it's actually uploaded to the GPU
                                texture.Apply(false, true);
                            }
                        }
                    }
                }
                // destroy SVG surface and document
                surface.Dispose();
            }

            // return the created texture (or null, in case of errors)
            return texture;
        }

        private Pair<Texture2D, Sprite> UpdateTexture(float desiredWidth, float desiredHeight, out float downScale)
        {
            float maxAllowedDimension = (float)SVGSurface.MaxDimension;
            float texWidth = desiredWidth;
            float texHeight = desiredHeight;
            float maxTexDimension = Math.Max(texWidth, texHeight);
            // we cannot exceed the maximum allowed surface dimension
            if (maxTexDimension > maxAllowedDimension)
            {
                float scl = maxAllowedDimension / maxTexDimension;
                texWidth *= scl;
                texHeight *= scl;
                downScale = scl;
            }
            else
            {
                downScale = 1;
            }
            // we want at least a 1x1 texture
            texWidth = Math.Max(Mathf.Floor(texWidth), 1);
            texHeight = Math.Max(Mathf.Floor(texHeight), 1);
            Texture2D texture = DrawSVG((uint)texWidth, (uint)texHeight, new SVGColor(ClearColor.r, ClearColor.g, ClearColor.b, ClearColor.a), FastUpload);
            if (texture != null)
            {
                // create the sprite equal to the whole texture (pivot in the middle)
                Sprite sprite = SVGAssetsUnity.CreateSprite(texture, new Vector2(0.5f, 0.5f));
                sprite.hideFlags = HideFlags.HideAndDontSave;
                return(new Pair<Texture2D, Sprite>(texture, sprite));
            }
            else
            {
                return(new Pair<Texture2D, Sprite>(null, null));
            }
        }

        private Pair<Texture2D, Sprite> UpdateTexture(float desiredSize, out float downScale)
        {
            float texWidth, texHeight;
        
            if (ScaleAdaption == SVGBackgroundScaleType.Horizontal)
            {
                texWidth = desiredSize;
                texHeight = (_svgDoc.Viewport.Height / _svgDoc.Viewport.Width) * texWidth;
            }
            else
            {
                texHeight = desiredSize;
                texWidth = (_svgDoc.Viewport.Width / _svgDoc.Viewport.Height) * texHeight;
            }
        
            return UpdateTexture(texWidth, texHeight, out downScale);
        }

        private void UpdateBackground()
        {
            if (_svgDoc != null)
            {
                SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    if ((Sliced && (Size >= 1)) || ((!Sliced) && (SlicedWidth >= 1) && (SlicedHeight >= 1)))
                    {
                        float downScale;
                        Pair<Texture2D, Sprite> texSpr;

                        if (Sliced)
                        {
                            _svgDoc.AspectRatio.MeetOrSlice = SVGMeetOrSlice.Slice;
                            texSpr = UpdateTexture(SlicedWidth, SlicedHeight, out downScale);
                        }
                        else
                        {
                            _svgDoc.AspectRatio.MeetOrSlice = SVGMeetOrSlice.Meet;
                            texSpr = UpdateTexture(Size, out downScale);
                        }

                        if ((texSpr.First != null) && (texSpr.Second != null))
                        {
                            _texture = texSpr.First;
                            _sprite = texSpr.Second;
                            renderer.sprite = _sprite;
                            transform.localScale = new Vector3(downScale, downScale, 1);
                        }
                    }
                }
            }
        }

        // For a non-sliced background, calculate the scale adaption and size parameters such that the background will cover the specified screen dimensions
        public Pair<SVGBackgroundScaleType, int> CoverFullScreen(int screenWidth, int screenHeight)
        {
            // load background SVG, if needed
            if (_svgDoc == null)
            {
                LoadSVG();
            }

            if (_svgDoc != null)
            {
                // try horizontal
                float texWidth = screenWidth;
                float texHeight = (_svgDoc.Viewport.Height / _svgDoc.Viewport.Width) * texWidth;

                return (texHeight >= screenHeight) ? new Pair<SVGBackgroundScaleType, int>(SVGBackgroundScaleType.Horizontal, screenWidth)
                                                   : new Pair<SVGBackgroundScaleType, int>(SVGBackgroundScaleType.Vertical, screenHeight);
            }
            else
            {
                return new Pair<SVGBackgroundScaleType, int>(SVGBackgroundScaleType.Horizontal, 0);
            }
        }

        public void UpdateBackground(bool fullUpdate)
        {
            // destroy current texture and sprite (if specified, destroy the SVG document too)
            DestroyAll(fullUpdate);

            if (SVGFile != null)
            {
                // load background SVG, if needed
                if (_svgDoc == null)
                {
                    LoadSVG();
                }
                // update the background with the desired size
                UpdateBackground();
            }
        }

        public float PixelWidth
        {
            // get the background sprite width, in pixels
            get
            {
                return ((_texture != null) ? _texture.width : 0);
            }
        }

        public float PixelHeight
        {
            // get the background sprite height, in pixels
            get
            {
                return ((_texture != null) ? _texture.height : 0);
            }
        }

        public float WorldWidth
        {
            // get the background sprite width, in world coordinates
            get
            {
                return (PixelWidth / SVGAssetsUnity.SPRITE_PIXELS_PER_UNIT);
            }
        }

        public float WorldHeight
        {
            // get the background sprite height, in world coordinates
            get
            {
                return (PixelHeight / SVGAssetsUnity.SPRITE_PIXELS_PER_UNIT);
            }
        }

        public float WorldLeft
        {
            // get the background sprite x-coordinate relative to its left edge, in world coordinates
            get
            {
                return gameObject.transform.position.x - (WorldWidth / 2);
            }
        }

        public float WorldRight
        {
            // get the background sprite x-coordinate relative to its right edge, in world coordinates
            get
            {
                return gameObject.transform.position.x + (WorldWidth / 2);
            }
        }

        void Start()
        {
            if (GenerateOnStart)
            {
                UpdateBackground(true);
            }
        }

        // Update is called once per frame
        void Update()
        {
        #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // if texture/sprite have not yet been generated, do it
                SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    if ((renderer.sprite == null) || (renderer.sprite.texture == null))
                    {
                        UpdateBackground(true);
                    }
                }
            }
        #endif
        }

    #if UNITY_EDITOR
        private bool RequirementsCheck()
        {
            SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                EditorUtility.DisplayDialog("Incompatible game object",
                                            string.Format("In order to work properly, the component {0} must be attached to a sprite object", GetType()),
                                            "Ok");
                DestroyImmediate(this);
                return false;
            }
            return true;
        }

        void Reset()
        {
            if (RequirementsCheck())
            {
                SVGFile = null;
                ScaleAdaption = SVGBackgroundScaleType.Horizontal;
                Size = 256;
                ClearColor = new Color(1, 1, 1, 0);
                GenerateOnStart = true;
                FastUpload = true;

                Sliced = false;
                SlicedWidth = 256;
                SlicedHeight = 256;

                _svgDoc = null;
                _texture = null;
                _sprite = null;
                DestroyAll(true);
            }
        }
    #endif

        // The background SVG text asset
        public TextAsset SVGFile;
        // Scale adaption
        public SVGBackgroundScaleType ScaleAdaption;
        // The size (horizontal or vertical), in pixels
        public int Size;
        // Clear color
        public Color ClearColor;
        public bool GenerateOnStart;
        public bool FastUpload;
        /*
            Sliced mode.

            Sliced mode is useful to generate static backgrounds that must cover exactly the whole device screen.
            Non-sliced mode is useful to generate static "scrollable" backgrounds.
        */
        public bool Sliced;
        public int SlicedWidth;
        public int SlicedHeight;
        // The loaded SVG document
        [NonSerialized]
        private SVGDocument _svgDoc;
        // Background sprite texture
        [NonSerialized]
        private Texture2D _texture;
        // Background sprite
        [NonSerialized]
        private Sprite _sprite;
    }
}
