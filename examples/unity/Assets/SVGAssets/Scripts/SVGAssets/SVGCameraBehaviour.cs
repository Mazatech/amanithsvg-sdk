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
    [ExecuteInEditMode]
    public class SVGCameraBehaviour : MonoBehaviour
    {
        private void Resize(int screenWidth, int screenHeight, bool shotEvent)
        {
            Camera camera = GetComponent<Camera>();
            // map the camera rectangle to the whole screen; NB: we handle orthographic cameras only
            camera.aspect = (float)screenWidth / screenHeight;
            camera.orthographicSize = (screenHeight * camera.rect.height) / (SVGAssetsUnity.SPRITE_PIXELS_PER_UNIT * 2);
            // keep track of current device dimensions
            _lastScreenWidth = screenWidth;
            _lastScreenHeight = screenHeight;
            // call OnResize handlers
            if ((OnResize != null) && shotEvent)
            {
                OnResize(screenWidth, screenHeight);
            }
        }

        public void Resize(bool shotEvent)
        {
            // update camera aspect ratio and orthographic size
            Resize((int)SVGAssetsUnity.ScreenWidth, (int)SVGAssetsUnity.ScreenHeight, shotEvent);
        }

        public float PixelWidth
        {
            // get the camera viewport width, in pixels
            get
            {
                return _lastScreenWidth;
            }
        }
    
        public float PixelHeight
        {
            // get the camera viewport height, in pixels
            get
            {
                return _lastScreenHeight;
            }
        }

        public float WorldWidth
        {
            // get the camera viewport width, in world coordinates
            get
            {
                return PixelWidth / SVGAssetsUnity.SPRITE_PIXELS_PER_UNIT;
            }
        }
    
        public float WorldHeight
        {
            // get the camera viewport height, in world coordinates
            get
            {
                return PixelHeight / SVGAssetsUnity.SPRITE_PIXELS_PER_UNIT;
            }
        }

        void Start()
        {
            // set the camera so that its viewing volume coincides with the whole device screen (or with the GameView, if inside Editor)
            Resize(false);
        }
    
        void Update()
        {
            // get the current screen size
            int curScreenWidth = (int)SVGAssetsUnity.ScreenWidth;
            int curScreenHeight = (int)SVGAssetsUnity.ScreenHeight;

            // if screen size has changed (e.g. device orientation changed), fire the event
            if ((curScreenWidth != _lastScreenWidth) || (curScreenHeight != _lastScreenHeight))
            {
                // update camera aspect ratio and orthographic size
                Resize(curScreenWidth, curScreenHeight, Application.isPlaying);
            }
        }

    #if UNITY_EDITOR

        // this script works with orthographic cameras only
        private bool RequirementsCheck()
        {
            bool ok = true;
            Camera camera = GetComponent<Camera>();

            if (camera == null || (!camera.orthographic))
            {
                EditorUtility.DisplayDialog("Incompatible game object",
                                            string.Format("In order to work properly, the component {0} must be attached to an orthographic camera", GetType()),
                                            "Ok");
                DestroyImmediate(this);
                ok = false;
            }

            return ok;
        }

        // Reset is called when the user hits the Reset button in the Inspector's context menu or when adding the component the first time.
        // This function is only called in editor mode. Reset is most commonly used to give good default values in the inspector.
        void Reset()
        {
            RequirementsCheck();
        }

    #endif

        public delegate void OnResizeEvent(int newScreenWidth, int newScreenHeight);
        public event OnResizeEvent OnResize;
        // device screen dimensions
        [NonSerialized]
        private int _lastScreenWidth;
        [NonSerialized]
        private int _lastScreenHeight;
    }
}
