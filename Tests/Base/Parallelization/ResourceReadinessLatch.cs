using System;
using System.Collections.Concurrent;
using System.Threading;

namespace NUnit.ParallelByResource
{
	/// <summary>
	/// A per-key one-shot readiness gate. One item prepares a shared resource and calls
	/// <see cref="MarkReady"/>; every other item that needs the resource calls
	/// <see cref="WaitReady"/> until then. Pairs with <see cref="LaneDisposition.Ungated"/>: the
	/// preparing item runs off-lane so it can't be blocked by, or block, the lane whose other items
	/// wait here.
	/// </summary>
	/// <remarks>
	/// Coordination only - it does not run anything and is independent of the dispatcher; a host
	/// drives it from its test setup/teardown. <see cref="MarkReady"/> should be signalled even when
	/// preparation fails, so waiters don't hang.
	/// </remarks>
	public sealed class ResourceReadinessLatch
	{
		readonly ConcurrentDictionary<string, ManualResetEventSlim> _gates;

		public ResourceReadinessLatch(StringComparer comparer)
		{
			_gates = new ConcurrentDictionary<string, ManualResetEventSlim>(comparer);
		}

		public ResourceReadinessLatch() : this(StringComparer.OrdinalIgnoreCase)
		{
		}

		ManualResetEventSlim Gate(string key) => _gates.GetOrAdd(key, static _ => new ManualResetEventSlim(false));

		/// <summary>Signal that <paramref name="key"/>'s resource is ready (release all waiters).</summary>
		public void MarkReady(string key) => Gate(key).Set();

		/// <summary>
		/// Block until <paramref name="key"/>'s resource is ready or <paramref name="timeout"/> elapses.
		/// Returns <see langword="false"/> on timeout.
		/// </summary>
		public bool WaitReady(string key, TimeSpan timeout) => Gate(key).Wait(timeout);
	}
}
