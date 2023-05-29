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
#if UNITY_EDITOR
    using UnityEditor;
#endif

namespace SVGAssets
{
    [ExecuteInEditMode]
    public class SVGCanvasBehaviour : MonoBehaviour
    {
        public void EnsureCanvasAssigned()
        {
            if (m_UIAtlas != null)
            {
                Canvas canvas = GetComponent<Canvas>();
                if (canvas != null)
                {
                    m_UIAtlas.CanvasScaleFactor = canvas.scaleFactor;
                    // we assign the Name property, so we can check if the canvas object has been renamed by the user
                    m_UIAtlas.Name = canvas.name;
                }
            }
        }

        void Start()
        {
            EnsureCanvasAssigned();
        }
    
        void Update()
        {
        }

        public SVGUIAtlas UIAtlas
        {
            get
            {
                return m_UIAtlas;
            }
        }

    #if UNITY_EDITOR
        // this script works with orthographic cameras only
        private bool RequirementsCheck()
        {
            if (GetComponent<Canvas>() == null)
            {
                EditorUtility.DisplayDialog("Incompatible game object",
                                            string.Format("In order to work properly, the component {0} must be attached to a canvas", GetType()),
                                            "Ok");
                DestroyImmediate(this);
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
                if (m_UIAtlas == null)
                {
                    m_UIAtlas = ScriptableObject.CreateInstance<SVGUIAtlas>();
                    m_UIAtlas.OutputFolder = SVGCanvasBehaviour.LastOutputFolder;
                    EnsureCanvasAssigned();
                }
            }
        }
    #endif

        [SerializeField]
        private SVGUIAtlas m_UIAtlas = null;

    #if UNITY_EDITOR
        [SerializeField]
        static public string LastOutputFolder = "Assets";
    #endif
    }
}
