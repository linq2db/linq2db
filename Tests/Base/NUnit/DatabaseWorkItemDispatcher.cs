#if !NETSTANDARD1_6
using NUnit.Framework.Internal.Execution;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
	internal class DatabaseWorkItemDispatcher : IWorkItemDispatcher
	{
		private readonly Thread _runnerThread;
		private readonly AutoResetEvent _hasItems = new AutoResetEvent(false);
		private readonly ConcurrentQueue<WorkItem> _workItems = new ConcurrentQueue<WorkItem>();

		int IWorkItemDispatcher.LevelOfParallelism => 0;

		public DatabaseWorkItemDispatcher()
		{
			_runnerThread = new Thread(RunnerThreadProc);
			_runnerThread.IsBackground = true;
			_runnerThread.Start();
		}

		private void RunnerThreadProc()
		{
			// TODO: rewrite
			while (true)
			{
				_hasItems.WaitOne(1000);
				if (_workItems.TryDequeue(out var workItem))
				{
					workItem.Execute();
					_hasItems.Set();
				}
			}
		}

		void IWorkItemDispatcher.Start(WorkItem topLevelWorkItem)
		{
			throw new InvalidOperationException("Shouldn't be called");
		}

		void IWorkItemDispatcher.Dispatch(WorkItem work)
		{
			_workItems.Enqueue(work);
			_hasItems.Set();
		}

		void IWorkItemDispatcher.CancelRun(bool force)
		{
			// TODO: implement
		}
	}
}
#endif
