using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sttplay.MediaPlayer
{
	/// <summary>
	/// Color Range
	/// </summary>
	public enum SCColorRange
	{
		UNSPECIFIED = 0,
		MPEG = 1, ///< the normal 219*2^(n-8) "MPEG" YUV ranges
		JPEG = 2, ///< the normal     2^n-1   "JPEG" YUV ranges
	};

	/// <summary>
	/// Color Space
	/// </summary>
	public enum SCColorSpace
	{
		RGB = 0,  ///< order of coefficients is actually GBR, also IEC 61966-2-1 (sRGB)
		BT709 = 1,  ///< also ITU-R BT1361 / IEC 61966-2-4 xvYCC709 / SMPTE RP177 Annex B
		UNSPECIFIED = 2,
		RESERVED = 3,
		FCC = 4,  ///< FCC Title 47 Code of Federal Regulations 73.682 (a)(20)
		BT470BG = 5,  ///< also ITU-R BT601-6 625 / ITU-R BT1358 625 / ITU-R BT1700 625 PAL & SECAM / IEC 61966-2-4 xvYCC601
		SMPTE170M = 6,  ///< also ITU-R BT601-6 525 / ITU-R BT1358 525 / ITU-R BT1700 NTSC
		SMPTE240M = 7,  ///< functionally identical to above
		YCGCO = 8,  ///< Used by Dirac / VC-2 and H.264 FRext, see ITU-T SG16
		YCOCG = YCGCO,
		BT2020_NCL = 9,  ///< ITU-R BT2020 non-constant luminance system
		BT2020_CL = 10, ///< ITU-R BT2020 constant luminance system
		SMPTE2085 = 11, ///< SMPTE 2085, Y'D'zD'x
		CHROMA_DERIVED_NCL = 12, ///< Chromaticity-derived non-constant luminance system
		CHROMA_DERIVED_CL = 13, ///< Chromaticity-derived constant luminance system
		ICTCP = 14, ///< ITU-R BT.2100-0, ICtCp
	};

	/// <summary>
	/// Unity internal color space
	/// </summary>
	public enum ShaderColorSpace
	{
		JPEG = 0,
		BT709,
		BT601
	}

	/// <summary>
	/// The class is responsible for drawing the video onto the material
	/// </summary>
	public class SCVideoRenderer
	{
		/// <summary>
		/// pixel format
		/// </summary>
		public PixelFormat PixelFmort { get; private set; }

		/// <summary>
		/// video width
		/// </summary>
		public int Width { get; private set; }

		/// <summary>
		/// video height
		/// </summary>
		public int Height { get; private set; }

		/// <summary>
		/// Renderer
		/// </summary>
		public SCRenderer SCRenderer { get; set; }

		private bool isChanged = false;
		private System.Action<bool> callback;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
#elif UNITY_ANDROID
        private NativeRenderer nativeRenderer;
#endif
		public SCVideoRenderer()
		{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

#elif UNITY_ANDROID
            nativeRenderer = new NativeRenderer();
            nativeRenderer.SendSignal(NativeRenderer.SIGNAL_CREATE, AndroidTools.GetEGLContext());
#endif
		}

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
#elif UNITY_ANDROID
		public System.IntPtr GetANativeWindow()
        {
            return AndroidTools.GetEGLContext() == System.IntPtr.Zero ? System.IntPtr.Zero : (System.IntPtr)nativeRenderer.SendSignal(NativeRenderer.SIGNAL_GET_SURFACE);
        }
#endif

		public void Dispose()
		{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

#elif UNITY_ANDROID
			if (nativeRenderer != null)
            {
                nativeRenderer.SendSignal(NativeRenderer.SIGNAL_DESTROY);
                nativeRenderer.Dispose();
            }
            nativeRenderer = null;
#endif
		}

		/// <summary>
		/// Create a renderer instance based on different pixel formats
		/// </summary>
		/// <param name="frame">frame data</param>
		/// <param name="type">update type</param>
		/// <returns></returns>
		private SCRenderer CreateRenderer(SCFrame frame)
		{
			PixelFormat fmt = (PixelFormat)frame.format;
			SCRenderer renderer = null;
			if (fmt == PixelFormat.YUV420P || fmt == PixelFormat.YUVJ420P)
				renderer = new SCRendererYUV420P();
			else if (fmt == PixelFormat.YUV422P || fmt == PixelFormat.YUVJ422P)
				renderer = new SCRendererYUV422P();
			else if (fmt == PixelFormat.YUV444P || fmt == PixelFormat.YUVJ444P)
				renderer = new SCRendererYUV444P();

			else if (fmt == PixelFormat.YUYV422)
				renderer = new SCRendererYUYV422();
			else if (fmt == PixelFormat.UYVY422)
				renderer = new SCRendererUYVY422();

			else if (fmt == PixelFormat.GRAY8)
				renderer = new SCRendererGray8();

			else if (fmt == PixelFormat.NV12)
				renderer = new SCRendererNV12();
			else if (fmt == PixelFormat.NV21)
				renderer = new SCRendererNV21();

			else if (fmt == PixelFormat.RGB24)
				renderer = new SCRendererRGB24();
			else if (fmt == PixelFormat.BGR24)
				renderer = new SCRendererBGR24();

			else if (fmt == PixelFormat.ARGB)
				renderer = new SCRendererARGB();
			else if (fmt == PixelFormat.RGBA)
				renderer = new SCRendererRGBA();
			else if (fmt == PixelFormat.ABGR)
				renderer = new SCRendererABGR();
			else if (fmt == PixelFormat.BGRA)
				renderer = new SCRendererBGRA();
			else if ((int)fmt == (int)HWPixelFormat.PIX_FMT_DXVA2)
				renderer = new SCRendererDXVA2();
			else if ((int)fmt == (int)HWPixelFormat.PIX_FMT_D3D11)
				renderer = new SCRendererD3D11VA();
			else if ((int)fmt == (int)HWPixelFormat.PIX_FMT_CUDA)
				renderer = new SCRendererCUDA();
			else if ((int)fmt == (int)HWPixelFormat.PIX_FMT_MEDIACODEC)
			{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
#elif UNITY_ANDROID
				renderer = new SCRendererMediaCodec();
                long size = ((long)frame.width) << 32 | (long)frame.height;
                System.IntPtr fbo = (System.IntPtr)nativeRenderer.SendSignal(NativeRenderer.SIGNAL_RESIZE, size);
                renderer.SetNativeRenderer(nativeRenderer, fbo);
#endif
			}
			else if ((int)fmt == (int)HWPixelFormat.PIX_FMT_VIDEOTOOLBOX)
			{
				renderer = new SCRendererVIDEOTOOLBOX();
			}

			renderer.PixelFmort = PixelFmort = fmt;
			Width = frame.width;
			Height = frame.height;
			if (renderer.PixelFmort <= PixelFormat.BGRA)
				Debug.Log("Renderer Type:" + fmt);
			else
				Debug.Log("Renderer Type:" + (HWPixelFormat)fmt);
			return renderer;
		}

		/// <summary>
		/// initialize renderer
		/// </summary>
		/// <param name="frame"></param>
		private void InitializeRenderer(SCFrame frame)
		{
			SCRenderer = CreateRenderer(frame);
		}


		/// <summary>
		/// Terminate mark
		/// </summary>
		public void TerminateRenderer()
		{
			Width = 0;
			Height = 0;
			PixelFmort = 0;
			if (SCRenderer != null)
				SCRenderer.TerminateRenderer();
			SCRenderer = null;
		}

		/// <summary>
		/// prepare data
		/// </summary>
		/// <param name="frame"></param>
		public void Renderer(SCFrame frame, System.Action<bool> callback, HardwareRenderContext ctx = null)
		{
			this.callback = callback;
			this.isChanged = false;
			if (frame.width != Width || frame.height != Height || frame.format != (int)PixelFmort)
			{
				TerminateRenderer();
				InitializeRenderer(frame);
				isChanged = true;
				SCMGR.GCCollect();
				SCRenderer.SetHardwareRenderContext(ctx);
				SCRenderer.IsVaild = true;
			}
			if (frame.data[7] != System.IntPtr.Zero)
				SCRenderer.SetNativeRenderer(null, frame.data[7]);
			SCRenderer.Renderer(frame);
		}

		public void Apply()
		{
			if (SCRenderer.IsVaild)
				SCRenderer.Apply();
			if (callback != null)
				callback(isChanged);
		}
	}
}