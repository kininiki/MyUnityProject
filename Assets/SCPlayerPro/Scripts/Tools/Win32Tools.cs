using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Sttplay.MediaPlayer
{
    public class Win32Tools
    {
        public const string moduleName = "sccore";

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetDevice();

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetD3D11TextureSharedHandle(IntPtr sharedTexture);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetSharedD3D11Texture2D(IntPtr device, IntPtr sharedHandle);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ReleaseSharedD3D11Texture2D(IntPtr texture);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetSharedD3D12Texture2D(IntPtr device, IntPtr sharedHandle);

		[DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void ReleaseSharedD3D12Texture2D(IntPtr texture);
	}
}
