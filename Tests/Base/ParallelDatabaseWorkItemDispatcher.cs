using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Execution;

namespace Tests
{
	// Custom work-item dispatcher that runs tests in parallel across database providers
	// while serializing tests that target the same provider (they share one physical
	// database). A provider's direct and remote (LinqService) variants map to the same
	// lane (NUnitUtils.GetContext strips the remote suffix), so they never overlap.
	//
	// NUnit has no built-in "parallelize by resource" capability (nunit/nunit#165); this
	// swaps in after assembly OneTimeSetUp, leaving NUnit's composite / completion / shift
	// machinery to the original dispatcher and routing only leaf provider cases to our own
	// per-provider lanes. See nunit/nunit#3122 for the original discussion.
	internal sealed class ParallelDatabaseWorkItemDispatcher : IWorkItemDispatcher
	{
		// Non-provider work (assembly / namespace / fixture composites and any provider-less
		// tests) is forwarded here so NUnit's normal execution machinery stays intact.
		readonly IWorkItemDispatcher _original;

		// All lanes coordinate through one gate so globally-exclusive ([NonParallelizable])
		// work runs alone: provider lanes take the read lock around each item, the exclusive
		// lane takes the write lock (which waits for every provider lane to go idle).
		readonly ReaderWriterLockSlim _gate = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

		readonly object                          _lanesLock    = new object();
		readonly Dictionary<string, SerialLane>  _providerLanes = new Dictionary<string, SerialLane>(StringComparer.OrdinalIgnoreCase);
		readonly SerialLane                      _exclusiveLane;

		public ParallelDatabaseWorkItemDispatcher(IWorkItemDispatcher original)
		{
			_original      = original;
			_exclusiveLane = new SerialLane("exclusive", _gate, exclusive: true);
		}

		public int LevelOfParallelism => _original.LevelOfParallelism;

		public void Start(WorkItem topLevelWorkItem)
		{
			// Start is called once by the engine at the very beginning of the run, before we
			// swap in (during the original dispatcher's run of the assembly item). It must
			// never reach us.
			throw new InvalidOperationException($"{nameof(ParallelDatabaseWorkItemDispatcher)} is installed after Start and must not receive it");
		}

		public void Dispatch(WorkItem work)
		{
			var strategy = work.ExecutionStrategy;

			// Items NUnit runs inline (provider-less leaf cases when not parallelized,
			// grouping suites, [SingleThreaded] content) execute on the calling thread,
			// mirroring ParallelWorkItemDispatcher.
			if (work.Context.IsSingleThreaded || strategy == ParallelExecutionStrategy.Direct)
			{
				work.Execute();
				return;
			}

			// [NonParallelizable] work must run with no provider lane active.
			if (strategy == ParallelExecutionStrategy.NonParallel)
			{
				_exclusiveLane.Enqueue(work);
				return;
			}

			// Parallel: route DB leaf cases by provider; everything else (composites,
			// provider-less tests) goes to the original dispatcher.
			var (context, _) = NUnitUtils.GetContext(work.Test);

			if (context == null)
			{
				_original.Dispatch(work);
				return;
			}

			GetProviderLane(context).Enqueue(work);
		}

		public void CancelRun(bool force)
		{
			_original.CancelRun(force);

			lock (_lanesLock)
			{
				foreach (var lane in _providerLanes.Values)
					lane.Complete();
			}

			_exclusiveLane.Complete();
		}

		SerialLane GetProviderLane(string context)
		{
			lock (_lanesLock)
			{
				if (!_providerLanes.TryGetValue(context, out var lane))
				{
					lane = new SerialLane(context, _gate, exclusive: false);
					_providerLanes.Add(context, lane);
				}

				return lane;
			}
		}

		// A single dedicated thread that executes its queued work items one at a time. Items
		// run under the shared read/write gate: provider lanes (read) run concurrently with
		// each other, the exclusive lane (write) runs alone.
		sealed class SerialLane
		{
			readonly BlockingCollection<WorkItem> _queue = new BlockingCollection<WorkItem>();
			readonly ReaderWriterLockSlim         _gate;
			readonly bool                         _exclusive;

			public SerialLane(string name, ReaderWriterLockSlim gate, bool exclusive)
			{
				_gate      = gate;
				_exclusive = exclusive;

				var thread = new Thread(Run)
				{
					IsBackground = true,
					Name         = $"l2db-test-lane:{name}",
				};

				thread.Start();
			}

			public void Enqueue(WorkItem work) => _queue.Add(work);

			public void Complete() => _queue.CompleteAdding();

			void Run()
			{
				foreach (var work in _queue.GetConsumingEnumerable())
				{
					if (_exclusive)
						_gate.EnterWriteLock();
					else
						_gate.EnterReadLock();

					try
					{
						// WorkItem.Execute() runs the item synchronously to completion and
						// raises WorkItemComplete (handled-error paths included), which drives
						// the parent countdown and run termination.
						work.Execute();
					}
					finally
					{
						if (_exclusive)
							_gate.ExitWriteLock();
						else
							_gate.ExitReadLock();
					}
				}
			}
		}
	}
}
