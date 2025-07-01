﻿//#define USING_TASK
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sttplay.MediaPlayer
{
	/// <summary>
	/// All function calls should be in the same thread
	/// </summary>

	public class SCThreadHandle
	{
#if USING_TASK
        public Task task;
#else
		public Thread thread;
#endif
		private Semaphore playerSem, renderSem;
		private bool isRun = false;
		private bool isExit = false;
		public bool isFree = true;
		public Action<SCThreadHandle> beginAction, endAction;
		public Action runAction;
		public SCThreadHandle()
		{
			isFree = true;
			playerSem = new Semaphore(0, int.MaxValue);
			renderSem = new Semaphore(0, int.MaxValue);
#if USING_TASK
            task = Task.Run(Run);
#else
			thread = new Thread(Run);
			thread.IsBackground = true;
#endif
		}

		public void Start()
		{
			Stop();
			isRun = true;
			renderSem.Release();
		}

		public void Stop()
		{
			if (isRun)
				playerSem.WaitOne();
			isRun = false;
		}


		private void Run()
		{
			if (beginAction != null)
				beginAction(this);
			while (!isExit)
			{
				renderSem.WaitOne();
				if (isExit)
					break;
				if (runAction != null)
					runAction();
				playerSem.Release();
			}
			if (endAction != null)
				endAction(this);
		}

		public void DisposeInternal()
		{
			isExit = true;
			renderSem.Release();
#if USING_TASK
            task.Wait();
#else
			thread.Join();
			thread = null;
#endif
			playerSem.Dispose();
			renderSem.Dispose();
		}
	}
	public class SCThreadManager
	{
		private Action<SCThreadHandle> beginAction, endAction;
		private List<SCThreadHandle> threadManagers = new List<SCThreadHandle>();
		public static SCThreadManager CreateThreadManager(Action<SCThreadHandle> begin = null, Action<SCThreadHandle> end = null)
		{
			SCThreadManager mgr = new SCThreadManager();
			mgr.beginAction = begin;
			mgr.endAction = end;
			return mgr;
		}


		public SCThreadHandle CreateThreadHandle(Action action)
		{
			bool needCreate = true;
			SCThreadHandle handle = null;
			for (int i = 0; i < threadManagers.Count; i++)
			{
				if (threadManagers[i].isFree)
				{
					handle = threadManagers[i];
					handle.isFree = false;
					needCreate = false;
					break;
				}
			}
			if (needCreate)
			{
				handle = new SCThreadHandle();
				handle.isFree = false;
				handle.beginAction = beginAction;
				handle.endAction = endAction;
#if !USING_TASK
				handle.thread.Start();
#endif
				threadManagers.Add(handle);
			}
			handle.runAction = action;
			handle.Start();
			return handle;
		}

		public void FreeThreadHandle(SCThreadHandle handle)
		{
			handle.Stop();
			handle.isFree = true;
		}

		public void Dispose()
		{
			foreach (var item in threadManagers)
			{
				item.DisposeInternal();
			}
			threadManagers.Clear();
		}
	}
}
