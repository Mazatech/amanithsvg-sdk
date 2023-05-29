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
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using UnityEditor;
#endif

namespace SVGAssets
{
        [Serializable]
    public class SVGAssetInput
    {
        // Constructor.
        public SVGAssetInput(TextAsset txtAsset, float scale, bool explodeGroups)
        {
            TxtAsset = txtAsset;
            Scale = scale;
            SeparateGroups = explodeGroups;
            Instantiable = false;
            InstanceBaseIdx = 0;
        }

    #if UNITY_EDITOR
        public string Hash()
        {
            string assetpath = AssetDatabase.GetAssetPath(TxtAsset);
            FileInfo fileInfo = new FileInfo(assetpath);
            return AssetDatabase.GetAssetPath(TxtAsset) + "_" + fileInfo.Length.ToString() + "_" + Scale.ToString() + "_" + SeparateGroups.ToString();
        }
    #endif
    
        // Text asset containing the svg xml.
        public TextAsset TxtAsset;
        // An additional scale offset.
        public float Scale;
        // If true, it tells the packer to not pack the whole SVG document, but instead to pack each first-level element separately.
        public bool SeparateGroups;
        // True if the document can be instantiated (i.e. groups of the whole rootmost <svg> element)
        public bool Instantiable;
        // Base index for instances to be created.
        public int InstanceBaseIdx;
    }

    // a link between a packed SVG and the relative SVG document
    public class PackedSvgAssetDocLink
    {
        // Constructor.
        public PackedSvgAssetDocLink(SVGAssetInput svgAsset, SVGDocument document)
        {
            Asset = svgAsset;
            Document = document;
        }
    
        public SVGAssetInput Asset { get; }
        public SVGDocument Document { get; }
    }
    
    // Reference counting on a single SVG document
    public class PackedSvgDocRef
    {
        // Constructor
        public PackedSvgDocRef(SVGDocument svgDoc, TextAsset txtAsset)
        {
            Document = svgDoc;
            TxtAsset = txtAsset;
            // SVGRenameImporter substitutes the ".svg" file extension with ".svg.txt" one (e.g. orc.svg --> orc.svg.txt)
            // Unity assets explorer does not show the last ".txt" postfix (e.g. in the editor we see orc.svg without the last .txt trait)
            // txtAsset.name does not contain the last ".txt" trait, but it still contains the ".svg"; at this level, we want to remove even that
            Name = txtAsset.name.Replace(".svg", "");
            RefCount = 0;
        }
    
        // Add references
        public uint Inc(uint increment)
        {
            RefCount += increment;
            return RefCount;
        }
    
        // Remove references
        public uint Dec(uint decrement)
        {
            if (RefCount >= decrement)
            {
                RefCount -= decrement;
            }
            else
            {
                RefCount = 0;
            }

            return RefCount;
        }
    
        // The referenced SVG document
        internal SVGDocument Document { get; }

        // Text asset
        internal TextAsset TxtAsset { get; }

        // Current reference count
        internal uint RefCount { get; private set; }

        // Document (short) name
        internal string Name { get; }
    }

    [Serializable]
    public class SVGSpriteData
    {
        // Constructor.
        public SVGSpriteData(Sprite sprite,
                             Vector2 pivot,
                             Vector4 border,
                             int zOrder,
                             int originalX,
                             int originalY,
                             float dstViewportWidth,
                             float dstViewportHeight,
                             float generationScale,
                             bool inCurrentInstancesGroup)
        {
            Sprite = sprite;
            Pivot = SVGUtils.FixVector2(pivot);
            Border = SVGUtils.FixVector4(border);
            ZOrder = zOrder;
            OriginalX = originalX;
            OriginalY = originalY;
            DstViewportWidth = dstViewportWidth;
            DstViewportHeight = dstViewportHeight;
            GenerationScale = generationScale;
            InCurrentInstancesGroup = inCurrentInstancesGroup;
        }

        public Sprite Sprite;
        public Vector2 Pivot;
        public Vector4 Border;
        // Z-order
        public int ZOrder;
        // Original rectangle corner
        public int OriginalX;
        public int OriginalY;
        // The used destination viewport width (induced by packing scale factor)
        public float DstViewportWidth;
        // The used destination viewport height (induced by packing scale factor)
        public float DstViewportHeight;
        // The scale factor used to generate this sprite
        public float GenerationScale;
        public bool InCurrentInstancesGroup;
    }

    [Serializable]
    public class SVGSpriteRef
    {
        // Constructor.
        public SVGSpriteRef(TextAsset txtAsset, int elemIdx)
        {
            TxtAsset = txtAsset;
            ElemIdx = elemIdx;
        }

        public bool Equals(SVGSpriteRef other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            // we are referencing the same sprite if the xml text asset is the same and element id matches
            return ((TxtAsset == other.TxtAsset) && (ElemIdx == other.ElemIdx)) ? true : false;
        }
    
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != typeof(SVGSpriteRef))
            {
                return false;
            }
            return Equals((SVGSpriteRef)obj);
        }
    
        public override int GetHashCode()
        {
            unchecked
            {
                if (TxtAsset != null)
                {
                    int res = ((TxtAsset.GetHashCode() * 397) ^ ElemIdx);
                    return res;
                }
                return 0;
            }
        }

        public TextAsset TxtAsset;
        public int ElemIdx; 
    }

    [Serializable]
    public class AssetFile
    {
        // Constructor.
        public AssetFile(string path, UnityEngine.Object obj)
        {
            Path = path;
            Object = obj;
        }

        public string Path;
        public UnityEngine.Object Object;
    }

    [Serializable]
    public class SVGSpriteAssetFile
    {
        // Constructor.
        public SVGSpriteAssetFile(string path, SVGSpriteRef spriteRef, SVGSpriteData spriteData)
        {
            Path = path;
            SpriteRef = spriteRef;
            SpriteData = spriteData;
        #if UNITY_EDITOR
            InstantiatedWidgetType = SVGUIWidgetType.Button;
        #endif
        }
    
        public string Path;
        public SVGSpriteRef SpriteRef;
        public SVGSpriteData SpriteData;
    #if UNITY_EDITOR
        public SVGUIWidgetType InstantiatedWidgetType;
    #endif
    }

    // A list of sprites relative to an SVG document
    [Serializable]
    public class SVGSpritesList
    {
        // Constructor
        public SVGSpritesList()
        {
            Sprites = new List<SVGSpriteRef>();
        }

        [SerializeField]
        public List<SVGSpriteRef> Sprites;
    }

    [Serializable]
    public class SVGSpritesDictionary : SerializableDictionary<SVGSpriteRef, SVGSpriteAssetFile>
    {
    }

    // Given a text asset (i.e. the SVG file) instance id, get the list of sprites and their original location
    [Serializable]
    public class SVGSpritesListDictionary : SerializableDictionary<int, SVGSpritesList>
    {
    }

    public class SVGRuntimeSprite
    {
        public SVGRuntimeSprite(Sprite sprite, float generationScale, SVGSpriteRef spriteReference)
        {
            Sprite = sprite;
            GenerationScale = generationScale;
            SpriteReference = spriteReference;
        }

        public Sprite Sprite { get; }

        public float GenerationScale { get; }

        public SVGSpriteRef SpriteReference { get; }
    }

    public class SVGRuntimeGenerator
    {
        // Constructor.
        SVGRuntimeGenerator()
        {
        }

        public static float ScaleFactorCalc(float referenceScreenWidth, float referenceScreenHeight,
                                            float currentWidth, float currentHeight,
                                            SVGScalerMatchMode matchMode, float match, float offsetScale)
        {
            SVGScaler scaler = new SVGScaler(referenceScreenWidth, referenceScreenHeight, matchMode, match, offsetScale);
            return scaler.ScaleFactorCalc(currentWidth, currentHeight);
        }

        private static SVGPackedBin[] GenerateBins(// input
                                                   List<SVGAssetInput> svgList,
                                                   int maxTexturesDimension,
                                                   int border,
                                                   bool pow2Textures,
                                                   float scale,
                                                   // output
                                                   Dictionary<int, PackedSvgAssetDocLink> processedAssets, Dictionary<uint, PackedSvgDocRef> loadedDocuments)
        {
            SVGPacker packer = SVGAssetsUnity.CreatePacker(scale, (uint)maxTexturesDimension, (uint)border, pow2Textures);
        
            // start the packing process
            if (packer.Begin() == SVGError.None)
            {
                foreach (SVGAssetInput svgAsset in svgList)
                {
                    int assetKey = svgAsset.TxtAsset.GetInstanceID();
                    // if the text asset has not been already processed, lets create an SVG document out of it
                    if (!processedAssets.ContainsKey(assetKey))
                    {
                        // create the SVG document
                        SVGDocument svgDoc = SVGAssetsUnity.CreateDocument(svgAsset.TxtAsset.text);
                        if (svgDoc != null)
                        {
                            PackedSvgDocRef svgDocRef = new PackedSvgDocRef(svgDoc, svgAsset.TxtAsset);
                            // add the document to the packer, and get back the actual number of packed bounding boxes
                            uint[] info = packer.Add(svgDoc, svgAsset.SeparateGroups, svgAsset.Scale);
                            // info[0] = number of collected bounding boxes
                            // info[1] = the actual number of packed bounding boxes
                            if (info != null && info[1] == info[0])
                            {
                                // increment references
                                svgDocRef.Inc(info[1]);
                                // keep track of the processed asset / created document
                                processedAssets.Add(assetKey, new PackedSvgAssetDocLink(svgAsset, svgDoc));
                                loadedDocuments.Add(svgDoc.Handle, svgDocRef);
                            }
                            else
                            {
                            #if UNITY_EDITOR
                                if (info[1] < info[0])
                                {
                                    EditorUtility.DisplayDialog("Some SVG elements cannot be packed!",
                                                                "Specified maximum texture dimensions do not allow to pack all SVG elements, please increase the value",
                                                                "Ok");
                                }
                            #else
                                UnityEngine.Debug.Log("Some SVG elements cannot be packed! Specified maximum texture dimensions do not allow to pack all SVG elements, please increase the value");
                            #endif
                                // close the packing process without doing anything
                                packer.End(false);
                                // free memory allocated by all loaded SVG documents
                                foreach (PackedSvgAssetDocLink docLink in processedAssets.Values)
                                {
                                    docLink.Document.Dispose();
                                }
                                // return the error
                                return null;
                            }
                        }
                    }
                    else
                    {
                        // get the (already processed svg) asset    
                        if (processedAssets.TryGetValue(assetKey, out PackedSvgAssetDocLink existingAsset))
                        {
                            // get the (already created) svg document
                            if (loadedDocuments.TryGetValue(existingAsset.Document.Handle, out PackedSvgDocRef svgDocRef))
                            {
                                // add the document to the packer, and get back the actual number of packed bounding boxes
                                uint[] info = packer.Add(svgDocRef.Document, svgAsset.SeparateGroups, svgAsset.Scale);
                                // info[0] = number of collected bounding boxes
                                // info[1] = the actual number of packed bounding boxes
                                if (info != null && info[1] == info[0])
                                {
                                    // increment references
                                    svgDocRef.Inc(info[1]);
                                    }
                                else
                                {
                                #if UNITY_EDITOR
                                    if (info[1] < info[0])
                                    {
                                        EditorUtility.DisplayDialog("Some SVG elements cannot be packed!",
                                                                    "Specified maximum texture dimensions do not allow to pack all SVG elements, please increase the value",
                                                                    "Ok");
                                    }
                                #else
                                    UnityEngine.Debug.Log("Some SVG elements cannot be packed! Specified maximum texture dimensions do not allow to pack all SVG elements, please increase the value");
                                #endif
                                    // close the packing process without doing anything
                                    packer.End(false);
                                    // free memory allocated by all loaded SVG documents
                                    foreach (PackedSvgAssetDocLink docLink in processedAssets.Values)
                                    {
                                        docLink.Document.Dispose();
                                    }
                                    // return the error
                                    return null;
                                }
                            }
                        }
                    }
                }
                // return generated bins
                return packer.End(true);
            }
            else
            {
                return null;
            }
        }

        private static bool GenerateSpritesFromBins(// input
                                                    SVGPackedBin[] bins,
                                                    Dictionary<uint, PackedSvgDocRef> loadedDocuments,
                                                    float generationScale,
                                                    Color clearColor,
                                                    bool fastUpload,
                                                    SVGSpritesDictionary previousSprites,
                                                    // output
                                                    List<Texture2D> textures, List<KeyValuePair<SVGSpriteRef, SVGSpriteData>> sprites,
                                                    SVGSpritesListDictionary spritesListDict)
        {
            if ((bins == null) || (loadedDocuments == null) || (textures == null) || (sprites == null))
            {
                return false;
            }
        
            // sprite reference/key used to get pivot
            SVGSpriteRef tmpRef = new SVGSpriteRef(null, 0);
        
            for (int i = 0; i < bins.Length; ++i)
            {
                // extract the bin
                SVGPackedBin bin = bins[i];
                // create drawing surface
                SVGSurfaceUnity surface = SVGAssetsUnity.CreateSurface(bin.Width, bin.Height);
            
                if (surface != null)
                {
                    // draw packed rectangles of the current bin
                    if (surface.Draw(bin, new SVGColor(clearColor.r, clearColor.g, clearColor.b, clearColor.a), SVGRenderingQuality.Better) == SVGError.None)
                    {
                        // bin rectangles
                        SVGPackedRectangle[] rectangles = bin.Rectangles;
                        // create a 2D texture compatible with the drawing surface
                        Texture2D texture = surface.CreateCompatibleTexture(true, false, HideFlags.None);

                        if (texture != null)
                        {
                            // push the created texture
                            textures.Add(texture);
                            for (int j = 0; j < rectangles.Length; ++j)
                            {
                                SVGPackedRectangle rect = rectangles[j];
                                // get access to the referenced SVG document
                                if (loadedDocuments.TryGetValue(rect.DocHandle, out PackedSvgDocRef svgDocRef))
                                {
                                    Vector2 pivot;
                                    Vector4 border;
                                    bool inCurrentInstancesGroup;
                                    // try to see if this sprite was previously generated, and if so get its pivot
                                    tmpRef.TxtAsset = svgDocRef.TxtAsset;
                                    tmpRef.ElemIdx = (int)rect.ElemIdx;
                                    // get the previous pivot if present, else start with a default centered pivot
                                    if (previousSprites.TryGetValue(tmpRef, out SVGSpriteAssetFile spriteAsset))
                                    {
                                        float deltaScaleRatio = generationScale / spriteAsset.SpriteData.GenerationScale;
                                        pivot = spriteAsset.SpriteData.Pivot;
                                        border = spriteAsset.SpriteData.Border * deltaScaleRatio;
                                        inCurrentInstancesGroup = spriteAsset.SpriteData.InCurrentInstancesGroup;
                                    }
                                    else
                                    {
                                        pivot = new Vector2(0.5f, 0.5f);
                                        border = Vector4.zero;
                                        inCurrentInstancesGroup = false;
                                    }
                                    // create a new sprite
                                    Sprite sprite = SVGAssetsUnity.CreateSprite(texture, new Rect(rect.X, ((int)bin.Height - rect.Y - rect.Height), rect.Width, rect.Height), pivot, border);
                                    sprite.name = svgDocRef.Name + "_" + rect.Name;
                                    // push the sprite reference
                                    SVGSpriteRef key = new SVGSpriteRef(svgDocRef.TxtAsset, (int)rect.ElemIdx);
                                    SVGSpriteData value = new SVGSpriteData(sprite, pivot, border,
                                                                            rect.ZOrder, rect.OriginalX, rect.OriginalY, rect.DstViewportWidth, rect.DstViewportHeight,
                                                                            generationScale, inCurrentInstancesGroup);
                                    sprites.Add(new KeyValuePair<SVGSpriteRef, SVGSpriteData>(key, value));
                                    // check if we are interested in getting, for each SVG document, the list of its generated sprites
                                    if (spritesListDict != null)
                                    {
                                        if (!spritesListDict.TryGetValue(svgDocRef.TxtAsset.GetInstanceID(), out SVGSpritesList spritesList))
                                        {
                                            // create the list of sprites location relative to the SVG text asset
                                            spritesList = new SVGSpritesList();
                                            spritesListDict.Add(svgDocRef.TxtAsset.GetInstanceID(), spritesList);
                                        }
                                        // add the new sprite the its list
                                        spritesList.Sprites.Add(key);
                                    }

                                    // decrement document references
                                    if (svgDocRef.Dec(1) == 0)
                                    {
                                        // we can free AmanithSVG native SVG document
                                        svgDocRef.Document.Dispose();
                                    }
                                }
                            }
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
                                    texture.Apply(false, false);
                                }
                            }
                        }
                    }
                    // destroy the AmanithSVG rendering surface
                    surface.Dispose();
                }
            }
            return true;
        }

        public static bool GenerateSprites(// input
                                           List<SVGAssetInput> svgList,
                                           int maxTexturesDimension,
                                           int border,
                                           bool pow2Textures,
                                           float scale,
                                           Color clearColor,
                                           bool fastUpload,
                                           SVGSpritesDictionary previousSprites,
                                           // output
                                           List<Texture2D> textures,
                                           List<KeyValuePair<SVGSpriteRef, SVGSpriteData>> sprites,
                                           SVGSpritesListDictionary spritesList)
        {
            bool ok = false;
            // create dictionaries
            Dictionary<int, PackedSvgAssetDocLink> processedAssets = new Dictionary<int, PackedSvgAssetDocLink>();
            Dictionary<uint, PackedSvgDocRef> loadedDocuments = new Dictionary<uint, PackedSvgDocRef>();
            // generate bins
            SVGPackedBin[] bins = GenerateBins(svgList, maxTexturesDimension, border, pow2Textures, scale, processedAssets, loadedDocuments);

            if (bins != null)
            {
                // generate textures and sprites
                ok = GenerateSpritesFromBins(bins, loadedDocuments, scale, clearColor, fastUpload, previousSprites, textures, sprites, spritesList);
            }

            return ok;
        }
    }

    public abstract class SVGBasicAtlas : ScriptableObject
    {
    #if UNITY_EDITOR

        protected abstract bool SvgAssetAdd(TextAsset newSvg, int index, bool alreadyExist);

        protected abstract string CalcAtlasHash();

    //----------------------------------------------------------------------------------------------------------------------------

        private void CheckAndCreateFolder(string parentFolder, string newFolderName)
        {
            string path = Application.dataPath + parentFolder.TrimStart("Assets".ToCharArray()) + "/" + newFolderName;

            if (!Directory.Exists(path))
            {
                AssetDatabase.CreateFolder(parentFolder, newFolderName);
            }
        }

        protected string CreateOutputFolders()
        {
            // OutputFolder, name, GUID
            Triplet<string, string, string> folder = OutputLocation();

            // OutputFolder/name
            CheckAndCreateFolder(folder.First, folder.Second);
            // OutputFolder/name/GUID
            CheckAndCreateFolder(folder.First + "/" + folder.Second, folder.Third);
            string atlasesPath = folder.First + "/" + folder.Second + "/" + folder.Third;
            // OutputFolder/name/GUID/Textures
            CheckAndCreateFolder(atlasesPath, "Textures");
            // OutputFolder/name/GUID/Sprites
            CheckAndCreateFolder(atlasesPath, "Sprites");
            return atlasesPath;
        }

        private Triplet<string, string, string> OutputLocation()
        {
            return new Triplet<string, string, string>(OutputFolder, name, GUID);
        }

        protected void DeleteTextures()
        {
            foreach (AssetFile file in m_GeneratedTexturesFiles)
            {
                AssetDatabase.DeleteAsset(file.Path);
            }
            m_GeneratedTexturesFiles.Clear();
        }

        protected void DeleteSprites()
        {
            foreach (KeyValuePair<SVGSpriteRef, SVGSpriteAssetFile> file in m_GeneratedSpritesFiles)
            {
                SVGSpriteAssetFile spriteAsset = file.Value;
                AssetDatabase.DeleteAsset(spriteAsset.Path);
            }

            m_GeneratedSpritesFiles.Clear();
        }

        protected void DeleteGameObjects(List<GameObject> objects)
        {
            if ((objects != null) && (objects.Count > 0))
            {
                foreach (GameObject gameObj in objects)
                {
                    DestroyImmediate(gameObj);
                }
            }
        }

        protected int SvgAssetIndexGet(TextAsset txtAsset)
        {
            int result = -1;

            if (txtAsset != null)
            {
                // find the SVG index inside the SvgList
                int j = m_SvgList.Count;
                for (int i = 0; i < j; ++i)
                {
                    SVGAssetInput svgAsset = m_SvgList[i];
                    if (svgAsset.TxtAsset == txtAsset)
                    {
                        // found!
                        result = i;
                        break;
                    }
                }
            }

            // if we have not found the svg index, return -1 as error
            return result;
        }

        protected bool SvgAssetMove(SVGAssetInput svgAsset, int fromIndex, int toIndex)
        {
            bool moved = false;

            if (fromIndex >= 0)
            {
                // clamp the destination index
                toIndex = SVGUtils.Clamp(toIndex, 0, m_SvgList.Count);
                // check if movement has sense
                if (fromIndex != toIndex)
                {
                    // perform the real movement
                    m_SvgList.Insert(toIndex, m_SvgList[fromIndex]);
                    if (toIndex <= fromIndex)
                    {
                        ++fromIndex;
                    }
                    m_SvgList.RemoveAt(fromIndex);
                    moved = true;
                }
            }

            return moved;
        }

        public abstract bool SvgAssetMove(SVGAssetInput svgAsset, int toIndex);

        // recalculate atlas hash
        protected void UpdateAtlasHash()
        {
            m_SvgListHashCurrent = CalcAtlasHash();
        }

        protected static string MD5Calc(string input)
        {
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = s_md5.ComputeHash(inputBytes);
            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        public bool SvgAssetAdd(TextAsset newSvg, int index)
        {
            bool ok;
            bool alreadyExist = false;

            index = SVGUtils.Clamp(index, 0, m_SvgList.Count);
            foreach (SVGAssetInput svgAsset in m_SvgList)
            {
                if (svgAsset.TxtAsset == newSvg)
                {
                    alreadyExist = true;
                    break;
                }
            }

            ok = SvgAssetAdd(newSvg, index, alreadyExist);
            if (ok)
            {
                // start with a basic identity scale offset
                m_SvgList.Insert(index, new SVGAssetInput(newSvg, 1.0f, false));
                // recalculate atlas hash
                UpdateAtlasHash();
            }
            return ok;
        }

        public bool SvgAssetAdd(TextAsset newSvg)
        {
            return SvgAssetAdd(newSvg, m_SvgList.Count);
        }

        public bool SvgAssetRemove(int index)
        {
            bool ok = false;

            if ((index >= 0) && (index < m_SvgList.Count))
            {
                m_SvgList.RemoveAt(index);
                // recalculate atlas hash
                UpdateAtlasHash();
                ok = true;
            }

            return ok;
        }

        public void SvgAssetScaleAdjustmentSet(SVGAssetInput svgAsset, float scale)
        {
            if ((svgAsset != null) && (svgAsset.Scale != scale))
            {
                svgAsset.Scale = scale;
                // recalculate atlas hash
                UpdateAtlasHash();
            }
        }

        public void SvgAssetSeparateGroupsSet(SVGAssetInput svgAsset, bool separateGroups)
        {
            if ((svgAsset != null) && (svgAsset.SeparateGroups != separateGroups))
            {
                svgAsset.SeparateGroups = separateGroups;
                // recalculate atlas hash
                UpdateAtlasHash();
            }
        }

        public bool NeedsUpdate()
        {
            // if current hash is different than the last valid one, atlas generator needs to be updated
            return (m_SvgListHashCurrent != m_SvgListHashOld) ? true : false;
        }

    #endif

        protected void Initialize()
        {
            if (string.IsNullOrEmpty(m_GUID))
            {
                m_GUID = (Guid.NewGuid().ToString()).Replace("-", "");
            }

            if (m_SvgList == null)
            {
                m_SvgList = new List<SVGAssetInput>();
            }
            // create the list of generated textures
            if (m_GeneratedTexturesFiles == null)
            {
                m_GeneratedTexturesFiles = new List<AssetFile>();
            }
            // create the list of generated sprites
            if (m_GeneratedSpritesFiles == null)
            {
                m_GeneratedSpritesFiles = new SVGSpritesDictionary();
            }

            // prepare structures for runtime generation
            if (m_RuntimeTextures == null)
            {
                m_RuntimeTextures = new List<Texture2D>();
            }
            if (m_RuntimeSprites == null)
            {
                m_RuntimeSprites = new SVGSpritesDictionary();
            }
            m_RuntimeGenerationScale = 0;
        #if UNITY_EDITOR
            // for each SVG text asset, a list of sprites (references) relative to the document
            if (m_GeneratedSpritesLists == null)
            {
                m_GeneratedSpritesLists = new SVGSpritesListDictionary();
            }
        #endif
        }

        // return true in case of success, else false
        protected bool UpdateRuntimeSpritesImplementation(float newScale)
        {
            bool ok;
            List<KeyValuePair<SVGSpriteRef, SVGSpriteData>> sprites = new List<KeyValuePair<SVGSpriteRef, SVGSpriteData>>();

            m_RuntimeTextures.Clear();
            m_RuntimeSprites.Clear();

            if ((ok = SVGRuntimeGenerator.GenerateSprites(// input
                                                          m_SvgList,
                                                          // conf parameters
                                                          m_MaxTexturesDimension, m_SpritesBorder, m_Pow2Textures, newScale, m_ClearColor, m_FastUpload,
                                                          // previous generated sprites
                                                          m_GeneratedSpritesFiles,
                                                          // output
                                                          m_RuntimeTextures, sprites,
                                                          // here we are not interested in getting, for each SVG document, the list of its generated sprites
                                                          null)))
            {
                // create the runtime sprites dictionary
                foreach (KeyValuePair<SVGSpriteRef, SVGSpriteData> data in sprites)
                {
                    m_RuntimeSprites.Add(data.Key, new SVGSpriteAssetFile("", data.Key, data.Value));
                }
                m_RuntimeGenerationScale = newScale;
            }

            return ok;
        }

    //----------------------------------------------------------------------------------------------------------------------------

        // Generate a sprite set, according a given canvas scale factor
        public bool GenerateSprites(float scaleFactor, List<Texture2D> textures, List<KeyValuePair<SVGSpriteRef, SVGSpriteData>> sprites)
        {
            return ((textures != null) && (sprites != null) && (scaleFactor > 0)) ? (SVGRuntimeGenerator.GenerateSprites(// input
                                                                                                                         m_SvgList,
                                                                                                                         // conf parameters
                                                                                                                         m_MaxTexturesDimension, m_SpritesBorder, m_Pow2Textures, scaleFactor, m_ClearColor, m_FastUpload,
                                                                                                                         // previous generated sprites
                                                                                                                         m_GeneratedSpritesFiles,
                                                                                                                         // output
                                                                                                                         textures, sprites,
                                                                                                                         // here we are not interested in getting, for each SVG document, the list of its generated sprites
                                                                                                                         null))
                                                                                  : false;
        }

        // return true if case of success, else false
        public bool UpdateRuntimeSprites(float scaleFactor)
        {
            return ((scaleFactor > 0) && (Math.Abs(m_RuntimeGenerationScale - scaleFactor) > float.Epsilon)) ? UpdateRuntimeSpritesImplementation(scaleFactor) : true;
        }

        // Given a text asset representing an SVG file, return the list of generated sprites relative to that document
        public List<SVGSpriteAssetFile> GetGeneratedSpritesByDocument(TextAsset txtAsset)
        {
            List<SVGSpriteAssetFile> result = null;

            if (txtAsset != null)
            {
                // create the output list
                result = new List<SVGSpriteAssetFile>();
                // populate it
                foreach (KeyValuePair<SVGSpriteRef, SVGSpriteAssetFile> file in m_GeneratedSpritesFiles)
                {
                    SVGSpriteAssetFile spriteAsset = file.Value;
                    if (spriteAsset.SpriteRef.TxtAsset == txtAsset)
                    {
                        result.Add(spriteAsset);
                    }
                }
            }

            return result;
        }

        public SVGSpriteAssetFile GetGeneratedSprite(SVGSpriteRef spriteRef)
        {
            return (m_GeneratedSpritesFiles.TryGetValue(spriteRef, out SVGSpriteAssetFile spriteAsset)) ? spriteAsset : null;
        }

        public SVGRuntimeSprite GetRuntimeSprite(SVGSpriteRef spriteRef)
        {
            SVGRuntimeSprite result = null;

            if (m_RuntimeGenerationScale > 0)
            {
                // get the requested sprite
                if (m_RuntimeSprites.TryGetValue(spriteRef, out SVGSpriteAssetFile spriteAsset))
                {
                    result = new SVGRuntimeSprite(spriteAsset.SpriteData.Sprite, m_RuntimeGenerationScale, spriteRef);
                }
            }

            return result;
        }

        public SVGRuntimeSprite GetSpriteByName(string spriteName)
        {
            SVGRuntimeSprite result = null;
            SVGSpritesDictionary spritesSet = Application.isPlaying ? m_RuntimeSprites : m_GeneratedSpritesFiles;
            float scale = Application.isPlaying ? m_RuntimeGenerationScale : m_EditorGenerationScale;

            if (spritesSet != null)
            {
                // slow linear search
                foreach (SVGSpriteAssetFile spriteAsset in spritesSet.Values())
                {
                    Sprite sprite = spriteAsset.SpriteData.Sprite;
                    if (sprite.name == spriteName)
                    {
                        // found!
                        result = new SVGRuntimeSprite(sprite, scale, spriteAsset.SpriteRef);
                        break;
                    }
                }
            }
  
            return result;
        }

        public int SvgAssetsCount()
        {
            return ((m_SvgList == null) ? 0 : m_SvgList.Count);
        }

        public SVGAssetInput SvgAsset(int index)
        {
            return ((m_SvgList == null) ? null : m_SvgList[index]);
        }

        public List<SVGAssetInput> SvgAssets()
        {
            return ((m_SvgList == null) ? null : (new List<SVGAssetInput>(m_SvgList)));
        }

        public int TextureAssetsCount()
        {
            return ((m_GeneratedTexturesFiles == null) ? 0 : m_GeneratedTexturesFiles.Count);
        }

        public AssetFile TextureAsset(int index)
        {
            return ((m_GeneratedTexturesFiles == null) ? null : m_GeneratedTexturesFiles[index]);
        }

        public List<AssetFile> TextureAssets()
        {
            return ((m_GeneratedTexturesFiles == null) ? null : (new List<AssetFile>(m_GeneratedTexturesFiles)));
        }

        public List<SVGSpriteAssetFile> SpriteAssets()
        {
            return ((m_GeneratedSpritesFiles == null) ? null : m_GeneratedSpritesFiles.Values());
        }

    //----------------------------------------------------------------------------------------------------------------------------

        public string GUID
        {
            get
            {
                return m_GUID;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                if (!Application.isPlaying) {
                    if (value != name)
                    {
                        name = value;
                    #if UNITY_EDITOR
                        UpdateAtlasHash();
                    #endif
                    }
                }
            }
        }

        public float OffsetScale
        {
            get
            {
                return m_OffsetScale;
            }
            set
            {
                if (!Application.isPlaying) {
                    m_OffsetScale = value;
                #if UNITY_EDITOR
                    UpdateAtlasHash();
                #endif
                }
            }
        }

        public bool Pow2Textures
        {
            get
            {
                return m_Pow2Textures;
            }
            set
            {
                if (!Application.isPlaying) {
                    m_Pow2Textures = value;
                #if UNITY_EDITOR
                    UpdateAtlasHash();
                #endif
                }
            }
        }

        public int MaxTexturesDimension
        {
            get
            {
                return m_MaxTexturesDimension;
            }
            set
            {
                if (!Application.isPlaying) {
                    m_MaxTexturesDimension = value;
                #if UNITY_EDITOR
                    UpdateAtlasHash();
                #endif
                }
            }
        }

        public int SpritesBorder
        {
            get
            {
                return m_SpritesBorder;
            }
            set
            {
                if (!Application.isPlaying) {
                    m_SpritesBorder = value;
                #if UNITY_EDITOR
                    UpdateAtlasHash();
                #endif
                }
            }
        }

        public Color ClearColor
        {
            get
            {
                return m_ClearColor;
            }
            set
            {
                if (!Application.isPlaying) {
                    m_ClearColor = value;
                #if UNITY_EDITOR
                    UpdateAtlasHash();
                #endif
                }
            }
        }

        public bool FastUpload
        {
            get
            {
                return m_FastUpload;
            }
            set
            {
                if (!Application.isPlaying)
                {
                    m_FastUpload = value;
                }
            }
        }

        public float EditorGenerationScale
        {
            get
            {
                return m_EditorGenerationScale;
            }
        }

    #if UNITY_EDITOR

        // Main output folder, where generated entities (i.e. sprites and textures) are written to
        public string OutputFolder
        {
            get
            {
                return m_OutputFolder;
            }
            set
            {
                // the given path must be relative to the project folder
                if (AssetDatabase.IsValidFolder(value))
                {
                    if (!Application.isPlaying) {
                        m_OutputFolder = value;
                        UpdateAtlasHash();
                    }
                }
            }
        }

        // Subfolder containing generated textures, relative to OutputFolder
        public string TexturesSubFolder
        {
            get
            {
                Triplet<string, string, string> outLocation = OutputLocation();
                return outLocation.Second + "/" + outLocation.Third + "/Textures";
            }
        }

        // Subfolder containing generated sprites, relative to OutputFolder
        public string SpritesSubFolder
        {
            get
            {
                Triplet<string, string, string> outLocation = OutputLocation();
                return outLocation.Second + "/" + outLocation.Third + "/Sprites";
            }
        }

        public string FullOutputFolder
        {
            get
            {
                Triplet<string, string, string> outLocation = OutputLocation();
                return (outLocation.First + "/" + outLocation.Second + "/" + outLocation.Third);
            }
        }

        public float SpritesPreviewSize
        {
            get
            {
                return m_SpritesPreviewSize;
            }
            set
            {
                m_SpritesPreviewSize = SVGUtils.Clamp((int)value, 4, 256);
            }
        }

        public bool Exporting
        {
            get
            {
                return m_Exporting;
            }
            set
            {
                m_Exporting = value;
            }
        }

    #endif  // UNITY_EDITOR
        //----------------------------------------------------------------------------------------------------------------------------

        // Unique persistent ID
        [SerializeField]
        protected string m_GUID = "";
        // Output folder; NB: path must be relative to the project folder (e.g. Assets/Atlases/FirstGameLevel)
        [SerializeField]
        protected string m_OutputFolder = "Assets";
        [SerializeField]
        protected float m_OffsetScale = 1.0f;
        [SerializeField]
        protected bool m_Pow2Textures = true;
        [SerializeField]
        // Maximum texture dimension
        protected int m_MaxTexturesDimension = 4096;
        [SerializeField]
        // Border between each generated sprite and its neighbors, in pixels
        protected int m_SpritesBorder = 1;
        // Clear color
        [SerializeField]
        protected Color m_ClearColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        // Fast texture upload
        [SerializeField]
        protected bool m_FastUpload = true;
        // List of SVG that we want to pack
        [SerializeField]
        protected List<SVGAssetInput> m_SvgList = null;
        // List of generated texture assets (files)
        [SerializeField]
        protected List<AssetFile> m_GeneratedTexturesFiles = null;
        // List of generated sprite assets (files)
        [SerializeField]
        protected SVGSpritesDictionary m_GeneratedSpritesFiles = null;
        // The scale factor used from the last generation within the editor
        [SerializeField]
        protected float m_EditorGenerationScale = 0.0f;
        // Texture atlases generated at runtime
        [NonSerialized]
        protected List<Texture2D> m_RuntimeTextures = null;
        // Sprites generated at runtime
        [NonSerialized]
        protected SVGSpritesDictionary m_RuntimeSprites = null;
        // Scale factor relative to the last runtime sprite/texture generation
        [NonSerialized]
        protected float m_RuntimeGenerationScale = 0.0f;

    #if UNITY_EDITOR
        // Hash relative to the last valid (i.e. updated) set of generated sprites
        [SerializeField]
        protected string m_SvgListHashOld = "";
        // The hash relative to the current list of input SVG
        [SerializeField]
        protected string m_SvgListHashCurrent = "";
        // The MD5 hash generator
        private static MD5 s_md5 = System.Security.Cryptography.MD5.Create();
        // For each SVG text asset, a list of sprites (references) relative to the document
        [SerializeField]
        protected SVGSpritesListDictionary m_GeneratedSpritesLists = null;
        [SerializeField]
        private float m_SpritesPreviewSize = 32.0f;
        // Used to flag atlas generators when building the scenes.
        [NonSerialized]
        private bool m_Exporting = false;
    #endif
    }
}
