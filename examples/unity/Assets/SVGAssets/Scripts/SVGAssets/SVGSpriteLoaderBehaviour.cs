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
    public class SVGSpriteLoaderBehaviour : MonoBehaviour
    {
        private void FixLocalPosition(float deltaScale)
        {
            if (deltaScale > 0)
            {
                Vector3 newPos = transform.localPosition;
                newPos.x *= deltaScale;
                newPos.y *= deltaScale;
                transform.localPosition = newPos;
                m_OldPos = newPos;
                transform.hasChanged = false;
            }
        }

        private void UpdateSprite(int currentScreenWidth, int currentScreenHeight)
        {
            if ((Atlas != null) && (SpriteReference != null))
            {
                SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    if (Atlas.UpdateRuntimeSprites(currentScreenWidth, currentScreenHeight, out float scale))
                    {
                        SVGRuntimeSprite newSprite = Atlas.GetRuntimeSprite(SpriteReference);
                        if (newSprite != null)
                        {
                            renderer.sprite = newSprite.Sprite;
                            // fix for the first time the sprite is regenerated at runtime
                            if (m_RuntimeScale == 0)
                            {
                                m_RuntimeScale = Atlas.EditorGenerationScale;
                            }
                            // update local position
                            FixLocalPosition(newSprite.GenerationScale / m_RuntimeScale);
                            m_RuntimeScale = newSprite.GenerationScale;
                        }
                    }
                }
            }
        }

        public void UpdateSprite(bool updateChildren, int currentScreenWidth, int currentScreenHeight)
        {
            // regenerate (if needed) the sprite
            UpdateSprite(currentScreenWidth, currentScreenHeight);
        
            if (updateChildren)
            {
                int childCount = transform.childCount;
                // update children
                for (int i = 0; i < childCount; ++i)
                {
                    GameObject gameObj = transform.GetChild(i).gameObject;
                    SVGSpriteLoaderBehaviour loader = gameObj.GetComponent<SVGSpriteLoaderBehaviour>();
                    if (loader != null)
                    {
                        loader.UpdateSprite(updateChildren, currentScreenWidth, currentScreenHeight);
                    }
                }
            }
        }

        void Start()
        {
            // keep track of the current position
            m_OldPos = transform.localPosition;
            // update/regenerate sprite, if requested
            if (ResizeOnStart)
            {
                UpdateSprite((int)SVGAssetsUnity.ScreenWidth, (int)SVGAssetsUnity.ScreenHeight);
            }
        }
    
        void LateUpdate()
        {
            // when position has changed (due to an animation keyframe) we must rescale respect to the original factor, not the last used one
            if (UpdateTransform && transform.hasChanged)
            {
                Vector3 newPos = transform.localPosition;
                if (newPos != m_OldPos)
                {
                    // fix the local position according to the original scale (i.e. the scale used to generate sprites from within the Unity editor)
                    FixLocalPosition(m_RuntimeScale / Atlas.EditorGenerationScale);
                }
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

            SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                EditorUtility.DisplayDialog("Incompatible game object",
                                            string.Format("In order to work properly, the component {0} requires the presence of a SpriteRenderer component", GetType()),
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
                Atlas = null;
                SpriteReference = null;
                ResizeOnStart = true;
                UpdateTransform = true;
                SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
                renderer.sprite = null;
            }
        }
    #endif

        // Atlas generator the sprite reference to
        public SVGAtlas Atlas;
        // Sprite reference
        public SVGSpriteRef SpriteReference;
        // True if sprite must be regenerated at Start, else false
        public bool ResizeOnStart;
        // True if we have to fix (local) position according to the (updated) sprite dimensions, else false
        public bool UpdateTransform;
        // Keep track of last (local) position
        [NonSerialized]
        private Vector3 m_OldPos;
        // Keep track of the scale used by last runtime generation
        [NonSerialized]
        private float m_RuntimeScale = 0;
    }
}
