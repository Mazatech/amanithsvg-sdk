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

namespace SVGAssets
{

    [ExecuteInEditMode]
    public class SVGTextureBehaviour : MonoBehaviour
    {
        private Texture2D DrawSVG(string svgXml,
                                  uint texWidth, uint texHeight,
                                  Color clearColor,
                                  bool fastUpload)
        {
            Texture2D texture = null;
            // create a drawing surface, with the same texture dimensions
            SVGSurfaceUnity surface = SVGAssetsUnity.CreateSurface(texWidth, texHeight);

            if (surface != null)
            {
                // create the SVG document
                SVGDocument document = SVGAssetsUnity.CreateDocument(svgXml);
                if (document != null)
                {
                    // draw the SVG document onto the surface
                    if (surface.Draw(document, new SVGColor(clearColor.r, clearColor.g, clearColor.b, clearColor.a), SVGRenderingQuality.Better) == SVGError.None)
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
                            if (surface.Copy(texture) == SVGError.None)
                            {
                                // call Apply() so it's actually uploaded to the GPU
                                texture.Apply(false, true);
                            }
                        }
                    }
                    // destroy SVG document
                    document.Dispose();
                }
                // destroy SVG surface
                surface.Dispose();
            }

            // return the created texture, or null in case of errors
            return texture;
        }

        private void EnsureMaterialProperties(Material m)
        {
            m.shader = Shader.Find("Sprites/Default");
            m.color = Color.white;
        }

        private void DrawSVG(Material m)
        {
            if (SVGFile != null)
            {
                m.mainTexture = DrawSVG(SVGFile.text,
                                        // we want at least a 1x1 texture
                                        (uint)Math.Max(1, TextureWidth), (uint)Math.Max(1, TextureHeight),
                                        ClearColor, FastUpload);
            }
        }

        // Use this for initialization
        void Start()
        {
            if (Application.isPlaying)
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();
                // ensure basic material properties for the display of a simple texture
                EnsureMaterialProperties(renderer.material);
                // draw SVG and assing the texture
                DrawSVG(renderer.material);
            }
        }

        // Update is called once per frame
        void Update()
        {
        #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // if texture/sprite have not yet been generated, do it
                Renderer renderer = gameObject.GetComponent<Renderer>();
                if ((renderer != null) && (renderer.sharedMaterial.mainTexture == null))
                {
                    // allocate a new material, in order to avoid the warning message
                    // "Instantiating material due to calling renderer.material during edit mode.
                    //  This will leak materials into the scene.
                    //  You most likely want to use renderer.sharedMaterial instead "
                    Material tmp = new Material(renderer.sharedMaterial);
                    // ensure basic material properties for the display of a simple texture
                    EnsureMaterialProperties(tmp);
                    // draw SVG and assing the texture
                    DrawSVG(tmp);
                    renderer.sharedMaterial = tmp;
                }
            }
        #endif
        }

        public TextAsset SVGFile = null;
        public int TextureWidth = 512;
        public int TextureHeight = 512;
        public Color ClearColor = Color.white;
        public bool FastUpload = true;
    }
}
