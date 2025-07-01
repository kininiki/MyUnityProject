using System.Threading;

namespace Sttplay.MediaPlayer
{
    public class NativeLoop
    {
        private const int SIGNAL_EXIT = 0x7fffffff;
        public const int SIGNAL_USER = 1024;
        private Semaphore playerSem, renderSem;
        private Mutex signalMux;
        private SCThreadHandle handle;
        private int signalValue;
        private object signalParam;
        private bool isExit = false;
        protected object RetValue { get; set; }
        public NativeLoop()
        {
            playerSem = new Semaphore(0, int.MaxValue);
            renderSem = new Semaphore(0, int.MaxValue);
            signalMux = new Mutex();
            handle = SCMGR.CreateThreadHandle(Run);
        }

        private void Run()
        {
            while (!isExit)
            {
                renderSem.WaitOne();
                if (signalValue == SIGNAL_EXIT)
                    isExit = true;
                else
                    Handle(signalValue, signalParam);
                playerSem.Release();
            }
        }

        protected virtual void Handle(int signal, object param) { }

        public object SendSignal(int signal, object param = null)
        {
            if (signalMux == null) return null;
            signalMux.WaitOne();
            signalValue = signal;
            signalParam = param;
            RetValue = 0;
            renderSem.Release();
            playerSem.WaitOne();
            object v = RetValue;
            signalMux.ReleaseMutex();
            return v;
        }

        public void Dispose()
        {
            SendSignal(SIGNAL_EXIT);
            SCMGR.ReleaseThreadHandle(handle);
            playerSem.Dispose();
            renderSem.Dispose();
            signalMux.Dispose();
        }
    }
}
