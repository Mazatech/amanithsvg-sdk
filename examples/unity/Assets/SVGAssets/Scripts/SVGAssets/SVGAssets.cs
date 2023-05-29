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

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;

namespace SVGAssets
{
    public static class AmanithSVG {

    #if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && (!UNITY_EDITOR)
        /* On iOS, everything gets built into one big binary, so "__Internal" is the name of the library to use */
        private const string libName = "__Internal";
    #else
        #if UNITY_EDITOR
            /* Windows editor will use libAmanithSVG.dll, Max OS X editor will use libAmanithSVG.bundle */
            private const string libName = "libAmanithSVG";
        #elif UNITY_STANDALONE_WIN
            /* Windows uses libAmanithSVG.dll */
            private const string libName = "libAmanithSVG";
        #elif UNITY_STANDALONE_OSX
            /* Mac OS X uses libAmanithSVG.bundle */
            private const string libName = "libAmanithSVG";
        #elif UNITY_STANDALONE_LINUX
            /* Linux uses libAmanithSVG.so please note that plugin name should not include the prefix ('lib') nor the extension ('.so') of the filename */
            private const string libName = "AmanithSVG";
        #elif UNITY_ANDROID
            /* Android uses libAmanithSVG.so please note that plugin name should not include the prefix ('lib') nor the extension ('.so') of the filename */
            private const string libName = "AmanithSVG";
        #else
            private const string libName = "libAmanithSVG";
        #endif
    #endif

        /* Invalid handle. */
        public const uint SVGT_INVALID_HANDLE                   = 0;

        /* SVGTboolean */
        public const uint SVGT_FALSE                            = 0;
        public const uint SVGT_TRUE                             = 1;

        // -----------------------------------------------------------------------
        // SVGTAspectRatioAlign
        //
        // Alignment indicates whether to force uniform scaling and, if so, the
        // alignment method to use in case the aspect ratio of the source viewport
        // doesn't match the aspect ratio of the destination (drawing surface)
        // viewport.
        // -----------------------------------------------------------------------

        /*
            Do not force uniform scaling.
            Scale the graphic content of the given element non-uniformly if necessary such that
            the element's bounding box exactly matches the viewport rectangle.
            NB: in this case, the <meetOrSlice> value is ignored.
        */
        public const uint SVGT_ASPECT_RATIO_ALIGN_NONE          = 0;

        /*
            Force uniform scaling.
            Align the <min-x> of the source viewport with the smallest x value of the destination (drawing surface) viewport.
            Align the <min-y> of the source viewport with the smallest y value of the destination (drawing surface) viewport.
        */
        public const uint SVGT_ASPECT_RATIO_ALIGN_XMINYMIN      = 1;

        /*
            Force uniform scaling.
            Align the <mid-x> of the source viewport with the midpoint x value of the destination (drawing surface) viewport.
            Align the <min-y> of the source viewport with the smallest y value of the destination (drawing surface) viewport.
        */
        public const uint SVGT_ASPECT_RATIO_ALIGN_XMIDYMIN      = 2;

        /*
            Force uniform scaling.
            Align the <max-x> of the source viewport with the maximum x value of the destination (drawing surface) viewport.
            Align the <min-y> of the source viewport with the smallest y value of the destination (drawing surface) viewport.
        */
        public const uint SVGT_ASPECT_RATIO_ALIGN_XMAXYMIN      = 3;

        /*
            Force uniform scaling.
            Align the <min-x> of the source viewport with the smallest x value of the destination (drawing surface) viewport.
            Align the <mid-y> of the source viewport with the midpoint y value of the destination (drawing surface) viewport.
        */
        public const uint SVGT_ASPECT_RATIO_ALIGN_XMINYMID      = 4;

        /*
            Force uniform scaling.
            Align the <mid-x> of the source viewport with the midpoint x value of the destination (drawing surface) viewport.
            Align the <mid-y> of the source viewport with the midpoint y value of the destination (drawing surface) viewport.
        */
        public const uint SVGT_ASPECT_RATIO_ALIGN_XMIDYMID      = 5;

        /*
            Force uniform scaling.
            Align the <max-x> of the source viewport with the maximum x value of the destination (drawing surface) viewport.
            Align the <mid-y> of the source viewport with the midpoint y value of the destination (drawing surface) viewport.
        */
        public const uint SVGT_ASPECT_RATIO_ALIGN_XMAXYMID      = 6;

        /*
            Force uniform scaling.
            Align the <min-x> of the source viewport with the smallest x value of the destination (drawing surface) viewport.
            Align the <max-y> of the source viewport with the maximum y value of the destination (drawing surface) viewport.
        */
        public const uint SVGT_ASPECT_RATIO_ALIGN_XMINYMAX      = 7;

        /*
            Force uniform scaling.
            Align the <mid-x> of the source viewport with the midpoint x value of the destination (drawing surface) viewport.
            Align the <max-y> of the source viewport with the maximum y value of the destination (drawing surface) viewport.
        */
        public const int SVGT_ASPECT_RATIO_ALIGN_XMIDYMAX       = 8;

        /*
            Force uniform scaling.
            Align the <max-x> of the source viewport with the maximum x value of the destination (drawing surface) viewport.
            Align the <max-y> of the source viewport with the maximum y value of the destination (drawing surface) viewport.
        */
        public const uint SVGT_ASPECT_RATIO_ALIGN_XMAXYMAX     = 9;


        // -----------------------------------------------------------------------
        // SVGTAspectRatioMeetOrSlice
        // -----------------------------------------------------------------------
        /*
            Scale the graphic such that:
            - aspect ratio is preserved
            - the entire viewBox is visible within the viewport
            - the viewBox is scaled up as much as possible, while still meeting the other criteria

            In this case, if the aspect ratio of the graphic does not match the viewport, some of the viewport will
            extend beyond the bounds of the viewBox (i.e., the area into which the viewBox will draw will be smaller
            than the viewport).
        */
        public const uint SVGT_ASPECT_RATIO_MEET                = 0;

        /*
            Scale the graphic such that:
            - aspect ratio is preserved
            - the entire viewport is covered by the viewBox
            - the viewBox is scaled down as much as possible, while still meeting the other criteria
        
            In this case, if the aspect ratio of the viewBox does not match the viewport, some of the viewBox will
            extend beyond the bounds of the viewport (i.e., the area into which the viewBox will draw is larger
            than the viewport).
        */
        public const uint SVGT_ASPECT_RATIO_SLICE              = 1;

        // -----------------------------------------------------------------------
        // SVGTErrorCode
        // -----------------------------------------------------------------------
        public const uint SVGT_NO_ERROR                         =  0;
        // it indicates that the library has not previously been initialized through the svgtInit() function
        public const uint SVGT_NOT_INITIALIZED_ERROR            =  1;
        // returned when one or more invalid document/surface handles are passed to AmanithSVG functions
        public const uint SVGT_BAD_HANDLE_ERROR                 =  2;
        // returned when one or more illegal arguments are passed to AmanithSVG functions
        public const uint SVGT_ILLEGAL_ARGUMENT_ERROR           =  3;
        // all AmanithSVG functions may signal an "out of memory" error
        public const uint SVGT_OUT_OF_MEMORY_ERROR              =  4;
        // returned when an invalid or malformed XML is passed to the svgtDocCreate
        public const uint SVGT_PARSER_ERROR                     =  5;
        // returned when a document fragment is technically in error (e.g. if an element has an attribute or property value which is not permissible according to SVG specifications or if the outermost element is not an <svg> element)
        public const uint SVGT_INVALID_SVG_ERROR                =  6;
        // returned when a current packing task is still open, and so the operation (e.g. svgtPackingBinInfo) is not allowed
        public const uint SVGT_STILL_PACKING_ERROR              =  7;
        // returned when there isn't a currently open packing task, and so the operation (e.g. svgtPackingAdd) is not allowed
        public const uint SVGT_NOT_PACKING_ERROR                =  8;
        // the specified resource, via svgtResourceSet, is not valid or it does not match the given resource type
        public const uint SVGT_INVALID_RESOURCE_ERROR           =  9;
        public const uint SVGT_UNKNOWN_ERROR                    = 10;

        // -----------------------------------------------------------------------
        // SVGTRenderingQuality
        // -----------------------------------------------------------------------
        /* disables antialiasing */
        public const uint SVGT_RENDERING_QUALITY_NONANTIALIASED = 0;
        /* causes rendering to be done at the highest available speed */
        public const uint SVGT_RENDERING_QUALITY_FASTER         = 1;
        /* causes rendering to be done with the highest available quality */
        public const uint SVGT_RENDERING_QUALITY_BETTER         = 2;

        // -----------------------------------------------------------------------
        // SVGTConfig
        // -----------------------------------------------------------------------
        // Used by AmanithSVG geometric kernel to approximate curves with straight line
        // segments (flattening). Valid range is [1; 100], where 100 represents the best quality.
        public const uint SVGT_CONFIG_CURVES_QUALITY            = 1;
        // The maximum number of different threads that can "work" (e.g. create surfaces, draw documents, etc) concurrently.
        // (READ-ONLY)
        public const uint SVGT_CONFIG_MAX_CURRENT_THREADS       = 2;
        // The maximum dimension allowed for drawing surfaces, in pixels. This is the maximum valid value that can be specified as
        // 'width' and 'height' for the svgtSurfaceCreate and svgtSurfaceResize functions.
        // (READ-ONLY)
        public const uint SVGT_CONFIG_MAX_SURFACE_DIMENSION     = 3;

        // -----------------------------------------------------------------------
        // SVGTStringID
        // -----------------------------------------------------------------------
        public const uint SVGT_VENDOR                           = 1;
        public const uint SVGT_VERSION                          = 2;
        public const uint SVGT_COMPILE_CONFIG_INFO              = 3;
        public const uint SVGT_OPENVG_VERSION                   = 4;
        public const uint SVGT_OPENVG_RENDERER                  = 5;
        public const uint SVGT_OPENVG_EXTENSIONS                = 6;
        public const uint SVGT_OPENVG_COMPILE_CONFIG_INFO       = 7;

        // -----------------------------------------------------------------------
        // SVGTLogLevel
        // -----------------------------------------------------------------------
        /*
            Errors that prevent correct rendering from being completed, such that malformed
            SVG (xml) content, negative dimensions for geometric shapes, and all those cases
            where a document fragment is technically in error according to SVG specifications
        */
        public const uint SVGT_LOG_LEVEL_ERROR                  = (1 << 0);
        /*
            Warning conditions that need to be taken care of, such that an outermost <svg>
            element without a 'width' or 'height' attribute, a missing font resource, and so on
        */
        public const uint SVGT_LOG_LEVEL_WARNING                = (1 << 1);
        // Informational message
        public const uint SVGT_LOG_LEVEL_INFO                   = (1 << 2);
        // All levels enabled
        public const uint SVGT_LOG_LEVEL_ALL                    = ((1 << 3) - 1);

        // -----------------------------------------------------------------------
        // SVGTResourceType
        // -----------------------------------------------------------------------
        // font resource (vector fonts like TTF and OTF are supported, bitmap fonts are not supported)
        public const uint SVGT_RESOURCE_TYPE_FONT               = 1;
        // bitmap/image resource (only PNG and JPEG are supported; 16 bits PNG are not supported)
        public const uint SVGT_RESOURCE_TYPE_IMAGE              = 2;

        // -----------------------------------------------------------------------
        // SVGTResourceHint
        // -----------------------------------------------------------------------
        // The given font resource must be selected when the font-family attribute matches the 'serif' generic family.
        public const uint SVGT_RESOURCE_HINT_DEFAULT_SERIF_FONT         = (1 << 0);
        // The given font resource must be selected when the font-family attribute matches the 'sans-serif' generic family.
        public const uint SVGT_RESOURCE_HINT_DEFAULT_SANS_SERIF_FONT    = (1 << 1);
        // The given font resource must be selected when the font-family attribute matches the 'monospace' generic family.
        public const uint SVGT_RESOURCE_HINT_DEFAULT_MONOSPACE_FONT     = (1 << 2);
        // The given font resource must be selected when the font-family attribute matches the 'cursive' generic family.
        public const uint SVGT_RESOURCE_HINT_DEFAULT_CURSIVE_FONT       = (1 << 3);
        // The given font resource must be selected when the font-family attribute matches the 'fantasy' generic family.
        public const uint SVGT_RESOURCE_HINT_DEFAULT_FANTASY_FONT       = (1 << 4);
        // The given font resource must be selected when the font-family attribute matches the 'system-ui' generic family.
        public const uint SVGT_RESOURCE_HINT_DEFAULT_SYSTEM_UI_FONT     = (1 << 5);
        // The given font resource must be selected when the font-family attribute matches the 'ui-serif' generic family.
        public const uint SVGT_RESOURCE_HINT_DEFAULT_UI_SERIF_FONT      = (1 << 6);
        // The given font resource must be selected when the font-family attribute matches the 'ui-sans-serif' generic family.
        public const uint SVGT_RESOURCE_HINT_DEFAULT_UI_SANS_SERIF_FONT = (1 << 7);
        // The given font resource must be selected when the font-family attribute matches the 'ui-monospace' generic family.
        public const uint SVGT_RESOURCE_HINT_DEFAULT_UI_MONOSPACE_FONT  = (1 << 8);
        // The given font resource must be selected when the font-family attribute matches the 'ui-rounded' generic family.
        public const uint SVGT_RESOURCE_HINT_DEFAULT_UI_ROUNDED_FONT    = (1 << 9);
        // The given font resource must be selected when the font-family attribute matches the 'emoji' generic family.
        public const uint SVGT_RESOURCE_HINT_DEFAULT_EMOJI_FONT         = (1 << 10);
        // The given font resource must be selected when the font-family attribute matches the 'math' generic family.
        public const uint SVGT_RESOURCE_HINT_DEFAULT_MATH_FONT          = (1 << 11);
        // The given font resource must be selected when the font-family attribute matches the 'fangsong' generic family.
        public const uint SVGT_RESOURCE_HINT_DEFAULT_FANGSONG_FONT      = (1 << 12);

        /* Packed rectangle */
        [StructLayout(LayoutKind.Sequential)]
        public struct SVGTPackedRect
        {
            // 'id' attribute, NULL if not present.
            public IntPtr elemName;
            // Original rectangle corner.
            public int originalX;
            public int originalY;
            // Rectangle corner position.
            public int x;
            public int y;
            // Rectangle dimensions.
            public int width;
            public int height;
            // SVG document handle.
            public uint docHandle;
            // 0 for the whole SVG, else the element (tree) index.
            public uint elemIdx;
            // Z-order.
            public int zOrder;
            // The used destination viewport width (induced by packing scale factor).
            public float dstViewportWidth;
            // The used destination viewport height (induced by packing scale factor).
            public float dstViewportHeight;
        };

        /*
            Retrieve the error code generated by the last called API function.
            The last-error code is maintained on a per-thread basis. Multiple threads do not overwrite each other's last-error code.

            Not all functions set the last-error code; however those who do, they set the last-error code both in the event of an error and of success.
            So svgtGetLastError should be called immediately when an API function's return value indicates that such a call will return useful data.

            If the API function is not documented to set the last-error code, the value returned by this function is simply the most recent last-error code to have been set.
        */
        [DllImport(libName)]
        public static extern uint svgtGetLastError();

        /*
            Initialize the library.
            This function does not set the last-error code (see svgtGetLastError).

            It returns SVGT_NO_ERROR if the operation was completed successfully, else an error code.
            NB: in multi-thread applications, this function must be called once, before any created/spawned thread makes use of the library functions.
        */
        [DllImport(libName)]
        public static extern uint svgtInit(uint screenWidth,
                                           uint screenHeight,
                                           float screenDpi);

        /*
            Destroy the library, freeing all allocated resources.
            NB: in multi-thread applications, this function must be called once, when all created/spawned threads have finished using the library functions.

            NB: this function does not set the last-error code (see svgtGetLastError).
        */
        [DllImport(libName)]
        public static extern void svgtDone();

        /*
            Configure parameters and thresholds for the AmanithSVG library.
            This function does not set the last-error code (see svgtGetLastError).

            This function can be called at any time, but it will have effect only if:

            - the library has not been already initialized by a previous call to svgtInit, or
            - the library has been already initialized (i.e. svgtInit has been already called) but no other
              functions, except for svgtMaxCurrentThreads, svgtSurfaceMaxDimension, svgtFontResourceSet, have
              been called.

            When 'config' refers to an integer parameter, the given 'value' is converted to an integer
            using a mathematical floor operation.

            This function returns:
            - SVGT_ILLEGAL_ARGUMENT_ERROR if the specified value is not valid for the given configuration parameter
            - SVGT_NO_ERROR if the library has been already initialized and one or more functions
              different than svgtMaxCurrentThreads, svgtSurfaceMaxDimension, svgtFontResourceSet, have been called.
              In this case the function does nothing: by calling svgtConfigGet, you will get back the same value
              that the configuration parameter had before the call to svgtConfigSet.
            - SVGT_NO_ERROR in all other cases (i.e. in case of success).
        */
        [DllImport(libName)]
        public static extern uint svgtConfigSet(uint config,
                                                float value);

        /*
            Get the current value relative to the specified configuration parameter.
            This function does not set the last-error code (see svgtGetLastError).

            If the given parameter is invalid (i.e. it does not correspond to any
            value of the SVGTConfig enum type), a negative number is returned.
        */
        [DllImport(libName)]
        public static extern float svgtConfigGet(uint config);

        /*
            Set the system / user-agent language.
            This function does not set the last-error code (see svgtGetLastError).

            The given argument must be a non-NULL, non-empty list of languages separated by semicolon (e.g. en-US;en-GB;it;es)

            This function can be called at any time, but it will have effect only if:

            - the library has not been already initialized by a previous call to svgtInit, or
            - the library has been already initialized (i.e. svgtInit has been already called) but no other
              functions, except for svgtMaxCurrentThreads, svgtSurfaceMaxDimension, svgtConfigGet,
              svgtConfigSet, svgtResourceSet have been called.

            This function returns:
            - SVGT_ILLEGAL_ARGUMENT_ERROR if the given string is NULL or empty
            - SVGT_NO_ERROR if the library has been already initialized and one or more functions
              different than svgtMaxCurrentThreads, svgtSurfaceMaxDimension, svgtResourceSet, have been called.
              In this case the function does nothing.
            - SVGT_NO_ERROR in all other cases (i.e. in case of success).

            NB: the library starts with a default "en" (generic English).
        */
        [DllImport(libName)]
        public static extern uint svgtLanguageSet(string languages);

        /*
            Get the maximum number of different threads that can "work" (e.g. create surfaces, create documents and draw them) concurrently.
            This function does not set the last-error code (see svgtGetLastError).

            In multi-thread applications, each thread can only draw documents it has created, on surfaces it has created.
            In other words, a thread cannot draw a document created by another thread or on surfaces belonging to other threads.

            The function can be called at any time and always returns a valid value.
        */
        [DllImport(libName)]
        public static extern uint svgtMaxCurrentThreads();

        /*
            Instruct the library about an external resource.
            All resources must be specified in advance after the call to svgtInit
            and before to create/spawn threads (i.e. no other functions, except for
            svgtMaxCurrentThreads, svgtSurfaceMaxDimension, svgtConfigGet, svgtConfigSet,
            svgtLanguageSet have been called).

            This function does not set the last-error code (see svgtGetLastError).

            'id' must be a not-empty null-terminated string.

            'buffer' must point to a read-only area containing the resource file.
            Such read-only buffer must be a valid and immutable memory area throughout the
            whole life of the application that uses the library. In multi-thread applications
            the 'buffer' memory must be accessible (readable) by all threads.

            'type' specifies the type of resource.

            'hints' must be a valid bitwise OR of values from the SVGTResourceHint enumeration.

            If a previous resource has been specified with the given 'id', the new provided
            'buffer' pointer is used.

            This function returns:
            - SVGT_NOT_INITIALIZED_ERROR if the library has not yet been initialized (see svgtInit)
            - SVGT_ILLEGAL_ARGUMENT_ERROR if 'id' is NULL or an empty string.
            - SVGT_ILLEGAL_ARGUMENT_ERROR if 'buffer' is NULL or if 'bufferSize' is zero
            - SVGT_ILLEGAL_ARGUMENT_ERROR if 'type' is not a valid value from the SVGTResourceType enumeration
            - SVGT_ILLEGAL_ARGUMENT_ERROR if 'hints' is not a bitwise OR of values from the SVGTResourceHint enumeration
            - SVGT_INVALID_RESOURCE_ERROR if 'type' is SVGT_RESOURCE_TYPE_FONT and the given buffer does not contain a valid font file
            - SVGT_INVALID_RESOURCE_ERROR if 'type' is SVGT_RESOURCE_TYPE_IMAGE and the given buffer does not contain a valid bitmap file
            - SVGT_NO_ERROR if the operation was completed successfully
        */
        [DllImport(libName)]
        public static extern uint svgtResourceSet(string id,
                                                  IntPtr buffer,
                                                  uint bufferSize,
                                                  uint type,
                                                  uint hints);

        /*
            Get the maximum dimension allowed for drawing surfaces.
            This function does not set the last-error code (see svgtGetLastError).

            This is the maximum valid value that can be specified as 'width' and 'height' for the svgtSurfaceCreate and svgtSurfaceResize functions.
            The function can be called at any time and always returns a valid value.
        */
        [DllImport(libName)]
        public static extern uint svgtSurfaceMaxDimension();

        /*
            Set the log buffer for the current thread.
            It is important to not specify the same buffer memory for different threads, because AmanithSVG does not synchronize write
            operations to the provided log buffer: in other words, each thread must provide its own log buffer.

            'logBuffer' can be NULL or a valid pointer to a characters buffer where AmanithSVG can log messages. If NULL, logging is disabled.
            Messages will be appended one after the other, as long as there is enough free space within the buffer. When there is no
            more space, messages will stop being written to the buffer.

            'logBufferCapacity' is the capacity of 'logBuffer', in characters. If zero is specified, logging is disabled.
            AmanithSVG has no way of checking the actual buffer capacity, so it is up to the caller to specify the correct value, otherwise
            memory errors will be possible (e.g. buffer overrun).

            'logLevel' must be a bitwise OR of the desired SVGTLogLevel values. If zero is specified, logging is disabled.

            NB: after calling this function, the buffer will be initialized as empty (i.e. a '\0' character will be written at the beginning).

            This function returns:
            - SVGT_NOT_INITIALIZED_ERROR if the library has not yet been initialized (see svgtInit)
            - SVGT_ILLEGAL_ARGUMENT_ERROR if 'logLevel' is not a bitwise OR of values from the SVGTLogLevel enumeration
        */
        [DllImport(libName)]
        public static extern uint svgtLogBufferSet(IntPtr logBuffer,
                                                   uint logBufferCapacity,
                                                   uint logLevel);

        /*
            Append the given message to the log buffer.
            Log buffer must have been already set through svgtLogBufferSet function, otherwise the message will be discarded.

            'message' must be a null-terminated string.

            'level' represents the message severity, and it determines its importance.

            This function returns:
            - SVGT_NOT_INITIALIZED_ERROR if the library has not yet been initialized (see svgtInit)
            - SVGT_ILLEGAL_ARGUMENT_ERROR if 'message' is null or if 'level' is not a value from the SVGTLogLevel enumeration
            - SVGT_NO_ERROR if no buffer has been set (via svgtLogBufferSet), or if the operation was completed successfully
        */
        [DllImport(libName)]
        public static extern uint svgtLogPrint(string message,
                                               uint level);

        /*
            Get information about the current thread log buffer.
            The 'info' parameter must be an array of (at least) 4 entries, it will be filled with:

            - info[0] = log level (a bitwise OR of SVGTLogLevel enumeration)
            - info[1] = buffer capacity, in characters
            - info[2] = current length, in characters (i.e. the total number of characters written, included the trailing '\0')
            - info[3] = fullness (SVGT_TRUE if buffer is full, else SVGT_FALSE)

            NB: please note that buffer fullness does not correspond, in general, to the length == capacity condition.
            So in order to know if buffer is full (i.e. AmanithSVG can no longer write any messages to it), simply
            check the returned information in info[3] entry.

            This function returns:
            - SVGT_NOT_INITIALIZED_ERROR if the library has not yet been initialized (see svgtInit)
            - SVGT_ILLEGAL_ARGUMENT_ERROR if 'info' pointer is NULL or if it's not properly aligned
        */
        [DllImport(libName)]
        public static extern uint svgtLogBufferInfo(uint[] info);

        /*
            Create a new drawing surface, specifying its dimensions in pixels.
    
            Specified width and height must be greater than zero; they are silently clamped to the value returned by the svgtSurfaceMaxDimension function.
            The user should call svgtSurfaceWidth, svgtSurfaceHeight after svgtSurfaceCreate in order to check real drawing surface dimensions.

            Return SVGT_INVALID_HANDLE in case of errors, else a valid drawing surface handle.
        */
        [DllImport(libName)]
        public static extern uint svgtSurfaceCreate(uint width,
                                                    uint height);

        /*
            Destroy a previously created drawing surface.

            It returns SVGT_NO_ERROR if the operation was completed successfully, else an error code.
        */
        [DllImport(libName)]
        public static extern uint svgtSurfaceDestroy(uint surface);

        /*
            Resize a drawing surface, specifying new dimensions in pixels.
        
            Specified newWidth and newHeight must be greater than zero; they are silently clamped to the value returned by the svgtSurfaceMaxDimension function.
            The user should call svgtSurfaceWidth, svgtSurfaceHeight after svgtSurfaceResize in order to check real drawing surface dimensions.

            After resizing, the surface viewport will be reset to the whole surface (see svgtSurfaceViewportGet / svgtSurfaceViewportSet).

            It returns SVGT_NO_ERROR if the operation was completed successfully, else an error code.
        */
        [DllImport(libName)]
        public static extern uint svgtSurfaceResize(uint surface,
                                                    uint newWidth,
                                                    uint newHeight);

        /*
            Get width dimension (in pixels), of the specified drawing surface.
            If the specified surface handle is not valid, 0 is returned.
        */
        [DllImport(libName)]
        public static extern uint svgtSurfaceWidth(uint surface);

        /*
            Get height dimension (in pixels), of the specified drawing surface.
            If the specified surface handle is not valid, 0 is returned.
        */
        [DllImport(libName)]
        public static extern uint svgtSurfaceHeight(uint surface);

        /*
            Get access to the drawing surface pixels.
            If the specified surface handle is not valid, NULL is returned.

            Please use this function to access surface pixels for read-only purposes (e.g. blit the surface
            on the screen, according to the platform graphic subsystem, upload pixels into a GPU texture, and so on).
            Writing or modifying surface pixels by hand is still possible, but not advisable.
        */
        [DllImport(libName)]
        public static extern IntPtr svgtSurfacePixels(uint surface);

        /*
            Copy drawing surface content into the specified pixels buffer.

            This function is useful for managed environments (e.g. C#, Unity, Java, Android), where the use of a direct pixels
            access (i.e. svgtSurfacePixels) is not advisable nor comfortable.
            If the 'redBlueSwap' flag is set to SVGT_TRUE, the copy process will also swap red and blue channels for each pixel; this
            kind of swap could be useful when dealing with OpenGL/Direct3D texture uploads (RGBA or BGRA formats).
            If the 'dilateEdgesFix' flag is set to SVGT_TRUE, the copy process will also perform a 1-pixel dilate post-filter; this
            dilate filter could be useful when surface pixels will be uploaded to OpenGL/Direct3D bilinear-filtered textures.

            This function returns:
            - SVGT_BAD_HANDLE_ERROR if the specified surface handle is not valid
            - SVGT_ILLEGAL_ARGUMENT_ERROR if 'dstPixels32' pointer is NULL or if it's not properly aligned
            - SVGT_NO_ERROR if the operation was completed successfully
        */
        [DllImport(libName)]
        public static extern uint svgtSurfaceCopy(uint surface,
                                                  IntPtr dstPixels32,
                                                  uint redBlueSwap,
                                                  uint dilateEdgesFix);

        /*
            Associate a native "hardware" texture to a drawing surface, setting parameters for the copy&destroy.

            'nativeTexturePtr' must be a valid native "hardware" texture (e.g. GLuint texture "name" on OpenGL/OpenGL ES,
            ID3D11Resource on D3D11, on Metal the id<MTLTexture> pointer).

            'nativeTextureWidth' and 'nativeTextureHeight' must be greater than zero.
        */
        [DllImport(libName)]
        public static extern uint svgtSurfaceTexturePtrSet(uint surface,
                                                           IntPtr nativeTexturePtr,
                                                           uint nativeTextureWidth,
                                                           uint nativeTextureHeight,
                                                           uint nativeTextureIsBGRA,
                                                           uint dilateEdgesFix);

        /*
            Get the native code callback to queue for Unity's renderer to invoke.
        */
        [DllImport(libName)]
        public static extern IntPtr svgtSurfaceTextureCopyAndDestroyFuncGet();


        /*
            Get current destination viewport (i.e. a drawing surface rectangular area), where to map the source document viewport.

            The 'viewport' parameter must be an array of (at least) 4 float entries, it will be filled with:
            - viewport[0] = top/left x
            - viewport[1] = top/left y
            - viewport[2] = width
            - viewport[3] = height

            This function returns:
            - SVGT_BAD_HANDLE_ERROR if specified surface handle is not valid
            - SVGT_ILLEGAL_ARGUMENT_ERROR if 'viewport' pointer is NULL or if it's not properly aligned
            - SVGT_NO_ERROR if the operation was completed successfully
        */
        [DllImport(libName)]
        public static extern uint svgtSurfaceViewportGet(uint surface,
                                                         float[] viewport);

        /*
            Set destination viewport (i.e. a drawing surface rectangular area), where to map the source document viewport.

            The 'viewport' parameter must be an array of (at least) 4 float entries, it must contain:
            - viewport[0] = top/left x
            - viewport[1] = top/left y
            - viewport[2] = width
            - viewport[3] = height

            The combined use of svgtDocViewportSet and svgtSurfaceViewportSet induces a transformation matrix, that will be used
            to draw the whole SVG document. The induced matrix grants that the document viewport is mapped onto the surface
            viewport (respecting the specified alignment): all SVG content will be drawn accordingly.

            This function returns:
            - SVGT_BAD_HANDLE_ERROR if specified surface handle is not valid
            - SVGT_ILLEGAL_ARGUMENT_ERROR if 'viewport' pointer is NULL or if it's not properly aligned
            - SVGT_ILLEGAL_ARGUMENT_ERROR if specified viewport width or height are less than or equal zero
            - SVGT_NO_ERROR if the operation was completed successfully

            NB: floating-point values of NaN are treated as 0, values of +Infinity and -Infinity are clamped to the largest and smallest available float values.
        */
        [DllImport(libName)]
        public static extern uint svgtSurfaceViewportSet(uint surface,
                                                         float[] viewport);

        /*
            Clear the whole drawing surface with the given color.
            Each color component must be a number between 0 and 1. Values outside this range will be clamped.

            It returns SVGT_NO_ERROR if the operation was completed successfully, else an error code.

            NB: floating-point values of NaN are treated as 0, values of +Infinity and -Infinity are clamped to the largest and smallest available float values.
        */
        [DllImport(libName)]
        public static extern uint svgtSurfaceClear(uint surface,
                                                   float r,
                                                   float g,
                                                   float b,
                                                   float a);

        /*
            Create and load an SVG document, specifying the whole xml string.

            Return SVGT_INVALID_HANDLE in case of errors, else a valid document handle.
        */
        [DllImport(libName)]
        public static extern uint svgtDocCreate(string xmlText);

        /*
            Destroy a previously created SVG document.

            It returns SVGT_NO_ERROR if the operation was completed successfully, else an error code.
        */
        [DllImport(libName)]
        public static extern uint svgtDocDestroy(uint svgDoc);

        /*
            SVG content itself optionally can provide information about the appropriate viewport region for
            the content via the 'width' and 'height' XML attributes on the outermost <svg> element.
            Use this function to get the suggested viewport width, in pixels.

            It returns -1 (i.e. an invalid width) in the following cases:
            - the library has not previously been initialized through the svgtInit function
            - outermost element is not an <svg> element
            - outermost <svg> element doesn't have a 'width' attribute specified
            - outermost <svg> element has a 'width' attribute specified in relative measure units (i.e. em, ex, % percentage)
            In all such cases the last-error code will be set to SVGT_INVALID_SVG_ERROR.
        */
        [DllImport(libName)]
        public static extern float svgtDocWidth(uint svgDoc);

        /*
            SVG content itself optionally can provide information about the appropriate viewport region for
            the content via the 'width' and 'height' XML attributes on the outermost <svg> element.
            Use this function to get the suggested viewport height, in pixels.

            It returns -1 (i.e. an invalid height) in the following cases:
            - the library has not previously been initialized through the svgtInit function
            - outermost element is not an <svg> element
            - outermost <svg> element doesn't have a 'height' attribute specified
            - outermost <svg> element has a 'height' attribute specified in relative measure units (i.e. em, ex, % percentage)
            In all such cases the last-error code will be set to SVGT_INVALID_SVG_ERROR.
        */
        [DllImport(libName)]
        public static extern float svgtDocHeight(uint svgDoc);

        /*
            Get the document (logical) viewport to map onto the destination (drawing surface) viewport.
            When an SVG document has been created through the svgtDocCreate function, the initial value
            of its viewport is equal to the 'viewBox' attribute present in the outermost <svg> element.
            If such element does not contain the viewBox attribute, SVGT_NO_ERROR is returned and viewport
            array will be filled with zeros.

            The 'viewport' parameter must be an array of (at least) 4 float entries, it will be filled with:
            - viewport[0] = top/left x
            - viewport[1] = top/left y
            - viewport[2] = width
            - viewport[3] = height

            This function returns:
            - SVGT_BAD_HANDLE_ERROR if specified document handle is not valid
            - SVGT_ILLEGAL_ARGUMENT_ERROR if 'viewport' pointer is NULL or if it's not properly aligned
            - SVGT_NO_ERROR if the operation was completed successfully
        */
        [DllImport(libName)]
        public static extern uint svgtDocViewportGet(uint svgDoc,
                                                     float[] viewport);

        /*
            Set the document (logical) viewport to map onto the destination (drawing surface) viewport.

            The 'viewport' parameter must be an array of (at least) 4 float entries, it must contain:
            - viewport[0] = top/left x
            - viewport[1] = top/left y
            - viewport[2] = width
            - viewport[3] = height

            This function returns:
            - SVGT_BAD_HANDLE_ERROR if specified document handle is not valid
            - SVGT_ILLEGAL_ARGUMENT_ERROR if 'viewport' pointer is NULL or if it's not properly aligned
            - SVGT_ILLEGAL_ARGUMENT_ERROR if specified viewport width or height are less than or equal zero
            - SVGT_NO_ERROR if the operation was completed successfully

            NB: floating-point values of NaN are treated as 0, values of +Infinity and -Infinity are clamped to the largest and smallest available float values.
        */
        [DllImport(libName)]
        public static extern uint svgtDocViewportSet(uint svgDoc,
                                                     float[] viewport);

        /*
            Get the document alignment.
            The alignment parameter indicates whether to force uniform scaling and, if so, the alignment method to use in case
            the aspect ratio of the document viewport doesn't match the aspect ratio of the surface viewport.

            The 'values' parameter must be an array of (at least) 2 unsigned integers entries, it will be filled with:
            - values[0] = alignment (see SVGTAspectRatioAlign)
            - values[1] = meetOrSlice (see SVGTAspectRatioMeetOrSlice)

            This function returns:
            - SVGT_BAD_HANDLE_ERROR if specified document handle is not valid
            - SVGT_ILLEGAL_ARGUMENT_ERROR if 'values' pointer is NULL or if it's not properly aligned
            - SVGT_NO_ERROR if the operation was completed successfully
        */
        [DllImport(libName)]
        public static extern uint svgtDocViewportAlignmentGet(uint svgDoc,
                                                              uint[] values);

        /*
            Set the document alignment.
            The alignment parameter indicates whether to force uniform scaling and, if so, the alignment method to use in case
            the aspect ratio of the document viewport doesn't match the aspect ratio of the surface viewport.

            The 'values' parameter must be an array of (at least) 2 unsigned integers entries, it must contain:
            - values[0] = alignment (see SVGTAspectRatioAlign)
            - values[1] = meetOrSlice (see SVGTAspectRatioMeetOrSlice)

            This function returns:
            - SVGT_BAD_HANDLE_ERROR if specified document handle is not valid
            - SVGT_ILLEGAL_ARGUMENT_ERROR if 'viewport' pointer is NULL or if it's not properly aligned
            - SVGT_ILLEGAL_ARGUMENT_ERROR if specified alignment is not a valid SVGTAspectRatioAlign value
            - SVGT_ILLEGAL_ARGUMENT_ERROR if specified meetOrSlice is not a valid SVGTAspectRatioMeetOrSlice value
            - SVGT_NO_ERROR if the operation was completed successfully
        */
        [DllImport(libName)]
        public static extern uint svgtDocViewportAlignmentSet(uint svgDoc,
                                                              uint[] values);

        /*
            Draw an SVG document, over the specified drawing surface, with the given rendering quality.

            This function returns:
            - SVGT_BAD_HANDLE_ERROR if specified document handle is not valid
            - SVGT_BAD_HANDLE_ERROR if specified surface handle is not valid
            - SVGT_ILLEGAL_ARGUMENT_ERROR if specified rendering quality is not a valid value from the SVGTRenderingQuality enumeration
            - SVGT_NO_ERROR if the operation was completed successfully
        */
        [DllImport(libName)]
        public static extern uint svgtDocDraw(uint svgDoc,
                                              uint surface,
                                              uint renderingQuality);

        /*
            Map a point, expressed in the document viewport system, into the surface viewport.
            The transformation will be performed according to the current document viewport (see svgtDocViewportGet) and the
            current surface viewport (see svgtSurfaceViewportGet).

            The 'dst' parameter must be an array of (at least) 2 float entries, it will be filled with:
            - dst[0] = transformed x
            - dst[1] = transformed y

            This function returns:
            - SVGT_BAD_HANDLE_ERROR if specified document handle is not valid
            - SVGT_BAD_HANDLE_ERROR if specified surface handle is not valid
            - SVGT_ILLEGAL_ARGUMENT_ERROR if 'dst' pointer is NULL or if it's not properly aligned
            - SVGT_NO_ERROR if the operation was completed successfully

            NB: floating-point values of NaN are treated as 0, values of +Infinity and -Infinity are clamped to the largest and smallest available float values.
        */
        [DllImport(libName)]
        public static extern uint svgtPointMap(uint svgDoc,
                                               uint surface,
                                               float x,
                                               float y,
                                               float[] dst);

        /*
            Start a packing task: one or more SVG documents will be collected and packed into bins, for the generation of atlases.

            Every collected SVG document/element will be packed into rectangular bins, whose dimensions won't exceed the specified 'maxDimension', in pixels.
            If SVGT_TRUE, 'pow2Bins' will force bins to have power-of-two dimensions.
            Each rectangle will be separated from the others by the specified 'border', in pixels.
            The specified 'scale' factor will be applied to all collected SVG documents/elements, in order to realize resolution-independent atlases.

            This function returns:
            - SVGT_STILL_PACKING_ERROR if a current packing task is still open
            - SVGT_ILLEGAL_ARGUMENT_ERROR if specified 'maxDimension' is 0
            - SVGT_ILLEGAL_ARGUMENT_ERROR if 'pow2Bins' is SVGT_TRUE and the specified 'maxDimension' is not a power-of-two number
            - SVGT_ILLEGAL_ARGUMENT_ERROR if specified 'border' itself would exceed the specified 'maxDimension' (border must allow a packable region of at least one pixel)
            - SVGT_ILLEGAL_ARGUMENT_ERROR if specified 'scale' factor is less than or equal 0
            - SVGT_NO_ERROR if the operation was completed successfully

            NB: floating-point values of NaN are treated as 0, values of +Infinity and -Infinity are clamped to the largest and smallest available float values.
        */
        [DllImport(libName)]
        public static extern uint svgtPackingBegin(uint maxDimension,
                                                   uint border,
                                                   uint pow2Bins,
                                                   float scale);

        /*
            Add an SVG document to the current packing task.

            If SVGT_TRUE, 'explodeGroups' tells the packer to not pack the whole SVG document, but instead to pack each first-level element separately.
            The additional 'scale' is used to adjust the document content to the other documents involved in the current packing process.

            The 'info' parameter will return some useful information, it must be an array of (at least) 2 entries and it will be filled with:
            - info[0] = number of collected bounding boxes
            - info[1] = the actual number of packed bounding boxes (boxes whose dimensions exceed the 'maxDimension' value specified to the svgtPackingBegin function, will be discarded)
        
            This function returns:
            - SVGT_NOT_PACKING_ERROR if there isn't a currently open packing task
            - SVGT_BAD_HANDLE_ERROR if specified document handle is not valid
            - SVGT_ILLEGAL_ARGUMENT_ERROR if specified 'scale' factor is less than or equal 0
            - SVGT_ILLEGAL_ARGUMENT_ERROR if 'info' pointer is NULL or if it's not properly aligned
            - SVGT_NO_ERROR if the operation was completed successfully

            NB: floating-point values of NaN are treated as 0, values of +Infinity and -Infinity are clamped to the largest and smallest available float values.
        */
        [DllImport(libName)]
        public static extern uint svgtPackingAdd(uint svgDoc,
                                                 uint explodeGroups,
                                                 float scale,
                                                 uint[] info);

        /*
            Close the current packing task and, if specified, perform the real packing algorithm.

            All collected SVG documents/elements (actually their bounding boxes) are packed into bins for later use (i.e. atlases generation).
            After calling this function, the application could use svgtPackingBinsCount, svgtPackingBinInfo and svgtPackingDraw in order to
            get information about the resulted packed elements and draw them.

            This function returns:
            - SVGT_NOT_PACKING_ERROR if there isn't a currently open packing task
            - SVGT_NO_ERROR if the operation was completed successfully
        */
        [DllImport(libName)]
        public static extern uint svgtPackingEnd(uint performPacking);

        /*
            Return the number of generated bins from the last packing task.
        
            This function returns a negative number in case of errors (e.g. if the current packing task has not been previously closed by a call to svgtPackingEnd).
        */
        [DllImport(libName)]
        public static extern int svgtPackingBinsCount();

        /*
            Return information about the specified bin.

            The requested bin is selected by its index; the 'binInfo' parameter must be an array of (at least) 3 entries, it will be filled with:
            - binInfo[0] = bin width, in pixels
            - binInfo[1] = bin height, in pixels
            - binInfo[2] = number of packed rectangles inside the bin

            This function returns:
            - SVGT_STILL_PACKING_ERROR if a current packing task is still open
            - SVGT_ILLEGAL_ARGUMENT_ERROR if specified 'binIdx' is not valid (must be >= 0 and less than the value returned by svgtPackingBinsCount function)
            - SVGT_ILLEGAL_ARGUMENT_ERROR if 'binInfo' pointer is NULL or if it's not properly aligned
            - SVGT_NO_ERROR if the operation was completed successfully
        */
        [DllImport(libName)]
        public static extern uint svgtPackingBinInfo(uint binIdx,
                                                     uint[] binInfo);

        /*
            Get access to packed rectangles, relative to a specified bin.

            The specified 'binIdx' must be >= 0 and less than the value returned by svgtPackingBinsCount function, else a NULL pointer will be returned.
            The returned pointer contains an array of packed rectangles, whose number is equal to the one gotten through the svgtPackingBinInfo function.
            The use case for which this function was created, it's to copy and cache the result of a packing process; then when needed (e.g. requested by
            the application), the rectangles can be drawn using the svgtPackingRectsDraw function.
        */
        [DllImport(libName)]
        public static extern IntPtr svgtPackingBinRects(uint binIdx);

        /*
            Draw a set of packed SVG documents/elements over the specified drawing surface.

            The drawing surface is cleared (or not) according to the current settings (see svgtClearColor and svgtClearPerform).
            After calling svgtPackingEnd, the application could use this function in order to draw packed elements before to start another
            packing task with svgtPackingBegin.

            This function returns:
            - SVGT_STILL_PACKING_ERROR if a current packing task is still open
            - SVGT_ILLEGAL_ARGUMENT_ERROR if specified 'binIdx' is not valid (must be >= 0 and less than the value returned by svgtPackingBinsCount function)
            - SVGT_ILLEGAL_ARGUMENT_ERROR if specified 'startRectIdx', along with 'rectsCount', identifies an invalid range of rectangles; defined:

                maxCount = binInfo[2] (see svgtPackingBinInfo)
                endRectIdx = 'startRectIdx' + 'rectsCount' - 1

            it must be ensured that 'startRectIdx' < maxCount and 'endRectIdx' < maxCount, else SVGT_ILLEGAL_ARGUMENT_ERROR is returned.

            - SVGT_BAD_HANDLE_ERROR if specified surface handle is not valid
            - SVGT_NO_ERROR if the operation was completed successfully
        */
        [DllImport(libName)]
        public static extern uint svgtPackingDraw(uint binIdx,
                                                  uint startRectIdx,
                                                  uint rectsCount,
                                                  uint surface,
                                                  uint renderingQuality);

        /*
            Draw a set of packed SVG documents/elements over the specified drawing surface.

            The drawing surface is cleared (or not) according to the current settings (see svgtClearColor and svgtClearPerform).
            The specified rectangles MUST NOT point to the memory returned by svgtPackingBinRects.

            This function returns:
            - SVGT_ILLEGAL_ARGUMENT_ERROR if 'rects' pointer is NULL or if it's not properly aligned
            - SVGT_BAD_HANDLE_ERROR if specified surface handle is not valid
            - SVGT_NO_ERROR if the operation was completed successfully
        */
        [DllImport(libName)]
        public static extern uint svgtPackingRectsDraw(IntPtr rects,
                                                       uint rectsCount,
                                                       uint surface,
                                                       uint renderingQuality);

        /*
            Get renderer and version information.
            This function does not set the last-error code (see svgtGetLastError).

            If specified 'name' is not a valid value from the SVGTStringID enumeration, an empty string is returned.
        */
        [DllImport(libName)]
        public static extern IntPtr svgtGetString(uint name);

        /*
            Convert the given boolean value to SVGT_TRUE / SVGT_FALSE.
        */
        public static uint svgtBool(bool value)
        {
            return value ? SVGT_TRUE : SVGT_FALSE;
        }

        /*
            Get the description of the specified error code.
        */
        public static string svgtErrorDesc(uint errorCode)
        {
            string desc;

            switch (errorCode)
            {
                case SVGT_NOT_INITIALIZED_ERROR:
                    desc = "AmanithSVG not initialized";
                    break;
                case SVGT_BAD_HANDLE_ERROR:
                    desc = "Bad handle";
                    break;
                case SVGT_ILLEGAL_ARGUMENT_ERROR:
                    desc = "Illegal argument";
                    break;
                case SVGT_OUT_OF_MEMORY_ERROR:
                    desc = "Out of memory";
                    break;
                case SVGT_PARSER_ERROR:
                    desc = "Invalid xml";
                    break;
                case SVGT_INVALID_SVG_ERROR:
                    desc = "Outermost element is not an <svg>, or there is a circular dependency (generated by <use> elements)";
                    break;
                case SVGT_STILL_PACKING_ERROR:
                    desc = "A packing task is still open/running";
                    break;
                case SVGT_NOT_PACKING_ERROR:
                    desc = "There is not an open/running packing task";
                    break;
                case SVGT_UNKNOWN_ERROR:
                    desc = "Unknown error";
                    break;
                case SVGT_NO_ERROR:
                default:
                    desc = "";
                    break;
            }

            return desc;
        }
    }

    public enum SVGError : uint
    {
        None = AmanithSVG.SVGT_NO_ERROR,
        // it indicates that the library has not previously been initialized through the svgtInit() function
        Uninitialized = AmanithSVG.SVGT_NOT_INITIALIZED_ERROR,
        // returned when one or more invalid document/surface handles are passed to AmanithSVG functions
        BadHandle = AmanithSVG.SVGT_BAD_HANDLE_ERROR,
        // returned when one or more illegal arguments are passed to AmanithSVG functions
        IllegalArgument = AmanithSVG.SVGT_ILLEGAL_ARGUMENT_ERROR,
        // all AmanithSVG functions may signal an "out of memory" error
        OutOfMemory = AmanithSVG.SVGT_OUT_OF_MEMORY_ERROR,
        // returned when an invalid or malformed XML is passed to the svgtDocCreate
        XMLParser = AmanithSVG.SVGT_PARSER_ERROR,
        // returned when the library detects that outermost element is not an <svg> element or there is a circular dependency (usually generated by <use> elements)
        InvalidSVG = AmanithSVG.SVGT_INVALID_SVG_ERROR,
        // returned when a current packing task is still open, and so the operation (e.g. svgtPackingBinInfo) is not allowed
        StillPacking = AmanithSVG.SVGT_STILL_PACKING_ERROR,
        // returned when there isn't a currently open packing task, and so the operation (e.g. svgtPackingAdd) is not allowed
        NotPacking = AmanithSVG.SVGT_NOT_PACKING_ERROR,
        Unknown = AmanithSVG.SVGT_UNKNOWN_ERROR
    }

    public enum SVGAlign : uint
    {
        /*
            SVGAlign

            Alignment indicates whether to force uniform scaling and, if so, the alignment method to use in case the aspect ratio of the source
            viewport doesn't match the aspect ratio of the destination (drawing surface) viewport.
        */

        /*
            Do not force uniform scaling.
            Scale the graphic content of the given element non-uniformly if necessary such that
            the element's bounding box exactly matches the viewport rectangle.
            NB: in this case, the <meetOrSlice> value is ignored.
        */
        None = AmanithSVG.SVGT_ASPECT_RATIO_ALIGN_NONE,

        /*
            Force uniform scaling.
            Align the <min-x> of the source viewport with the smallest x value of the destination (drawing surface) viewport.
            Align the <min-y> of the source viewport with the smallest y value of the destination (drawing surface) viewport.
        */
        XMinYMin = AmanithSVG.SVGT_ASPECT_RATIO_ALIGN_XMINYMIN,

        /*
            Force uniform scaling.
            Align the <mid-x> of the source viewport with the midpoint x value of the destination (drawing surface) viewport.
            Align the <min-y> of the source viewport with the smallest y value of the destination (drawing surface) viewport.
        */
        XMidYMin = AmanithSVG.SVGT_ASPECT_RATIO_ALIGN_XMIDYMIN,

        /*
            Force uniform scaling.
            Align the <max-x> of the source viewport with the maximum x value of the destination (drawing surface) viewport.
            Align the <min-y> of the source viewport with the smallest y value of the destination (drawing surface) viewport.
        */
        XMaxYMin = AmanithSVG.SVGT_ASPECT_RATIO_ALIGN_XMAXYMIN,

        /*
            Force uniform scaling.
            Align the <min-x> of the source viewport with the smallest x value of the destination (drawing surface) viewport.
            Align the <mid-y> of the source viewport with the midpoint y value of the destination (drawing surface) viewport.
        */
        XMinYMid = AmanithSVG.SVGT_ASPECT_RATIO_ALIGN_XMINYMID,

        /*
            Force uniform scaling.
            Align the <mid-x> of the source viewport with the midpoint x value of the destination (drawing surface) viewport.
            Align the <mid-y> of the source viewport with the midpoint y value of the destination (drawing surface) viewport.
        */
        XMidYMid = AmanithSVG.SVGT_ASPECT_RATIO_ALIGN_XMIDYMID,

        /*
            Force uniform scaling.
            Align the <max-x> of the source viewport with the maximum x value of the destination (drawing surface) viewport.
            Align the <mid-y> of the source viewport with the midpoint y value of the destination (drawing surface) viewport.
        */
        XMaxYMid = AmanithSVG.SVGT_ASPECT_RATIO_ALIGN_XMAXYMID,

        /*
            Force uniform scaling.
            Align the <min-x> of the source viewport with the smallest x value of the destination (drawing surface) viewport.
            Align the <max-y> of the source viewport with the maximum y value of the destination (drawing surface) viewport.
        */
        XMinYMax = AmanithSVG.SVGT_ASPECT_RATIO_ALIGN_XMINYMAX,

        /*
            Force uniform scaling.
            Align the <mid-x> of the source viewport with the midpoint x value of the destination (drawing surface) viewport.
            Align the <max-y> of the source viewport with the maximum y value of the destination (drawing surface) viewport.
        */
        XMidYMax = AmanithSVG.SVGT_ASPECT_RATIO_ALIGN_XMIDYMAX,

        /*
            Force uniform scaling.
            Align the <max-x> of the source viewport with the maximum x value of the destination (drawing surface) viewport.
            Align the <max-y> of the source viewport with the maximum y value of the destination (drawing surface) viewport.
        */
        XMaxYMax = AmanithSVG.SVGT_ASPECT_RATIO_ALIGN_XMAXYMAX
    }

    public enum SVGMeetOrSlice : uint
    {
        /*
            Scale the graphic such that:
            - aspect ratio is preserved
            - the entire viewBox is visible within the viewport
            - the viewBox is scaled up as much as possible, while still meeting the other criteria

            In this case, if the aspect ratio of the graphic does not match the viewport, some of the viewport will
            extend beyond the bounds of the viewBox (i.e., the area into which the viewBox will draw will be smaller
            than the viewport).
        */
        Meet = AmanithSVG.SVGT_ASPECT_RATIO_MEET,

        /*
            Scale the graphic such that:
            - aspect ratio is preserved
            - the entire viewport is covered by the viewBox
            - the viewBox is scaled down as much as possible, while still meeting the other criteria
        
            In this case, if the aspect ratio of the viewBox does not match the viewport, some of the viewBox will
            extend beyond the bounds of the viewport (i.e., the area into which the viewBox will draw is larger
            than the viewport).
        */
        Slice = AmanithSVG.SVGT_ASPECT_RATIO_SLICE
    }

    public enum SVGRenderingQuality : uint
    {
        // Disables antialiasing
        NonAntialiased = AmanithSVG.SVGT_RENDERING_QUALITY_NONANTIALIASED,
        // Causes rendering to be done at the highest available speed
        Faster = AmanithSVG.SVGT_RENDERING_QUALITY_FASTER,
        // Causes rendering to be done with the highest available quality
        Better = AmanithSVG.SVGT_RENDERING_QUALITY_BETTER
    }

    [Flags]
    public enum SVGLogLevel : uint
    {
        // Custom name for "Nothing" option
        None = 0,
        // Error conditions
        Error = AmanithSVG.SVGT_LOG_LEVEL_ERROR,
        // Warning conditions
        Warning = AmanithSVG.SVGT_LOG_LEVEL_WARNING,
        // Informational message
        Info = AmanithSVG.SVGT_LOG_LEVEL_INFO
    }

    public enum SVGResourceType : uint
    {
        // Font resource (vector fonts like TTF and OTF are supported, bitmap fonts are not supported)
        Font = AmanithSVG.SVGT_RESOURCE_TYPE_FONT,
        // Bitmap/image resource (only PNG and JPEG are supported; 16 bits PNG are not supported)
        Image = AmanithSVG.SVGT_RESOURCE_TYPE_IMAGE
    }

    [Flags]
    public enum SVGResourceHint : uint
    {
        // Custom name for "Nothing" option
        None = 0,
        // The given font resource must be selected when the font-family attribute matches the 'serif' generic family
        DefaultSerif = AmanithSVG.SVGT_RESOURCE_HINT_DEFAULT_SERIF_FONT,
        // The given font resource must be selected when the font-family attribute matches the 'sans-serif' generic family
        DefaultSansSerif = AmanithSVG.SVGT_RESOURCE_HINT_DEFAULT_SANS_SERIF_FONT,
        // The given font resource must be selected when the font-family attribute matches the 'monospace' generic family
        DefaultMonospace = AmanithSVG.SVGT_RESOURCE_HINT_DEFAULT_MONOSPACE_FONT,
        // The given font resource must be selected when the font-family attribute matches the 'cursive' generic family
        DefaultCursive = AmanithSVG.SVGT_RESOURCE_HINT_DEFAULT_CURSIVE_FONT,
        // The given font resource must be selected when the font-family attribute matches the 'fantasy' generic family
        DefaultFantasy = AmanithSVG.SVGT_RESOURCE_HINT_DEFAULT_FANTASY_FONT,
        // The given font resource must be selected when the font-family attribute matches the 'system-ui' generic family
        DefaultSystemUI = AmanithSVG.SVGT_RESOURCE_HINT_DEFAULT_SYSTEM_UI_FONT,
        // The given font resource must be selected when the font-family attribute matches the 'ui-serif' generic family
        DefaultUISerif = AmanithSVG.SVGT_RESOURCE_HINT_DEFAULT_UI_SERIF_FONT,
        // The given font resource must be selected when the font-family attribute matches the 'ui-sans-serif' generic family
        DefaultUISansSerif = AmanithSVG.SVGT_RESOURCE_HINT_DEFAULT_UI_SANS_SERIF_FONT,
        // The given font resource must be selected when the font-family attribute matches the 'ui-monospace' generic family
        DefaultUIMonospace = AmanithSVG.SVGT_RESOURCE_HINT_DEFAULT_UI_MONOSPACE_FONT,
        // The given font resource must be selected when the font-family attribute matches the 'ui-rounded' generic family
        DefaultUIRounded = AmanithSVG.SVGT_RESOURCE_HINT_DEFAULT_UI_ROUNDED_FONT,
        // The given font resource must be selected when the font-family attribute matches the 'emoji' generic family
        DefaultEmoji = AmanithSVG.SVGT_RESOURCE_HINT_DEFAULT_EMOJI_FONT,
        // The given font resource must be selected when the font-family attribute matches the 'math' generic family
        DefaultMath = AmanithSVG.SVGT_RESOURCE_HINT_DEFAULT_MATH_FONT,
        // The given font resource must be selected when the font-family attribute matches the 'fangsong' generic family
        DefaultFangsong = AmanithSVG.SVGT_RESOURCE_HINT_DEFAULT_FANGSONG_FONT
    }

    /*
        Simple color class.
    */
    public class SVGColor
    {
        // Constructor.
        public SVGColor()
        {
            Red = 1.0f;
            Green = 1.0f;
            Blue = 1.0f;
            Alpha = 0.0f;
        }

        // Set constructor.
        public SVGColor(float r, float g, float b) : this(r, g, b, 1.0f)
        {
        }

        // Set constructor.
        public SVGColor(float r, float g, float b, float a)
        {
            // clamp each component in the [0; 1] range
            Red = (r < 0.0f) ? 0.0f : (r > 1.0f) ? 1.0f : r;
            Green = (g < 0.0f) ? 0.0f : (g > 1.0f) ? 1.0f : g;
            Blue = (b < 0.0f) ? 0.0f : (b > 1.0f) ? 1.0f : b;
            Alpha = (a < 0.0f) ? 0.0f : (a > 1.0f) ? 1.0f : a;
        }

        // Red component (read only).
        public float Red { get; }

        // Green component (read only).
        public float Green { get; }

        // Blue component (read only).
        public float Blue { get; }

        // Alpha component (read only).
        public float Alpha { get; }

        public static readonly SVGColor Aliceblue = new SVGColor(0.941f, 0.973f, 1.000f);
        public static readonly SVGColor Antiquewhite = new SVGColor(0.980f, 0.922f, 0.843f);
        public static readonly SVGColor Aqua = new SVGColor(0.000f, 1.000f, 1.000f);
        public static readonly SVGColor Aquamarine = new SVGColor(0.498f, 1.000f, 0.831f);
        public static readonly SVGColor Azure = new SVGColor(0.941f, 1.000f, 1.000f);
        public static readonly SVGColor Beige = new SVGColor(0.961f, 0.961f, 0.863f);
        public static readonly SVGColor Bisque = new SVGColor(1.000f, 0.894f, 0.769f);
        public static readonly SVGColor Black = new SVGColor(0.000f, 0.000f, 0.000f);
        public static readonly SVGColor Blanchedalmond = new SVGColor(1.000f, 0.922f, 0.804f);
        public static readonly SVGColor Blueviolet = new SVGColor(0.541f, 0.169f, 0.886f);
        public static readonly SVGColor Brown = new SVGColor(0.647f, 0.165f, 0.165f);
        public static readonly SVGColor Burlywood = new SVGColor(0.871f, 0.722f, 0.529f);
        public static readonly SVGColor Cadetblue = new SVGColor(0.373f, 0.620f, 0.627f);
        public static readonly SVGColor Chartreuse = new SVGColor(0.498f, 1.000f, 0.000f);
        public static readonly SVGColor Chocolate = new SVGColor(0.824f, 0.412f, 0.118f);
        public static readonly SVGColor Coral = new SVGColor(1.000f, 0.498f, 0.314f);
        public static readonly SVGColor Cornflowerblue = new SVGColor(0.392f, 0.584f, 0.929f);
        public static readonly SVGColor Cornsilk = new SVGColor(1.000f, 0.973f, 0.863f);
        public static readonly SVGColor Crimson = new SVGColor(0.863f, 0.078f, 0.235f);
        public static readonly SVGColor Cyan = new SVGColor(0.000f, 1.000f, 1.000f);
        public static readonly SVGColor Darkblue = new SVGColor(0.000f, 0.000f, 0.545f);
        public static readonly SVGColor Darkcyan = new SVGColor(0.000f, 0.545f, 0.545f);
        public static readonly SVGColor Darkgoldenrod = new SVGColor(0.722f, 0.525f, 0.043f);
        public static readonly SVGColor Darkgray = new SVGColor(0.663f, 0.663f, 0.663f);
        public static readonly SVGColor Darkgreen = new SVGColor(0.000f, 0.392f, 0.000f);
        public static readonly SVGColor Darkgrey = new SVGColor(0.663f, 0.663f, 0.663f);
        public static readonly SVGColor Darkkhaki = new SVGColor(0.741f, 0.718f, 0.420f);
        public static readonly SVGColor Darkmagenta = new SVGColor(0.545f, 0.000f, 0.545f);
        public static readonly SVGColor Darkolivegreen = new SVGColor(0.333f, 0.420f, 0.184f);
        public static readonly SVGColor Darkorange = new SVGColor(1.000f, 0.549f, 0.000f);
        public static readonly SVGColor Darkorchid = new SVGColor(0.600f, 0.196f, 0.800f);
        public static readonly SVGColor Darkred = new SVGColor(0.545f, 0.000f, 0.000f);
        public static readonly SVGColor Darksalmon = new SVGColor(0.914f, 0.588f, 0.478f);
        public static readonly SVGColor Darkseagreen = new SVGColor(0.561f, 0.737f, 0.561f);
        public static readonly SVGColor Darkslateblue = new SVGColor(0.282f, 0.239f, 0.545f);
        public static readonly SVGColor Darkslategray = new SVGColor(0.184f, 0.310f, 0.310f);
        public static readonly SVGColor Darkslategrey = new SVGColor(0.184f, 0.310f, 0.310f);
        public static readonly SVGColor Darkturquoise = new SVGColor(0.000f, 0.808f, 0.820f);
        public static readonly SVGColor Darkviolet = new SVGColor(0.580f, 0.000f, 0.827f);
        public static readonly SVGColor Deeppink = new SVGColor(1.000f, 0.078f, 0.576f);
        public static readonly SVGColor Deepskyblue = new SVGColor(0.000f, 0.749f, 1.000f);
        public static readonly SVGColor Dimgray = new SVGColor(0.412f, 0.412f, 0.412f);
        public static readonly SVGColor Dimgrey = new SVGColor(0.412f, 0.412f, 0.412f);
        public static readonly SVGColor Dodgerblue = new SVGColor(0.118f, 0.565f, 1.000f);
        public static readonly SVGColor Firebrick = new SVGColor(0.698f, 0.133f, 0.133f);
        public static readonly SVGColor Floralwhite = new SVGColor(1.000f, 0.980f, 0.941f);
        public static readonly SVGColor Forestgreen = new SVGColor(0.133f, 0.545f, 0.133f);
        public static readonly SVGColor Fuchsia = new SVGColor(1.000f, 0.000f, 1.000f);
        public static readonly SVGColor Gainsboro = new SVGColor(0.863f, 0.863f, 0.863f);
        public static readonly SVGColor Ghostwhite = new SVGColor(0.973f, 0.973f, 1.000f);
        public static readonly SVGColor Gold = new SVGColor(1.000f, 0.843f, 0.000f);
        public static readonly SVGColor Goldenrod = new SVGColor(0.855f, 0.647f, 0.125f);
        public static readonly SVGColor Gray = new SVGColor(0.502f, 0.502f, 0.502f);
        public static readonly SVGColor Greenyellow = new SVGColor(0.678f, 1.000f, 0.184f);
        public static readonly SVGColor Grey = new SVGColor(0.502f, 0.502f, 0.502f);
        public static readonly SVGColor Honeydew = new SVGColor(0.941f, 1.000f, 0.941f);
        public static readonly SVGColor Hotpink = new SVGColor(1.000f, 0.412f, 0.706f);
        public static readonly SVGColor Indianred = new SVGColor(0.804f, 0.361f, 0.361f);
        public static readonly SVGColor Indigo = new SVGColor(0.294f, 0.000f, 0.510f);
        public static readonly SVGColor Ivory = new SVGColor(1.000f, 1.000f, 0.941f);
        public static readonly SVGColor Khaki = new SVGColor(0.941f, 0.902f, 0.549f);
        public static readonly SVGColor Lavender = new SVGColor(0.902f, 0.902f, 0.980f);
        public static readonly SVGColor Lavenderblush = new SVGColor(1.000f, 0.941f, 0.961f);
        public static readonly SVGColor Lawngreen = new SVGColor(0.486f, 0.988f, 0.000f);
        public static readonly SVGColor Lemonchiffon = new SVGColor(1.000f, 0.980f, 0.804f);
        public static readonly SVGColor Lightblue = new SVGColor(0.678f, 0.847f, 0.902f);
        public static readonly SVGColor Lightcoral = new SVGColor(0.941f, 0.502f, 0.502f);
        public static readonly SVGColor Lightcyan = new SVGColor(0.878f, 1.000f, 1.000f);
        public static readonly SVGColor Lightgoldenrodyellow = new SVGColor(0.980f, 0.980f, 0.824f);
        public static readonly SVGColor Lightgray = new SVGColor(0.827f, 0.827f, 0.827f);
        public static readonly SVGColor Lightgreen = new SVGColor(0.565f, 0.933f, 0.565f);
        public static readonly SVGColor Lightgrey = new SVGColor(0.827f, 0.827f, 0.827f);
        public static readonly SVGColor Lightpink = new SVGColor(1.000f, 0.714f, 0.757f);
        public static readonly SVGColor Lightsalmon = new SVGColor(1.000f, 0.627f, 0.478f);
        public static readonly SVGColor Lightseagreen = new SVGColor(0.125f, 0.698f, 0.667f);
        public static readonly SVGColor Lightskyblue = new SVGColor(0.529f, 0.808f, 0.980f);
        public static readonly SVGColor Lightslategray = new SVGColor(0.467f, 0.533f, 0.600f);
        public static readonly SVGColor Lightslategrey = new SVGColor(0.467f, 0.533f, 0.600f);
        public static readonly SVGColor Lightsteelblue = new SVGColor(0.690f, 0.769f, 0.871f);
        public static readonly SVGColor Lightyellow = new SVGColor(1.000f, 1.000f, 0.878f);
        public static readonly SVGColor Lime = new SVGColor(0.000f, 1.000f, 0.000f);
        public static readonly SVGColor Limegreen = new SVGColor(0.196f, 0.804f, 0.196f);
        public static readonly SVGColor Linen = new SVGColor(0.980f, 0.941f, 0.902f);
        public static readonly SVGColor Magenta = new SVGColor(1.000f, 0.000f, 1.000f);
        public static readonly SVGColor Maroon = new SVGColor(0.502f, 0.000f, 0.000f);
        public static readonly SVGColor Mediumaquamarine = new SVGColor(0.400f, 0.804f, 0.667f);
        public static readonly SVGColor Mediumblue = new SVGColor(0.000f, 0.000f, 0.804f);
        public static readonly SVGColor Mediumorchid = new SVGColor(0.729f, 0.333f, 0.827f);
        public static readonly SVGColor Mediumpurple = new SVGColor(0.576f, 0.439f, 0.859f);
        public static readonly SVGColor Mediumseagreen = new SVGColor(0.235f, 0.702f, 0.443f);
        public static readonly SVGColor Mediumslateblue = new SVGColor(0.482f, 0.408f, 0.933f);
        public static readonly SVGColor Mediumspringgreen = new SVGColor(0.000f, 0.980f, 0.604f);
        public static readonly SVGColor Mediumturquoise = new SVGColor(0.282f, 0.820f, 0.800f);
        public static readonly SVGColor Mediumvioletred = new SVGColor(0.780f, 0.082f, 0.522f);
        public static readonly SVGColor Midnightblue = new SVGColor(0.098f, 0.098f, 0.439f);
        public static readonly SVGColor Mintcream = new SVGColor(0.961f, 1.000f, 0.980f);
        public static readonly SVGColor Mistyrose = new SVGColor(1.000f, 0.894f, 0.882f);
        public static readonly SVGColor Moccasin = new SVGColor(1.000f, 0.894f, 0.710f);
        public static readonly SVGColor Navajowhite = new SVGColor(1.000f, 0.871f, 0.678f);
        public static readonly SVGColor Navy = new SVGColor(0.000f, 0.000f, 0.502f);
        public static readonly SVGColor Oldlace = new SVGColor(0.992f, 0.961f, 0.902f);
        public static readonly SVGColor Olive = new SVGColor(0.502f, 0.502f, 0.000f);
        public static readonly SVGColor Olivedrab = new SVGColor(0.420f, 0.557f, 0.137f);
        public static readonly SVGColor Orange = new SVGColor(1.000f, 0.647f, 0.000f);
        public static readonly SVGColor Orangered = new SVGColor(1.000f, 0.271f, 0.000f);
        public static readonly SVGColor Orchid = new SVGColor(0.855f, 0.439f, 0.839f);
        public static readonly SVGColor Palegoldenrod = new SVGColor(0.933f, 0.910f, 0.667f);
        public static readonly SVGColor Palegreen = new SVGColor(0.596f, 0.984f, 0.596f);
        public static readonly SVGColor Paleturquoise = new SVGColor(0.686f, 0.933f, 0.933f);
        public static readonly SVGColor Palevioletred = new SVGColor(0.859f, 0.439f, 0.576f);
        public static readonly SVGColor Papayawhip = new SVGColor(1.000f, 0.937f, 0.835f);
        public static readonly SVGColor Peachpuff = new SVGColor(1.000f, 0.855f, 0.725f);
        public static readonly SVGColor Peru = new SVGColor(0.804f, 0.522f, 0.247f);
        public static readonly SVGColor Pink = new SVGColor(1.000f, 0.753f, 0.796f);
        public static readonly SVGColor Plum = new SVGColor(0.867f, 0.627f, 0.867f);
        public static readonly SVGColor Powderblue = new SVGColor(0.690f, 0.878f, 0.902f);
        public static readonly SVGColor Purple = new SVGColor(0.502f, 0.000f, 0.502f);
        public static readonly SVGColor Rosybrown = new SVGColor(0.737f, 0.561f, 0.561f);
        public static readonly SVGColor Royalblue = new SVGColor(0.255f, 0.412f, 0.882f);
        public static readonly SVGColor Saddlebrown = new SVGColor(0.545f, 0.271f, 0.075f);
        public static readonly SVGColor Salmon = new SVGColor(0.980f, 0.502f, 0.447f);
        public static readonly SVGColor Sandybrown = new SVGColor(0.957f, 0.643f, 0.376f);
        public static readonly SVGColor Seagreen = new SVGColor(0.180f, 0.545f, 0.341f);
        public static readonly SVGColor Seashell = new SVGColor(1.000f, 0.961f, 0.933f);
        public static readonly SVGColor Sienna = new SVGColor(0.627f, 0.322f, 0.176f);
        public static readonly SVGColor Silver = new SVGColor(0.753f, 0.753f, 0.753f);
        public static readonly SVGColor Skyblue = new SVGColor(0.529f, 0.808f, 0.922f);
        public static readonly SVGColor Slateblue = new SVGColor(0.416f, 0.353f, 0.804f);
        public static readonly SVGColor Slategray = new SVGColor(0.439f, 0.502f, 0.565f);
        public static readonly SVGColor Slategrey = new SVGColor(0.439f, 0.502f, 0.565f);
        public static readonly SVGColor Snow = new SVGColor(1.000f, 0.980f, 0.980f);
        public static readonly SVGColor Springgreen = new SVGColor(0.000f, 1.000f, 0.498f);
        public static readonly SVGColor Steelblue = new SVGColor(0.275f, 0.510f, 0.706f);
        public static readonly SVGColor Tan = new SVGColor(0.824f, 0.706f, 0.549f);
        public static readonly SVGColor Teal = new SVGColor(0.000f, 0.502f, 0.502f);
        public static readonly SVGColor Thistle = new SVGColor(0.847f, 0.749f, 0.847f);
        public static readonly SVGColor Tomato = new SVGColor(1.000f, 0.388f, 0.278f);
        public static readonly SVGColor Turquoise = new SVGColor(0.251f, 0.878f, 0.816f);
        public static readonly SVGColor Violet = new SVGColor(0.933f, 0.510f, 0.933f);
        public static readonly SVGColor Wheat = new SVGColor(0.961f, 0.871f, 0.702f);
        public static readonly SVGColor White = new SVGColor(1.000f, 1.000f, 1.000f);
        public static readonly SVGColor Whitesmoke = new SVGColor(0.961f, 0.961f, 0.961f);
        public static readonly SVGColor Yellow = new SVGColor(1.000f, 1.000f, 0.000f);
        public static readonly SVGColor Yellowgreen = new SVGColor(0.604f, 0.804f, 0.196f);
        public static readonly SVGColor Clear = new SVGColor(0.0f, 0.0f, 0.0f, 0.0f);
    }

    /*
        Simple 2D point class (float coordinates).
    */
    public class SVGPoint
    {
        // Constructor.
        public SVGPoint()
        {
            X = 0.0f;
            Y = 0.0f;
        }

        // Set constructor.
        public SVGPoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        // Abscissa.
        public float X { get; }

        // Ordinate.
        public float Y { get; }
    }

    /*
        This class represents a couple of values, one taken from the SVGAlign enum
        type and the other taken from the SVGMeetOrSlice enum type.
    
        Alignment indicates whether to force uniform scaling and, if so, the alignment
        method to use in case the aspect ratio of the source viewport doesn't match the
        aspect ratio of the destination viewport.
    */
    public class SVGAspectRatio
    {
        // Constructor.
        public SVGAspectRatio()
        {
            _alignment = SVGAlign.XMidYMid;
            _meetOrSlice = SVGMeetOrSlice.Meet;
            Changed = true;
        }

        // Set constructor.
        public SVGAspectRatio(SVGAlign alignment, SVGMeetOrSlice meetOrSlice)
        {
            _alignment = alignment;
            _meetOrSlice = meetOrSlice;
            Changed = true;
        }

        // Alignment.
        public SVGAlign Alignment
        {
            get
            {
                return _alignment;
            }
            set
            {
                _alignment = value;
                Changed = true;
            }
        }

        // Meet or slice.
        public SVGMeetOrSlice MeetOrSlice
        {
            get
            {
                return _meetOrSlice;
            }
            set
            {
                _meetOrSlice = value;
                Changed = true;
            }
        }

        // Keep track of changes.
        internal bool Changed { get; set; }

        // Alignment.
        private SVGAlign _alignment;
        // Meet or slice.
        private SVGMeetOrSlice _meetOrSlice;
    }

    /*
        SVG viewport.

        A viewport represents a rectangular area, specified by its top/left corner, a width and an height.
        The positive x-axis points towards the right, the positive y-axis points down.
    */
    public class SVGViewport
    {
        // Constructor.
        public SVGViewport()
        {
            _x = 0.0f;
            _y = 0.0f;
            _width = 0.0f;
            _height = 0.0f;
            Changed = true;
        }

        // Set constructor.
        public SVGViewport(float x, float y, float width, float height)
        {
            _x = x;
            _y = y;
            _width = (width < 0.0f) ? 0.0f : width;
            _height = (height < 0.0f) ? 0.0f : height;
            Changed = true;
        }

        // Top/left corner, abscissa.
        public float X
        {
            get
            {
                return _x;
            }
            set
            {
                _x = value;
                Changed = true;
            }
        }

        // Top/left corner, ordinate.
        public float Y
        {
            get
            {
                return _y;
            }
            set
            {
                _y = value;
                Changed = true;
            }
        }

        // Viewport width.
        public float Width
        {
            get
            {
                return _width;
            }
            set
            {
                _width = (value < 0.0f) ? 0.0f : value;
                Changed = true;
            }
        }

        // Viewport height.
        public float Height
        {
            get
            {
                return _height;
            }
            set
            {
                _height = (value < 0.0f) ? 0.0f : value;
                Changed = true;
            }
        }

        // Keep track of changes.
        internal bool Changed { get; set; }

        // Top/left corner, x.
        private float _x;
        // Top/left corner, y.
        private float _y;
        // Viewport width.
        private float _width;
        // Viewport height.
        private float _height;
    }

    /*
        SVG document.

        An SVG document can be created through SVGAssets.CreateDocument function, specifying the xml text.
        The document will be parsed immediately, and the internal drawing tree will be created.

        Once the document has been created, it can be drawn several times onto one (or more) drawing surface.
        In order to draw a document:

        (1) create a drawing surface using SVGAssets.CreateSurface
        (2) call surface Draw method, specifying the document to draw
    */
    public class SVGDocument : IDisposable
    {
        // Constructor.
        internal SVGDocument(uint handle)
        {
            uint err;
            float[] viewport = new float[4];
            uint[] aspectRatio = new uint[2];

            // keep track of the AmanithSVG document handle
            Handle = handle;
            _disposed = false;

            // get document viewport
            if ((err = AmanithSVG.svgtDocViewportGet(Handle, viewport)) == AmanithSVG.SVGT_NO_ERROR)
            {
                _viewport = new SVGViewport(viewport[0], viewport[1], viewport[2], viewport[3]);
            }
            else
            {
                _viewport = null;
                // log an error message
                SVGAssets.LogError("SVGDocument::SVGDocument getting viewport failed", (SVGError)err);
            }

            // get viewport aspect ratio/alignment
            if ((err = AmanithSVG.svgtDocViewportAlignmentGet(Handle, aspectRatio)) == AmanithSVG.SVGT_NO_ERROR)
            {
                _aspectRatio = new SVGAspectRatio((SVGAlign)aspectRatio[0], (SVGMeetOrSlice)aspectRatio[1]);
            }
            else
            {
                _aspectRatio = null;
                // log an error message
                SVGAssets.LogError("SVGDocument::SVGDocument getting aspect ratio failed ", (SVGError)err);
            }
        }

        // Destructor.
        ~SVGDocument()
        {
            Dispose(false);
        }

        // Implement IDisposable.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // check to see if Dispose has already been called
            if (!_disposed)
            {
                // if disposing equals true, dispose all managed and unmanaged resources
                if (disposing)
                {
                    // dispose managed resources (nothing to do here)
                }
                // dispose unmanaged resources
                if (Handle != AmanithSVG.SVGT_INVALID_HANDLE)
                {
                    AmanithSVG.svgtDocDestroy(Handle);
                    Handle = AmanithSVG.SVGT_INVALID_HANDLE;
                }
                // disposing has been done
                _disposed = true;
            }
        }

        // If needed, update document viewport at AmanithSVG backend side; it returns SVGError.None if the operation was completed successfully, else an error code.
        internal SVGError UpdateViewport()
        {
            uint err = AmanithSVG.SVGT_NO_ERROR;

            // set document viewport (AmanithSVG backend)
            if ((_viewport != null) && _viewport.Changed)
            {
                float[] viewport = new float[4] { _viewport.X, _viewport.Y, _viewport.Width, _viewport.Height };
                if ((err = AmanithSVG.svgtDocViewportSet(Handle, viewport)) != AmanithSVG.SVGT_NO_ERROR)
                {
                    // log an error message
                    SVGAssets.LogError("SVGDocument::UpdateViewport setting viewport failed", (SVGError)err);
                }
                else
                {
                    _viewport.Changed = false;
                }
            }

            // set document viewport aspect ratio/alignment (AmanithSVG backend)
            if ((err == AmanithSVG.SVGT_NO_ERROR) && (_aspectRatio != null) && _aspectRatio.Changed)
            {
                uint[] aspectRatio = new uint[2] { (uint)_aspectRatio.Alignment, (uint)_aspectRatio.MeetOrSlice };
                if ((err = AmanithSVG.svgtDocViewportAlignmentSet(Handle, aspectRatio)) != AmanithSVG.SVGT_NO_ERROR)
                {
                    // log an error message
                    SVGAssets.LogError("SVGDocument::UpdateViewport setting aspect ratio failed", (SVGError)err);
                }
                else
                {
                    _aspectRatio.Changed = false;
                }
            }

            return (SVGError)err;
        }

        /*
            Map a point, expressed in the document viewport system, into the surface viewport.
            The transformation will be performed according to the current document viewport and the
            current surface viewport.
        */
        public SVGPoint PointMap(SVGSurface surface,
                                 SVGPoint p)
        {
            SVGPoint zero = new SVGPoint(0.0f, 0.0f);

            if ((surface != null) && (p != null))
            {
                uint err;
                float[] dst = new float[2];

                // update document viewport (AmanithSVG backend)
                if (UpdateViewport() != SVGError.None)
                {
                    return zero;
                }
                // update surface viewport (AmanithSVG backend)
                if (surface.UpdateViewport() != SVGError.None)
                {
                    return zero;
                }
                // map the specified point
                if ((err = AmanithSVG.svgtPointMap(Handle, surface.Handle, p.X, p.Y, dst)) != AmanithSVG.SVGT_NO_ERROR)
                {
                    // log an error message
                    SVGAssets.LogError("SVGDocument::PointMap mapping point failed", (SVGError)err);
                    return zero;
                }
                // return the result
                return new SVGPoint(dst[0], dst[1]);
            }
            else
            {
                return zero;
            }
        }


        // AmanithSVG document handle (read only).
        public uint Handle { get; protected set; } = AmanithSVG.SVGT_INVALID_HANDLE;

        /*
            SVG content itself optionally can provide information about the appropriate viewport region for
            the content via the 'width' and 'height' XML attributes on the outermost <svg> element.
            Use this property to get the suggested viewport width, in pixels.

            It returns a negative number (i.e. an invalid width) in the following cases:
            - the library has not previously been initialized through the svgtInit function
            - outermost element is not an <svg> element
            - outermost <svg> element doesn't have a 'width' attribute specified
            - outermost <svg> element has a negative 'width' attribute specified (e.g. width="-50")
            - outermost <svg> element has a 'width' attribute specified in relative measure units (i.e. em, ex, % percentage)
        */
        public float Width
        {
            get
            {
                return AmanithSVG.svgtDocWidth(Handle);
            }
        }

        /*
            SVG content itself optionally can provide information about the appropriate viewport region for
            the content via the 'width' and 'height' XML attributes on the outermost <svg> element.
            Use this property to get the suggested viewport height, in pixels.

            It returns a negative number (i.e. an invalid height) in the following cases:
            - the library has not previously been initialized through the svgtInit function
            - outermost element is not an <svg> element
            - outermost <svg> element doesn't have a 'height' attribute specified
            - outermost <svg> element has a negative 'height' attribute specified (e.g. height="-30")
            - outermost <svg> element has a 'height' attribute specified in relative measure units (i.e. em, ex, % percentage)
        */
        public float Height
        {
            get
            {
                return AmanithSVG.svgtDocHeight(Handle);
            }
        }

        /*
            The document (logical) viewport to map onto the destination (drawing surface) viewport.
            When an SVG document has been created through the SVGAssets.CreateDocument function, the initial
            value of its viewport is equal to the 'viewBox' attribute present in the outermost <svg> element.
        */
        public SVGViewport Viewport
        {
            get
            {
                return _viewport;
            }
            set
            {
                if (value != null)
                {
                    _viewport = value;
                }
            }
        }

        /*
            Viewport aspect ratio.
            The alignment parameter indicates whether to force uniform scaling and, if so, the alignment method to use in case
            the aspect ratio of the document viewport doesn't match the aspect ratio of the surface viewport.
        */
        public SVGAspectRatio AspectRatio
        {
            get
            {
                return _aspectRatio;
            }
            set
            {
                if (value != null)
                {
                    _aspectRatio = value;
                }
            }
        }

        // Track whether Dispose has been called.
        private bool _disposed;
        // Viewport.
        private SVGViewport _viewport;
        // Viewport aspect ratio/alignment.
        private SVGAspectRatio _aspectRatio;
    }

    /*
        Drawing surface.

        A drawing surface is just a rectangular area made of pixels, where each pixel is represented internally by a 32bit unsigned integer.
        A pixel is made of four 8-bit components: red, green, blue, alpha.
 
        Coordinate system is the same of SVG specifications: top/left pixel has coordinate (0, 0), with the positive x-axis pointing towards
        the right and the positive y-axis pointing down.
    */
    public class SVGSurface : IDisposable
    {
        // Constructor.
        internal SVGSurface(uint handle)
        {
            uint err;
            float[] viewport = new float[4];

            // keep track of the AmanithSVG surface handle
            Handle = handle;
            _disposed = false;

            // get surface viewport
            if ((err = AmanithSVG.svgtSurfaceViewportGet(Handle, viewport)) == AmanithSVG.SVGT_NO_ERROR)
            {
                _viewport = new SVGViewport(viewport[0], viewport[1], viewport[2], viewport[3]);
            }
            else
            {
                _viewport = null;
                // log an error message
                SVGAssets.LogError("SVGSurface::SVGSurface getting viewport failed", (SVGError)err);
            }
        }

        // Destructor.
        ~SVGSurface()
        {
            Dispose(false);
        }

        // Implement IDisposable.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // check to see if Dispose has already been called
            if (!_disposed)
            {
                // if disposing equals true, dispose all managed and unmanaged resources
                if (disposing)
                {
                    // dispose managed resources (nothing to do here)
                }
                // dispose unmanaged resources
                if (Handle != AmanithSVG.SVGT_INVALID_HANDLE)
                {
                    AmanithSVG.svgtSurfaceDestroy(Handle);
                    Handle = AmanithSVG.SVGT_INVALID_HANDLE;
                }
                // disposing has been done
                _disposed = true;
            }
        }

        /*
            Resize the surface, specifying new dimensions in pixels.
            After resizing, the surface viewport will be reset to the whole surface.

            It returns SVGError.None if the operation was completed successfully, else an error code.
        */
        public SVGError Resize(uint newWidth,
                               uint newHeight)
        {
            uint err;

            if ((err = AmanithSVG.svgtSurfaceResize(Handle, newWidth, newHeight)) != AmanithSVG.SVGT_NO_ERROR)
            {
                SVGAssets.LogError("SVGSurface::Resize resizing failed", (SVGError)err);
            }
            else
            {
                // svgtSurfaceResize will reset the surface viewport, so we must perform the same operation here
                _viewport = new SVGViewport(0.0f, 0.0f, Width, Height);
            }

            return (SVGError)err;
        }

        // If needed, update surface viewport at AmanithSVG backend side; it returns SVGError.None if the operation was completed successfully, else an error code.
        internal SVGError UpdateViewport()
        {
            uint err = AmanithSVG.SVGT_NO_ERROR;

            // set surface viewport (AmanithSVG backend)
            if ((_viewport != null) && _viewport.Changed)
            {
                float[] viewport = new float[4] { _viewport.X, _viewport.Y, _viewport.Width, _viewport.Height };
                if ((err = AmanithSVG.svgtSurfaceViewportSet(Handle, viewport)) != AmanithSVG.SVGT_NO_ERROR)
                {
                    // log an error message
                    SVGAssets.LogError("SVGSurface::UpdateViewport setting viewport failed", (SVGError)err);
                }
                else
                {
                    _viewport.Changed = false;
                }
            }

            return (SVGError)err;
        }

        /*
            Draw an SVG document, on this drawing surface.

            First the drawing surface is cleared if a valid (i.e. not null) clear color is provided.
            Then the specified document, if valid, is drawn.

            It returns SVGError.None if the operation was completed successfully, else an error code.
        */
        public SVGError Draw(SVGDocument document,
                             SVGColor clearColor,
                             SVGRenderingQuality renderingQuality)
        {
            uint err = AmanithSVG.SVGT_NO_ERROR;

            if (clearColor != null)
            {
                // clear the surface
                err = AmanithSVG.svgtSurfaceClear(Handle, clearColor.Red, clearColor.Green, clearColor.Blue, clearColor.Alpha);
                if (err != AmanithSVG.SVGT_NO_ERROR)
                {
                    SVGAssets.LogError("SVGSurface::Draw clearing surface failed", (SVGError)err);
                }
            }

            if ((err == AmanithSVG.SVGT_NO_ERROR) && (document != null))
            {
                // update document viewport (AmanithSVG backend)
                if ((err = (uint)document.UpdateViewport()) == AmanithSVG.SVGT_NO_ERROR)
                {
                    // update surface viewport (AmanithSVG backend)
                    if ((err = (uint)UpdateViewport()) == AmanithSVG.SVGT_NO_ERROR)
                    {
                        // draw the document
                        if ((err = AmanithSVG.svgtDocDraw(document.Handle, Handle, (uint)renderingQuality)) != AmanithSVG.SVGT_NO_ERROR)
                        {
                            SVGAssets.LogError("SVGSurface::Draw drawing document failed", (SVGError)err);
                        }
                    }
                }
            }

            return (SVGError)err;
        }

        public SVGError Draw(SVGPackedBin bin,
                             SVGColor clearColor,
                             SVGRenderingQuality renderingQuality)
        {
            uint err = AmanithSVG.SVGT_NO_ERROR;

            if (clearColor != null)
            {
                // clear the surface
                err = AmanithSVG.svgtSurfaceClear(Handle, clearColor.Red, clearColor.Green, clearColor.Blue, clearColor.Alpha);
                if (err != AmanithSVG.SVGT_NO_ERROR)
                {
                    SVGAssets.LogError("SVGSurface::Draw clearing surface failed", (SVGError)err);
                }
            }

            if ((err == AmanithSVG.SVGT_NO_ERROR) && (bin != null) && (bin.Rectangles.Length > 0))
            {
                if ((err = AmanithSVG.svgtPackingRectsDraw(bin.NativeRectangles, (uint)bin.Rectangles.Length, Handle, (uint)renderingQuality)) != AmanithSVG.SVGT_NO_ERROR)
                {
                    SVGAssets.LogError("SVGSurface::Draw drawing packed rectangles failed", (SVGError)err);
                }
            }

            return (SVGError)err;
        }

        /*
            Copy drawing surface content (i.e. pixels) into the specified destination array.

            If the 'redBlueSwap' flag is true, the copy process will also swap red and blue channels for each pixel; this
            kind of swap could be useful when dealing with OpenGL/Direct3D texture uploads (RGBA or BGRA formats).

            If the 'dilateEdgesFix' flag is true, the copy process will also perform a 1-pixel dilate post-filter; this
            dilate filter could be useful when surface pixels will be uploaded to OpenGL/Direct3D bilinear-filtered textures.
        */
        public SVGError Copy(int[] dstPixels32,
                             bool redBlueSwap,
                             bool dilateEdgesFix)
        {
            uint err;

            if (dstPixels32 == null)
            {
                err = AmanithSVG.SVGT_ILLEGAL_ARGUMENT_ERROR;
            }
            else
            {
                int n = (int)Width * (int)Height;
                // dstPixels32 must contain at least 'n' pixels
                if (dstPixels32.Length < n)
                {
                    err = AmanithSVG.SVGT_ILLEGAL_ARGUMENT_ERROR;
                }
                else
                {
                    // "pin" the array in memory, so we can pass direct pointer to the native plugin, without costly marshaling array of structures
                    GCHandle bufferHandle = GCHandle.Alloc(dstPixels32, GCHandleType.Pinned);

                    try
                    {
                        IntPtr bufferPtr = bufferHandle.AddrOfPinnedObject();
                        // copy pixels from internal drawing surface to destination pixels array; NB: AmanithSVG buffer is always in BGRA format (i.e. B = LSB, A = MSB)
                        err = AmanithSVG.svgtSurfaceCopy(Handle, bufferPtr, AmanithSVG.svgtBool(redBlueSwap), AmanithSVG.svgtBool(dilateEdgesFix));
                    }
                    finally
                    {
                        // free the pinned array handle
                        if (bufferHandle.IsAllocated)
                        {
                            bufferHandle.Free();
                        }
                    }
                }
            }

            return (SVGError)err;
        }

        // AmanithSVG surface handle (read only).
        public uint Handle { get; protected set; } = AmanithSVG.SVGT_INVALID_HANDLE;

        // Get current surface width, in pixels.
        public uint Width
        {
            get
            {
                return AmanithSVG.svgtSurfaceWidth(Handle);
            }
        }

        // Get current surface height, in pixels.
        public uint Height
        {
            get
            {
                return AmanithSVG.svgtSurfaceHeight(Handle);
            }
        }

        /*
            The surface viewport (i.e. a drawing surface rectangular area), where to map the source document viewport.
            The combined use of surface and document viewport, induces a transformation matrix, that will be used to draw
            the whole SVG document. The induced matrix grants that the document viewport is mapped onto the surface
            viewport (respecting the specified alignment): all SVG content will be drawn accordingly.
        */
        public SVGViewport Viewport
        {
            get
            {
                return _viewport;
            }
            set
            {
                if (value != null)
                {
                    _viewport = value;
                }
            }
        }

        // The maximum width/height dimension that can be specified to the SVGSurface.Resize and SVGAssets.CreateSurface functions.
        public static uint MaxDimension
        {
            get
            {
                return AmanithSVG.svgtSurfaceMaxDimension();
            }
        }

        // Track whether Dispose has been called.
        private bool _disposed = false;
        // Viewport.
        private SVGViewport _viewport;
    }

    public enum SVGScalerMatchMode : uint
    {
        // Do not scale packed SVG.
        None = 0,
        // Scale each packed SVG according to width.
        Horizontal = 1,
        // Scale each packed SVG according to height.
        Vertical = 2,
        // Scale each packed SVG according to the minimum dimension between width and height.
        MinDimension = 3,
        // Scale each packed SVG according to the maximum dimension between width and height.
        MaxDimension = 4,
        // Expand the canvas area either horizontally or vertically, so the size of the canvas will never be smaller than the reference.
        Expand = 5,
        // Crop the canvas area either horizontally or vertically, so the size of the canvas will never be larger than the reference.
        Shrink = 6,
        // Scale each packed SVG with the width as reference, the height as reference, or something in between.
        MatchWidthOrHeight = 7
    };

    public class SVGScaler
    {
        // Constructor.
        public SVGScaler(float referenceWidth, float referenceHeight, SVGScalerMatchMode matchMode, float match, float offsetScale)
        {
            ReferenceWidth = referenceWidth;
            ReferenceHeight = referenceHeight;
            MatchMode = matchMode;
            Match = match;
            OffsetScale = offsetScale;
        }

        public float ReferenceWidth { get; set; }

        public float ReferenceHeight { get; set; }

        public SVGScalerMatchMode MatchMode { get; set; }

        public float Match { get; set; }

        public float OffsetScale { get; set; }

        public float ScaleFactorCalc(float currentWidth, float currentHeight)
        {
            float scale;
            bool referenceLandscape, currentLandscape;

            switch (MatchMode)
            {
                case SVGScalerMatchMode.Horizontal:
                    scale = currentWidth / ReferenceWidth;
                    break;

                case SVGScalerMatchMode.Vertical:
                    scale = currentHeight / ReferenceHeight;
                    break;

                case SVGScalerMatchMode.MinDimension:
                    referenceLandscape = (ReferenceWidth > ReferenceHeight) ? true : false;
                    currentLandscape = (currentWidth > currentHeight) ? true : false;
                    if (referenceLandscape != currentLandscape)
                    {
                        scale = (currentWidth <= currentHeight) ? (currentWidth / ReferenceHeight) : (currentHeight / ReferenceWidth);
                    }
                    else
                    {
                        scale = (currentWidth <= currentHeight) ? (currentWidth / ReferenceWidth) : (currentHeight / ReferenceHeight);
                    }
                    break;

                case SVGScalerMatchMode.MaxDimension:
                    referenceLandscape = (ReferenceWidth > ReferenceHeight) ? true : false;
                    currentLandscape = (currentWidth > currentHeight) ? true : false;
                    if (referenceLandscape != currentLandscape)
                    {
                        scale = (currentWidth >= currentHeight) ? (currentWidth / ReferenceHeight) : (currentHeight / ReferenceWidth);
                    }
                    else
                    {
                        scale = (currentWidth >= currentHeight) ? (currentWidth / ReferenceWidth) : (currentHeight / ReferenceHeight);
                    }
                    break;

                case SVGScalerMatchMode.Expand:
                    scale = Math.Max(currentWidth / ReferenceWidth, currentHeight / ReferenceHeight);
                    break;

                case SVGScalerMatchMode.Shrink:
                    scale = Math.Min(currentWidth / ReferenceWidth, currentHeight / ReferenceHeight);
                    break;

                case SVGScalerMatchMode.MatchWidthOrHeight:
                {
                    /*
                        We take the log of the relative width and height before taking the average. Then we transform it back in the original space.
                        The reason to transform in and out of logarithmic space is to have better behavior.
                        If one axis has twice resolution and the other has half, it should even out if widthOrHeight value is at 0.5.
                        In normal space the average would be (0.5 + 2) / 2 = 1.25
                        In logarithmic space the average is (-1 + 1) / 2 = 0
                    */
                    float logWidth = (float)(Math.Log(currentWidth / ReferenceWidth) / Math.Log(2));
                    float logHeight = (float)(Math.Log(currentHeight / ReferenceHeight) / Math.Log(2));
                    // clamp between 0 and 1
                    float t = Math.Max(0, Math.Min(1.0f, Match));
                    // lerp
                    float logWeightedAverage = ((1.0f - t) * logWidth) + (t * logHeight);
                    scale = (float)Math.Pow(2, logWeightedAverage);
                    break;
                }

                default:
                    scale = 1.0f;
                    break;
            }

            return (scale * OffsetScale);
        }
    }

    public class SVGPackedRectangle
    {
        // Constructor.
        public SVGPackedRectangle(uint docHandle,
                                  uint elemIdx,
                                  string name,
                                  int originalX,
                                  int originalY,
                                  int x, int y,
                                  int width,
                                  int height,
                                  int zOrder,
                                  float dstViewportWidth,
                                  float dstViewportHeight)
        {
            DocHandle = docHandle;
            ElemIdx = elemIdx;
            Name = name;
            OriginalX = originalX;
            OriginalY = originalY;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            ZOrder = zOrder;
            DstViewportWidth = dstViewportWidth;
            DstViewportHeight = dstViewportHeight;
        }

        // SVG document native handle
        public uint DocHandle { get; }

        // SVG element (unique) identifier inside its document
        public uint ElemIdx { get; }

        public string Name { get; }

        // Original rectangle corner (x pixel coordinate)
        public int OriginalX { get; }

        // Original rectangle corner (y pixel coordinate)
        public int OriginalY { get; }

        // Rectangle corner position (x pixel coordinate)
        public int X { get; }

        // Rectangle corner position (y pixel coordinate)
        public int Y { get; }

        // Rectangle width, in pixels
        public int Width { get; }

        // Rectangle height, in pixels
        public int Height { get; }

        // Z-order
        public int ZOrder { get; }

        // The used destination viewport width (induced by packing scale factor)
        public float DstViewportWidth { get; }

        // The used destination viewport height (induced by packing scale factor)
        public float DstViewportHeight { get; }
    }

    public class SVGPackedBin
    {
        // Constructor
        internal SVGPackedBin(uint index, uint width, uint height, uint rectsCount, IntPtr nativeRectsPtr)
        {
            Index = index;
            Width = width;
            Height = height;
            Rectangles = new SVGPackedRectangle[rectsCount];
            NativeRectangles = IntPtr.Zero;
            Build(nativeRectsPtr, rectsCount);
        }

        // Destructor.
        ~SVGPackedBin()
        {
            if (NativeRectangles != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(NativeRectangles);
            }
        }

        // Bin index
        public uint Index { get; }

        // Bin width, in pixels
        public uint Width { get; }

        // Bin height, in pixels
        public uint Height { get; }

        // Packed rectangles inside the bin
        internal SVGPackedRectangle[] Rectangles { get; }

        internal IntPtr NativeRectangles { get; private set; }

        private string GenElementName(AmanithSVG.SVGTPackedRect rect)
        {
            // build element name to be displayed in Unity editor
            return (rect.elemName != IntPtr.Zero) ? System.Runtime.InteropServices.Marshal.PtrToStringAnsi(rect.elemName) : rect.elemIdx.ToString();
        }

        private void Build(IntPtr nativeRectsPtr, uint rectsCount)
        {
            uint i;
        #if UNITY_WP_8_1 && !UNITY_EDITOR
            int rectSize = Marshal.SizeOf<AmanithSVG.SVGTPackedRect>();
        #else
            int rectSize = Marshal.SizeOf(typeof(AmanithSVG.SVGTPackedRect));
        #endif
            // allocate unmanaged memory, in order to make a copy of given nativeRects
            IntPtr rectsCopyPtr = Marshal.AllocHGlobal((int)rectsCount * rectSize);

            // keep track of the native buffer
            NativeRectangles = rectsCopyPtr;

            // fill rectangles
            for (i = 0; i < rectsCount; ++i)
            {
                // rectangle generated by AmanithSVG packer
            #if UNITY_WP_8_1 && !UNITY_EDITOR
                AmanithSVG.SVGTPackedRect rect = (AmanithSVG.SVGTPackedRect)Marshal.PtrToStructure<AmanithSVG.SVGTPackedRect>(rectsPtr);
            #else
                AmanithSVG.SVGTPackedRect rect = (AmanithSVG.SVGTPackedRect)Marshal.PtrToStructure(nativeRectsPtr, typeof(AmanithSVG.SVGTPackedRect));
            #endif
                // set the rectangle
                Rectangles[i] = new SVGPackedRectangle(rect.docHandle, rect.elemIdx, GenElementName(rect),
                                                       rect.originalX, rect.originalY, rect.x, rect.y, rect.width, rect.height,
                                                       rect.zOrder, rect.dstViewportWidth, rect.dstViewportHeight);
                // copy 
                Marshal.StructureToPtr(rect, rectsCopyPtr, false);
                // next packed rectangle
                nativeRectsPtr = (IntPtr)(nativeRectsPtr.ToInt64() + rectSize);
                rectsCopyPtr = (IntPtr)(rectsCopyPtr.ToInt64() + rectSize);
            }
        }
    }

    public class SVGPacker
    {
        // Constructor.
        internal SVGPacker(float scale, uint maxTexturesDimension, uint border, bool pow2Textures)
        {
            Scale = Math.Abs(scale);
            _maxTexturesDimension = maxTexturesDimension;
            _border = border;
            _pow2Textures = pow2Textures;
            FixMaxDimension();
            FixBorder();
        }

        public float Scale { get; }

        public uint MaxTexturesDimension
        {
            get
            {
                return _maxTexturesDimension;
            }

            set
            {
                _maxTexturesDimension = value;
                FixMaxDimension();
                FixBorder();
            }
        }
    
        public uint Border
        {
            get
            {
                return _border;
            }

            set
            {
                _border = value;
                FixBorder();
            }
        }

        public bool Pow2Textures
        {
            get
            {
                return _pow2Textures;
            }

            set
            {
                _pow2Textures = value;
                FixMaxDimension();
                FixBorder();
            }
        }

        /*
            Start a packing task: one or more SVG documents will be collected and packed into bins, for the generation of atlases.

            Every collected SVG document/element will be packed into rectangular bins, whose dimensions won't exceed the specified 'maxTexturesDimension' (see constructor), in pixels.
            If true, 'pow2Textures' (see constructor) will force bins to have power-of-two dimensions.
            Each rectangle will be separated from the others by the specified 'border' (see constructor), in pixels.
            The specified 'scale' (see constructor) factor will be applied to all collected SVG documents/elements, in order to realize resolution-independent atlases.
        */
        public SVGError Begin()
        {
            uint err = AmanithSVG.svgtPackingBegin(_maxTexturesDimension, _border, AmanithSVG.svgtBool(_pow2Textures), Scale);

            // check for errors
            if (err != AmanithSVG.SVGT_NO_ERROR)
            {
                SVGAssets.LogError("SVGPacker::Begin starting of packing process failed", (SVGError)err);
            }

            return (SVGError)err;
        }

        /*
            Add an SVG document to the current packing task.

            If true, 'explodeGroups' tells the packer to not pack the whole SVG document, but instead to pack each first-level element separately.
            The additional 'scale' is used to adjust the document content to the other documents involved in the current packing process.

            The function will return some useful information, an array of 2 entries and it will be filled with:
            - info[0] = number of collected bounding boxes
            - info[1] = the actual number of packed bounding boxes (boxes whose dimensions exceed the 'maxTexturesDimension' value specified through the constructor, will be discarded)
        */
        public uint[] Add(SVGDocument svgDoc, bool explodeGroup, float scale)
        {
            uint[] info = new uint[2];
            // add an SVG document to the current packing task, and get back information about collected bounding boxes
            uint err = AmanithSVG.svgtPackingAdd(svgDoc.Handle, AmanithSVG.svgtBool(explodeGroup), scale, info);
        
            // check for errors
            if (err != AmanithSVG.SVGT_NO_ERROR)
            {
                SVGAssets.LogError("SVGPacker::Add adding document to packing process failed", (SVGError)err);
                // dereference the result
                info = null;
            }
            // info[0] = number of collected bounding boxes
            // info[1] = the actual number of packed bounding boxes (boxes whose dimensions exceed the 'maxDimension' value specified to the svgtPackingBegin function, will be discarded)
            return info;
        }

        /*
            Close the current packing task and, if specified, perform the real packing algorithm.

            All collected SVG documents/elements (actually their bounding boxes) are packed into bins for later use (i.e. atlases generation).
            After calling this function, the application could use the SVGSurface.Draw method in order to draw the returned packed elements.
        */
        public SVGPackedBin[] End(bool performPacking)
        {
            uint err;
            SVGPackedBin[] bins = null;

            // close the current packing task
            if ((err = AmanithSVG.svgtPackingEnd(AmanithSVG.svgtBool(performPacking))) != AmanithSVG.SVGT_NO_ERROR)
            {
                SVGAssets.LogError("SVGPacker::End ending packing process failed", (SVGError)err);
            }
            else
            if (performPacking)
            {
                // get number of generated bins
                int binsCount = AmanithSVG.svgtPackingBinsCount();
                if (binsCount > 0)
                {
                    // allocate space for bins
                    uint[] binInfo = new uint[3];
                    bins = new SVGPackedBin[binsCount];

                    if ((binInfo == null) || (bins == null))
                    {
                        err = AmanithSVG.SVGT_OUT_OF_MEMORY_ERROR;
                    }
                    else
                    {
                        // fill bins information
                        for (uint i = 0; (i < (uint)binsCount) && (err == AmanithSVG.SVGT_NO_ERROR); ++i)
                        {
                            // get bin information
                            if ((err = AmanithSVG.svgtPackingBinInfo(i, binInfo)) == AmanithSVG.SVGT_NO_ERROR)
                            {
                                // get packed rectangles
                                IntPtr rectsPtr = AmanithSVG.svgtPackingBinRects(i);
                                // at this stage a null-pointer should not be returned, we insert a check just for safety reasons
                                if (rectsPtr != IntPtr.Zero)
                                {
                                    bins[i] = new SVGPackedBin(i, binInfo[0], binInfo[1], binInfo[2], rectsPtr);
                                }
                            }
                        }
                    }
                    // in case of errors dereference the result
                    if (err != AmanithSVG.SVGT_NO_ERROR)
                    {
                        bins = null;
                    }
                }
            }

            return bins;
        }

        private void FixMaxDimension()
        {
            if (_maxTexturesDimension == 0)
            {
                _maxTexturesDimension = 1;
            }
            else
            {
                // check power-of-two option
                if (_pow2Textures && (!SVGUtils.IsPow2(_maxTexturesDimension)))
                {
                    // set maxTexturesDimension to the smallest power of two value greater (or equal) to it
                    _maxTexturesDimension = SVGUtils.Pow2Get(_maxTexturesDimension);
                }
            }
        }

        private void FixBorder()
        {
            // border must allow a packable region of at least one pixel
            uint maxAllowedBorder = ((_maxTexturesDimension & 1) != 0) ? (_maxTexturesDimension / 2) : ((_maxTexturesDimension - 1) / 2);

            if (_border > maxAllowedBorder)
            {
                _border = maxAllowedBorder;
            }
        }

        private uint _maxTexturesDimension;
        private uint _border;
        private bool _pow2Textures;
    }

    [Serializable]
    public abstract class SVGAssetsConfig
    {
        [Serializable]
        public abstract class SVGResource
        {
            protected SVGResource(string id,
                                  SVGResourceType type,
                                  SVGResourceHint hints)
            {
                Id = id;
                Type = type;
                Hints = hints;
            }

            // Get in-memory binary data for the resource.
            public abstract byte[] GetBytes();

            // Resource identifier.
            public string Id;
            // Resource type.
            public SVGResourceType Type;
            // Resource hints.
            public SVGResourceHint Hints;
        }

        // Constructor, device screen properties must be supplied (with/height in pixels and dpi).
        internal SVGAssetsConfig(uint screenWidth,
                                 uint screenHeight,
                                 float screenDpi)
        {
            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;
            ScreenDpi = screenDpi;

            // a 0 or negative value means "keep the default one"
            CurvesQuality = DefaultCurvesQuality;
            // user-agent language settings, start with standard English
            Language = DefaultLanguage;

            // log settings
            LogLevel = DefaultLogLevel;
            LogCapacity = DefaultLogCapacity;
        }

        /* Screen width, in pixels. */
        public uint ScreenWidth;

        /* Screen height, in pixels. */
        public uint ScreenHeight;

        /* Screen dpi. */
        public float ScreenDpi;

        /*
            Curves quality, used by AmanithSVG geometric kernel to approximate curves
            with straight line segments (flattening). Valid range is [1; 100], where 100
            represents the best quality.

            A zero or negative value means "keep the default one".
        */
        public float CurvesQuality;

        /*
            User-agent language settings; a list of languages separated by semicolon (e.g. "en-US;en-GB;it;es")
            It must not be empty.
        */
        public string Language;

        /* Log level, if SVGLogLevel.None is specified, logging is disabled. */
        public SVGLogLevel LogLevel;

        /* Log capacity, in characters; if zero is specified, logging is disabled. */
        public uint LogCapacity;

        /*
            Get the number of (external) resources provided by this configuration.
            It must be implemented by all derived classes.
        */
        public abstract int ResourcesCount();

        /*
            Get a resource given an index.
            If the given index is less than zero or greater or equal to the value
            returned by ResourcesCount, a null resource is returned.

            It must be implemented by all derived classes.
        */
        public abstract SVGResource GetResource(int index);

        // Check if the given language string is valid.
        static public bool IsLanguageValid(string language)
        {
            // must not be empty
            return (!string.IsNullOrEmpty(language));
        }

        // Default configuration.
        public const string DefaultLanguage = "en";
        public float DefaultCurvesQuality
        {
            get
            {
                return AmanithSVG.svgtConfigGet(AmanithSVG.SVGT_CONFIG_CURVES_QUALITY);
            }
        }
        // Enable all log levels.
        public const SVGLogLevel DefaultLogLevel = SVGLogLevel.Error | SVGLogLevel.Warning | SVGLogLevel.Info;
        // 32k log capacity.
        public const uint DefaultLogCapacity = 32768;
    }

    public static class SVGAssets
    {
        /* Has the class already been initialized? */
        public static bool IsInitialized()
        {
            return s_initialized;
        }

        /* Load the given configuration. */
        private static void ConfigurationLoad(SVGAssetsConfig config)
        {
            uint err = AmanithSVG.SVGT_NO_ERROR;
            int resourcesCount = config.ResourcesCount();

            // set curves quality, if needed; a zero or negative value means "keep the default one"
            if (config.CurvesQuality > 0)
            {
                err = AmanithSVG.svgtConfigSet(AmanithSVG.SVGT_CONFIG_CURVES_QUALITY, config.CurvesQuality);
            }

            // set user-agent language
            if (SVGAssetsConfig.IsLanguageValid(config.Language))
            {
                err = AmanithSVG.svgtLanguageSet(config.Language);
            }

            if (resourcesCount > 0)
            {
                // create the resources array (pointers to unmanaged buffers)
                s_resources = new List<IntPtr>(resourcesCount);

                // load resources
                for (int i = 0; i < resourcesCount; ++i)
                {
                    // get a resource
                    SVGAssetsConfig.SVGResource resource = config.GetResource(i);

                    if (resource != null)
                    {
                        IntPtr buffer = IntPtr.Zero;
                        // get in-memory binary data for the font resource
                        byte[] data = resource.GetBytes();
                        int dataLength = data.Length;

                        try
                        {
                            // allocate unmanaged buffer memory
                            buffer = Marshal.AllocHGlobal(dataLength);
                        }
                        finally
                        {
                            if (buffer != IntPtr.Zero)
                            {
                                // copy data from managed 8-bit unsigned integer array to the allocated unmanaged buffer
                                Marshal.Copy(data, 0, buffer, dataLength);
                                // provide AmanithSVG with the resource
                                err = AmanithSVG.svgtResourceSet(resource.Id, buffer, (uint)dataLength, (uint)resource.Type, (uint)resource.Hints);
                                if (err == AmanithSVG.SVGT_NO_ERROR)
                                {
                                    // keep track of handle
                                    s_resources.Add(buffer);
                                }
                            }
                        }
                    }
                }
            }

            // NB: we don't return an error code, because even if there was an error in the
            // load of configuration, it would not affect the functioning of AmanithSVG
        }

        /* Release allocated resources during AmanithSVG initialization. */
        private static void ConfigurationUnload()
        {
            // unload resources (i.e. release unmanaged buffers containing JPEG/PNG/TTF/OTF/WOFF files)
            if (s_resources != null)
            {
                foreach (IntPtr buffer in s_resources)
                {
                    // release memory allocated for the buffer
                    Marshal.FreeHGlobal(buffer);
                }
                // empty the list
                s_resources.Clear();
                s_resources = null;
            }
        }

        private static void LogBuffersInit()
        {
            // create the dictionary that links each calling thread to a log buffer
            s_logBuffers = new ConcurrentDictionary<int, IntPtr>();
        }

        /* Get AmanithSVG log buffer for the current thread. */
        private static IntPtr LogBufferGet(bool createIfNotExist)
        {
            IntPtr buffer = IntPtr.Zero;

            // library must have been initialized, with a given configuration that enable log facility
            if ((IsInitialized() && (s_initConfig != null)) && ((s_initConfig.LogCapacity > 0) && (s_initConfig.LogLevel != SVGLogLevel.None)))
            {
                // get current thread identifier
                int threadId = Thread.CurrentThread.ManagedThreadId;
                // try to get the buffer associated with the thread identifier
                if (!s_logBuffers.TryGetValue(threadId, out buffer))
                {
                    if (createIfNotExist)
                    {
                        // buffer was not found, create a new one
                        buffer = Marshal.AllocHGlobal((int)s_initConfig.LogCapacity);
                        if (buffer != IntPtr.Zero)
                        {
                            // set AmanithSVG log for the calling thread
                            // NB: after calling this function, the buffer will be initialized as empty
                            // (i.e. a '\0' character will be written at the beginning)
                            uint err = AmanithSVG.svgtLogBufferSet(buffer, s_initConfig.LogCapacity, (uint)s_initConfig.LogLevel);
                            if (err == AmanithSVG.SVGT_NO_ERROR)
                            {
                                _ = s_logBuffers.TryAdd(threadId, buffer);
                            }
                        }
                    }
                }
            }

            return buffer;
        }

        /* Release all created log buffers */
        private static void LogBuffersDestroy()
        {
            if (s_logBuffers != null)
            {
                foreach (KeyValuePair<int, IntPtr> entry in s_logBuffers)
                {
                    // release memory allocated for the buffer
                    Marshal.FreeHGlobal(entry.Value);
                }
                // empty the whole dictionary
                s_logBuffers.Clear();
                s_logBuffers = null;
            }
        }

        /* Ensure that per-thread data structures needed by AmanithSVG are assigned. */
        private static void ThreadDataEnsure()
        {
            IntPtr logBuffer = LogBufferGet(true);
        }

        /*
            Initialize AmanithSVG native library and load the given configuration.
            The provided settings include screen metrics, user-agent language and
            possible resources (fonts and images).

            It returns SVGError.None if the operation was completed successfully, else
            an error code.
        */
        internal static SVGError Init(SVGAssetsConfig config)
        {
            uint err = AmanithSVG.SVGT_NO_ERROR;

            if (!IsInitialized())
            {
                // initialize AmanithSVG native library
                if ((err = AmanithSVG.svgtInit(config.ScreenWidth, config.ScreenHeight, config.ScreenDpi)) == AmanithSVG.SVGT_NO_ERROR)
                {
                    // load configuration
                    if (config != null)
                    {
                        ConfigurationLoad(config);
                    }
                    // create the dictionary that links each calling thread to a log buffer
                    LogBuffersInit();
                    // keep track of the configuration with which the class was initialized
                    s_initConfig = config;
                    // now the class is initialized
                    s_initialized = true;
                }
            }

            return (SVGError)err;
        }

        /*
            Destroy the library, freeing all allocated resources.
        */
        internal static void Done()
        {
            if (IsInitialized())
            {
                // uninitialize AmanithSVG native library
                AmanithSVG.svgtDone();
                // release unmanaged buffers containing TTF/OTF files
                ConfigurationUnload();
                // release log buffers
                LogBuffersDestroy();
                // we have finished
                s_initConfig = null;
                s_initialized = false;
            }
        }

        /* Is AmanithSVG logging enabled for the current thread? */
        private static bool IsLoggingEnabled()
        {
            return LogBufferGet(false) != IntPtr.Zero;
        }

        /* Get AmanithSVG log content as a string. */
        internal static string LogGet()
        {
            string result = "";
            IntPtr logBuffer = LogBufferGet(false);

            if (logBuffer != IntPtr.Zero)
            {
                uint[] info = new uint[4] { 0, 0, 0, 0 };
                // get information about the log buffer
                uint err = AmanithSVG.svgtLogBufferInfo(info);
                // info[2] = current length, in characters (i.e. the total number of characters
                // written, included the trailing '\0')
                if ((err == AmanithSVG.SVGT_NO_ERROR) && (info[2] > 1))
                {
                    result = Marshal.PtrToStringAnsi(logBuffer, (int)info[2]);
                }
            }

            return result;
        }

        /* Append the given message to the AmanithSVG log buffer set for the current thread. */
        internal static SVGError LogPrint(string message,
                                          SVGLogLevel level)
        {
            uint err = AmanithSVG.SVGT_NO_ERROR;

            if (IsLoggingEnabled())
            {
                // append a tag which tells us the source of the print operation
                string msg = s_logTag + Environment.NewLine + message;
                // append the message to the AmanithSVG log buffer too (discard the return value)
                err = AmanithSVG.svgtLogPrint(msg, (uint)level);
            }

            return (SVGError)err;
        }

        /*
            Clear the AmanithSVG log buffer set for the current thread.

            If specified by 'stopLogging' parameter, it is possible to stop AmanithSVG from
            logging messages for the current thread. In this case, logging can be reactivated
            (for the calling thread) by calling this method with a 'false' parameter.
        */
        internal static SVGError LogClear(bool stopLogging)
        {
            uint err = AmanithSVG.SVGT_NO_ERROR;

            if (IsInitialized())
            {
                if (stopLogging)
                {
                    // switch off logging at AmanithSVG side
                    if ((err = AmanithSVG.svgtLogBufferSet(IntPtr.Zero, s_initConfig.LogCapacity, (uint)SVGLogLevel.None)) == AmanithSVG.SVGT_NO_ERROR) {
                        // NB: we don't remove the buffer pointer from dictionary, we still keep
                        // track of it; next time this function will be called with a 'false'
                        // parameter, logging will be activated again
                    }
                }
                else {
                    // get log buffer (create it if it does not exist)
                    IntPtr buffer = LogBufferGet(true);

                    if (buffer != null) {
                        // set AmanithSVG log for the calling thread
                        // NB: after calling this function, the buffer will be initialized as empty
                        // (i.e. a '\0' character will be written at the beginning)
                        err = AmanithSVG.svgtLogBufferSet(buffer, s_initConfig.LogCapacity, (uint)s_initConfig.LogLevel);
                    }
                }
            }

            return (SVGError)err;
        }

        /* Append an informational message to the AmanithSVG log buffer set for the current thread. */
        internal static SVGError LogInfo(string message)
        {
            return LogPrint(message, SVGLogLevel.Info);
        }

        /* Append a warning message to the AmanithSVG log buffer set for the current thread. */
        internal static SVGError LogWarning(string message)
        {
            return LogPrint(message, SVGLogLevel.Warning);
        }

        /* Append an error message to the AmanithSVG log buffer set for the current thread. */
        internal static SVGError LogError(string message)
        {
            return LogPrint(message, SVGLogLevel.Error);
        }

        /* Append an error message to the AmanithSVG log buffer set for the current thread. */
        internal static SVGError LogError(string message,
                                          SVGError err)
        {
            return LogError(message + '(' + AmanithSVG.svgtErrorDesc((uint)err) + ')');
        }

        /*
            Create a drawing surface, specifying its dimensions in pixels.

            Supplied dimensions should be positive numbers greater than zero, else
            a null instance will be returned.
        */
        internal static SVGSurface CreateSurface(uint width,
                                                 uint height)
        {
            SVGSurface result = null;

            if (IsInitialized() && (width > 0) && (height > 0))
            {
                // ensure that per-thread data structures needed by AmanithSVG are assigned
                ThreadDataEnsure();

                // create the surface
                uint handle = AmanithSVG.svgtSurfaceCreate(width, height);
                result = (handle != AmanithSVG.SVGT_INVALID_HANDLE) ? (new SVGSurface(handle)) : null;
            }

            return result;
        }

        internal static uint CreateSurfaceHandle(uint width,
                                                 uint height)
        {
            uint result = AmanithSVG.SVGT_INVALID_HANDLE;

            if (IsInitialized() && (width > 0) && (height > 0))
            {
                // ensure that per-thread data structures needed by AmanithSVG are assigned
                ThreadDataEnsure();

                // create the surface handle
                result = AmanithSVG.svgtSurfaceCreate(width, height);
            }

            return result;
        }

        /*
            Create and load an SVG document, specifying the whole XML string.
            If supplied XML string is null or empty, a null instance will be returned.
        */
        internal static SVGDocument CreateDocument(string xmlText)
        {
            SVGDocument result = null;

            if (IsInitialized() && (!string.IsNullOrEmpty(xmlText)))
            {
                // ensure that per-thread data structures needed by AmanithSVG are assigned
                ThreadDataEnsure();

                // create the document
                uint handle = AmanithSVG.svgtDocCreate(xmlText);
                result = (handle != AmanithSVG.SVGT_INVALID_HANDLE) ? (new SVGDocument(handle)) : null;
            }

            return result;
        }

        /*
            Create an SVG packer, specifying a scale factor.

            Every collected SVG document/element will be packed into rectangular bins, whose
            dimensions won't exceed the specified 'maxTexturesDimension' in pixels.

            If true, 'pow2Textures' will force bins to have power-of-two dimensions.

            Each rectangle will be separated from the others by the specified 'border' in pixels.

            The specified 'scale' factor will be applied to all collected SVG documents/elements,
            in order to realize resolution-independent atlases.
        */
        internal static SVGPacker CreatePacker(float scale,
                                               uint maxTexturesDimension,
                                               uint border,
                                               bool pow2Textures)
        {
            SVGPacker result = null;

            if (IsInitialized())
            {
                // ensure that per-thread data structures needed by AmanithSVG are assigned
                ThreadDataEnsure();

                // create the packer
                result = new SVGPacker(scale, maxTexturesDimension, border, pow2Textures);
            }

            return result;
        }

        /* Get library version. */
        public static string GetVersion()
        {
            return Marshal.PtrToStringAnsi(AmanithSVG.svgtGetString(AmanithSVG.SVGT_VERSION));
        }

        /* Get library vendor. */
        public static string GetVendor()
        {
            return Marshal.PtrToStringAnsi(AmanithSVG.svgtGetString(AmanithSVG.SVGT_VENDOR));
        }

        private static string s_logTag = "SVGAssets";
        /* Keep track if AmanithSVG library has been initialized. */
        private static bool s_initialized = false;
        /* Handles of in-memory binary resource files (images: JPEG, PNG; fonts: OTF, TTF, WOFF). */
        private static List<IntPtr> s_resources = null;
        /*
            Log buffers.

            It is important to not specify the same buffer memory for different
            threads, because AmanithSVG does not synchronize write operations to
            the provided log buffer: in other words, each thread must provide its
            own log buffer.
        */
        private static ConcurrentDictionary<int, IntPtr> s_logBuffers = null;
        /* The configuration with which the class was initialized. */
        private static SVGAssetsConfig s_initConfig = null;
    }
}
