using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Sttplay.MediaPlayer
{
    public class OSXTools
    {
#if UNITY_EDITOR
        public const string moduleName = "sccore";
#else
#if UNITY_IOS
        public const string moduleName = "__Internal";
#else
        public const string moduleName = "sccore";
#endif
#endif

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetDevice();

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateCVMetalTextureCache(IntPtr device);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ReleaseCVMetalTextureCache(IntPtr cache);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateCVTextureFromImage(IntPtr cache, IntPtr pixelBuffer, int y);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ReleaseCVTexture(IntPtr tex);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetMTLTexture(IntPtr tex);
    }
}
