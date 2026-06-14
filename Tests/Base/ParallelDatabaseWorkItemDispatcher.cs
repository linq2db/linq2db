using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

using NUnit.Framework;
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
	public sealed class ParallelDatabaseWorkItemDispatcher : IWorkItemDispatcher
	{
		// Non-provider work (assembly / namespace / fixture composites and any provider-less
		// tests) is forwarded here so NUnit's normal execution machinery stays intact.
		readonly IWorkItemDispatcher _original;

		// All lanes coordinate through one gate so globally-exclusive ([NonParallelizable])
		// work runs alone: provider lanes take the read lock around each item, the exclusive
		// lane takes the write lock (which waits for every provider lane to go idle).
		readonly ReaderWriterLockSlim _gate = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

		// Only one remote (LinqService) test runs at a time across all lanes. They share one
		// in-process server whose worker threads resolve the originating test through a single
		// register (CustomTestContext._remote, unreachable by the test's AsyncLocal), so
		// concurrent remote tests on different providers would corrupt each other's capture.
		readonly object _remoteGate = new object();

		// True (per-thread) while a thread already holds the gate (read or write). A work item
		// can synchronously dispatch follow-up items on the same thread - a NonParallel fixture
		// running its inline Direct children, or any leaf whose completion triggers its parent's
		// OneTimeTearDown - and this flag lets those nested dispatches skip re-entering the
		// non-recursive gate (which would throw LockRecursionException).
		[ThreadStatic]
		static bool _gateHeld;

		readonly object                          _lanesLock    = new object();
		readonly Dictionary<string, SerialLane>  _providerLanes = new Dictionary<string, SerialLane>(StringComparer.OrdinalIgnoreCase);
		readonly SerialLane                      _exclusiveLane;

		public ParallelDatabaseWorkItemDispatcher(IWorkItemDispatcher original)
		{
			_original      = original;
			_exclusiveLane = new SerialLane("exclusive", _gate, _remoteGate, exclusive: true);
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
			// Already inside the exclusive write lock (a [NonParallelizable] suite's subtree):
			// run the whole subtree inline on this thread so every descendant - including provider
			// leaves - stays under the write lock instead of escaping to a provider/read lane.
			if (_gateHeld)
			{
				work.Execute();
				return;
			}

			// [NonParallelizable] work runs on the globally-exclusive lane. Detected via the
			// ParallelScope.None property rather than ExecutionStrategy: a method-level mark
			// yields strategy Direct (the work item's TypeInfo is null, so NUnit returns Direct
			// before testing the None flag), yet the suite still carries the None scope.
			if (IsNonParallel(work))
			{
				_exclusiveLane.Enqueue(work);
				return;
			}

			// Composites (assembly / namespace / fixture suites) run no test body of their own;
			// they only dispatch children (which come back to us individually), so they go to the
			// original dispatcher, keeping NUnit's completion / shift machinery intact.
			if (work is CompositeWorkItem)
			{
				_original.Dispatch(work);
				return;
			}

			var (context, isRemote) = NUnitUtils.GetContext(work.Test);

			// Provider leaf cases go to the provider lane, except CreateDatabase, which must stay
			// off the lane so a provider's other tests can wait on its readiness latch without
			// deadlocking the single-thread lane.
			if (context != null && !NUnitUtils.IsCreateDatabase(work.Test))
			{
				GetProviderLane(context).Enqueue(work, isRemote);
				return;
			}

			// Provider-less leaf, CreateDatabase, Direct / SingleThreaded content: run on the
			// calling thread under the read gate so it is excluded by the exclusive lane.
			RunGated(work);
		}

		// True for [NonParallelizable] work. Checked via the ParallelScope.None property because a
		// method-level mark produces ExecutionStrategy.Direct (the work item's TypeInfo is null,
		// short-circuiting NUnit's strategy computation before the None flag is examined).
		static bool IsNonParallel(WorkItem work)
		{
			if (work.ExecutionStrategy == ParallelExecutionStrategy.NonParallel)
				return true;

			return work.Test.Properties.Get(PropertyNames.ParallelScope) is ParallelScope scope
				&& (scope & ParallelScope.None) != 0;
		}

		// Runs a leaf body under the read gate on the current thread, unless we are already inside
		// the exclusive write lock (a NonParallel fixture's inline children), in which case the
		// body is already covered and re-entering the non-recursive gate would throw.
		void RunGated(WorkItem work)
		{
			if (_gateHeld)
			{
				work.Execute();
				return;
			}

			_gate.EnterReadLock();
			_gateHeld = true;
			try
			{
				work.Execute();
			}
			finally
			{
				_gateHeld = false;
				_gate.ExitReadLock();
			}
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
					lane = new SerialLane(context, _gate, _remoteGate, exclusive: false);
					_providerLanes.Add(context, lane);
				}

				return lane;
			}
		}

		// A single dedicated thread that executes its queued work items one at a time. Items
		// run under the shared read/write gate: provider lanes (read) run concurrently with
		// each other, the exclusive lane (write) runs alone. Remote (LinqService) items on a
		// provider lane additionally take the remote gate so only one runs globally at a time.
		sealed class SerialLane
		{
			readonly BlockingCollection<(WorkItem work, bool isRemote)> _queue = new BlockingCollection<(WorkItem, bool)>();
			readonly ReaderWriterLockSlim                              _gate;
			readonly object                                            _remoteGate;
			readonly bool                                              _exclusive;

			public SerialLane(string name, ReaderWriterLockSlim gate, object remoteGate, bool exclusive)
			{
				_gate       = gate;
				_remoteGate = remoteGate;
				_exclusive  = exclusive;

				var thread = new Thread(Run)
				{
					IsBackground = true,
					Name         = $"l2db-test-lane:{name}",
				};

				thread.Start();
			}

			public void Enqueue(WorkItem work, bool isRemote = false) => _queue.Add((work, isRemote));

			public void Complete() => _queue.CompleteAdding();

			void Run()
			{
				foreach (var (work, isRemote) in _queue.GetConsumingEnumerable())
				{
					// Serialize remote tests globally before taking the per-run gate, so a lane
					// waiting for its turn at a remote test doesn't pin a read lock meanwhile.
					var holdsRemoteGate = !_exclusive && isRemote;
					if (holdsRemoteGate)
						Monitor.Enter(_remoteGate);

					if (_exclusive)
						_gate.EnterWriteLock();
					else
						_gate.EnterReadLock();

					_gateHeld = true;

					try
					{
						// WorkItem.Execute() runs the item synchronously to completion and
						// raises WorkItemComplete (handled-error paths included), which drives
						// the parent countdown and run termination.
						work.Execute();
					}
					finally
					{
						_gateHeld = false;

						if (_exclusive)
							_gate.ExitWriteLock();
						else
							_gate.ExitReadLock();

						if (holdsRemoteGate)
							Monitor.Exit(_remoteGate);
					}
				}
			}
		}
	}
}
