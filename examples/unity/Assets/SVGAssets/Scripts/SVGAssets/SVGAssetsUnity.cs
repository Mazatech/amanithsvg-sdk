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
#if UNITY_EDITOR || UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE || UNITY_WII || UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID || UNITY_PS4 || UNITY_XBOXONE || UNITY_TIZEN || UNITY_TVOS || UNITY_WSA || UNITY_WSA_10_0 || UNITY_WINRT || UNITY_WINRT_10_0 || UNITY_WEBGL || UNITY_FACEBOOK || UNITY_ADS || UNITY_ANALYTICS
    #define UNITY_ENGINE
#endif

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

#if UNITY_ENGINE

using System;
using System.IO;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;
#if UNITY_EDITOR
    using UnityEditor;
using System.Net.NetworkInformation;
#endif

// Extension of SVGSurface class for Unity
public class SVGSurfaceUnity : SVGSurface
{
    // Constructor.
    internal SVGSurfaceUnity(uint handle) : base(handle)
    {
    }

    /*
        Create a 2D texture compatible with the drawing surface.

        NB: textures passed to Copy and CopyAndDestroy must be created
        through this function.
    */
    public Texture2D CreateCompatibleTexture(bool bilinearFilter,
                                             bool wrapRepeat,
                                             HideFlags hideFlags = HideFlags.HideAndDontSave)
    {
        uint width = Width;
        uint height = Height;
        // try to use BGRA format, because on little endian architectures will speedup the upload of texture pixels
        // by avoiding swizzling (e.g. glTexImage2D / glTexSubImage2D)
        bool bgraSupport = SystemInfo.SupportsTextureFormat(TextureFormat.BGRA32);
        TextureFormat format = bgraSupport ? TextureFormat.BGRA32 : TextureFormat.RGBA32;
        Texture2D texture = new Texture2D((int)width, (int)height, format, false);

        if (texture != null)
        {
            texture.filterMode = bilinearFilter ? FilterMode.Bilinear : FilterMode.Point;
            if ((SystemInfo.npotSupport == NPOTSupport.Full) || ((SVGUtils.IsPow2(width)) && (SVGUtils.IsPow2(height))))
            {
                texture.wrapMode = wrapRepeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            }
            else
            {
                texture.wrapMode = TextureWrapMode.Clamp;
            }
            texture.anisoLevel = 1;
            texture.hideFlags = hideFlags;
        #if UNITY_EDITOR
            texture.alphaIsTransparency = true;
        #endif
        }
        else
        {
            SVGAssets.LogError("SVGSurfaceUnity::CreateCompatibleTexture allocating a new Texture2D failed", SVGError.OutOfMemory);
        }

        return texture;
    }

    /*
        Copy drawing surface content into the specified Unity texture.
        
        If the 'dilateEdgesFix' flag is true, the copy process will also perform
        a 1-pixel dilate post-filter; this dilate filter could be useful when
        surface pixels will be uploaded to OpenGL/Direct3D bilinear-filtered textures.

        NB: the given texture must have been created by the CreateCompatibleTexture method.

        It returns SVGError.None if the operation was completed successfully, else
        an error code.
    */
    private SVGError Copy(Texture2D texture,
                          bool dilateEdgesFix)
    {
        uint err = AmanithSVG.SVGT_NO_ERROR;
        bool redBlueSwap = (texture.format == TextureFormat.RGBA32);

        unsafe
        {
            NativeArray<byte> texels = texture.GetRawTextureData<byte>();
            IntPtr texelsPtr = (IntPtr)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(texels);

            // the unsafe but faster approach
            if (texelsPtr != IntPtr.Zero)
            {
                // copy pixels from internal drawing surface to destination pixels array; NB: AmanithSVG buffer is always in BGRA format (i.e. B = LSB, A = MSB)
                err = AmanithSVG.svgtSurfaceCopy(Handle, texelsPtr, AmanithSVG.svgtBool(redBlueSwap), AmanithSVG.svgtBool(dilateEdgesFix));
                if (err != AmanithSVG.SVGT_NO_ERROR)
                {
                    SVGAssets.LogError("SVGSurfaceUnity::Copy copy from surface pixels to texture failed", (SVGError)err);
                }
            }
            // the safe but slow approaches
            else
            {
                uint pixelsCount = Width * Height;
                SVGAssets.LogWarning("SVGSurfaceUnity::Copy getting raw texture data failed; now switching to the slower copy approach");

                if (redBlueSwap || dilateEdgesFix)
                {
                    IntPtr tempBufferPtr = IntPtr.Zero;
                    try
                    {
                        tempBufferPtr = Marshal.AllocHGlobal((int)pixelsCount * 4);
                        if (tempBufferPtr != IntPtr.Zero)
                        {
                            // copy pixels from internal drawing surface to destination pixels array; NB: AmanithSVG buffer is always in BGRA format (i.e. B = LSB, A = MSB)
                            if ((err = AmanithSVG.svgtSurfaceCopy(Handle, tempBufferPtr, AmanithSVG.svgtBool(redBlueSwap), AmanithSVG.svgtBool(dilateEdgesFix))) == AmanithSVG.SVGT_NO_ERROR)
                            {
                                // fill texture pixel memory with raw data; NB: later we must call texture.Apply to actually upload it to the GPU
                                texture.LoadRawTextureData(tempBufferPtr, (int)pixelsCount * 4);
                            }
                            else
                            {
                                SVGAssets.LogError("SVGSurfaceUnity::Copy copy from surface pixels to temporary buffer failed", (SVGError)err);
                            }
                        }
                        else
                        {
                            err = AmanithSVG.SVGT_OUT_OF_MEMORY_ERROR;
                            SVGAssets.LogError("SVGSurfaceUnity::Copy unable to allocate (AllocHGlobal) temporary buffer", (SVGError)err);
                        }
                    }
                    finally
                    {
                        if (tempBufferPtr != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(tempBufferPtr);
                        }
                    }
                }
                else
                {
                    // we can use AmanithSVG pixels directly; NB: AmanithSVG buffer is always in BGRA format (i.e. B = LSB, A = MSB)
                    IntPtr pixels = AmanithSVG.svgtSurfacePixels(Handle);
                    // fill texture pixel memory with raw data; NB: later we must call texture.Apply to actually upload it to the GPU
                    texture.LoadRawTextureData(pixels, (int)pixelsCount * 4);
                }
            }
        }

        return (SVGError)err;
    }

    /*
        Copy drawing surface content into the specified Unity texture.
        NB: the given texture must have been created by the CreateCompatibleTexture
        method.

        It returns SVGError.None if the operation was completed successfully, else
        an error code.
    */
    public SVGError Copy(Texture2D texture)
    {
        SVGError err;

        if (texture != null)
        {
            err = Copy(texture, texture.filterMode != FilterMode.Point);
        }
        else
        {
            err = SVGError.IllegalArgument;
            SVGAssets.LogWarning("SVGSurfaceUnity::Copy illegal null texture parameter");
        }

        return err;
    }

    /*
        Copy drawing surface content into the specified Unity texture, then destroy
        the native drawing surface. The SVGSurfaceUnity instance is not destroyed, but
        its native AmanithSVG counterpart it will. The result will be that every
        called method will fail silently.

        In order to ensure the maximum speed, the copy process will use native GPU
        platform-specific methods:

        - UpdateSubresource (Direct3D 11)
        - glTexSubImage2D (OpenGL and OpenGL ES)
        - replaceRegion (Metal)

        NB: the given texture must have been created by the CreateCompatibleTexture
        method.

        It returns SVGError.None if the operation was completed successfully, else
        an error code.
    */
    public SVGError CopyAndDestroy(Texture2D texture)
    {
        uint err;

        if (texture != null)
        {
            // set the target texture handle
            IntPtr hwPtr = texture.GetNativeTexturePtr();
            if ((err = AmanithSVG.svgtSurfaceTexturePtrSet(Handle, hwPtr,
                                                           (uint)texture.width, (uint)texture.height,
                                                           AmanithSVG.svgtBool(texture.format == TextureFormat.BGRA32),
                                                           AmanithSVG.svgtBool(texture.filterMode != FilterMode.Point))) != AmanithSVG.SVGT_NO_ERROR)
            {
                SVGAssets.LogError("SVGSurfaceUnity::CopyAndDestroy setting native texture data failed", (SVGError)err);
            }
            else
            {
                GL.IssuePluginEvent(AmanithSVG.svgtSurfaceTextureCopyAndDestroyFuncGet(), (int)Handle);
                // set the surface internal handle to invalid, because the copy&destroy will take care to free the surface memory after the copy
                // NB: after the copy this surface won't be usable anymore
                Handle = AmanithSVG.SVGT_INVALID_HANDLE;
            }
        }
        else
        {
            err = AmanithSVG.SVGT_ILLEGAL_ARGUMENT_ERROR;
            SVGAssets.LogWarning("SVGSurfaceUnity::CopyAndDestroy illegal null texture parameter");
        }

        return (SVGError)err;
    }

    /*
        Draw an SVG document on this drawing surface, then generate a texture out of it.

        First the drawing surface is cleared if a valid (i.e. not null) clear color is provided.
        Then the specified document, if valid, is drawn. And finally a texture is created
        by calling the 'CreateCompatibleTexture' method and the surface content is copied into it.

        It returns a non-null texture if the operation was completed successfully, else a null instance.
    */
    public Texture2D DrawTexture(SVGDocument document,
                                 SVGColor clearColor,
                                 SVGRenderingQuality renderingQuality = SVGRenderingQuality.Better,
                                 bool bilinearFilter = false)
    {
        Texture2D texture = null;

        // draw SVG
        if (Draw(document, clearColor, renderingQuality) == SVGError.None)
        {
            // create a 2D texture compatible with the drawing surface
            texture = CreateCompatibleTexture(bilinearFilter, false);
            // copy the surface content into the texture
            if (Copy(texture) == SVGError.None)
            {
                // call Apply() so it's actually uploaded to the GPU
                texture.Apply(false, true);
            }
        }

        return texture;
    }
}

// SVGAssets configuration for Unity
[Serializable]
public class SVGAssetsConfigUnity : SVGAssetsConfig
{
    // SVGFontResource for Unity
    [Serializable]
    public class SVGResourceUnity : SVGResource
    {
        // Constructor
        public SVGResourceUnity(TextAsset resourceAsset,
                                string strId,
                                SVGResourceType type,
                                //SVGResourceHint hints) : base(Path.GetFileNameWithoutExtension(resourceAsset.name), type, hints)
                                SVGResourceHint hints) : base(strId, type, hints)
        {
            Asset = resourceAsset;
        }

        // Get in-memory binary data for the resource.
        public override byte[] GetBytes()
        {
            return Asset.bytes;
        }

    #if UNITY_EDITOR
        /*
            Get the current resource hints, ensuring a proper bitfield for the Unity editor

            "For enums backed by an unsigned type, the "Everything" option should have
            the value corresponding to all bits set (i.e. ~0 in an unchecked context
            or the MaxValue constant for the type)"

            See https://docs.unity3d.com/ScriptReference/EditorGUILayout.EnumFlagsField.html
        */
        public SVGResourceHint HintsEditor
        {
            get
            {
                // NB: image resources do not support any hint
                return (Type == SVGResourceType.Font) ? ((Hints == s_hintsAll) ? (SVGResourceHint)(uint.MaxValue) : Hints) : SVGResourceHint.None;
            }
            set
            {
                // NB: image resources do not support any hint
                Hints = (Type == SVGResourceType.Font) ? (value & s_hintsAll) : SVGResourceHint.None;
            }
        }
    #endif // UNITY_EDITOR

        [SerializeField]
        public TextAsset Asset;

    #if UNITY_EDITOR
        static private SVGResourceHint s_hintsAll = SVGResourceHint.DefaultSerif | SVGResourceHint.DefaultSansSerif | SVGResourceHint.DefaultMonospace |
                                                    SVGResourceHint.DefaultCursive | SVGResourceHint.DefaultFantasy | SVGResourceHint.DefaultSystemUI |
                                                    SVGResourceHint.DefaultUISerif | SVGResourceHint.DefaultUISansSerif | SVGResourceHint.DefaultUIMonospace |
                                                    SVGResourceHint.DefaultUIRounded | SVGResourceHint.DefaultEmoji | SVGResourceHint.DefaultMath |
                                                    SVGResourceHint.DefaultFangsong;
    #endif
    }

    // Constructor
    public SVGAssetsConfigUnity(uint screenWidth,
                                uint screenHeight,
                                float screenDpi) : base(screenWidth, screenHeight, screenDpi)
    {
        // list of external resources (fonts and images)
        _resources = new List<SVGResourceUnity>();
    }

    /*
        Get the number of (external) resources provided by this configuration.
        Mandatory implementation from SVGAssetsConfig.
    */
    public override int ResourcesCount()
    {
        return (_resources == null) ? 0 : _resources.Count;
    }

    /*
        Get a resource given an index.
        If the given index is less than zero or greater or equal to the value
        returned by ResourcesCount, a null resource is returned.

        Mandatory implementation from SVGAssetsConfig.
    */
    public override SVGResource GetResource(int index)
    {
        return (_resources?[index]);
    }

#if UNITY_EDITOR

    // Used by AmanithSVG geometric kernel to approximate curves with straight line
    // segments (flattening). Valid range is [1; 100], where 100 represents the best quality.
    // NB: a 0 or negative value means "keep the default one"
    public new float CurvesQuality
    {
        get
        {
            return base.CurvesQuality;
        }

        set
        {
            base.CurvesQuality = (value <= 0) ? 0 : Math.Max(1.0f, Math.Min(100.0f, value));
        }
    }

    // Add a new resource asset
    public bool ResourceAdd(TextAsset newResourceAsset,
                            int index)
    {
        bool ok;
        bool alreadyExist = false;

        index = SVGUtils.Clamp(index, 0, _resources.Count);
        foreach (SVGResourceUnity resource in _resources)
        {
            if (resource.Asset == newResourceAsset)
            {
                alreadyExist = true;
                break;
            }
        }

        if (alreadyExist)
        {
            // show warning
            EditorUtility.DisplayDialog("Can't add the same file multiple times!",
                                        string.Format("The list of SVG resources already includes the {0} file.", newResourceAsset.name),
                                        "Ok");
            ok = false;
        }
        else
        {
            bool supported;
            SVGResourceType type;
            // use filename as resource id (e.g. arial.ttf)
            string id = Path.GetFileNameWithoutExtension(newResourceAsset.name);
            string ext = (Path.GetExtension(newResourceAsset.name)).ToLower();

            switch (ext) {
                // check supported vector fonts formats
                case ".otf":
                case ".ttf":
                case ".woff":
                case ".woff2":
                    type = SVGResourceType.Font;
                    supported = true;
                    break;
                // check supported image formats
                case ".jpg":
                case ".jpeg":
                case ".png":
                    type = SVGResourceType.Image;
                    supported = true;
                    break;
                default:
                    type = SVGResourceType.Font;
                    supported = false;
                    break;
            }

            if (!supported) {
                // show warning
                EditorUtility.DisplayDialog("Unsupported resource type!",
                                            string.Format("{0} is neither an image (JPEG/PNG) nor a vector font (OTF/TTF/WOFF).", newResourceAsset.name),
                                            "Ok");
                ok = false;
            }
            else {
                _resources.Insert(index, new SVGResourceUnity(newResourceAsset, id, type, SVGResourceHint.None));
                ok = true;
            }
        }

        return ok;
    }

    // Find the position (index) of the given font asset, within the internal list.
    private int ResourceIndexGet(TextAsset resourceAsset)
    {
        // -1 means "not found"
        int result = -1;

        if (resourceAsset != null)
        {
            // find the index inside the fonts list
            for (int i = 0; i < _resources.Count; ++i)
            {
                SVGResourceUnity resource = _resources[i];
                if (resource.Asset == resourceAsset)
                {
                    // found!
                    result = i;
                    break;
                }
            }
        }

        return result;
    }

    // Change a resource position within the internal list
    private bool ResourceMove(int fromIndex,
                              int toIndex)
    {
        bool moved = false;

        if (fromIndex >= 0)
        {
            // clamp the destination index
            int toIdx = SVGUtils.Clamp(toIndex, 0, _resources.Count);
            // check if movement has sense
            if (fromIndex != toIdx)
            {
                // perform the real movement
                _resources.Insert(toIdx, _resources[fromIndex]);
                if (toIdx <= fromIndex)
                {
                    ++fromIndex;
                }
                _resources.RemoveAt(fromIndex);
                moved = true;
            }
        }

        return moved;
    }

    // Change position of the given resource
    public bool ResourceMove(SVGResourceUnity resource,
                             int toIndex)
    {
        int fromIndex = ResourceIndexGet(resource.Asset);
        return ResourceMove(fromIndex, toIndex);
    }

    // Remove the resource, from the internal list, relative to the given index
    public bool ResourceRemove(int index)
    {
        bool ok = false;

        if ((index >= 0) && (index < _resources.Count))
        {
            _resources.RemoveAt(index);
            ok = true;
        }

        return ok;
    }

    /*
        Get the current log level, ensuring a proper bitfield for the Unity editor

        "For enums backed by an unsigned type, the "Everything" option should have
        the value corresponding to all bits set (i.e. ~0 in an unchecked context
        or the MaxValue constant for the type)"

        See https://docs.unity3d.com/ScriptReference/EditorGUILayout.EnumFlagsField.html
    */
    public SVGLogLevel LogLevelEditor
    {
        get
        {
            return (LogLevel == s_logLevelAll) ? (SVGLogLevel)(uint.MaxValue) : LogLevel;
        }
        set
        {
            LogLevel = value & s_logLevelAll;
        }
    }

#endif // UNITY_EDITOR

    // List of resources.
    [SerializeField]
    private List<SVGResourceUnity> _resources;

#if UNITY_EDITOR
    static private SVGLogLevel s_logLevelAll = SVGLogLevel.Error | SVGLogLevel.Warning | SVGLogLevel.Info;
#endif
}

// Implementation of 
public static class SVGAssetsUnity
{
    /* Get screen resolution width, in pixels. */
    public static uint ScreenWidth
    {
        get
        {
            if (Application.isPlaying)
            {
                return (uint)Screen.width;
            }
            else
            {
                Vector2 view = SVGUtils.GetGameView();
                return (uint)view.x;
            }
        }
    }

    /* Get screen resolution height, in pixels. */
    public static uint ScreenHeight
    {
        get
        {
            if (Application.isPlaying)
            {
                return (uint)Screen.height;
            }
            else
            {
                Vector2 view = SVGUtils.GetGameView();
                return (uint)view.y;
            }
        }
    }

    /* Get screen dpi. */
    public static float ScreenDpi
    {
        get
        {
            float dpi = Screen.dpi;
            return (dpi <= 0.0f) ? 96.0f : dpi;
        }
    }

    /* Get screen orientation. */
    public static ScreenOrientation ScreenOrientation
    {
        get
        {
        #if UNITY_EDITOR
            return (ScreenHeight > ScreenWidth) ? ScreenOrientation.Portrait : ScreenOrientation.Landscape;
        #else
            return Screen.orientation;
        #endif
        }
    }

    private static bool IsInitialized()
    {
        return SVGAssets.IsInitialized();
    }

    // Initialize SVGAssets for Unity.
    private static SVGError Init()
    {
        // load external configuration file/asset
        SVGAssetsConfigUnityScriptable configAsset = Resources.Load(SVGAssetsConfigUnityScriptable.s_configAssetName) as SVGAssetsConfigUnityScriptable;
        SVGAssetsConfigUnity config = configAsset?.Config;

        // if configuration does not exist, create a new default one
        if (config == null)
        {
            config = new SVGAssetsConfigUnity(ScreenWidth, ScreenHeight, ScreenDpi);
            Debug.LogWarning("SVGAssetsUnity configuration asset file does not exist, language settings and resources won't be loaded!");
        }
        else
        {
            // override screen parameters, in order to get the actual ones
            config.ScreenWidth = ScreenWidth;
            config.ScreenHeight = ScreenHeight;
            config.ScreenDpi = ScreenDpi;
        }

        return SVGAssets.Init(config);
    }

    // Release SVGAssets for Unity.
    public static void Done()
    {
        if (IsInitialized())
        {
            // output AmanithSVG log content, using Unity log facilities
            string logContent = SVGAssets.LogGet();
            if (!string.IsNullOrEmpty(logContent))
            {
                Debug.Log("AmanithSVG log");
                Debug.Log(logContent);
            }

            // uninitialize AmanithSVG library
            SVGAssets.Done();
        }
    }

    // Make sure to uninitialize SVGAssets when application quits
    [RuntimeInitializeOnLoadMethod]
    static void RunOnStart()
    {
        Application.quitting += Done;
    }

    /*
        Create a drawing surface, specifying its dimensions in pixels.

        Supplied dimensions should be positive numbers greater than zero, else
        a null instance will be returned.
    */
    public static SVGSurfaceUnity CreateSurface(uint width,
                                                uint height)
    {
        // initialize AmanithSVG, if needed
        if (!IsInitialized())
        {
            Init();
        }

        // create the surface
        uint handle = SVGAssets.CreateSurfaceHandle(width, height);
        return (handle != AmanithSVG.SVGT_INVALID_HANDLE) ? (new SVGSurfaceUnity(handle)) : null;
    }

    /*
        Create and load an SVG document, specifying the whole XML string.
        If supplied XML string is null or empty, a null instance will be returned.
    */
    public static SVGDocument CreateDocument(string xmlText)
    {
        // initialize AmanithSVG, if needed
        if (!IsInitialized())
        {
            Init();
        }

        // create the document
        return SVGAssets.CreateDocument(xmlText);
    }

    /*
        Create an SVG packer, specifying a scale factor.

        Every collected SVG document/element will be packed into rectangular bins,
        whose dimensions won't exceed the specified 'maxTexturesDimension' in pixels.

        If true, 'pow2Textures' will force bins to have power-of-two dimensions.
        Each rectangle will be separated from the others by the specified 'border' in pixels.

        The specified 'scale' factor will be applied to all collected SVG documents/elements,
        in order to realize resolution-independent atlases.
    */
    public static SVGPacker CreatePacker(float scale,
                                         uint maxTexturesDimension,
                                         uint border,
                                         bool pow2Textures)
    {
        // initialize AmanithSVG, if needed
        if (!IsInitialized())
        {
            Init();
        }

        // create the packer
        return SVGAssets.CreatePacker(scale, maxTexturesDimension, border, pow2Textures);
    }

    /*
        Create an SVG packer, specifying a scale factor.

        Every collected SVG document/element will be packed into rectangular bins.

        Each rectangle will be separated from the others by the specified 'border' in pixels.

        The specified 'scale' factor will be applied to all collected SVG documents/elements,
        in order to realize resolution-independent atlases.

        NB: maximum dimension of textures is auto-detected.
    */
    public static SVGPacker CreatePacker(float scale,
                                         uint border)
    {
        // auto-detect the maximum texture dimension and npot support
        uint maxTexturesDimension = Math.Min((uint)SystemInfo.maxTextureSize, SVGSurface.MaxDimension);
        bool pow2Textures = SystemInfo.npotSupport == NPOTSupport.Full;

        return CreatePacker(scale, maxTexturesDimension, border, pow2Textures);
    }

    /*
        Create a sprite out of the given texture.
        The sprite will use the specified rectangular section of the texture, and it will have the provided pivot.
    */
    public static Sprite CreateSprite(Texture2D texture,
                                      Rect rect,
                                      Vector2 pivot,
                                      Vector4 border)
    {
        return Sprite.Create(texture, rect, pivot, SPRITE_PIXELS_PER_UNIT, 0, SpriteMeshType.FullRect, border);
    }

    /*
        Create a sprite out of the given texture.
        The sprite will use the specified rectangular section of the texture, and it will have the provided pivot.
    */
    public static Sprite CreateSprite(Texture2D texture,
                                      Rect rect,
                                      Vector2 pivot)
    {
        return CreateSprite(texture, rect, pivot, Vector4.zero);
    }

    /*
        Create a sprite out of the given texture.
        The sprite will cover the whole texture, and it will have the provided pivot.
    */
    public static Sprite CreateSprite(Texture2D texture,
                                      Vector2 pivot)
    {
        return CreateSprite(texture, new Rect(0, 0, texture.width, texture.height), pivot);
    }

    // Append an informational message to the AmanithSVG log buffer set for the current thread.
    public static SVGError LogInfo(string message)
    {
        // initialize AmanithSVG, if needed
        if (!IsInitialized())
        {
            Init();
        }

        return SVGAssets.LogPrint(message, SVGLogLevel.Info);
    }

    // Append a warning message to the AmanithSVG log buffer set for the current thread.
    public static SVGError LogWarning(string message)
    {
        // initialize AmanithSVG, if needed
        if (!IsInitialized())
        {
            Init();
        }

        return SVGAssets.LogPrint(message, SVGLogLevel.Warning);
    }

    // Append an error message to the AmanithSVG log buffer set for the current thread.
    public static SVGError LogError(string message)
    {
        // initialize AmanithSVG, if needed
        if (!IsInitialized())
        {
            Init();
        }

        return SVGAssets.LogPrint(message, SVGLogLevel.Error);
    }

    // Append an error message to the AmanithSVG log buffer set for the current thread.
    public static SVGError LogError(string message,
                                    SVGError err)
    {
        // initialize AmanithSVG, if needed
        if (!IsInitialized())
        {
            Init();
        }

        return SVGAssets.LogError(message, err);
    }

    // Scaling to map pixels in the image to world space units, used by all generated sprites.
    public const float SPRITE_PIXELS_PER_UNIT = 100.0f;
}

#endif  // UNITY_ENGINE
