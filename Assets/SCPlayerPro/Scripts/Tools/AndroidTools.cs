using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Sttplay.MediaPlayer
{
    public class AndroidTools
    {
        public const string moduleName = "sccore";

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetDevice")]
        public static extern IntPtr GetEGLContext();
    }
}
