using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Sttplay.MediaPlayer
{
    public class UnityNativePlugin
    {

#if UNITY_IOS && !UNITY_EDITOR_OSX
        private const string moduleName = "__Internal";
#else
		private const string moduleName = "sccore";
#endif

		[DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr GetRenderEventFunc();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void UnityRenderEventDelegate(int eventID);

		[DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
		private static extern void SetUnityRenderEventCallback(UnityRenderEventDelegate callback);

#if UNITY_2019_4_OR_NEWER
		[AOT.MonoPInvokeCallback(typeof(UnityRenderEventDelegate))]
#endif
		private static void RenderEventCallback(int eventID)
		{
			UNPID = 1;
			if (UnityNativeRenderEvent != null)
				UnityNativeRenderEvent(eventID);
			UNPID = 2;
		}

		[DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void RegisterClearContext(IntPtr render, IntPtr texture, IntPtr d3d11tex, IntPtr d3d12tex);

		[DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void RegisterClearAll();

		public static void Initilize()
		{
			callbackDelegate = new UnityRenderEventDelegate(RenderEventCallback);
			SetUnityRenderEventCallback(callbackDelegate);
		}

		public static void Terminate()
		{
			callbackDelegate = null;
			SetUnityRenderEventCallback(null);
		}

		public static void Post()
		{
			GL.IssuePluginEvent(GetRenderEventFunc(), 0);
		}

		private static UnityRenderEventDelegate callbackDelegate = null;
		public static event Action<int> UnityNativeRenderEvent;

		public static int UNPID = 0;
	}
}
