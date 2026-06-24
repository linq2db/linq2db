using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

using NUnit.Framework;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Execution;

namespace NUnit.ParallelByResource
{
	// Custom NUnit work-item dispatcher that runs tests in parallel across shared resources while
	// serializing tests that contend for the same resource. An <see cref="IResourceLaneStrategy"/>
	// maps each leaf test to a resource key (its "lane"); tests on the same key never overlap, tests
	// on different keys run concurrently.
	//
	// NUnit has no built-in "parallelize by resource" capability (nunit/nunit#165); this swaps in
	// after assembly OneTimeSetUp, leaving NUnit's composite / completion / shift machinery to the
	// original dispatcher and routing only leaf cases to our own per-resource lanes. See
	// nunit/nunit#3122 for the original discussion.
	public sealed class ResourceLaneDispatcher : IWorkItemDispatcher
	{
		// Work the strategy doesn't bind to a resource (assembly / namespace / fixture composites and
		// any unkeyed tests) is forwarded here so NUnit's normal execution machinery stays intact.
		readonly IWorkItemDispatcher _original;

		readonly IResourceLaneStrategy _strategy;
		readonly IParallelDiagnostics  _diag;

		// All lanes coordinate through one gate so globally-exclusive ([NonParallelizable])
		// work runs alone: resource lanes take the read lock around each item, the exclusive
		// lane takes the write lock (which waits for every resource lane to go idle).
		readonly ReaderWriterLockSlim _gate = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

		// At most one item flagged RequiresSecondaryMutex runs at a time across all lanes. Hosts use
		// this for items that, beyond their own resource lane, also share one process-wide secondary
		// resource (e.g. a single in-process server) that can't tolerate concurrent use. A binary
		// SemaphoreSlim is the right primitive: it gates one permit across the different lane threads
		// without the thread-affinity / reentrancy semantics of Monitor / Lock.
		readonly SemaphoreSlim _secondaryMutex = new SemaphoreSlim(1, 1);

		// Caps how many resource lanes execute concurrently (excess lanes queue behind it). Bounds the
		// aggregate per-lane memory footprint (DataConnection + query / materialization caches) on small
		// CI agents — an uncapped lane count OOM-ed the multi-context legs. The exclusive lane is not
		// throttled (it already runs alone under the write lock).
		readonly SemaphoreSlim _laneThrottle;

		// True (per-thread) while a thread already holds the gate (read or write). A work item
		// can synchronously dispatch follow-up items on the same thread - a NonParallel fixture
		// running its inline children, or any leaf whose completion triggers its parent's
		// OneTimeTearDown - and this flag lets those nested dispatches skip re-entering the
		// non-recursive gate (which would throw LockRecursionException). Thread-local, not
		// async-local: the nested dispatch is synchronous on the same thread.
		static readonly ThreadLocal<bool> _gateHeld = new();

		readonly Lock                            _lanesLock     = new();
		readonly Dictionary<string, SerialLane>  _resourceLanes = new Dictionary<string, SerialLane>(StringComparer.OrdinalIgnoreCase);
		readonly SerialLane                      _exclusiveLane;

		public ResourceLaneDispatcher(IWorkItemDispatcher original, IResourceLaneStrategy strategy, int maxLanes, IParallelDiagnostics? diagnostics = null)
		{
			var lanes = Math.Max(1, maxLanes);

			_original      = original;
			_strategy      = strategy;
			_diag          = diagnostics ?? NullParallelDiagnostics.Instance;
			_laneThrottle  = new SemaphoreSlim(lanes, lanes);
			_exclusiveLane = new SerialLane("exclusive", _gate, _secondaryMutex, _laneThrottle, _diag, exclusive: true);
		}

		public int LevelOfParallelism => _original.LevelOfParallelism;

		public void Start(WorkItem topLevelWorkItem)
		{
			// Start is called once by the engine at the very beginning of the run, before we
			// swap in (during the original dispatcher's run of the assembly item). It must
			// never reach us.
			throw new InvalidOperationException($"{nameof(ResourceLaneDispatcher)} is installed after Start and must not receive it");
		}

		public void Dispatch(WorkItem work)
		{
			// Already inside the exclusive write lock (a [NonParallelizable] suite's subtree):
			// run the whole subtree inline on this thread so every descendant - including resource
			// leaves - stays under the write lock instead of escaping to a resource/read lane.
			if (_gateHeld.Value)
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
				_diag.Log($"dispatch->exclusive test={work.Test.Name}");
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

			var assignment = _strategy.Classify(work) ?? LaneAssignment.GatedInline();

			switch (assignment.Disposition)
			{
				// Resource leaf: route to the resource's serial lane (remote/secondary items also
				// take the secondary mutex via the RequiresSecondaryMutex flag).
				case LaneDisposition.SerialLane:
					GetResourceLane(assignment.ResourceKey!).Enqueue(work, assignment.RequiresSecondaryMutex);
					return;

				// Ungated: run immediately, off-lane and outside the gate. Used for resource
				// preparation (e.g. create/seed) that the resource's other items wait on via a
				// readiness latch; running it under the read lock would let a long-held exclusive
				// write lock (a slow [NonParallelizable] fixture running its whole subtree inline)
				// starve it, deadlocking the latch. The preparation touches only its own resource,
				// none of the shared global state the exclusive lane guards, so running it
				// concurrently is safe.
				case LaneDisposition.Ungated:
					_diag.Log($"dispatch->ungated key={assignment.ResourceKey} test={work.Test.Name}");
					work.Execute();
					return;

				// Unkeyed leaf, Direct / SingleThreaded content: run on the calling thread under the
				// read gate so it is excluded by the exclusive lane.
				default:
					RunGated(work);
					return;
			}
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
			if (_gateHeld.Value)
			{
				work.Execute();
				return;
			}

			_gate.EnterReadLock();
			_gateHeld.Value = true;
			try
			{
				work.Execute();
			}
			finally
			{
				_gateHeld.Value = false;
				_gate.ExitReadLock();
			}
		}

		public void CancelRun(bool force)
		{
			_original.CancelRun(force);

			lock (_lanesLock)
			{
				foreach (var lane in _resourceLanes.Values)
					lane.Complete();
			}

			_exclusiveLane.Complete();
		}

		SerialLane GetResourceLane(string key)
		{
			lock (_lanesLock)
			{
				if (!_resourceLanes.TryGetValue(key, out var lane))
				{
					lane = new SerialLane(key, _gate, _secondaryMutex, _laneThrottle, _diag, exclusive: false);
					_resourceLanes.Add(key, lane);
				}

				return lane;
			}
		}

		// A single dedicated thread that executes its queued work items one at a time. Items
		// run under the shared read/write gate: resource lanes (read) run concurrently with
		// each other, the exclusive lane (write) runs alone. Items flagged for the secondary
		// mutex on a resource lane additionally take it so only one runs globally at a time.
		sealed class SerialLane
		{
			readonly BlockingCollection<(WorkItem work, bool secondary)> _queue = new BlockingCollection<(WorkItem, bool)>();
			readonly ReaderWriterLockSlim                                _gate;
			readonly SemaphoreSlim                                       _secondaryMutex;
			readonly SemaphoreSlim                                       _laneThrottle;
			readonly IParallelDiagnostics                                _diag;
			readonly bool                                                _exclusive;

			public SerialLane(string name, ReaderWriterLockSlim gate, SemaphoreSlim secondaryMutex, SemaphoreSlim laneThrottle, IParallelDiagnostics diag, bool exclusive)
			{
				_gate           = gate;
				_secondaryMutex = secondaryMutex;
				_laneThrottle   = laneThrottle;
				_diag           = diag;
				_exclusive      = exclusive;

				var thread = new Thread(Run)
				{
					IsBackground = true,
					Name         = $"parallel-by-resource-lane:{name}",
				};

				thread.Start();
			}

			public void Enqueue(WorkItem work, bool secondary = false) => _queue.Add((work, secondary));

			public void Complete() => _queue.CompleteAdding();

			void Run()
			{
				foreach (var (work, secondary) in _queue.GetConsumingEnumerable())
				{
					// Acquire the global secondary mutex before taking the per-run gate, so a lane
					// waiting for its turn at a secondary-resource item doesn't pin a read lock meanwhile.
					var holdsSecondary = !_exclusive && secondary;
					if (holdsSecondary)
						_secondaryMutex.Wait();

					// Cap concurrent resource lanes (acquired after the secondary mutex, before the gate,
					// for the same reason the gate is taken last: a lane waiting its turn must not pin a
					// throttle permit while blocked on the secondary mutex). The exclusive lane is not
					// throttled — it already runs alone under the write lock.
					var holdsThrottle = !_exclusive;
					if (holdsThrottle)
						_laneThrottle.Wait();

					if (_exclusive)
						_gate.EnterWriteLock();
					else
						_gate.EnterReadLock();

					_gateHeld.Value = true;

					var diagSw = _exclusive ? System.Diagnostics.Stopwatch.StartNew() : null;
					if (_exclusive)
						_diag.Log($"exclusive-writelock-acquired test={work.Test.Name}");

					try
					{
						// WorkItem.Execute() runs the item synchronously to completion and
						// raises WorkItemComplete (handled-error paths included), which drives
						// the parent countdown and run termination.
						work.Execute();
					}
					finally
					{
						if (diagSw != null)
							_diag.Log($"exclusive-writelock-released test={work.Test.Name} heldMs={diagSw.ElapsedMilliseconds}");

						_gateHeld.Value = false;

						if (_exclusive)
							_gate.ExitWriteLock();
						else
							_gate.ExitReadLock();

						if (holdsThrottle)
							_laneThrottle.Release();

						if (holdsSecondary)
							_secondaryMutex.Release();
					}
				}
			}
		}
	}
}
