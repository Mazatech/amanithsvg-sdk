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
#if UNITY_2_6
    #define UNITY_2_X
    #define UNITY_2_PLUS
#elif UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5
    #define UNITY_3_X
    #define UNITY_2_PLUS
    #define UNITY_3_PLUS
#elif UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
    #define UNITY_4_X
    #define UNITY_2_PLUS
    #define UNITY_3_PLUS
    #define UNITY_4_PLUS
#elif UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_4_OR_NEWER
    #define UNITY_5_X
    #define UNITY_2_PLUS
    #define UNITY_3_PLUS
    #define UNITY_4_PLUS
    #define UNITY_5_PLUS
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

namespace SVGAssets
{
    public class SVGAtlas : SVGBasicAtlas
    {
    #if UNITY_EDITOR

        private void FixAnimationClip(AnimationClip clip, float deltaScaleX, float deltaScaleY)
        {
         #if UNITY_5_4_OR_NEWER
            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);

            foreach (var binding in curveBindings)
            {
                // check for keyframe animation of localPosition
                bool localPosX = (binding.propertyName.IndexOf("LocalPosition.x", StringComparison.OrdinalIgnoreCase) >= 0) ? true : false;
                bool localPosY = (binding.propertyName.IndexOf("LocalPosition.y", StringComparison.OrdinalIgnoreCase) >= 0) ? true : false;
                if (localPosX || localPosY)
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                    // get the scale factor
                    float deltaScale = localPosX ? deltaScaleX : deltaScaleY;

                    // "Note that the keys array is by value, i.e. getting keys returns a copy of all keys and setting keys copies them into the curve"
                    Keyframe[] keys = curve.keys;
                    for (int i = 0; i < keys.Length; ++i) {
                        keys[i].value *= deltaScale;
                        keys[i].inTangent *= deltaScale;
                        keys[i].outTangent *= deltaScale;
                    }
                    curve.keys = keys;
                    // set the new keys
                    clip.SetCurve(binding.path, typeof(Transform), binding.propertyName, curve);
                }
            }
        #else
            AnimationClipCurveData[] curves = AnimationUtility.GetAllCurves(clip, true);
            foreach (AnimationClipCurveData curveData in curves)
            {
                // check for keyframe animation of localPosition
                bool localPosX = (curveData.propertyName.IndexOf("LocalPosition.x", StringComparison.OrdinalIgnoreCase) >= 0) ? true : false;
                bool localPosY = (curveData.propertyName.IndexOf("LocalPosition.y", StringComparison.OrdinalIgnoreCase) >= 0) ? true : false;

                if (localPosX || localPosY)
                {
                    AnimationCurve curve = curveData.curve;
                    // get the scale factor
                    float deltaScale = localPosX ? deltaScaleX : deltaScaleY;

                    // "Note that the keys array is by value, i.e. getting keys returns a copy of all keys and setting keys copies them into the curve"
                    Keyframe[] keys = curve.keys;
                    for (int i = 0; i < keys.Length; ++i) {
                        keys[i].value *= deltaScale;
                        keys[i].inTangent *= deltaScale;
                        keys[i].outTangent *= deltaScale;
                    }
                    curve.keys = keys;
                    // set the new keys
                    clip.SetCurve(curveData.path, curveData.type, curveData.propertyName, curve);
                }
            }
        #endif
        }

        private void FixPositions(GameObject gameObj, float deltaScaleX, float deltaScaleY)
        {
            Vector3 newPos = gameObj.transform.localPosition;
            newPos.x *= deltaScaleX;
            newPos.y *= deltaScaleY;
            gameObj.transform.localPosition = newPos;

            // fix Animation components
            Animation[] animations = gameObj.GetComponents<Animation>();
            foreach (Animation animation in animations)
            {
                foreach (AnimationState animState in animation)
                {
                    if (animState.clip != null)
                    {
                        FixAnimationClip(animState.clip, deltaScaleX, deltaScaleY);
                    }
                }
            }

            // fix Animator components
            Animator[] animators = gameObj.GetComponents<Animator>();
            foreach (Animator animator in animators)
            {
                UnityEditor.Animations.AnimatorController animController = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            #if UNITY_5_PLUS
                for (int i = 0; i < animController.layers.Length; i++)
            #else
                for (int i = 0; i < animator.layerCount; i++)
            #endif
                {
                #if UNITY_5_PLUS
                    UnityEditor.Animations.AnimatorStateMachine stateMachine = animController.layers[i].stateMachine;
                    for (int j = 0; j < stateMachine.states.Length; j++)
                #else
                    UnityEditor.Animations.AnimatorStateMachine stateMachine = animController.GetLayer(i).stateMachine;
                    for (int j = 0; j < stateMachine.stateCount; j++)
                #endif
                    {
                    #if UNITY_5_PLUS
                        UnityEditor.Animations.ChildAnimatorState state = stateMachine.states[j];
                        Motion mtn = state.state.motion;
                    #else
                        UnityEditor.Animations.AnimatorState state = stateMachine.GetState(j);
                        Motion mtn = state.GetMotion();
                    #endif

                        if (mtn != null)
                        {
                            AnimationClip clip = mtn as AnimationClip;
                            FixAnimationClip(clip, deltaScaleX, deltaScaleY);
                        }
                    }
                }
            }
        }

        private void GetSpritesInstances(List<GameObject> spritesInstances)
        {
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
            foreach (GameObject gameObj in allObjects)
            {
                // check if the game object is an "SVG sprite" instance of this atlas generator
                if (gameObj.activeInHierarchy)
                {
                    SVGSpriteLoaderBehaviour loader = gameObj.GetComponent<SVGSpriteLoaderBehaviour>();
                    // we must be sure that the loader component must refer to this atlas
                    if ((loader != null) && (loader.Atlas == this))
                    {
                        // add this instance to the output lists
                        spritesInstances.Add(gameObj);
                    }
                }
            }
        }

        private void UpdateEditorSprites(float newScale)
        {
            // get the list of instantiated SVG sprites
            List<GameObject> spritesInstances = new List<GameObject>();
            GetSpritesInstances(spritesInstances);
            // regenerate the list of sprite locations
            m_GeneratedSpritesLists = new SVGSpritesListDictionary();

            if (m_SvgList.Count <= 0)
            {
                AssetDatabase.StartAssetEditing();
                // delete previously generated textures (i.e. get all GeneratedTextures entries and delete the relative files)
                DeleteTextures();
                // delete previously generated sprites (i.e. get all GeneratedSprites entries and delete the relative files)
                DeleteSprites();

                if (spritesInstances.Count > 0)
                {
                    bool remove = EditorUtility.DisplayDialog("Missing sprite!",
                                                              string.Format("{0} gameobjects reference sprites that do not exist anymore. Would you like to remove them from the scene?", spritesInstances.Count),
                                                              "Remove", "Keep");
                    if (remove)
                    {
                        DeleteGameObjects(spritesInstances);
                    }
                }
                AssetDatabase.StopAssetEditing();
                // input SVG list is empty, simply reset both hash
                m_SvgListHashOld = m_SvgListHashCurrent = "";
                return;
            }

            // generate textures and sprites
            List<Texture2D> textures = new List<Texture2D>();
            List<KeyValuePair<SVGSpriteRef, SVGSpriteData>> sprites = new List<KeyValuePair<SVGSpriteRef, SVGSpriteData>>();

            if (SVGRuntimeGenerator.GenerateSprites(// input
                                                    m_SvgList, m_MaxTexturesDimension, m_SpritesBorder, m_Pow2Textures, newScale, m_ClearColor, m_FastUpload, m_GeneratedSpritesFiles,
                                                    // output
                                                    textures, sprites, m_GeneratedSpritesLists))
            {
                int i, j;

                if ((m_EditorGenerationScale > 0) && (newScale != m_EditorGenerationScale))
                {
                    // calculate how much we have to scale (relative) positions
                    float deltaScale = newScale / m_EditorGenerationScale;
                    // fix objects positions and animations
                    foreach (GameObject gameObj in spritesInstances)
                    {
                        FixPositions(gameObj, deltaScale, deltaScale);
                    }
                }
                // keep track of the new generation scale
                m_EditorGenerationScale = newScale;

                AssetDatabase.StartAssetEditing();
                // delete previously generated textures (i.e. get all GeneratedTextures entries and delete the relative files)
                DeleteTextures();
                // delete previously generated sprites (i.e. get all GeneratedSprites entries and delete the relative files)
                DeleteSprites();
                // ensure the presence of needed subdirectories
                string atlasesPath = CreateOutputFolders();
                string texturesDir = atlasesPath + "/Textures/";
                string spritesDir = atlasesPath + "/Sprites/";
                // save new texture assets
                i = 0;
                foreach (Texture2D texture in textures)
                {
                    string textureFileName = texturesDir + "texture" + i + ".asset";
                    // save texture
                    AssetDatabase.CreateAsset(texture, textureFileName);

                    // DEBUG STUFF
                    //byte[] pngData = texture.EncodeToPNG();
                    //if (pngData != null)
                    //  System.IO.File.WriteAllBytes(texturesDir + "texture" + i + ".png", pngData);

                    // keep track of the saved texture
                    m_GeneratedTexturesFiles.Add(new AssetFile(textureFileName, texture));
                    i++;
                }
                // save sprite assets
                j = sprites.Count;
                for (i = 0; i < j; ++i)
                {
                    // get sprite reference and its pivot
                    SVGSpriteRef spriteRef = sprites[i].Key;
                    SVGSpriteData spriteData = sprites[i].Value;

                    // build sprite file name
                    string spriteFileName = spritesDir + spriteData.Sprite.name + ".asset";
                    // save sprite asset
                    AssetDatabase.CreateAsset(spriteData.Sprite, spriteFileName);
                    // keep track of the saved sprite and its pivot
                    m_GeneratedSpritesFiles.Add(spriteRef, new SVGSpriteAssetFile(spriteFileName, spriteRef, spriteData));
                }
                AssetDatabase.StopAssetEditing();

                // for already instantiated (SVG) game object, set the new sprites
                // in the same loop we keep track of those game objects that reference missing sprites (i.e. sprites that do not exist anymore)
                List<GameObject> missingSpriteObjs = new List<GameObject>();
                foreach (GameObject gameObj in spritesInstances)
                {
                    SVGSpriteLoaderBehaviour spriteLoader = gameObj.GetComponent<SVGSpriteLoaderBehaviour>();
                
                    if (spriteLoader.SpriteReference.TxtAsset != null)
                    {
                        if (m_GeneratedSpritesFiles.TryGetValue(spriteLoader.SpriteReference, out SVGSpriteAssetFile spriteAsset))
                        {
                            // link the new sprite to the renderer
                            SpriteRenderer renderer = gameObj.GetComponent<SpriteRenderer>();
                            if (renderer != null)
                            {
                                SVGSpriteData spriteData = spriteAsset.SpriteData;
                                // assign the new sprite
                                renderer.sprite = spriteData.Sprite;
                                // NB: existing instances do not change sorting order!
                            }
                        }
                        else
                            missingSpriteObjs.Add(gameObj);
                    }
                }

                if (missingSpriteObjs.Count > 0)
                {
                    bool remove = EditorUtility.DisplayDialog("Missing sprite!",
                                                              string.Format("{0} gameobjects reference sprites that do not exist anymore. Would you like to remove them from the scene?", missingSpriteObjs.Count),
                                                              "Remove", "Keep");
                    if (remove)
                    {
                        DeleteGameObjects(missingSpriteObjs);
                    }
                }

                // now SVG documents are instantiable
                foreach (SVGAssetInput svgAsset in m_SvgList)
                {
                    svgAsset.Instantiable = true;
                }
                // keep track of the new hash
                m_SvgListHashOld = m_SvgListHashCurrent;
            }
        }

        // used by SVGAtlasEditor, upon the "Update" button click (recalcScale = true); this function is used even
        // in the build post-processor, in order to restore sprites (recalcScale = false)
        public void UpdateEditorSprites(bool recalcScale)
        {
            float newScale;

            if (recalcScale)
            {
                Vector2 gameViewRes = SVGUtils.GetGameView();

                float currentWidth = (m_DeviceTestWidth <= 0) ? gameViewRes.x : m_DeviceTestWidth;
                float currentHeight = (m_DeviceTestHeight <= 0) ? gameViewRes.y : m_DeviceTestHeight;
                newScale = SVGRuntimeGenerator.ScaleFactorCalc(m_ReferenceWidth, m_ReferenceHeight, currentWidth, currentHeight, m_ScaleType, m_Match, m_OffsetScale);
            }
            else
            {
                newScale = m_EditorGenerationScale;
            }

            UpdateEditorSprites(newScale);
        }

        private static void UpdatePivotHierarchy(GameObject gameObj, Vector2 delta, uint depthLevel)
        {
            SVGSpriteLoaderBehaviour loader = gameObj.GetComponent<SVGSpriteLoaderBehaviour>();

            if (loader != null)
            {
                Vector2 realDelta = (depthLevel > 0) ? (new Vector2(-delta.x, -delta.y)) : (new Vector2(delta.x * gameObj.transform.localScale.x, delta.y * gameObj.transform.localScale.y));
                Vector2 newPos = new Vector2(gameObj.transform.localPosition.x + realDelta.x, gameObj.transform.localPosition.y + realDelta.y);
                // modify the current node
                gameObj.transform.localPosition = newPos;
            }

            // traverse children
            int j = gameObj.transform.childCount;
            for (int i = 0; i < j; ++i)
            {
                GameObject child = gameObj.transform.GetChild(i).gameObject;
                UpdatePivotHierarchy(child, delta, depthLevel + 1);
            }
        }

        public void UpdatePivot(SVGSpriteAssetFile spriteAsset, Vector2 newPivot)
        {
            SVGSpriteRef spriteRef = spriteAsset.SpriteRef;
            SVGSpriteData spriteData = spriteAsset.SpriteData;
            Sprite oldSprite = spriteData.Sprite;
            // keep track of pivot movement
            Vector2 deltaPivot = newPivot - spriteData.Pivot;
            Vector2 deltaMovement = (new Vector2(deltaPivot.x * oldSprite.rect.width, deltaPivot.y * oldSprite.rect.height)) / SVGAssetsUnity.SPRITE_PIXELS_PER_UNIT;
            // create a new sprite (same texture, same rectangle, different pivot)
            Sprite newSprite = SVGAssetsUnity.CreateSprite(oldSprite.texture, oldSprite.rect, newPivot, spriteData.Border);
            GameObject[] allObjects = FindObjectsOfType<GameObject>();

            foreach (GameObject gameObj in allObjects)
            {
                if (gameObj.activeInHierarchy)
                {
                    SVGSpriteLoaderBehaviour loader = gameObj.GetComponent<SVGSpriteLoaderBehaviour>();
                    // we must be sure that the loader component must refer to this atlas
                    if ((loader != null) && (loader.Atlas == this))
                    {
                        // check if the instance uses the specified sprite
                        if (loader.SpriteReference.TxtAsset == spriteRef.TxtAsset && loader.SpriteReference.ElemIdx == spriteRef.ElemIdx)
                        {
                            UpdatePivotHierarchy(gameObj, deltaMovement, 0);
                        }
                    }
                }
            }

            spriteData.Pivot = newPivot;
            newSprite.name = oldSprite.name;
            EditorUtility.CopySerialized(newSprite, oldSprite);
            SVGUtils.MarkObjectDirty(oldSprite);
            // destroy the temporary sprite
            DestroyImmediate(newSprite);
        }

        private void SortingOrdersCompact(SVGAssetInput svgAsset)
        {
            List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();
            // get the list of instantiated sprites relative to this atlas generator
            List<GameObject> spritesInstances = new List<GameObject>();
            GetSpritesInstances(spritesInstances);
        
            foreach (GameObject gameObj in spritesInstances)
            {
                SVGSpriteLoaderBehaviour spriteLoader = gameObj.GetComponent<SVGSpriteLoaderBehaviour>();
                SVGSpriteRef spriteRef = spriteLoader.SpriteReference;
                // if the sprite belongs to the specified SVG asset input, keep track of it
                if (spriteRef.TxtAsset == svgAsset.TxtAsset)
                {
                    SpriteRenderer renderer = gameObj.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        spriteRenderers.Add(renderer);
                    }
                }
            }

            if (spriteRenderers.Count > 0)
            {
                // order the list by current sorting order
                spriteRenderers.Sort(delegate (SpriteRenderer renderer1, SpriteRenderer renderer2)
                {
                    if (renderer1.sortingOrder < renderer2.sortingOrder)
                    {
                        return -1;
                    }
                    if (renderer1.sortingOrder > renderer2.sortingOrder)
                    {
                        return 1;
                    }
                    return 0;
                });

                int j = spriteRenderers.Count;
                for (int i = 0; i < j; ++i)
                {
                    SpriteRenderer renderer = spriteRenderers[i];
                    int currentOrder = renderer.sortingOrder;
                    // isolate high part
                    int svgIndex = currentOrder & SPRITES_SORTING_DOCUMENTS_MASK;
                    // assign the new order
                    renderer.sortingOrder = SortingOrderCalc(svgIndex, i);
                }
                svgAsset.InstanceBaseIdx = j;
            }
            else
            {
                // there are no sprite instances relative to the specified SVG, so we can start from 0
                svgAsset.InstanceBaseIdx = 0;
            }
        }

        private void ResetGroupFlags(SVGSpritesList spritesList)
        {
            // now we can unflag sprites
            foreach (SVGSpriteRef spriteRef in spritesList.Sprites)
            {
                // get sprite and its data
                if (m_GeneratedSpritesFiles.TryGetValue(spriteRef, out SVGSpriteAssetFile spriteAsset))
                {
                    SVGSpriteData spriteData = spriteAsset.SpriteData;
                    spriteData.InCurrentInstancesGroup = false;
                }
            }
        }

        private bool InstancesGroupWrap(SVGAssetInput svgAsset, int spritesCount)
        {
            int rangeLo = svgAsset.InstanceBaseIdx;
            int rangeHi = rangeLo + spritesCount;
            return (rangeHi >= SPRITES_SORTING_MAX_INSTANCES) ? true : false;
        }

        private void NextInstancesGroup(SVGAssetInput svgAsset, SVGSpritesList spritesList, int instantiationCount)
        {
            int spritesCount = spritesList.Sprites.Count;

            svgAsset.InstanceBaseIdx += spritesCount;
            if (InstancesGroupWrap(svgAsset, spritesCount))
            {
                // try to compact used sorting orders (looping game objects that reference this svg)
                SortingOrdersCompact(svgAsset);

                // after compaction, if the instantiation of one or all sprites belonging to the new instances group will wrap
                // we have two options:
                //
                // 1. to instantiate sprites in the normal consecutive way, wrapping aroung SPRITES_SORTING_MAX_INSTANCES: in this case a part of sprites will
                // result (sortingOrder) consistent, but the whole sprites group won't
                //
                // 2. to reset the base index to 0 and generate the sprites according to their natural z-order: in this case the whole sprites group will
                // be (sortingOrder) consistent, but it is not granted to be totally (z)separated from other sprites/instances
                //

                if (InstancesGroupWrap(svgAsset, spritesCount))
                {
                    svgAsset.InstanceBaseIdx = 0;
                    /*
                    // option 2
                    if (instantiationCount > 1)
                        packedSvg.InstanceBaseIdx = 0;
                    // for single sprite instantiation we implicitly use option 1
                    */
                }
            }

            // now we can unflag sprites
            ResetGroupFlags(spritesList);
        }

        private static int SortingOrderCalc(int svgIndex, int instance)
        {
            svgIndex = svgIndex % SPRITES_SORTING_MAX_DOCUMENTS;
            instance = instance % SPRITES_SORTING_MAX_INSTANCES;

            return ((svgIndex << SPRITES_SORTING_INSTANCES_BITS) + instance);
        }

        private static int SortingOrderCalc(int svgIndex, int instanceBaseIdx, int zOrder)
        {
            return SortingOrderCalc(svgIndex, instanceBaseIdx + zOrder);
        }

        private int SortingOrderGenerate(SVGSpriteAssetFile spriteAsset)
        {
            if (spriteAsset != null)
            {
                SVGSpriteRef spriteRef = spriteAsset.SpriteRef;
                SVGSpriteData spriteData = spriteAsset.SpriteData;

                int svgIndex = SvgAssetIndexGet(spriteRef.TxtAsset);
                if (svgIndex >= 0)
                {
                    SVGAssetInput svgAsset = m_SvgList[svgIndex];

                    // if needed, advance in the instances group
                    if (spriteData.InCurrentInstancesGroup)
                    {
                        // get the list of sprites (references) relative to the SVG input asset
                        if (m_GeneratedSpritesLists.TryGetValue(svgAsset.TxtAsset.GetInstanceID(), out SVGSpritesList spritesList))
                        {
                            // advance instances group, telling that we are going to instantiate one sprite only
                            NextInstancesGroup(svgAsset, spritesList, 1);
                        }
                    }
                    return SortingOrderCalc(svgIndex, svgAsset.InstanceBaseIdx, spriteData.ZOrder);
                }
            }
            return -1;
        }

        // recalculate sorting orders of instantiated sprites: changing is due only to SVG index, so the lower part (group + zNatural) is left unchanged
        private void SortingOrdersUpdateSvgIndex()
        {
            // get the list of instantiated sprites relative to this atlas generator
            List<GameObject> spritesInstances = new List<GameObject>();
            GetSpritesInstances(spritesInstances);
        
            foreach (GameObject gameObj in spritesInstances)
            {
                SVGSpriteLoaderBehaviour spriteLoader = gameObj.GetComponent<SVGSpriteLoaderBehaviour>();
                SpriteRenderer renderer = gameObj.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    SVGSpriteRef spriteRef = spriteLoader.SpriteReference;
                    int svgIndex = SvgAssetIndexGet(spriteRef.TxtAsset);
                    if (svgIndex >= 0)
                    {
                        int instance = renderer.sortingOrder & SPRITES_SORTING_INSTANCES_MASK;
                        renderer.sortingOrder = SortingOrderCalc(svgIndex, instance);
                    }
                }
            }
        }

        private GameObject Instantiate(SVGSpriteAssetFile spriteAsset, int sortingOrder)
        {
            SVGSpriteRef spriteRef = spriteAsset.SpriteRef;
            SVGSpriteData spriteData = spriteAsset.SpriteData;
            GameObject gameObj = new GameObject(spriteData.Sprite.name);
            SpriteRenderer renderer = gameObj.AddComponent<SpriteRenderer>();
            SVGSpriteLoaderBehaviour spriteLoader = gameObj.AddComponent<SVGSpriteLoaderBehaviour>();
            renderer.sprite = spriteData.Sprite;
            renderer.sortingOrder = sortingOrder;
            spriteLoader.Atlas = this;
            spriteLoader.SpriteReference = spriteRef;
            spriteLoader.ResizeOnStart = true;
            spriteData.InCurrentInstancesGroup = true;
            return gameObj;
        }

        private GameObject InstantiateSprite(SVGSpriteAssetFile spriteAsset, Vector3 worldPos, int sortingOrder)
        {
            GameObject gameObj = Instantiate(spriteAsset, sortingOrder);
            // assign world position
            gameObj.transform.position = worldPos;
            return gameObj;
        }

        public GameObject InstantiateSprite(SVGSpriteRef spriteRef, Vector3 worldPos)
        {
            if (m_GeneratedSpritesFiles.TryGetValue(spriteRef, out SVGSpriteAssetFile spriteAsset))
            {
                int sortingOrder = SortingOrderGenerate(spriteAsset);
                GameObject gameObj = Instantiate(spriteAsset, sortingOrder);
                // assign world position
                gameObj.transform.position = worldPos;
                return gameObj;
            }
            return null;
        }

        public GameObject InstantiateSprite(SVGSpriteRef spriteRef)
        {
            if (m_GeneratedSpritesFiles.TryGetValue(spriteRef, out SVGSpriteAssetFile spriteAsset))
            {
                int sortingOrder = SortingOrderGenerate(spriteAsset);
                return Instantiate(spriteAsset, sortingOrder);
            }
            return null;
        }

        public GameObject[] InstantiateGroups(SVGAssetInput svgAsset)
        {
            List<GameObject> instances = new List<GameObject>();

            if ((svgAsset != null) && m_GeneratedSpritesLists.TryGetValue(svgAsset.TxtAsset.GetInstanceID(), out SVGSpritesList spritesList))
            {
                int spritesCount = spritesList.Sprites.Count;
                int svgIndex = SvgAssetIndexGet(svgAsset.TxtAsset);

                if ((svgIndex >= 0) && (spritesCount > 0))
                {
                    bool advanceInstancesGroup = false;
                    // list of sprite assets (file) relative to the specified SVG; in this case we can set the right list capacity
                    List<SVGSpriteAssetFile> spriteAssets = new List<SVGSpriteAssetFile>(spritesCount);

                    foreach (SVGSpriteRef spriteRef in spritesList.Sprites)
                    {
                        if (m_GeneratedSpritesFiles.TryGetValue(spriteRef, out SVGSpriteAssetFile spriteAsset))
                        {
                            SVGSpriteData spriteData = spriteAsset.SpriteData;

                            // if there is a single sprite already instantiated in the current group, we have to advance in the next instances group
                            if (spriteData.InCurrentInstancesGroup)
                            {
                                advanceInstancesGroup = true;
                            }
                            // keep track of this sprite asset
                            spriteAssets.Add(spriteAsset);
                        }
                    }

                    if (spriteAssets.Count > 0)
                    {
                        if (advanceInstancesGroup)
                        {
                            // advance in the instances group, telling that we are going to instantiate N sprites
                            NextInstancesGroup(svgAsset, spritesList, spriteAssets.Count);
                        }

                        foreach (SVGSpriteAssetFile spriteAsset in spriteAssets)
                        {
                            SVGSpriteData spriteData = spriteAsset.SpriteData;
                            Sprite sprite = spriteData.Sprite;
                            Vector2 pivot = spriteData.Pivot;
                            //float scl = 1 / spriteData.Scale;
                            float scl = 1;
                            // make sure that the center of "destination viewport" of the original document
                            // will correspond to (0, 0) in world space; in other words, if a sprite pivot
                            // point is located at the center of "destination viewport", then it will be
                            // (0, 0) in world space
                            float px = (((sprite.rect.width * pivot.x) + spriteData.OriginalX) * scl) - (spriteData.DstViewportWidth / 2);
                            float py = (spriteData.DstViewportHeight / 2) - (((sprite.rect.height * (1 - pivot.y)) + spriteData.OriginalY) * scl);
                            Vector2 worldPos = new Vector2(px / SVGAssetsUnity.SPRITE_PIXELS_PER_UNIT, py / SVGAssetsUnity.SPRITE_PIXELS_PER_UNIT);
                            // instantiate the object
                            int sortingOrder = SortingOrderCalc(svgIndex, svgAsset.InstanceBaseIdx, spriteData.ZOrder);
                            // instantiate the sprite
                            GameObject newObj = InstantiateSprite(spriteAsset, worldPos, sortingOrder);
                            newObj.transform.localScale = new Vector3(scl, scl, 1);
                            spriteData.InCurrentInstancesGroup = true;
                            // keep track of this new instance
                            instances.Add(newObj);
                        }
                    }
                }
            }

            // return the created instances
            return instances.ToArray();
        }

        protected override bool SvgAssetAdd(TextAsset newSvg, int index, bool alreadyExist)
        {
            if (alreadyExist)
            {
                // show warning
                EditorUtility.DisplayDialog("Can't add the same SVG file multiple times!",
                                            string.Format("The list of SVG assets already contains the {0} file.", newSvg.name),
                                            "Ok");
                return false;
            }
            else
            {
                if (m_SvgList.Count < SPRITES_SORTING_MAX_DOCUMENTS)
                {
                    return true;
                }
                else
                {
                    // show warning
                    EditorUtility.DisplayDialog("Can't add the SVG file, slots full!",
                                                string.Format("SVG list cannot exceed its maximum capacity of {0} entries. Try to merge some SVG files.", SPRITES_SORTING_MAX_DOCUMENTS),
                                                "Ok");

                    return false;
                }
            }
        }

        public override bool SvgAssetMove(SVGAssetInput svgAsset, int toIndex)
        {
            int fromIndex = SvgAssetIndexGet(svgAsset.TxtAsset);
            bool moved = SvgAssetMove(svgAsset, fromIndex, toIndex);

            if (moved)
            {
                // recalculate sorting orders of instantiated sprites
                SortingOrdersUpdateSvgIndex();
            }
            return moved;
        }

        // return true if the atlas needs an update (i.e. a call to UpdateSprites), else false
        protected override string CalcAtlasHash()
        {
            int count = m_SvgList.Count;

            if (count > 0)
            {
                // we want the parameters string to come always in front (when sorted)
                string[] hashList = new string[count + 1];
                // parameters string
                string paramsStr = "#*";
                paramsStr += m_ReferenceWidth + "-";
                paramsStr += m_ReferenceHeight + "-";
                paramsStr += m_DeviceTestWidth + "-";
                paramsStr += m_DeviceTestHeight + "-";
                paramsStr += m_ScaleType + "-";
                paramsStr += m_Match + "-";
                paramsStr += m_OffsetScale + "-";
                paramsStr += m_Pow2Textures + "-";
                paramsStr += m_MaxTexturesDimension + "-";
                paramsStr += m_SpritesBorder + "-";
                paramsStr += m_ClearColor.ToString() + "-";
                paramsStr += FullOutputFolder;
                hashList[0] = paramsStr;
                // for each input SVG row we define an "id string"
                for (int i = 0; i < count; ++i)
                {
                    hashList [i + 1] = m_SvgList[i].Hash();
                }
                // sort strings, so we can be SVG rows order independent
                Array.Sort(hashList);
                // return MD5 hash
                return MD5Calc(string.Join("-", hashList));
            }
            return "";
        }

    #endif // UNITY_EDITOR

        // Calculate the scale factor that would be used to generate sprites if the screen would have the specified dimensions
        public float ScaleFactorCalc(int currentScreenWidth, int currentScreenHeight)
        {
            return SVGRuntimeGenerator.ScaleFactorCalc(m_ReferenceWidth, m_ReferenceHeight, currentScreenWidth, currentScreenHeight, m_ScaleType, m_Match, m_OffsetScale);
        }

        // Generate a sprite set, according to specified screen dimensions; return true if case of success, else false
        public bool GenerateSprites(int currentScreenWidth, int currentScreenHeight, List<Texture2D> textures, List<KeyValuePair<SVGSpriteRef, SVGSpriteData>> sprites)
        {
            float scale = ScaleFactorCalc(currentScreenWidth, currentScreenHeight);
            return GenerateSprites(scale, textures, sprites);
        }

        // return true if case of success, else false
        public bool UpdateRuntimeSprites(int currentScreenWidth, int currentScreenHeight, out float scale)
        {
            float newScale = SVGRuntimeGenerator.ScaleFactorCalc(m_ReferenceWidth, m_ReferenceHeight, currentScreenWidth, currentScreenHeight, m_ScaleType, m_Match, m_OffsetScale);

            if (Math.Abs(m_RuntimeGenerationScale - newScale) > float.Epsilon)
            {
                scale = newScale;
                return UpdateRuntimeSprites(newScale);
            }
            else
            {
                scale = m_RuntimeGenerationScale;
                return true;
            }
        }

        void OnEnable()
        {
            Initialize();

            if (m_ReferenceWidth == 0)
            {
                m_ReferenceWidth = (int)SVGAssetsUnity.ScreenWidth;
            }

            if (m_ReferenceHeight == 0)
            {
                m_ReferenceHeight = (int)SVGAssetsUnity.ScreenHeight;
            }

        #if UNITY_EDITOR
            if (m_DeviceTestWidth == 0)
            {
                m_DeviceTestWidth = m_ReferenceWidth;
                }

            if (m_DeviceTestHeight == 0)
            {
                m_DeviceTestHeight = m_ReferenceHeight;
            }
        #endif
        }

        public int ReferenceWidth
        {
            get
            {
                return m_ReferenceWidth;
            }
            set
            {
                m_ReferenceWidth = value;
            #if UNITY_EDITOR
                UpdateAtlasHash();
            #endif
            }
        }

        public int ReferenceHeight
        {
            get
            {
                return m_ReferenceHeight;
            }
            set
            {
                m_ReferenceHeight = value;
            #if UNITY_EDITOR
                UpdateAtlasHash();
            #endif
            }
        }

    #if UNITY_EDITOR
        public int DeviceTestWidth
        {
            get
            {
                return m_DeviceTestWidth;
            }
            set
            {
                m_DeviceTestWidth = value;
                UpdateAtlasHash();
            }
        }
    
        public int DeviceTestHeight
        {
            get
            {
                return m_DeviceTestHeight;
            }
            set
            {
                m_DeviceTestHeight = value;
                UpdateAtlasHash();
            }
        }
    #endif

        public SVGScalerMatchMode ScaleType
        {
            get
            {
                return m_ScaleType;
            }
            set
            {
                m_ScaleType = value;
            #if UNITY_EDITOR
                UpdateAtlasHash();
            #endif
            }
        }

        public float Match
        {
            get
            {
                return m_Match;
            }
            set
            {
                m_Match = value;
            #if UNITY_EDITOR
                UpdateAtlasHash();
            #endif
            }
        }

        // Scale adaption
        [SerializeField]
        private int m_ReferenceWidth = 0;
        [SerializeField]
        private int m_ReferenceHeight = 0;
    #if UNITY_EDITOR
        [SerializeField]
        private int m_DeviceTestWidth = 0;
        [SerializeField]
        private int m_DeviceTestHeight = 0;
    #endif
        [SerializeField]
        private SVGScalerMatchMode m_ScaleType = SVGScalerMatchMode.MatchWidthOrHeight;
        [SerializeField]
        private float m_Match = 0.5f;

        // number of bits (sortingOrder) dedicated to SVG index
        public const int SPRITES_SORTING_DOCUMENTS_BITS = 4;
        // number of bits (sortingOrder) dedicated to instance index + zOrder
        public const int SPRITES_SORTING_INSTANCES_BITS = (15 - SPRITES_SORTING_DOCUMENTS_BITS);
        // Isolate the sortingOrder high part
        public const int SPRITES_SORTING_DOCUMENTS_MASK = (((1 << SPRITES_SORTING_DOCUMENTS_BITS) - 1) << SPRITES_SORTING_INSTANCES_BITS);
        // Isolate the sortingOrder low part
        public const int SPRITES_SORTING_INSTANCES_MASK = ((1 << SPRITES_SORTING_INSTANCES_BITS) - 1);
        // Max number of SVG inputs/row
        public const int SPRITES_SORTING_MAX_DOCUMENTS = (1 << SPRITES_SORTING_DOCUMENTS_BITS);
        // Max number of sprites instances for each SVG document
        public const int SPRITES_SORTING_MAX_INSTANCES = (1 << SPRITES_SORTING_INSTANCES_BITS);
    }
}
