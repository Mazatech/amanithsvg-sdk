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
using System.Collections;
using UnityEngine;

namespace SVGAssets
{
    public class SVGTextureAtlasBehaviour : MonoBehaviour
    {
        private void DestroyDocument(SVGDocument document)
        {
            if (document != null)
            {
                document.Dispose();
            }
        }

        private Texture2D DrawAtlas(string svgXml1, string svgXml2, string svgXml3, string svgXml4,
                                    uint texWidth, uint texHeight,
                                    Color clearColor,
                                    bool fastUpload)
        {
            Texture2D texture = null;
            // create a drawing surface, with the same texture dimensions
            SVGSurfaceUnity surface = SVGAssetsUnity.CreateSurface(texWidth, texHeight);
        
            if (surface != null)
            {
                // create the SVG documents
                SVGDocument document1 = SVGAssetsUnity.CreateDocument(svgXml1);
                SVGDocument document2 = SVGAssetsUnity.CreateDocument(svgXml2);
                SVGDocument document3 = SVGAssetsUnity.CreateDocument(svgXml3);
                SVGDocument document4 = SVGAssetsUnity.CreateDocument(svgXml4);

                if ((document1 != null) && (document2 != null) && (document3 != null) && (document4 != null))
                {
                    // draw the SVG document1 onto the surface
                    surface.Viewport = new SVGViewport(0.0f, 0.0f, texWidth / 2.0f, texHeight / 2.0f);
                    surface.Draw(document1, new SVGColor(clearColor.r, clearColor.g, clearColor.b, clearColor.a), SVGRenderingQuality.Better);
                    // draw the SVG document2 onto the surface
                    surface.Viewport = new SVGViewport(texWidth / 2.0f, 0.0f, texWidth / 2.0f, texHeight / 2.0f);
                    surface.Draw(document2, null, SVGRenderingQuality.Better);
                    // draw the SVG document3 onto the surface
                    surface.Viewport = new SVGViewport(0.0f, texHeight / 2.0f, texWidth / 2.0f, texHeight / 2.0f);
                    surface.Draw(document3, null, SVGRenderingQuality.Better);
                    // draw the SVG document4 onto the surface
                    surface.Viewport = new SVGViewport(texWidth / 2.0f, texHeight / 2.0f, texWidth / 2.0f, texHeight / 2.0f);
                    surface.Draw(document4, null, SVGRenderingQuality.Better);

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

                // destroy SVG documents
                DestroyDocument(document1);
                DestroyDocument(document2);
                DestroyDocument(document3);
                DestroyDocument(document4);

                // destroy SVG surface
                surface.Dispose();
            }

            // return the created texture, or null in case of errors
            return texture;
        }

        // Use this for initialization
        void Start()
        {
            if ((SVGFile1 != null) && (SVGFile2 != null) && (SVGFile3 != null) && (SVGFile4 != null))
            {
                Shader shader = Shader.Find("Sprites/Default");

                if (shader != null)
                {
                    GetComponent<Renderer>().material.shader = shader;
                    GetComponent<Renderer>().material.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                    // set texture onto our material
                    GetComponent<Renderer>().material.mainTexture = DrawAtlas(SVGFile1.text, SVGFile2.text, SVGFile3.text, SVGFile4.text,
                                                                              (uint)Math.Max(2, TextureWidth), (uint)Math.Max(2, TextureHeight),
                                                                              ClearColor, FastUpload);
                }
                else
                {
                    SVGAssetsUnity.LogWarning("SVGTextureAtlasBehaviour::Start cannot find Sprites/Default shader, so SVG rendering is skipped at all");
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
        }

        public TextAsset SVGFile1 = null;
        public TextAsset SVGFile2 = null;
        public TextAsset SVGFile3 = null;
        public TextAsset SVGFile4 = null;
        public int TextureWidth = 512;
        public int TextureHeight = 512;
        public Color ClearColor = new Color(1, 1, 1, 1);
        public bool FastUpload = true;
    }
}
