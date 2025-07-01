using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;

namespace Sttplay.MediaPlayer
{
    /// <summary>
    /// Renderer instance 
    /// </summary>
    public class SCRendererVIDEOTOOLBOX : SCRenderer
    {
        private bool isFirst = true;
        private System.IntPtr yCache, uvCache;
        private System.IntPtr yTexRef, uvTexRef;
        private System.IntPtr yMTLTex, uvMTLTex;
       
        public override void InitRenderer(SCFrame frame)
        {
            SourceTextures = new Texture2D[2];
            RenderMaterial = new Material(Shader.Find("Sttplay/NV12"));
            SourceTextures[0] = Texture2D.CreateExternalTexture(frame.width, frame.height, TextureFormat.R8, false, false, yMTLTex);
            SourceTextures[1] = Texture2D.CreateExternalTexture(frame.linesize[1] / 2, frame.height / 2, TextureFormat.RG16, false, false, uvMTLTex);

            base.InitRenderer(frame);
        }

        public override void Renderer(SCFrame frame)
        {
            if (frame.format != (int)HWPixelFormat.PIX_FMT_VIDEOTOOLBOX)
            {
                Debug.LogError("The pixel format is different from the canvas type");
                return;
            }
            if (isFirst)
            {
                var device = OSXTools.GetDevice();
                yCache = OSXTools.CreateCVMetalTextureCache(device);
                uvCache = OSXTools.CreateCVMetalTextureCache(device);
                isFirst = false;
            }
            this.frame = frame;
        }

       
        public override void Apply()
        {
            if (yCache == System.IntPtr.Zero || uvCache == System.IntPtr.Zero) return;
            ReleaseTexRef();
            yTexRef = OSXTools.CreateCVTextureFromImage(yCache, frame.data[3], 1);
            uvTexRef = OSXTools.CreateCVTextureFromImage(uvCache, frame.data[3], 0);
            yMTLTex = OSXTools.GetMTLTexture(yTexRef);
            uvMTLTex = OSXTools.GetMTLTexture(uvTexRef);
            if (!isInit)
            {
                isInit = true;
                InitRenderer(frame);
                SetColorSpace(frame);
            }
            SourceTextures[0].UpdateExternalTexture(yMTLTex);
            SourceTextures[1].UpdateExternalTexture(uvMTLTex);
            RenderMaterial.SetTexture("_YTex", SourceTextures[0]);
            RenderMaterial.SetTexture("_UVTex", SourceTextures[1]);
            base.Apply();
    
            
        }

        private void ReleaseTexRef()
        {
            if (yMTLTex != System.IntPtr.Zero)
            {
                OSXTools.ReleaseCVTexture(yTexRef);
                OSXTools.ReleaseCVTexture(uvTexRef);
                yTexRef = uvTexRef = System.IntPtr.Zero;
            }
        }
        public override void TerminateRenderer()
        {
            base.TerminateRenderer();
            ReleaseTexRef();
            OSXTools.ReleaseCVMetalTextureCache(yCache);
            OSXTools.ReleaseCVMetalTextureCache(uvCache);
            yCache = uvCache = System.IntPtr.Zero;
        }
    }
}
