using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

/// <summary>
/// This file defines the C# interface of the external function,
/// Of course you can port it to other programs
/// </summary>
namespace Sttplay.MediaPlayer
{
    /// <summary>
    /// log level , such as Log, LogWarning, LogError
    /// </summary>
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// All hardware deviceType data types are enumerated here
    /// This does not mean that the platform supports all types
    /// </summary>
    public enum HWDeviceType
    {
        AUTO = 0,
        VDPAU,
        CUDA,
        VAAPI,
        DXVA2,
        QSV,
        VIDEOTOOLBOX,
        D3D11VA,
        DRM,
        OPENCL,
        MEDIACODEC,
        VULKAN
    }

    /// <summary>
    /// All Output pixel format data types are enumerated here
    /// Automatic selection will be set as the default output format. 
    /// If the replaced output format is not in the enumerated range, 
    /// it will be automatically selected as BGRA again. 
    /// </summary>
    public enum PixelFormat
    {
        AUTO = -1,

        YUV420P = 0,
        YUV422P = 4,
        YUV444P = 5,

        YUYV422 = 1,
        UYVY422 = 15,

        GRAY8 = 8,

        YUVJ420P = 12,
        YUVJ422P = 13,
        YUVJ444P = 14,

        NV12 = 23,
        NV21 = 24,

        RGB24 = 2,
        BGR24 = 3,

        ARGB = 25,
        RGBA = 26,
        ABGR = 27,
        BGRA = 28,

    };

    public enum HWPixelFormat
	{
		PIX_FMT_DXVA2 = 51,
		PIX_FMT_CUDA = 117,
		PIX_FMT_VIDEOTOOLBOX = 158,
		PIX_FMT_MEDIACODEC = 165,
		PIX_FMT_D3D11 = 172,
	}

    public enum SampleFormat
    {

        SAM_FMT_UNKOW = -1,
        SAM_FMT_U8,          ///< unsigned 8 bits
        SAM_FMT_S16,         ///< signed 16 bits
        SAM_FMT_S32,         ///< signed 32 bits
        SAM_FMT_FLT,         ///< float
        SAM_FMT_DBL,         ///< double

        SAM_FMT_U8P,         ///< unsigned 8 bits, planar
        SAM_FMT_S16P,        ///< signed 16 bits, planar
        SAM_FMT_S32P,        ///< signed 32 bits, planar
        SAM_FMT_FLTP,        ///< float, planar
        SAM_FMT_DBLP,        ///< double, planar
        SAM_FMT_S64,         ///< signed 64 bits
        SAM_FMT_S64P,        ///< signed 64 bits, planar
    };

    public enum AudioOutputFormat
    {
        S16 = SampleFormat.SAM_FMT_S16,
        S32 = SampleFormat.SAM_FMT_S32,
        FLT = SampleFormat.SAM_FMT_FLT
    }

    /// <summary>
    /// Create SCFrame mark, mark whether to copy or move data when calling ImageCopy
    /// </summary>
    public enum SCFrameFlag
    {
        Copy = 0,
        Move
    }

    /// <summary>
    /// open configuration
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class SCConfiguration
    {
        public int disableVideo = 0;
        public int disableAudio = 0;
        public int disableSubtitle = 1;
        public int videoTrack = 0;
        public int audioTrack = 0;
        public int subtitleTrack = 0;
        public int enableHWAccel = 1;
        public int hwaccelType = (int)HWDeviceType.AUTO;
        public int extractHWFrame = 1;                              //extract hwaccel frame, there set 1
        public int outputPixfmt = (int)PixelFormat.AUTO;
        public int openMode = (int)MediaType.LocalFile;
        public int cameraWidth = 640;
        public int cameraHeight = 480;
        public float cameraFPS = 30.0f;
        public long lparam = 0;
    };

    public enum CodecType
    {
        Video,
        Audio
    }

    public enum CodecID
    {
        H264 = 27,
        HEVC = 173,

        AAC = 86018
    }

    /// <summary>
    /// Basic video parameters 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class VideoParams
    {
        public int pixfmt;
        public int width;
        public int height;
        public float fps;
        public int mfDuration;
    };

    /// <summary>
    /// Basic audio parameters 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class AudioParams
    {
        public int freq;
        public int channels;
        public long channel_layout;
        public int fmt;
    };

    /// <summary>
    /// Basic audio parameters 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class SubtitleParams
    {
        public int reserve;
    };

    /// <summary>
    /// Basic media parameters
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class OpenCallbackContext
    {
        public long duration;
        public IntPtr videoParams;
        public IntPtr audioParams;
        public IntPtr subtitleParams;
        public bool realtime;
        public bool localfile;
    };

    /// <summary>
    /// This parameter will be used after audioplayer is successfully opened
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class PlayerParams
    {
        /// <summary>
        /// Media audio parameters 
        /// </summary>
        public IntPtr srcap;

        /// <summary>
        /// In some special cases, the device does not support the audio format of the media, 
        /// so you need to change the audio output format
        /// </summary>
        public IntPtr dstap;

        /// <summary>
        /// hardware buffer size
        /// </summary>
        public int hwSize;
    }

    /// <summary>
    /// video, audio
    /// </summary>
    public enum FrameType
    {
        Video = 0,
        Audio = 1,
        Subtitle = 3
    }

    /// <summary>
    /// The meaning of context_type extension of SCFrame structure
    /// </summary>
    public enum FrameContextType
    {
        FRAME = 0,
        EOF             //Everything in this frame is invalid, to the end of the video
    }

    /// <summary>
    /// An important structure 
    /// You can get useful data from this structure 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SCFrame
    {
        public int media_type;          //For details, please see FrameType
        public int context_type;        //For details, please see FrameContextType
        public int width;               //frame pixel width
        public int height;              //frame pixel height
        public int width_ps;            //frame physics width (calculate aspect ratio)
        public int height_ps;           //frame physics height (calculate aspect ratio)
        public double rotation;         //frame rotation
        public int format;              //frame format
        public int color_range;         //color range
        public int color_space;         //color space
        public long pts;                //pts of media files 
        public long pts_ms;             //The pts of media files are based on physical time
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public int[] linesize;          //line size, and width are not necessarily the same
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public IntPtr[] data;           //data pointer
        public double duration;         //duration

        public int nb_samples;          //number of audio samples (per channel) described by this frame
        public int sample_rate;         //sample rate

        public IntPtr frame;          //avframe
        public IntPtr subtitle;
        public int uploaded;
        public IntPtr fn;
        public int flag;
        public IntPtr hwctx;
    }

    /// <summary>
    /// capture open result
    /// </summary>
    public enum CaptureOpenResult
    {
        SUCCESS = 0,
        CERTIFICATE_INVALID = -1,
        PARAMETERS_ERROR = -2,
        FAILED = -3,
    }

    public enum DeviceType
    {
        VideoInput = 0,
        AudioInput
    }

    /// <summary>
    /// audio player open result
    /// </summary>
    public enum AudioPlayerOpenResult
    {
        SUCCESS = 0,
        DeviceError = -1,
        FormatNotSupport = -2
    }

    /// <summary>
    /// resample struct
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ResampleData
    {
        public int length;
        public int nbSamples;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public IntPtr[] data;
    }

    /// <summary>
    /// resample struct
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct SoundData
    {
        public int length;
        public IntPtr data;
    };

    public enum AudioDriverType
    {
        Auto,
        Wasapi,
        DSound,
    }

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public class CaptureOpenSem
    {
		public IntPtr trigger;
		public int code;
		public IntPtr error;
		public IntPtr ctx;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public class InterruptSem
	{
		public IntPtr trigger;
		public IntPtr error;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public class AudioOpenSem
	{
		public IntPtr trigger;
		public int code;
		public IntPtr error;
		public IntPtr ctx;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class AudioPlaySem
	{
        public IntPtr other;
        public IntPtr self;
        public IntPtr stream;
        public int len;
	}

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class LogSem
    {
        public IntPtr other;
        public IntPtr self;
        public IntPtr log;
        public int level;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class PreviewSem
	{
		public IntPtr interruptSem;
		public int openState;
		public int width;
		public int height;
		public IntPtr data;
		public float brightness;
		public int fmt;
	}

    public class ISCNative
    {

#if UNITY_IOS && !UNITY_EDITOR_OSX
        public const string SCCore = "__Internal";
        public const string SCUtility = "__Internal";
        public const string AudioPlayer = "__Internal";
#else
        public const string SCCore = "sccore";
        public const string SCUtility = "sccore";
        public const string AudioPlayer = "sccore";
#endif
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LogCallbackDelegate(int level, IntPtr user);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CaptureOpenCallbackDelegate(int state, IntPtr error, IntPtr ctx, IntPtr user);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void InterruptCallbackDelegate(IntPtr error, IntPtr user);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void AudioDeviceOpenCallbackDelegate(int code, IntPtr error, IntPtr param, IntPtr user);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void AudioPlayCallbackDelegate(IntPtr stream, int len, IntPtr user);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CamInfoCallbackDelegate(IntPtr user, IntPtr info);

		//**********************************************************************************************
		//                                  SCUtility
		//**********************************************************************************************

		[DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
		private static extern void EnableUDPLog(IntPtr ip, int port);

		[DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        private static extern int EnableLogFile(int mode, IntPtr filename);

		[DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetLogSem();

        [DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SCLog(int level, byte[] log);

        [DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetLogCallback(LogCallbackDelegate callback);

        [DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
        public static extern long GetTimestampUTC();

        [DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
        public static extern long GetTimestamp();

        [DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SleepMs(int ms);

        [DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Memset(IntPtr dst, byte val, int len);

        [DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Memcopy(IntPtr dst, IntPtr src, int len);

		//**********************************************************************************************
		//                                  SCCore
		//**********************************************************************************************

#if UNITY_ANDROID
        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr  GetJavaVM(ref int version);
#endif

		[DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
		public static extern void EnableCallbackSem(int enable);

		[DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr StreamCaptureProVersion();

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void InitializeStreamCapturePro();

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TerminateStreamCapturePro();

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateStreamCapture();

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ReleaseStreamCapture(IntPtr capture);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        private static extern void AsyncOpenStreamCapture(IntPtr capture, IntPtr url, IntPtr configuration, CaptureOpenCallbackDelegate cb, IntPtr user);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetStreamCaptureOpenSem(IntPtr capture);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetStreamCaptureInterruptSem(IntPtr capture);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CloseStreamCapture(IntPtr capture);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetInterruptCallback(IntPtr capture, InterruptCallbackDelegate callback, IntPtr user);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetOpenOptions(IntPtr capture, IntPtr option);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetCaptureLoop(IntPtr capture, int loop);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SeekFastPercent(IntPtr capture, double percent);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SeekFastMs(IntPtr capture, int ms);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TryGrabFrame(IntPtr capture, int type, ref IntPtr frame);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TryGrabLastFrame(IntPtr capture, int type, ref IntPtr frame);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TryGrabNextFrame(IntPtr capture, int type, ref IntPtr frame);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetStreamIndex(IntPtr capture, int type, int index);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetStreamIndex(IntPtr capture, int type, ref int index);

        //[DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        //public static extern void ClearAllCache(IntPtr capture);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FrameMoveToLast(IntPtr capture, int type);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateSCFrame(IntPtr src, int align, int flags);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ReleaseSCFrame(IntPtr src);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImageCopy(IntPtr dst, IntPtr src);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr MemoryAlignment(IntPtr srcdata, int linepixelsize, int height, int linesize, byte[] destdata);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetMediaInfo(IntPtr capture);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetSubtitle(IntPtr capture, IntPtr frame);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetPFCount(IntPtr capture, int type, ref int packetCount, ref int frameCount);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateFrameConvert();

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ReleaseFrameConvert(IntPtr convert);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ConvertFrame(IntPtr convert, IntPtr scframe, int format);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PushRawDataToCapture(IntPtr capture, byte[] data, int size);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        private static extern void AddPixelFormatFilter(int pixelFormat);

		[DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
		private static extern void RemovePixelFormatFilter(int pixelFormat);

		[DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr BeginPreviewContext(IntPtr url, int vFlip);

		[DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SetPreviewTargetTimestamp(long ms);

		[DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
		public static extern void EndPreviewContext();

		public static IntPtr BeginPreviewContext(string url, bool vFlip)
		{
			IntPtr strPtr = StringToIntPtr(url);
			var ptr = BeginPreviewContext(strPtr, vFlip ? 1 : 0);
			ReleaseStringIntPtr(strPtr);
            return ptr;
		}

		//**********************************************************************************************
		//                                  Device
		//**********************************************************************************************

		[DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetCameraCount();

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetCameraName")]
        private static extern IntPtr GetCameraNameNative(int index, int raw);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        private static extern void GetCameraInfomation(byte[] camname, IntPtr user, CamInfoCallbackDelegate cb, int async);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetMaxDisplayFrequency();

        //**********************************************************************************************
        //                                  AudioPlayer
        //**********************************************************************************************

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetAudioDriver(int audioDriver);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateAudioPlayer();

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ReleaseAudioPlayer(IntPtr player);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        private static extern void AsyncOpenAudioPlayer(IntPtr player, IntPtr device, IntPtr inputAS, int paused, AudioDeviceOpenCallbackDelegate openCb, IntPtr openUser, AudioPlayCallbackDelegate playCb, IntPtr playUser);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetAudioPlayerOpenSem(IntPtr player);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CloseAudioPlayer(IntPtr player);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PausedAudioPlayer(IntPtr player, int isPaused);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ClearAudioPlayer(IntPtr player);

		[DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr GetAudioPlaySem(IntPtr player);

		[DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void MixAudioFormat(IntPtr stream, IntPtr src, int len, float srcVolume, float dstVolume, int format);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetBytesPerSample(int format);

        //**********************************************************************************************
        //                                  Resampler
        //**********************************************************************************************
        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateResampler();

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ReleaseResampler(IntPtr resampler);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern int OpenResampler(IntPtr resampler, IntPtr srcap, IntPtr dstap);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CloseResampler(IntPtr resampler);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ResamplePush(IntPtr resampler, IntPtr[] data, int nbSamples);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ResampleInputFormatVerify(IntPtr resampler, int format);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ResampleGet(IntPtr resampler, int nbSamples);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ResamplePop(IntPtr resampler, int nbSamples);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ResampleTempo(IntPtr resampler, IntPtr data, int nbSamples, float tempo);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetUnprocessedSamples(IntPtr resampler);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ResampleClear(IntPtr resampler);

        //=====================================================================
        // ByteArray
        //=====================================================================
        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateByteArray();

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ReleaseByteArray(IntPtr arr);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetByteArraySize(IntPtr arr);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetByteArrayData(IntPtr arr);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PushDataToByteArray(IntPtr arr, IntPtr data, int len);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void RemoveRangeFromByteArray(IntPtr arr, int len);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ClearByteArray(IntPtr arr);

        //=====================================================================
        // SCThread
        //=====================================================================

        public static void AddPixelFormatFilter(HWPixelFormat pixelFormat)
        {
            AddPixelFormatFilter((int)pixelFormat);
        }

        public static void RemovePixelFormatFilter(HWPixelFormat pixelFormat)
        {
            RemovePixelFormatFilter((int)pixelFormat);
        }

        [DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SCSemaphore_Create(uint initial_value);

        [DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SCSemaphore_Destroy(IntPtr sem);

		[DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SCSemaphore_Wait(IntPtr sem);

		[DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SCSemaphore_WaitTimeout(IntPtr sem, uint ms);

        [DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SCSemaphore_TryWait(IntPtr sem);

        [DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SCSemaphore_Post(IntPtr sem);

        [DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint SCSemaphore_Value(IntPtr sem);

        [DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SCThread_WorkerCount();

        public const int FINISH_FLG = 0xFFFF;
        public const int NO_ARG = 0x0001;
        public const int HAS_ARG = 0x0002;
        public const int OPT_STRING = 0x0002;
        public const int OPT_INT = 0x0004;
        public const int OPT_FLOAT = 0x008;
        public class SCOption
        {
            public SCOption(string name, int flag, string value)
            {
                this.name = name;
                this.flag = flag;
                this.value = value;
            }
            public string name;
            public int flag;
            public string value;
        };
        public static int GetOptions(string inArgs, SCOption[] options, List<SCOption> outOptions)
        {
            outOptions.Clear();
            string[] result = inArgs.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (result.Length <= 0) return 0;
            foreach (var item in result)
            {
                string str = item.Replace('\t', ' ');
                string[] group = str.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                string optname = group[0].Trim();
                SCOption o = find_option(options, optname);
                if (o == null)
                    continue;
                if (o.flag == HAS_ARG && group.Length < 2)
                    continue;
                if (o.flag == HAS_ARG)
                    outOptions.Add(new SCOption(o.name, o.flag, group[1].Trim()));
                else if (o.flag == NO_ARG)
                    outOptions.Add(new SCOption(o.name, o.flag, ""));
            }
            return outOptions.Count;
        }

        private static SCOption find_option(SCOption[] po, string name)
        {
            SCOption opt = null;
            for (int i = 0; i < po.Length; i++)
            {
                if (name == po[i].name)
                {
                    opt = po[i];
                    break;
                }
            }
            return opt;
        }

        public static void EnableUDPLog(IPAddress ip, int port)
		{
            var pip = StringToIntPtr(ip.ToString());
            EnableUDPLog(pip, port);
            ReleaseStringIntPtr(pip);
		}

        public enum LogFileMode
		{
            None,
            One,
            Mul
		}
        public static bool EnableLogFile(LogFileMode mode, string filename)
		{
            var pfn = StringToIntPtr(filename);
            int ret = EnableLogFile((int)mode, pfn);
            ReleaseStringIntPtr(pfn);
            return ret == 1;
		}

        public static void SCLog(LogLevel level, string log)
        {
            SCLog((int)level, StringToByteArray(log));
        }
        public static byte[] StringToByteArray(string str)
        {
            return System.Text.Encoding.UTF8.GetBytes(str).Concat(new byte[1] { 0 }).ToArray();
        }

        public static IntPtr StructureToIntPtr(object structObj)
        {
            int size = Marshal.SizeOf(structObj);
            IntPtr structPtr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(structObj, structPtr, false);
            return structPtr;
        }

        public static void ReleaseStructIntPtr(IntPtr ptr)
        {
            Marshal.FreeHGlobal(ptr);
        }

        public static IntPtr StringToIntPtr(string str)
        {
            byte[] buff = StringToByteArray(str);
            IntPtr ptr = Marshal.AllocHGlobal(buff.Length);
            Marshal.Copy(buff, 0, ptr, buff.Length);
            return ptr;
        }

        public static void ReleaseStringIntPtr(IntPtr ptr)
        {
            Marshal.FreeHGlobal(ptr);
        }

		#region Log callback
		public static LogCallbackDelegate LogCallbck;
#if UNITY_2019_4_OR_NEWER
        [AOT.MonoPInvokeCallback(typeof(LogCallbackDelegate))]
#endif
        private static void OnLogCallback(int level, IntPtr log)
        {
            LogLevel l1 = (LogLevel)level;
            string l2 = Marshal.PtrToStringAnsi(log);
            if (logCallback != null)
                logCallback(l1, l2);
        }
        private static Action<LogLevel, string> logCallback;
        public static void SetLogCallback(Action<LogLevel, string> callback)
        {
            if (callback == null) LogCallbck = null;
            else LogCallbck = OnLogCallback;
            SetLogCallback(LogCallbck);
            logCallback = callback;
        }
		#endregion

		#region Open callback
		private static CaptureOpenCallbackDelegate CaptureOpenCallbck = OnCaptureOpenCallback;
#if UNITY_2019_4_OR_NEWER
        [AOT.MonoPInvokeCallback(typeof(CaptureOpenCallbackDelegate))]
#endif
        private static void OnCaptureOpenCallback(int state, IntPtr error, IntPtr ctx, IntPtr user)
        {
            if (captureOpenResult != null)
                captureOpenResult((CaptureOpenResult)state, Marshal.PtrToStringAnsi(error), Marshal.PtrToStructure<OpenCallbackContext>(ctx), user);
        }

        private static Action<CaptureOpenResult, string, OpenCallbackContext, IntPtr> captureOpenResult;
        public static void AsyncOpenStreamCapture(IntPtr capture, string url, SCConfiguration configuration, Action<CaptureOpenResult, string, OpenCallbackContext, IntPtr> callback, IntPtr user)
		{
            captureOpenResult = callback;
			var pconfig = ISCNative.StructureToIntPtr(configuration);
			var purl = ISCNative.StringToIntPtr(url.Trim());
            AsyncOpenStreamCapture(capture, purl, pconfig, CaptureOpenCallbck, user);
			ISCNative.ReleaseStructIntPtr(pconfig);
			ISCNative.ReleaseStringIntPtr(purl);
		}
		#endregion

		#region Interrupt callback
		private static InterruptCallbackDelegate InterruptCallback = OnInterruptCallback;
#if UNITY_2019_4_OR_NEWER
        [AOT.MonoPInvokeCallback(typeof(InterruptCallbackDelegate))]
#endif
        private static void OnInterruptCallback(IntPtr error, IntPtr user)
        {
            if (interruptResult != null)
                interruptResult(Marshal.PtrToStringAnsi(error), user);
        }

        private static Action<string, IntPtr> interruptResult;
        public static void SetInterruptCallback(IntPtr capture, Action<string, IntPtr> callback, IntPtr user)
		{
            interruptResult = callback;
            SetInterruptCallback(capture, InterruptCallback, user);
		}
        #endregion

        #region Audio device opencallback

        private static AudioDeviceOpenCallbackDelegate AudioDeviceOpenCallback = OnAudioDeviceOpenCallback;
#if UNITY_2019_4_OR_NEWER
        [AOT.MonoPInvokeCallback(typeof(AudioDeviceOpenCallbackDelegate))]
#endif
        private static void OnAudioDeviceOpenCallback(int code, IntPtr error, IntPtr param, IntPtr user)
        {
            if (audioOpenResult != null)
                audioOpenResult((AudioPlayerOpenResult)code, Marshal.PtrToStringAnsi(error), Marshal.PtrToStructure<PlayerParams>(param), user);
        }

        private static Action<AudioPlayerOpenResult, string, PlayerParams, IntPtr> audioOpenResult;
        private static Action<IntPtr, int, IntPtr> audioPlayResult;
        public static void AsyncOpenAudioPlayer(IntPtr player, IntPtr device, IntPtr inputAS, int paused, Action<AudioPlayerOpenResult, string, PlayerParams, IntPtr> openCb, IntPtr openUser, Action<IntPtr, int, IntPtr> playCb, IntPtr playUser)
		{
            audioOpenResult = openCb;
            audioPlayResult = playCb;
            AsyncOpenAudioPlayer(player, device, inputAS, paused, AudioDeviceOpenCallback, openUser, AudioPlayCallback, playUser);
		}

        private static AudioPlayCallbackDelegate AudioPlayCallback = OnAudioPlayCallback;
#if UNITY_2019_4_OR_NEWER
        [AOT.MonoPInvokeCallback(typeof(AudioPlayCallbackDelegate))]
#endif
        private static void OnAudioPlayCallback(IntPtr stream, int len, IntPtr user)
        {
            if (audioPlayResult != null)
                audioPlayResult(stream, len, user);
        }
		#endregion

		#region Camera
		private static Action<string> cameraInfoCallback;

        public static string GetCameraName(int index, int raw)
		{
			IntPtr strPtr = GetCameraNameNative(index, raw);
			int len = 0;
			while (Marshal.ReadByte(strPtr, len) > 0) { len++; }
			byte[] strBytes = new byte[len];
			Marshal.Copy(strPtr, strBytes, 0, len);
			string cameraName = System.Text.Encoding.UTF8.GetString(strBytes);
            return cameraName;
        }
        public static List<string> GetDeviceList(DeviceType type)
		{
			List<string> devices = new List<string>();
			int count = ISCNative.GetCameraCount();
			for (int i = 0; i < count; i++)
				devices.Add(GetCameraName(i, 0));
			return devices;
		}

		public static void GetCameraInfomation(string deviceName, Action<string> cb)
		{
			cameraInfoCallback = cb;
			ISCNative.CamInfoCallbackDelegate icb = GetCameraInfoCallback;
			ISCNative.GetCameraInfomation(ISCNative.StringToByteArray(deviceName), IntPtr.Zero, icb, 0);
		}

#if UNITY_2019_4_OR_NEWER
		[AOT.MonoPInvokeCallback(typeof(ISCNative.CamInfoCallbackDelegate))]
#endif
		private static void GetCameraInfoCallback(IntPtr user, IntPtr info)
		{
			string infostr = Marshal.PtrToStringUni(info);
			byte[] bs = System.Text.Encoding.Unicode.GetBytes(infostr);
			infostr = System.Text.Encoding.UTF8.GetString(bs);
			if (cameraInfoCallback != null) cameraInfoCallback(infostr.Remove(infostr.LastIndexOf('\n')));
		}
#endregion
	}
}