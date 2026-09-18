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
		//
		// The host chooses the value. Note the constraint this bounds is memory while the default the host
		// currently derives is a CPU count, so on a many-core, modest-memory agent the cap is loosest
		// exactly where it binds hardest - that default is a proxy that CI has shown to work, not a
		// measurement. A host on such an agent should set it explicitly rather than take the default.
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
		// Separate keyspace from _resourceLanes: a resource's preparation and its ordinary tests share a
		// key but must run on different threads, or the preparation queues behind the very items waiting
		// on it.
		readonly Dictionary<string, SerialLane>  _ungatedLanes  = new Dictionary<string, SerialLane>(StringComparer.OrdinalIgnoreCase);
		readonly SerialLane                      _exclusiveLane;

		public ResourceLaneDispatcher(IWorkItemDispatcher original, IResourceLaneStrategy strategy, int maxLanes, IParallelDiagnostics? diagnostics = null)
		{
			var lanes = Math.Max(1, maxLanes);

			_original      = original;
			_strategy      = strategy;
			_diag          = diagnostics ?? NullParallelDiagnostics.Instance;
			_laneThrottle  = new SemaphoreSlim(lanes, lanes);
			_exclusiveLane = new SerialLane("exclusive", _gate, _secondaryMutex, _laneThrottle, _diag, LaneGating.Write);
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

			var assignment = _strategy.Classify(work.Test) ?? LaneAssignment.GatedInline();

			switch (assignment.Disposition)
			{
				// Resource leaf: route to the resource's serial lane (remote/secondary items also
				// take the secondary mutex via the RequiresSecondaryMutex flag).
				case LaneDisposition.SerialLane:
					GetResourceLane(assignment.ResourceKey!).Enqueue(work, assignment.RequiresSecondaryMutex);
					return;

				// Ungated: run on the key's own dedicated thread, taking neither the gate nor a throttle
				// permit. Used for resource preparation (e.g. create/seed) that the resource's other
				// items wait on via a readiness latch. Two things have to be true for that latch not to
				// deadlock, and they need different mechanisms:
				//
				//  - it must not take the read lock, or a long-held exclusive write lock (a slow
				//    [NonParallelizable] fixture running its whole subtree inline) would starve it, and
				//  - it must not occupy an NUnit worker either. Running it inline on the dispatching
				//    worker satisfied the first but not the second: every worker can be parked in
				//    RunGated behind a waiting writer, leaving nobody to reach this branch.
				//
				// Its own thread also lets each resource's preparation overlap the others, instead of
				// CompositeWorkItem.RunChildren driving them one at a time on a single worker - which is
				// what made the waiters' timeout budget cumulative across every provider in the run.
				// The preparation touches only its own resource, none of the shared global state the
				// exclusive lane guards, so running it concurrently is safe.
				case LaneDisposition.Ungated:
					_diag.Log($"dispatch->ungated key={assignment.ResourceKey} test={work.Test.Name}");
					GetUngatedLane(assignment.ResourceKey!).Enqueue(work);
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
				&& scope.HasFlag(ParallelScope.None);
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

			CompleteLanes();
		}

		/// <summary>
		/// Ends every lane and waits briefly for its thread to exit, reporting any that does not. Call once
		/// the run is over (assembly teardown).
		/// </summary>
		/// <remarks>
		/// Without this a normal run never completes a lane - only <see cref="CancelRun"/> did - so every
		/// lane thread stayed parked until the process exited and a lane still stuck on an item was
		/// invisible: the run simply hung. The join turns that into a named lane in the log. Lane threads
		/// are background threads, so a lane that never exits cannot hold up the process either way.
		/// </remarks>
		/// <param name="report">Receives one message per lane that did not exit within the timeout.</param>
		/// <param name="timeout">How long to wait for each lane thread. Short by design - this is a
		/// diagnostic, not a barrier.</param>
		public void Shutdown(Action<string> report, TimeSpan timeout)
		{
			var lanes = CompleteLanes();

			foreach (var lane in lanes)
			{
				// Never join the lane we are running on. A work item's completion cascades its parents'
				// teardowns onto the completing thread, so the host's assembly teardown - and therefore this
				// call - runs on whichever lane executed the last item. Joining it can only ever burn the
				// whole timeout and then report the caller as stuck.
				if (lane.IsCurrentThread)
					continue;

				if (!lane.Join(timeout))
					report($"[parallel] lane '{lane.Name}' did not finish within {timeout.TotalSeconds:0.#}s - it is still running or stuck on '{lane.CurrentTestName ?? "(none)"}'");
			}
		}

		List<SerialLane> CompleteLanes()
		{
			var lanes = new List<SerialLane>();

			lock (_lanesLock)
			{
				lanes.AddRange(_resourceLanes.Values);
				lanes.AddRange(_ungatedLanes.Values);
			}

			lanes.Add(_exclusiveLane);

			foreach (var lane in lanes)
				lane.Complete();

			return lanes;
		}

		SerialLane GetResourceLane(string key) => GetLane(_resourceLanes, key, LaneGating.Read, key);

		SerialLane GetUngatedLane(string key) => GetLane(_ungatedLanes, key, LaneGating.None, $"ungated:{key}");

		SerialLane GetLane(Dictionary<string, SerialLane> lanes, string key, LaneGating gating, string name)
		{
			lock (_lanesLock)
			{
				if (!lanes.TryGetValue(key, out var lane))
				{
					lane = new SerialLane(name, _gate, _secondaryMutex, _laneThrottle, _diag, gating);
					lanes.Add(key, lane);
				}

				return lane;
			}
		}

		// How a lane's items relate to the shared gate.
		enum LaneGating
		{
			// Resource lane: takes the read lock, so it runs concurrently with the other resource lanes
			// and is excluded by the exclusive lane. Throttled, and honours the secondary mutex.
			Read,
			// Globally-exclusive lane: takes the write lock, so it runs alone. Not throttled - it already
			// runs alone - and the secondary mutex is redundant for the same reason.
			Write,
			// Ungated: no lock, no throttle permit. For resource preparation that other items wait on via
			// a readiness latch, which must be reachable while the write lock is held and must not depend
			// on an NUnit worker being free.
			None,
		}

		// A single dedicated thread that executes its queued work items one at a time, under the gating
		// its LaneGating prescribes. Items flagged for the secondary mutex on a resource lane
		// additionally take it so only one runs globally at a time.
		sealed class SerialLane
		{
			readonly BlockingCollection<(WorkItem work, bool secondary)> _queue = new BlockingCollection<(WorkItem, bool)>();
			readonly ReaderWriterLockSlim                                _gate;
			readonly SemaphoreSlim                                       _secondaryMutex;
			readonly SemaphoreSlim                                       _laneThrottle;
			readonly IParallelDiagnostics                                _diag;
			readonly LaneGating                                          _gating;
			readonly string                                              _name;
			readonly Thread                                              _thread;

			// Name of the item the lane is running, for the stuck-lane report. Written by the lane thread
			// and read by whoever calls Shutdown, so it is deliberately just a torn-read-safe reference.
			volatile string?                                             _currentTestName;

			public string  Name            => _name;
			public string? CurrentTestName => _currentTestName;

			// True when the caller is running on this lane's own thread. Reachable: a work item's completion
			// cascades its parents' teardowns onto the completing thread, so assembly teardown - and thus
			// Shutdown - runs on whichever lane ran the last item.
			public bool IsCurrentThread => _thread == Thread.CurrentThread;

			public bool Join(TimeSpan timeout) => _thread.Join(timeout);

			public SerialLane(string name, ReaderWriterLockSlim gate, SemaphoreSlim secondaryMutex, SemaphoreSlim laneThrottle, IParallelDiagnostics diag, LaneGating gating)
			{
				_gate           = gate;
				_secondaryMutex = secondaryMutex;
				_laneThrottle   = laneThrottle;
				_diag           = diag;
				_gating         = gating;
				_name           = name;

				_thread = new Thread(Run)
				{
					IsBackground = true,
					Name         = $"parallel-by-resource-lane:{name}",
				};

				_thread.Start();
			}

			public void Enqueue(WorkItem work, bool secondary = false)
			{
				// Complete() has been called (CancelRun), so nothing will consume this item and Add would
				// throw on an NUnit worker. Run it inline instead: the run is ending, and a work item that
				// never executes never raises WorkItemComplete, which would hang the parent countdown
				// rather than let the cancellation finish.
				if (!_queue.TryAdd((work, secondary)))
					work.Execute();
			}

			public void Complete() => _queue.CompleteAdding();

			void Run()
			{
				foreach (var (work, secondary) in _queue.GetConsumingEnumerable())
				{
					// An exception escaping the body would end this loop and kill the lane thread. Nothing
					// would report it - the thread is a background thread - and the queue would keep
					// accepting items nobody consumes, so their WorkItemComplete never fires and the run
					// hangs on a child countdown that cannot reach zero. Contain it per item instead.
					_currentTestName = work.Test.Name;

					try
					{
						RunItem(work, secondary);
					}
					catch (Exception ex)
					{
						_diag.Log($"lane-item-failed test={work.Test.Name} error={ex}");
						TestContext.Progress.WriteLine($"[parallel] lane '{_name}' failed to run {work.Test.Name}, continuing: {ex}");
					}
					finally
					{
						_currentTestName = null;
					}
				}
			}

			void RunItem(WorkItem work, bool secondary)
			{
				// Acquire the global secondary mutex before taking the per-run gate, so a lane
				// waiting for its turn at a secondary-resource item doesn't pin a read lock meanwhile.
				var holdsSecondary = _gating == LaneGating.Read && secondary;
				if (holdsSecondary)
					_secondaryMutex.Wait();

				// Cap concurrent resource lanes (acquired after the secondary mutex, before the gate,
				// for the same reason the gate is taken last: a lane waiting its turn must not pin a
				// throttle permit while blocked on the secondary mutex). The exclusive lane is not
				// throttled — it already runs alone under the write lock — and an ungated lane must
				// not be, or it could not run while the throttle is saturated.
				var holdsThrottle = _gating == LaneGating.Read;
				if (holdsThrottle)
					_laneThrottle.Wait();

				if (_gating == LaneGating.Write)
					_gate.EnterWriteLock();
				else if (_gating == LaneGating.Read)
					_gate.EnterReadLock();

				// Left false on an ungated lane: it holds no lock, so a nested dispatch from its item
				// must go through the normal routing rather than being run inline as already-covered.
				if (_gating != LaneGating.None)
					_gateHeld.Value = true;

				var exclusive = _gating == LaneGating.Write;
				var diagSw    = exclusive ? System.Diagnostics.Stopwatch.StartNew() : null;
				if (exclusive)
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

					if (_gating != LaneGating.None)
						_gateHeld.Value = false;

					if (_gating == LaneGating.Write)
						_gate.ExitWriteLock();
					else if (_gating == LaneGating.Read)
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
