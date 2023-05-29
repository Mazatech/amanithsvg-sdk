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
using UnityEngine.UI;
#if UNITY_EDITOR
    using UnityEditor;
#endif

namespace SVGAssets 
{
    public class SVGUISpriteLoaderBehaviour : MonoBehaviour
    {

        private void UpdateSprite(float canvasScaleFactor)
        {
            if ((UIAtlas != null) && (SpriteReference != null))
            {
                Image uiImage = gameObject.GetComponent<Image>();
                if (uiImage != null)
                {
                    if (UIAtlas.UpdateRuntimeSprites(canvasScaleFactor))
                    {
                        SVGRuntimeSprite newSprite = UIAtlas.GetRuntimeSprite(SpriteReference);
                        if (newSprite != null)
                        {
                            // NB: we change sprite only, anchors and position will be updated by the canvas
                            uiImage.sprite = newSprite.Sprite;
                        }
                    }
                }
            }
        }

        public void UpdateSprite()
        {
            Image uiImage = gameObject.GetComponent<Image>();
            // update/regenerate sprite, if requested
            if (((uiImage != null) && (UIAtlas != null) && (uiImage.canvas != null)))
            {
                UpdateSprite(uiImage.canvas.scaleFactor * UIAtlas.OffsetScale);
            }
        }

        void Start()
        {
            // update/regenerate sprite, if requested
            if (ResizeOnStart)
            {
                UpdateSprite();
            }
        }

    #if UNITY_EDITOR
        private bool RequirementsCheck()
        {
            // get list of attached components
            Component[] components = gameObject.GetComponents(GetType());
            // check for duplicate components
            foreach (Component component in components)
            {
                if (component == this)
                {
                    continue;
                }
                // show warning
                EditorUtility.DisplayDialog("Can't add the same component multiple times!",
                                            string.Format("The component {0} can't be added because {1} already contains the same component.", GetType(), gameObject.name),
                                            "Ok");
                // destroy the duplicate component
                DestroyImmediate(this);
            }

            Image uiImage = gameObject.GetComponent<Image>();
            if (uiImage == null)
            {
                EditorUtility.DisplayDialog("Incompatible game object",
                                            string.Format("In order to work properly, the component {0} requires the presence of a UI.Image component", GetType()),
                                            "Ok");
                return false;
            }

            return true;
        }

        // Reset is called when the user hits the Reset button in the Inspector's context menu or when adding the component the first time.
        // This function is only called in editor mode. Reset is most commonly used to give good default values in the inspector.
        void Reset()
        {
            if (RequirementsCheck())
            {
                UIAtlas = null;
                SpriteReference = null;
                ResizeOnStart = true;
                Image uiImage = gameObject.GetComponent<Image>();
                uiImage.type = Image.Type.Simple;
                uiImage.material = null;
                if (uiImage.canvas != null) {
                    // set the atlas associated to the canvas (see SVGCanvasBehaviour)
                    SVGCanvasBehaviour svgCanvas = uiImage.canvas.GetComponent<SVGCanvasBehaviour>();
                    if (svgCanvas != null)
                    {
                        UIAtlas = svgCanvas.UIAtlas;
                    }
                }
            }
        }
    #endif

        // Atlas generator for this sprite reference
        public SVGUIAtlas UIAtlas;
        // Sprite reference
        public SVGSpriteRef SpriteReference;
        // true if sprite must be regenerated at Start, else false
        public bool ResizeOnStart;
    }
}
