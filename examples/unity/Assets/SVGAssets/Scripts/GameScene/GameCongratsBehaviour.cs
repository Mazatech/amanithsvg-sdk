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
using SVGAssets;

namespace MemoryGameScene
{
    public class GameCongratsBehaviour : MonoBehaviour {

        void OnDestroy()
        {
            if (_document != null)
            {
                // release SVG document
                _document.Dispose();
            }
        }

        public void Resize(int newScreenWidth,
                           int newScreenHeight)
        {
            // load the SVG document, if needed
            if ((_document == null) && (SVGFile != null))
            {
                _document = SVGAssetsUnity.CreateDocument(SVGFile.text);
            }

            if (_document != null)
            {
                // congratulation SVG is squared by design, we choose to generate a texture with
                // a size equal to 3/5 of the smallest screen dimension; e.g. on a 1920 x 1080
                // device screen, texture will have a size of (1080 * 3) / 5 = 648 pixels
                uint size = (uint)(Math.Min(newScreenWidth, newScreenHeight) * 3) / 5;
                // create a drawing surface
                SVGSurfaceUnity surface = SVGAssetsUnity.CreateSurface(size, size);
                // draw SVG and generate the relative texture
                Texture2D texture = surface.DrawTexture(_document, SVGColor.Clear, SVGRenderingQuality.Better, true);
                // create the sprite out of the texture
                gameObject.GetComponent<SpriteRenderer>().sprite = SVGAssetsUnity.CreateSprite(texture, new Vector2(0.5f, 0.5f));
                // destroy the temporary drawing surface
                surface.Dispose();
            }
        }

    #if UNITY_EDITOR

        // Ensure basic requirements
        private SpriteRenderer RequirementsCheck()
        {
            // a SpriteRenderer is mandatory
            SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                // NB: the default 'sortingOrder' property value will be 0, that is just fine because
                // it means that this congratulation object/sprite will be displayed upon the background
                renderer = gameObject.AddComponent<SpriteRenderer>();   
            }

            return renderer;
        }
    
        // Reset is called when the user hits the Reset button in the Inspector's context menu or when adding the component the first time.
        // This function is only called in editor mode. Reset is most commonly used to give good default values in the inspector.
        void Reset()
        {
            SpriteRenderer renderer = RequirementsCheck();
            if (renderer == null)
            {
                EditorUtility.DisplayDialog("Incompatible game object",
                                            string.Format("In order to work properly, the component {0} requires the presence of a SpriteRenderer component", GetType()),
                                            "Ok");
            }
            SVGFile = null;
            _document = null;
        }

    #endif  // UNITY_EDITOR

        // SVG to be displayed when the player wins.
        public TextAsset SVGFile;
        // The loaded SVG document.
        [NonSerialized]
        private SVGDocument _document = null;
    }
}
