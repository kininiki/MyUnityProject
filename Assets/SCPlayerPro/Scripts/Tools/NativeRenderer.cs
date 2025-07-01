using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sttplay.MediaPlayer
{
    public class NativeRenderer : NativeLoop
    {
        public const int SIGNAL_CREATE = SIGNAL_USER + 1;
        public const int SIGNAL_DESTROY = SIGNAL_USER + 2;
        public const int SIGNAL_GET_SURFACE = SIGNAL_USER + 3;
        public const int SIGNAL_RESIZE = SIGNAL_USER + 4;
        public const int SIGNAL_UPDATE = SIGNAL_USER + 5;
        public const int SIGNAL_DRAW = SIGNAL_USER + 6;

        private IntPtr renderer;
        private IntPtr texture;
        protected override void Handle(int signal, object param)
        {
            if(signal == SIGNAL_CREATE)
            {
                renderer = XRendererEx.XRendererEx_Create(IntPtr.Zero, (IntPtr)param);
                texture = XRendererEx.XTextureEx_Create(renderer);
            }
            else if(signal == SIGNAL_DESTROY)
            {
                Release();
            }
            else if(signal == SIGNAL_GET_SURFACE)
            {
                RetValue = XRendererEx.XTextureEx_GetANativeWindow(texture);
            }
            else if(signal == SIGNAL_RESIZE)
            {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
                Release();
                renderer = XRendererEx.XRendererEx_Create(IntPtr.Zero, IntPtr.Zero);
                texture = XRendererEx.XTextureEx_Create(renderer);
#endif
                long size = (long)param;
                int width = (int)(size >> 32);
                int height = (int)size;
                XRendererEx.XRendererEx_Resize(renderer, width, height);
                IntPtr renderTarget = IntPtr.Zero;
                XRendererEx.XRendererEx_RenderTarget(renderer, ref renderTarget);
                float w = 0, h = 0;
                XRendererEx.XRendererEx_GetSceneViewportSize(renderer, ref w, ref h);
                XRendererEx.XRendererEx_SetScaling(renderer, w, h, 1);
                RetValue = renderTarget;
            }
            else if(signal == SIGNAL_UPDATE)
            {
                SCFrame frame = (SCFrame)param;
                RetValue = XRendererEx.XTextureEx_Update(texture, frame.width, frame.height, frame.format, frame.linesize, frame.data, frame.hwctx);
            }
            else if(signal == SIGNAL_DRAW)
            {
                XRendererEx.XRendererEx_Draw(renderer, texture);
                XRendererEx.XRendererEx_Present(renderer, 0);

            }
        }
        private void Release()
        {
            if(texture != IntPtr.Zero)
                XRendererEx.XTextureEx_Destroy(texture);
            texture = IntPtr.Zero;
            if(renderer != null)
                XRendererEx.XRendererEx_Destroy(renderer);
            renderer = IntPtr.Zero;
        }
    }
}
