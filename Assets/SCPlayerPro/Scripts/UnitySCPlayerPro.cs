using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Sttplay.MediaPlayer
{

    [System.Serializable]
    public class CaptureOpenCallback : UnityEvent<CaptureOpenResult, string, OpenCallbackContext> { }

    [System.Serializable]
    public class RenderFrame : UnityEvent<SCRenderer> { }

    [System.Serializable]
    public class InterruptReadMedia : UnityEvent<string> { }

    public class UnitySCPlayerPro : MonoBehaviour
    {

        /// <summary>
        /// URL When the choice of mediatype is different, url has different meanings
        /// LocalOrNetworkFile:local file, http, https
        /// </summary>
        public string url;

        /// <summary>
        /// Mark whether player is closed
        /// Open failure is also considered not close
        /// </summary>
        public bool Closed { get { return core.Closed; } }

        /// <summary>
        /// Mark whether player successfully opened media
        /// </summary>
        public bool OpenSuccessed { get { return core.OpenSuccessed; } }

        /// <summary>
        /// Whether to disable video 
        /// </summary>
        public bool disableVideo = false;
        /// <summary>
        /// Whether to disable audio
        /// </summary>
        public bool disableAudio = false;

        //public bool disableSubtitle = true;
        public int defaultVideoTrack = 0;
        public int defaultAudioTrack = 0;
        //public int defaultSubtitleTrack = 0;

        /// <summary>
        /// Whether to enable hardware acceleration
        /// Not all videos support hardware acceleration.
        /// If you enable this option, hardware acceleration will be tried first, 
        /// and if it fails, the CPU will be used for decoding. 
        /// </summary>
        public bool enableHWAccel = true;

        /// <summary>
        /// Extract frame data to memory
        /// </summary>
        public bool extractHWFrame = true;

        /// <summary>
        /// Hardware device type when video hardware accelerates decoding 
        /// Not all of the current platforms are supported, 
        /// if the current option does not support, set as the default 
        /// </summary>
        public HWDeviceType HWAccelType = HWDeviceType.AUTO;

        /// <summary>
        /// Pixel format of output SCFrame 
        /// </summary>
        public PixelFormat outputPixelFormat = PixelFormat.AUTO;

        public MediaType openMode = MediaType.LocalFile;

        public int cameraWidth = 0;
        public int cameraHeight = 0;
        public float cameraFPS = 0.0f;
        public string options;

        /// <summary>
        /// Whether to open the media when UnityPlayer starts 
        /// </summary>
        public bool autoOpen = true;

        /// <summary>
        /// Play directly after opening or stay at the first frame
        /// </summary>
        public bool openAndPlay = true;

        /// <summary>
        /// Whether the media is played in a loop 
        /// This option is valid only when the mediaType is LocalOrNetworkFile 
        /// </summary>
        public bool loop = false;
        private bool _loop;

        /// <summary>
        /// Media volume
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float volume = 0.5f;

        /// <summary>
        /// Playback speed
        /// </summary>
        [Range(0.5f, 2.0f)]
        public float speed = 1.0f;

        /// <summary>
        /// Whether the marker is in a paused state 
        /// </summary>
        public bool IsPaused { get { return core.IsPaused; } }

        /// <summary>
        /// Current playback timestamp, valid when the mediaType is LocalOrNetFile
        /// </summary>
        public long CurrentTime { get { return core.CurrentTime; } }

        /// <summary>
        /// The total duration of the media, valid when the mediaType is LocalOrNetFile 
        /// </summary>
        public long Duration { get { return core.Duration; } }

        public bool EnableVsync { get { return core.EnableLogicVsync; } set { core.EnableLogicVsync = value; } }


        /// <summary>
        /// Called when player demux succeeds or failed
        /// </summary>
        public CaptureOpenCallback onCaptureOpenCallbackEvent;

        /// <summary>
        /// Called when opening 
        /// </summary>
        public UnityEvent onOpenEvent;

        /// <summary>
        /// Called when closing 
        /// </summary>
        public UnityEvent onCloseEvent;

        /// <summary>
        /// Called when renderer is changed
        /// </summary>
        public RenderFrame onRendererChangedEvent;

        /// <summary>
        /// Called when the video has finished playing, whether looping or not 
        /// </summary>
        public UnityEvent onStreamFinishedEvent;

        /// <summary>
        /// Called after the first frame is drawn, if there is no video stream, this event will not be called 
        /// </summary>
        public RenderFrame onFirstFrameRenderEvent;

        /// <summary>
        /// Called when player demux read pakcet failed
        /// </summary>
        public InterruptReadMedia onInterruptEvent;

        /// <summary>
        /// Called after each frame of video is drawn , if there is no video stream, this event will not be called 
        /// @Tip:
        /// The alignment of the frame here is 1-byte alignment
        /// </summary>
        public RenderFrame onRenderVideoFrameEvent;

        /// <summary>
        /// Called after each frame of audio is drawn , if there is no audio stream, this event will not be called 
        /// The callback thread is not the main thread
        /// </summary>
        public RenderFrame onRenderAudioFrameEvent;

        /// <summary>
        /// File type to open
        /// </summary>
        public MediaType mediaType = MediaType.LocalFile;

        /// <summary>
        /// Video rendering through this object
        /// </summary>
        public SCVideoRenderer VideoRenderer { get; private set; }

        /// <summary>
        /// Player core class, the player uses this class for media file playback, 
        /// and users can also transplant this class to WPF programs or WinForm programs.
        /// </summary>
        private SCPlayerPro core;
        private bool isFirst = true;
        private bool containVideo = true;
        private AutoResetEvent renderEvent = new AutoResetEvent(false);
        private Mutex renderMux = new Mutex();
        private bool renderClosed = true;
        private int framePixelFormat = (int)PixelFormat.AUTO;
        public int decoderFps;
        private int _decoderFps;
        private long lastTs;
        private bool allowDraw;
        private enum RenderingStrategy
		{
            Unknow,
            Software,
            Hardware,
		}
        private HardwareRenderContext hwRenderCtx;
        private Mutex hwRenderCtxMux = new Mutex();
        private struct RenderCommand
		{
            public bool isFirst;
            public SCFrame frame;
            public IntPtr retVal;
		}
        private Queue<RenderCommand> renderQueue = new Queue<RenderCommand>();
        private RenderingStrategy renderStrategy;
        private SCPlayerProContext playerContext;
        /// <summary>
        /// Initialize the plugin and set up events
        /// </summary>
        private void Awake()
        {
            SCMGR.InitSCPlugins(this);
            if (!SCMGR.VersionVerifySuccessed) return;
            playerContext = SCPlayerProManager.CreatePlayer();
            core = playerContext.player;
            VideoRenderer = playerContext.renderer;
            UnityNativePlugin.UnityNativeRenderEvent += NativeRender;
#if UNITY_EDITOR
            core.EnableLogicVsync = false;
#else
            core.EnableLogicVsync = QualitySettings.vSyncCount == 1;
#endif
            core.onStreamFinishedEvent += OnStreamFinished;
            core.onCaptureOpenCallbackEvent += OnCaptureOpenCallback;
            core.onInterruptCallbackEvent += OnInterruptCallback;
            core.onDrawAudioFrameEvent += OnDrawAudioFrame;
            core.onDrawVideoFrameEvent += OnDrawVideoFrame;
            _loop = loop;
            lastTs = ISCNative.GetTimestamp();
        }

		public string GetRenderPixelFormat()
        {
            if (Enum.IsDefined(typeof(PixelFormat), (PixelFormat)framePixelFormat))
                return ((PixelFormat)framePixelFormat).ToString();
			return ((HWPixelFormat)framePixelFormat).ToString();
        }

        private void OnDrawAudioFrame(IntPtr pcm, int length)
        {
            core.InvokeAsync(() =>
            {
                if (isFirst && !containVideo)
                {
                    isFirst = false;
                    if (onFirstFrameRenderEvent != null)
                    {
                        try
                        {
                            onFirstFrameRenderEvent.Invoke(null);
                        }
                        catch { }
                    }
                }
                if (onRenderAudioFrameEvent != null)
                {
                    try
                    {
                        onRenderAudioFrameEvent.Invoke(null);
                    }
                    catch { }
                }
            });
        }

        private void Start()
        {
            if (autoOpen)
                Open(openMode);
        }

        /// <summary>
        /// When the Open function is called, 
        /// the function will be called back regardless of whether it is opened or not, 
        /// unless you call the Close function before the successful opening
        /// </summary>
        /// <param name="result">open result</param>
        /// <param name="error">error infomation</param>
        /// <param name="context">video or audio param</param>
        private void OnCaptureOpenCallback(CaptureOpenResult result, string error, OpenCallbackContext context)
        {
            if (result == CaptureOpenResult.SUCCESS)
			{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
				hwRenderCtxMux.WaitOne();
				hwRenderCtx = SCMGR.GetRenderContext();
				hwRenderCtxMux.ReleaseMutex();
#elif UNITY_ANDROID
#endif
				Debug.Log(string.Format("{0} open successed", url));
            }
            else
                Debug.LogWarning(string.Format("{0} open failed : {1}", url, error));
            if (context != null)
            {
                containVideo = context.videoParams == IntPtr.Zero ? false : true;
            }
            if (onCaptureOpenCallbackEvent != null)
            {
                try
                {
                    onCaptureOpenCallbackEvent.Invoke(result, error, context);
                }
                catch { }
            }

        }

        /// <summary>
        /// This function will be called when the reading of the data packet fails, 
        /// such as camera unplugging, network interruption, etc.
        /// </summary>
        /// <param name="error">error log</param>
        private void OnInterruptCallback(string error)
        {
            if (onInterruptEvent != null)
            {
                try
                {
                    onInterruptEvent.Invoke(error);
                }
                catch { }
            }
        }

        /// <summary>
        /// open media
        /// </summary>
        /// <param name="url"></param>
        public void Open(MediaType openMode, string url = null)
        {
            if (onOpenEvent != null)
            {
                try
                {
                    onOpenEvent.Invoke();
                }
                catch { }
            }

            Close();
            if (core == null) return;
            isFirst = true;
            if (string.IsNullOrEmpty(url))
                url = this.url;
            core.DisableVideo = disableVideo;
            core.DisableAudio = disableAudio;
            core.DisableSubtitle = true;
            core.DefaultVideoTrack = defaultVideoTrack;
            core.DefaultAudioTrack = defaultAudioTrack;
            core.DefaultSubtitleTrack = 0;

            core.EnableHWAccel = enableHWAccel;
            core.HWAccelType = HWAccelType;
            core.OutputPixelFormat = outputPixelFormat;

            core.OpenMode = this.openMode = openMode;

            core.CameraWidth = cameraWidth;
            core.CameraHeight = cameraHeight;
            core.CameraFPS = cameraFPS;
            core.Options = options;

            core.OpenAndPlay = openAndPlay;
            core.Loop = loop;
            core.Volume = volume;
            core.Speed = speed;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            core.LParam = XRendererEx.XRendererEx_GetD3D11Device(0).ToInt64();
#elif UNITY_ANDROID
            if(core.LParam == 0)
                core.LParam = VideoRenderer.GetANativeWindow().ToInt64();       
#endif
			core.ExtractHWFrame = extractHWFrame;
            renderClosed = false;
            renderStrategy = RenderingStrategy.Unknow;
            this.url = url;
            core.Open(openMode, url);
        }

		/// <summary>
		/// seek to the next video frame 
		/// </summary>
		public void StepNextFrame()
		{
            core.StepNextFrame();
		}

        /// <summary>
        /// replay video
        /// </summary>
        /// <param name="paused">pause or play</param>
        public void Replay(bool paused)
        {
            if (core == null) return;
            core.Replay(paused);
        }

        /// <summary>
        /// close media
        /// </summary>
        public void Close()
        {
            if (onCloseEvent != null)
            {
                try
                {
                    onCloseEvent.Invoke();
                }
                catch { }
            }
            if (core == null) return;
            allowDraw = false;
            isFirst = false;
            renderMux.WaitOne();
            renderClosed = true;
            renderMux.ReleaseMutex();
            renderEvent.Set();
            core.Close();
            VideoRenderer.TerminateRenderer();
            hwRenderCtxMux.WaitOne();
            renderQueue.Clear();
            renderStrategy = RenderingStrategy.Unknow;
            if(hwRenderCtx != null)
			{
                SCMGR.FreeRenderContext(hwRenderCtx);
                hwRenderCtx = null;
			}
            hwRenderCtxMux.ReleaseMutex();
        }

        /// <summary>
        /// Seek to key frame quickly according to percentage
        /// </summary>
        /// <param name="percent"></param>
        public void SeekFastPercent(double percent)
        {
            if (core == null) return;
            core.SeekFastPercent(percent);
        }

        /// <summary>
        /// Seek to key frame quickly according to ms
        /// </summary>
        /// <param name="ms"></param>
        public void SeekFastMilliSecond(int ms)
        {
            if (core == null) return;
            core.SeekFastMilliSecond(ms);
        }

        /// <summary>
        /// play
        /// </summary>
        public void Play()
        {
            if (core == null) return;
            core.Play();
        }

        /// <summary>
        /// pause
        /// </summary>
        public void Pause()
        {
            if (core == null) return;
            core.Pause();
        }


        /// <summary>
        /// draw video frame
        /// set volume
        /// </summary>
        private void Update()
        {
            DrawImp();
            HandleRenderQueue();
            if (core == null) return;
            if (core.Volume != volume)
                core.Volume = volume;
            if (core.Speed != speed)
                core.Speed = speed;
            if (loop != _loop)
            {
                _loop = loop;
                core.Loop = loop;
            }

            if (ISCNative.GetTimestamp() - lastTs >= 1000000)
            {
                lastTs += 1000000;
                decoderFps = _decoderFps;
                _decoderFps = 0;
            }
        }

		private void HandleRenderQueue()
		{
            hwRenderCtxMux.WaitOne();
            if (renderQueue.Count <= 0)
			{
                hwRenderCtxMux.ReleaseMutex();
                return;
			}
            RenderCommand cmd = renderQueue.Dequeue();
			cmd.frame.data[7] = cmd.retVal;
			VideoRenderer.Renderer(cmd.frame, DrawResult, hwRenderCtx);
            hwRenderCtxMux.ReleaseMutex();
            VideoRenderer.Apply();
		}

        private void DrawResult(bool changed)
		{
			if (changed)
			{
				if (onRendererChangedEvent != null)
				{
					try
					{
						onRendererChangedEvent.Invoke(VideoRenderer.SCRenderer);
					}
					catch { }
				}

			}

			if (isFirst)
			{
				isFirst = false;
				if (onFirstFrameRenderEvent != null)
				{
					try
					{
						onFirstFrameRenderEvent.Invoke(VideoRenderer.SCRenderer);
					}
					catch { }
				}
			}

			if (onRenderVideoFrameEvent != null)
			{
				try
				{
					onRenderVideoFrameEvent.Invoke(VideoRenderer.SCRenderer);
				}
				catch { }
			}

			//renderEvent.Set();
		}

        private void DrawImp()
        {
			if (renderStrategy != RenderingStrategy.Software) return;
			if (core == null || core.Closed || !core.AllowDraw) return;
			SCFrame frame = core.LockFrame();
			framePixelFormat = frame.format;
			VideoRenderer.Renderer(frame, DrawResult);
            VideoRenderer.Apply();
			core.UnlockFrame();
			renderEvent.Set();
        }

		private void NativeRender(int eventID)
		{
            hwRenderCtxMux.WaitOne();
            NativeRenderImp();
            hwRenderCtxMux.ReleaseMutex();
		}

        private void NativeRenderImp()
		{
			if (renderStrategy != RenderingStrategy.Hardware) return;
			if (hwRenderCtx == null)
			{
				ISCNative.SCLog(LogLevel.Error, "hw render ctx is null");
				return;
			}
			if (core == null || core.Closed || !allowDraw) return;
            allowDraw = false;
			if (hwRenderCtx.released)
			{
				ISCNative.SCLog(LogLevel.Error, "hw render ctx released");
				return;
			}

			SCFrame frame = core.LockFrame();
			framePixelFormat = frame.format;
			RenderCommand cmd = new RenderCommand();
			cmd.isFirst = false;
			if (hwRenderCtx.renderer == IntPtr.Zero)
			{
				hwRenderCtx.renderer = XRendererEx.XRendererEx_Create(System.IntPtr.Zero, System.IntPtr.Zero);
				hwRenderCtx.texture = XRendererEx.XTextureEx_Create(hwRenderCtx.renderer);
				XRendererEx.XRendererEx_Resize(hwRenderCtx.renderer, frame.width, frame.height);
				XRendererEx.XRendererEx_Clear(hwRenderCtx.renderer, 1, 0, 0, 1);
				cmd.isFirst = true;
			}
			bool changed = false;
			if (hwRenderCtx.width != frame.width || hwRenderCtx.height != frame.height)
			{
				SCMGR.ReleaseSharedTexture(hwRenderCtx);
				changed = true;
			}
			hwRenderCtx.width = frame.width;
			hwRenderCtx.height = frame.height;
			float w = 0, h = 0;
			XRendererEx.XRendererEx_GetSceneViewportSize(hwRenderCtx.renderer, ref w, ref h);
			XRendererEx.XRendererEx_SetScaling(hwRenderCtx.renderer, w, h, 1);
			XRendererEx.XTextureEx_Update(hwRenderCtx.texture, frame.width, frame.height, (int)frame.format, frame.linesize, frame.data, frame.hwctx);
			XRendererEx.XRendererEx_Draw(hwRenderCtx.renderer, hwRenderCtx.texture);
			XRendererEx.XRendererEx_Present(hwRenderCtx.renderer, 0);
			cmd.frame = frame;
			core.UnlockFrame();
			if (changed)
			{
				IntPtr renderTarget = IntPtr.Zero;
				XRendererEx.XRendererEx_RenderTarget(hwRenderCtx.renderer, ref renderTarget);
				hwRenderCtx.sharedTexture = cmd.retVal = SCMGR.GetSharedTexture(renderTarget);
			}
			renderEvent.Set();
			renderQueue.Enqueue(cmd);
		}

		private void OnDrawVideoFrame(SCFrame frame)
        {
            if(renderStrategy == RenderingStrategy.Unknow)
			{
			    renderStrategy = Enum.IsDefined(typeof(HWPixelFormat), (HWPixelFormat)frame.format) ? RenderingStrategy.Hardware : RenderingStrategy.Software;
#if UNITY_EDITOR
				core.EnableLogicVsync = false;
#else
                core.EnableLogicVsync = renderStrategy == RenderingStrategy.Hardware ? false : QualitySettings.vSyncCount == 1;
#endif
            }
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
#elif UNITY_ANDROID
            renderStrategy = RenderingStrategy.Software;
#endif
			_decoderFps++;
            if(core.FrameRate - SCMGR.GetMaxDisplayFrequency() > 5)
			{
                allowDraw = true;
                return;
			}
            renderMux.WaitOne();
            allowDraw = true;
            if (renderClosed)
            {
                renderMux.ReleaseMutex();
                return;
            }
            renderMux.ReleaseMutex();
            renderEvent.WaitOne();
        }

        /// <summary>
        /// Whether the current playback mode is looping, it will be called after media playback is complete.
        /// </summary>
        private void OnStreamFinished()
        {
            if (onStreamFinishedEvent != null)
            {
                try
                {
                    onStreamFinishedEvent.Invoke();
                }
                catch { }
            }

        }

        /// <summary>
        /// Release all resources of the player. 
        /// The user does not need to call this function. 
        /// All operations will be invalid after the function is called.
        /// </summary>
        public void ReleaseCore()
        {
            Close();
            if (core == null) return;
            SCPlayerProManager.ReleasePlayer(playerContext);
            VideoRenderer.TerminateRenderer();
            SCMGR.RemovePlayer(this);
            UnityNativePlugin.UnityNativeRenderEvent -= NativeRender;
            core.onStreamFinishedEvent -= OnStreamFinished;
            core.onCaptureOpenCallbackEvent -= OnCaptureOpenCallback;
            core.onInterruptCallbackEvent -= OnInterruptCallback;
            core.onDrawAudioFrameEvent -= OnDrawAudioFrame;
            core.onDrawVideoFrameEvent -= OnDrawVideoFrame;
            core = null;
        }
        private void OnDestroy()
        {
            ReleaseCore();
        }
    }

}
