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
    public class SCRendererD3D11VA : SCRenderer
    {
        public override void InitRenderer(SCFrame frame)
        {
			if (renderTarget == System.IntPtr.Zero) 
				return;
			if (hwRenderCtx == null)
				return;
			if(hwRenderCtx.released)
			{
				ISCNative.SCLog(LogLevel.Error, "hw render texture released");
			}
			if(renderTarget == System.IntPtr.Zero)
			{
				ISCNative.SCLog(LogLevel.Error, "renderTarget is null");
				return;
			}
			SourceTextures = new Texture2D[1];
			RenderMaterial = new Material(Shader.Find("Sttplay/D3D11VA"));
			SourceTextures[0] = Texture2D.CreateExternalTexture(frame.width, frame.height, SCMGR.IsVersionGreaterThan_2022() ? TextureFormat.BGRA32 : TextureFormat.RGBA32, false, false, renderTarget);
			base.InitRenderer(frame);
        }

        public override void Renderer(SCFrame frame)
        {

			this.frame = frame;
		}

        public override void Apply()
        {
			if (!isInit)
			{
				isInit = true;
				InitRenderer(frame);
				SetColorSpace(frame);
			}
			if (SourceTextures == null) return;
			RenderMaterial.SetTexture("_Tex", SourceTextures[0]);
			base.Apply();
        }

        public override void TerminateRenderer()
        {
            base.TerminateRenderer();
            if (SourceTextures == null) return;
			Object.DestroyImmediate(SourceTextures[0]);
			SourceTextures[0] = null;
		}
    }
}
