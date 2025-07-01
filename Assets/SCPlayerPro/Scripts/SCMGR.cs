#pragma warning disable CS0162
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
#endif

namespace Sttplay.MediaPlayer
{

	public class HardwareRenderContext
	{
		public System.IntPtr renderer;
		public System.IntPtr texture;
        public System.IntPtr sharedTexture;
        public int width, height;
        public bool released;
        public int delayFrameRef;
	}

    /// <summary>
    /// Manage SCPlayerPro's singleton
    /// The user uses the SCPlayerPro scene in this scene to generate an object and mount the component
    /// The update function of this class also drives Dispatcher.WakeAll()
    /// </summary>
    public class SCMGR : MonoBehaviour
    {
        public const bool EnableCallbackSem = true;
        public const string minVersion_2019 = "2019.4.32";
        public const string minVersion_2020 = "2020.3.24";
        public const string minVersion_2021 = "2021.3.0";
        public const string minVersion_2022 = "2022.3.0";
        public static bool VersionVerifySuccessed { get; private set; }
        public static SCMGR Instance { get; private set; }
        public static bool IsPaused { get; private set; }
        private static List<UnitySCPlayerPro> players = new List<UnitySCPlayerPro>();
        private static List<SCPlayerPro> cores = new List<SCPlayerPro>();
        public static event System.Action SCEnvironmentInitilize;
        public static event System.Action SCEnvironmentTerminate;
        private static bool nextInit = false;

        private static AndroidJavaClass unityPlayer;
        private static AndroidJavaObject currentActivity;
        private static AndroidJavaObject context;
        private static AndroidJavaClass debugClass;
        private static int frameRate;
        [SerializeField]
        private AudioDriverType audioDriver = AudioDriverType.Auto;

        private static SCThreadManager normalThreadMgr;
        private static bool isExit = false;
        private static SCThreadHandle logHandle;
        //private static SCThreadHandle renderHandle;
        private static List<HardwareRenderContext> renderContextList = new List<HardwareRenderContext>();
        public static int HardwareRenderCountextCount { get { return renderContextList.Count; } }
        private static IntPtr sem;
        public static int RCL{ get {return renderContextList.Count;} }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void UnityAutoInit()
        {
#if UNITY_EDITOR && (UNITY_2019_4_OR_NEWER)
            //EditorApplication.wantsToQuit -= UnityEditorQuit;
            //EditorApplication.wantsToQuit += UnityEditorQuit;
#endif
        }

        /// <summary>
        /// Initialize SCPlayerPro and add the player to the players list
        /// </summary>
        /// <param name="player"></param>
        public static void InitSCPlugins(UnitySCPlayerPro player)
        {
            if (Instance == null)
                new GameObject().AddComponent<SCMGR>().Awake();
            if(player != null)
                players.Add(player);
        }

        public static void InitSCPlugins()
		{
            InitSCPlugins(null);
        }

        /// <summary>
        /// Set the instance to singleton mode, and do not destroy the object 
        /// </summary>
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            else if (Instance == this)
                return;
            
            if (IsVersionGreaterThan(Application.unityVersion))
			{
				UnityNativePlugin.Initilize();
			}
            else
			{
#if UNITY_EDITOR
                string error = $"SCPlayerPro requires the minimum Unity version to be \n" +
                    $">={minVersion_2019}\n" +
                    $">={minVersion_2020}\n" +
                    $">={minVersion_2021}\n" +
                    $">={minVersion_2022}\n" +
                    "or higher";
                EditorUtility.DisplayDialog("Error", $"{error}.\nThe application is about to exit.", "OK");
                Debug.LogError(error);
				EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                return;
			}
			gameObject.name = "SCMGR";
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
#elif UNITY_ANDROID
            unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
            AndroidJavaClass jclz = new AndroidJavaClass("com.sttplay.MediaPlayer.TimeUtility");
            long creationts = jclz.CallStatic<long>("GetCreationTime", GetUrlFromSCSCAssets(""));
            if(jclz.CallStatic<long>("GetPackageLastUpdateTime", currentActivity) > creationts)
            {
                if(System.IO.Directory.Exists(GetUrlFromSCSCAssets("")))
                    System.IO.Directory.Delete(GetUrlFromSCSCAssets(""), true);
                AndroidJavaObject assetManager = currentActivity.Call<AndroidJavaObject>("getAssets");
                new AndroidJavaClass("com.sttplay.MediaPlayer.FileUtility").CallStatic("CopyAssets", "SCAssets", Application.persistentDataPath + "/", assetManager);
            }
            int jniVer = 0;
            System.IntPtr jvm = ISCNative.GetJavaVM(ref jniVer);
            XRendererEx.XRendererEx_SetJavaVM(jvm, jniVer);
#endif
            frameRate = Screen.currentResolution.refreshRate;
            if(normalThreadMgr == null)
                normalThreadMgr = SCThreadManager.CreateThreadManager();
            lock(renderContextList)
                UnityNativePlugin.RegisterClearAll();

            InitilizeSCPlayerPro();
            VersionVerifySuccessed = true;
            lock (renderContextList)
                UnityNativePlugin.UnityNativeRenderEvent += UnityNativeRender;
			StartCoroutine(Loop());
#if UNITY_EDITOR
			EditorApplication.pauseStateChanged += OnPauseModeStateChanged;
#if UNITY_EDITOR && (UNITY_2019_4_OR_NEWER)
            CompilationPipeline.compilationStarted += OnBeginCompileScripts;
#endif //UNITY_VERSION

#endif
        }
		private IEnumerator Loop()
		{
			while (true)
			{
				yield return new WaitForEndOfFrame();
				UnityNativePlugin.Post();
			}
		}

		private static bool IsVersionGreaterThan(string currentVersion)
		{
            string[] currentVersionParts = currentVersion.Split('.');
            int currentMajor = int.Parse(currentVersionParts[0]);
            switch(currentMajor)
			{
                case 2019:
                    return IsVersionGreaterThan(currentVersion, minVersion_2019);
				case 2020:
					return IsVersionGreaterThan(currentVersion, minVersion_2020);
				case 2021:
					return IsVersionGreaterThan(currentVersion, minVersion_2021);
				case 2022:
					return IsVersionGreaterThan(currentVersion, minVersion_2022);
				case 2023:
				case 6000:
                    return true;
				default:
                    return false;
			}
            
        }

        private static bool IsVersionGreaterThan(string currentVersion, string targetVersion)
		{
			string[] currentVersionParts = currentVersion.Split('.');
			string[] targetVersionParts = targetVersion.Split('.');

			int currentMajor = int.Parse(currentVersionParts[0]);
			int currentMinor = int.Parse(currentVersionParts[1]);
			int currentPatch = int.Parse(currentVersionParts[2].Split('f')[0].Split('p')[0]);

			int targetMajor = int.Parse(targetVersionParts[0]);
			int targetMinor = int.Parse(targetVersionParts[1]);
			int targetPatch = int.Parse(targetVersionParts[2]);

			if (currentMajor > targetMajor)
			{
				return true;
			}
			if (currentMajor == targetMajor)
			{
				if (currentMinor > targetMinor)
				{
					return true;
				}
				if (currentMinor == targetMinor)
				{
					if (currentPatch >= targetPatch)
					{
						return true;
					}
				}
			}

			return false;
		}

        public static bool IsVersionGreaterThan_2022()
		{
            return IsVersionGreaterThan(Application.unityVersion, minVersion_2022);
		}

		public static int GetMaxDisplayFrequency()
        {
            return frameRate;
        }

#if UNITY_EDITOR && (UNITY_2019_4_OR_NEWER)
        private void OnBeginCompileScripts(object obj)
        {
            OnDestroy();
        }
        private void OnPauseModeStateChanged(PauseState state)
        {
            IsPaused = state == PauseState.Paused ? true : false;
        }
#endif

        /// <summary>
        /// initialize scplugins
        /// </summary>
        private static void InitilizeSCPlayerPro()
        {
            ISCNative.EnableUDPLog(System.Net.IPAddress.Parse("127.0.0.1"), 5555);
            ISCNative.EnableLogFile(ISCNative.LogFileMode.One, "sclog");
            try
            {
				ISCNative.AddPixelFormatFilter(HWPixelFormat.PIX_FMT_DXVA2);
				ISCNative.AddPixelFormatFilter(HWPixelFormat.PIX_FMT_CUDA);
				ISCNative.AddPixelFormatFilter(HWPixelFormat.PIX_FMT_VIDEOTOOLBOX);
				ISCNative.AddPixelFormatFilter(HWPixelFormat.PIX_FMT_MEDIACODEC);
                ISCNative.AddPixelFormatFilter(HWPixelFormat.PIX_FMT_D3D11);
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11)
				{
                    try
                    {
                        System.IntPtr d3d11Device = Win32Tools.GetDevice();
                        if (d3d11Device == System.IntPtr.Zero)
                            throw new System.Exception("ID3D11Device is null");
                        XRendererEx.XRendererEx_SetD3D11Device(Win32Tools.GetDevice());
                        if(QualitySettings.activeColorSpace == ColorSpace.Gamma)
						{
							ISCNative.RemovePixelFormatFilter(HWPixelFormat.PIX_FMT_D3D11);
							//ISCNative.RemovePixelFormatFilter(HWPixelFormat.PIX_FMT_DXVA2);
							ISCNative.RemovePixelFormatFilter(HWPixelFormat.PIX_FMT_CUDA);
						}
                    }
                    catch { }
				}
				else if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D12)
				{
					try
					{
						System.IntPtr d3d12Device = Win32Tools.GetDevice();
						if (d3d12Device == System.IntPtr.Zero)
							throw new System.Exception("ID3D12Device is null");
						//ISCNative.RemovePixelFormatFilter(HWPixelFormat.PIX_FMT_D3D11);
					}
					catch { }
				}
#elif UNITY_ANDROID
                if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2 || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3) 
                {
                    try
                    {
                        System.IntPtr eglContext = AndroidTools.GetEGLContext();
                        if (eglContext == System.IntPtr.Zero)
                            throw new System.Exception("EGLContext is null");
                        ISCNative.RemovePixelFormatFilter(HWPixelFormat.PIX_FMT_MEDIACODEC);
                    }
                    catch { }
				}
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS
                if(SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Metal)

                {
                    try
                    {
                        System.IntPtr metalDevice = OSXTools.GetDevice();
                        if (metalDevice == System.IntPtr.Zero)
                            throw new System.Exception("MetalDevice is null");
                        ISCNative.RemovePixelFormatFilter(HWPixelFormat.PIX_FMT_VIDEOTOOLBOX);
                    }
                    catch { }
                }
#endif
                isExit = false;
                ISCNative.EnableCallbackSem(EnableCallbackSem ? 1 : 0);
                if(EnableCallbackSem)
				{
					sem = ISCNative.GetLogSem();
					logHandle = SCMGR.CreateThreadHandle(() =>
					{
						while (true)
						{
							var logSem = Marshal.PtrToStructure<LogSem>(sem);
							ISCNative.SCSemaphore_Wait(logSem.other);
							if (isExit)
								break;
							logSem = Marshal.PtrToStructure<LogSem>(sem);
							LogLevel level = (LogLevel)logSem.level;
							string log = Marshal.PtrToStringAnsi(logSem.log);
							LogCallback(level, log);
							ISCNative.SCSemaphore_Post(logSem.self);
						}
					});
				}
				else
				{
                    ISCNative.SetLogCallback((level, log) => { LogCallback((LogLevel)level, log); });
                }
                ISCNative.SetAudioDriver((int)Instance.audioDriver);
                ISCNative.InitializeStreamCapturePro();
                nextInit = true;
            }
            catch (System.Exception ex)
            {
                //#if UNITY_EDITOR || UNITY_STANDALONE_WIN
                //                SCWin32.MessageBox(
                //                   SCWin32.GetProcessWnd(),
                //                   "The MICROSOFT VISUAL C++ 2015 - 2022 RUNTIME library is missing, please install it and try again.\n" + ex.ToString(),
                //                   "Error",
                //                   SCWin32.MB_ICONERROR);
                //#elif UNITY_ANDROID
                //                currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                //                {
                //                    AndroidJavaClass Toast = new AndroidJavaClass("android.widget.Toast");
                //                    AndroidJavaObject javaString = new AndroidJavaObject("java.lang.String", ex.ToString());
                //                    AndroidJavaObject toast = Toast.CallStatic<AndroidJavaObject>("makeText", context, javaString, Toast.GetStatic<int>("LENGTH_LONG"));
                //                    toast.Call("show");
                //                }));
                //#endif
                Debug.LogError(ex);
            }
        }

        /// <summary>
        /// terminate scplugins
        /// </summary>
        private static void TerminateSCPlayerPro()
        {
            if (SCEnvironmentTerminate != null)
                SCEnvironmentTerminate();
            ISCNative.TerminateStreamCapturePro();
            isExit = true;
            if(EnableCallbackSem && sem != System.IntPtr.Zero)
			{
				var logSem = Marshal.PtrToStructure<LogSem>(sem);
				ISCNative.SCSemaphore_Post(logSem.other);
				SCMGR.ReleaseThreadHandle(logHandle);
                ISCNative.EnableCallbackSem(0);
                sem = System.IntPtr.Zero;
			}
        }

        /// <summary>
        /// The local program will call back the function to Unity
        /// </summary>
        /// <param name="level">log level</param>
        /// <param name="msg">msg</param>
        public static void LogCallback(LogLevel level, string msg)
        {
            if (level == LogLevel.Info)
                Debug.LogFormat("<b>{0}</b>", msg);
            else if (level == LogLevel.Warning)
                Debug.LogWarningFormat("<b>{0}</b>", msg);
            else
                Debug.LogErrorFormat("<b>{0}</b>", msg);
        }

        public static void RemovePlayer(UnitySCPlayerPro player)
        {
            if (players.Contains(player))
                players.Remove(player);
        }
        /// <summary>
        /// release scplayerpro
        /// </summary>
        /// <param name="player"></param>
        private static void ReleasePlayer(UnitySCPlayerPro player)
        {
            player.ReleaseCore();
            RemovePlayer(player);
        }

        /// <summary>
        /// Drive Dispatcher 
        /// </summary>
        private void Update()
        {
            if (nextInit)
            {
                nextInit = false;
                if (SCEnvironmentInitilize != null)
                    SCEnvironmentInitilize();
            }
            foreach (var item in cores)
                item.Update();
        }

        private void FixedUpdate()
        {
            Update();
        }

        public static void GCCollect()
		{
			Resources.UnloadUnusedAssets();
			System.GC.Collect();
		}

        public static void AddPlayer(SCPlayerPro player)
        {
            cores.Add(player);
        }
        public static void RemovePlayer(SCPlayerPro player)
        {
            cores.Remove(player);
        }

        public static HardwareRenderContext GetRenderContext()
		{
            HardwareRenderContext ctx = null;
            lock (renderContextList)
			{
                ctx = new HardwareRenderContext();
                renderContextList.Add(ctx);
			}
            return ctx;
		}

        public static void FreeRenderContext(HardwareRenderContext rc)
		{
            lock (renderContextList)
			{
                rc.delayFrameRef = 10;
                rc.released = true;
			}
		}

        public static IntPtr GetSharedTexture(IntPtr renderTarget)
		{
			IntPtr sharedHandle = Win32Tools.GetD3D11TextureSharedHandle(renderTarget);
			return Win32Tools.GetSharedD3D11Texture2D(Win32Tools.GetDevice(), sharedHandle);
		}

        public static void ReleaseSharedTexture(HardwareRenderContext rc)
		{
            if(rc.sharedTexture != IntPtr.Zero)
			{
                Win32Tools.ReleaseSharedD3D11Texture2D(rc.sharedTexture);
                rc.sharedTexture = IntPtr.Zero;
			}                
		}
        private static void ReleaseHardwareRenderContext(HardwareRenderContext rc)
		{
            ReleaseSharedTexture(rc);
            if (rc.texture != IntPtr.Zero)
                XRendererEx.XTextureEx_Destroy(rc.texture);
            if (rc.renderer != IntPtr.Zero)
                XRendererEx.XRendererEx_Destroy(rc.renderer);
            rc.texture = rc.renderer = IntPtr.Zero;
		}


		private static void UnityNativeRender(int eventID)
		{
            lock (renderContextList)
                NativeRender();
		}

		private static void NativeRender()
		{
            for(int i = 0; i < renderContextList.Count; i++)
			{
                if(renderContextList[i].released)
				{
                    if(--renderContextList[i].delayFrameRef <= 0)
					{
						ReleaseHardwareRenderContext(renderContextList[i]);
						renderContextList.RemoveAt(i);
						break;
					}
				}
			}
		}

        private static void ClearContext()
		{
			for (int i = 0; i < renderContextList.Count; i++)
			{
			    UnityNativePlugin.RegisterClearContext(renderContextList[i].renderer, renderContextList[i].texture, renderContextList[i].sharedTexture, IntPtr.Zero);
			}
		}

        /// <summary>
        /// release all player
        /// </summary>
        private void OnDestroy()
        {
            lock (renderContextList)
                UnityNativePlugin.UnityNativeRenderEvent -= UnityNativeRender;
            while (players.Count > 0)
                ReleasePlayer(players[0]);
            SCPlayerProManager.ReleaseAll();
            lock (renderContextList)
                ClearContext();
            TerminateSCPlayerPro();
            UnityNativePlugin.Terminate();
#if UNITY_EDITOR
            if(normalThreadMgr != null)
                normalThreadMgr.Dispose();
            EditorApplication.pauseStateChanged -= OnPauseModeStateChanged;
#endif
        }

        public static string GetUrlFromSCSCAssets(string url)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
                return Application.streamingAssetsPath + "/SCAssets/" + url;
            else if (Application.platform == RuntimePlatform.Android)
                return Application.persistentDataPath + "/SCAssets/" + url;
            else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
                return Application.streamingAssetsPath + "/SCAssets/" + url;
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
                return Application.streamingAssetsPath + "/SCAssets/" + url;
            return null;
        }

        public static SCThreadHandle CreateThreadHandle(System.Action action)
        {
            return normalThreadMgr.CreateThreadHandle(action);
        }
        public static void ReleaseThreadHandle(SCThreadHandle handle)
        {
            if(handle != null)
                normalThreadMgr.FreeThreadHandle(handle);
        }

        public static string GetMemoryInfo()
        {
            string info = "";
            if (Application.platform == RuntimePlatform.Android)
            {
                if(debugClass == null)
                    debugClass = new AndroidJavaClass("android.os.Debug");

                long maxNativeHeapSize = debugClass.CallStatic<long>("getNativeHeapSize") / 1000000;

                long allocatedNativeHeapSize = debugClass.CallStatic<long>("getNativeHeapAllocatedSize") / 1000000;

                long freeNativeHeapSize = debugClass.CallStatic<long>("getNativeHeapFreeSize") / 1000000;

                info += $"Max Native Heap Size: {maxNativeHeapSize} MB\n";
                info += $"Allocated Native Heap Size: {allocatedNativeHeapSize} MB\n";
                info += $"Free Native Heap Size: {freeNativeHeapSize} MB";
            }
            else
            {
                //Debug.LogError("This code is intended for Android platform only.");
            }
            return info;
        }
    }
}