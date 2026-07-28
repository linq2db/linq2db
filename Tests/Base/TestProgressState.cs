using System;
using System.Collections.Generic;
using System.Threading;

using NUnit.Framework.Interfaces;

namespace Tests
{
	/// <summary>
	/// The accounting behind the <c>--test-progress</c> heartbeat: run counters, the bounded recent-failure list and
	/// the deferred-commit protocol used by result-rewriting command wrappers. Deliberately free of file IO, static
	/// state and command-line dependencies so it can be exercised directly by tests;
	/// <see cref="TestProgressTracker"/> owns a single instance and adds the publishing side.
	/// <para>
	/// <b>Deferred commit.</b> An <c>IWrapSetUpTearDown</c> wrapper such as <see cref="ThrowsWhenAttribute"/> rewrites
	/// the test result <em>after</em> the reporter action — which NUnit nests inside it — has already sampled the
	/// outcome. Such a wrapper calls <see cref="BeginDeferred"/> before running the inner command, which makes
	/// <see cref="CompleteTest"/> hold the sampled outcome back instead of booking it; <see cref="CommitDeferred"/>
	/// then books the unit exactly once, with the final verdict. No provisional outcome is ever counted, added to the
	/// recent-failure list, or published — so each unit is booked exactly once and the bounded list only ever holds
	/// real failures.
	/// </para>
	/// <para>
	/// The protocol is self-correcting if NUnit's command ordering ever changes: when <see cref="CompleteTest"/> finds
	/// no deferral in flight (the reporter ran <em>outside</em> the wrapper, so it already sampled the final verdict)
	/// it books the unit immediately, and the wrapper's <see cref="CommitDeferred"/> becomes a no-op.
	/// </para>
	/// </summary>
	public sealed class TestProgressState
	{
		/// <summary>Upper bound on the failures retained for the heartbeat's <c>recentFailures</c> list.</summary>
		public const int MaxRecentFailures = 20;

		const int MaxMessageLength = 500;

		readonly Lock                                _sync     = new();
		readonly List<(string Test, string Message)> _failures = new();
		readonly Dictionary<string, DeferredUnit>    _deferred = new(StringComparer.Ordinal);
		readonly Action<bool>?                       _publish;

		long    _total;
		long    _started;
		long    _completed;
		long    _passed;
		long    _failed;
		long    _skipped;
		string? _current;
		bool    _done;

		/// <param name="publish">
		/// Invoked on every state change, while the state lock is held — so a reader that pulls the counters from
		/// inside the callback sees a consistent snapshot. The argument is <see langword="true"/> when the change
		/// should bypass write throttling. Pass <see langword="null"/> (the default) for a state that only accumulates.
		/// </param>
		public TestProgressState(Action<bool>? publish = null)
		{
			_publish = publish;
		}

		/// <summary>Discovered test-case count of the run (the largest suite count wins).</summary>
		public long Total     { get { lock (_sync) return _total;     } }
		/// <summary>Tests that have entered execution.</summary>
		public long Started   { get { lock (_sync) return _started;   } }
		/// <summary>Tests booked with a final verdict.</summary>
		public long Completed { get { lock (_sync) return _completed; } }
		/// <summary>Tests booked as passed.</summary>
		public long Passed    { get { lock (_sync) return _passed;    } }
		/// <summary>Tests booked as failed.</summary>
		public long Failed    { get { lock (_sync) return _failed;    } }
		/// <summary>Tests booked as skipped.</summary>
		public long Skipped   { get { lock (_sync) return _skipped;   } }
		/// <summary>Most recently started test, or <see langword="null"/> once the run is done.</summary>
		public string? Current { get { lock (_sync) return _current;  } }
		/// <summary>Whether the run has finished.</summary>
		public bool Done      { get { lock (_sync) return _done;      } }
		/// <summary>Number of units held back awaiting a deferred commit.</summary>
		public int  Pending   { get { lock (_sync) return _deferred.Count; } }

		/// <summary>Snapshot of the bounded recent-failure list, oldest first.</summary>
		public IReadOnlyList<(string Test, string Message)> RecentFailures
		{
			get { lock (_sync) return _failures.ToArray(); }
		}

		/// <summary>Raises the run total to <paramref name="testCaseCount"/> if it is larger than the current one.</summary>
		public void SetTotal(long testCaseCount)
		{
			lock (_sync)
			{
				if (testCaseCount > _total)
					_total = testCaseCount;
			}
		}

		/// <summary>Records that <paramref name="fullName"/> has started executing.</summary>
		public void StartTest(string fullName)
		{
			lock (_sync)
			{
				_started++;
				_current = fullName;

				Publish(force: false);
			}
		}

		/// <summary>
		/// Opens a deferral for <paramref name="fullName"/>: until the matching <see cref="CommitDeferred"/> arrives,
		/// <see cref="CompleteTest"/> only samples the outcome instead of booking it. Nests — several wrappers on one
		/// test each open (and close) their own level, and only the outermost one, which sees the final verdict, books.
		/// </summary>
		public void BeginDeferred(string fullName)
		{
			lock (_sync)
			{
				if (!_deferred.TryGetValue(fullName, out var unit))
					_deferred[fullName] = unit = new DeferredUnit();

				unit.Depth++;
			}
		}

		/// <summary>
		/// Books the final outcome of <paramref name="fullName"/> — unless a deferral is in flight, in which case the
		/// outcome is only sampled and <see cref="CommitDeferred"/> books it.
		/// </summary>
		public void CompleteTest(string fullName, TestStatus status, string? message)
		{
			lock (_sync)
			{
				if (_deferred.TryGetValue(fullName, out var unit))
				{
					unit.Sampled = true;
					unit.Status  = status;
					unit.Message = message;

					return;
				}

				Book(fullName, status, message);
			}
		}

		/// <summary>
		/// Closes one deferral level for <paramref name="fullName"/>. The outermost level books the unit with the
		/// verdict passed here — the wrapper's final one, after any rewrite. A no-op when no deferral is in flight, or
		/// when the reporter never sampled the test (an exception escaping around it), so nothing is booked twice.
		/// </summary>
		public void CommitDeferred(string fullName, TestStatus status, string? message)
		{
			lock (_sync)
			{
				if (!_deferred.TryGetValue(fullName, out var unit))
					return;

				if (--unit.Depth > 0)
					return;

				_deferred.Remove(fullName);

				if (unit.Sampled)
					Book(fullName, status, message);
			}
		}

		/// <summary>
		/// Marks the run finished, first booking any unit still held back — a wrapper that never committed (an
		/// exception escaping between the sample and the commit) must not silently drop its unit from the tally.
		/// </summary>
		public void MarkDone()
		{
			lock (_sync)
			{
				if (_deferred.Count > 0)
				{
					foreach (var pair in _deferred)
					{
						if (pair.Value.Sampled)
							Book(pair.Key, pair.Value.Status, pair.Value.Message);
					}

					_deferred.Clear();
				}

				if (_done)
					return;

				_done    = true;
				_current = null;

				Publish(force: true);
			}
		}

		void Book(string fullName, TestStatus status, string? message)
		{
			_completed++;
			// Deliberately do NOT clear _current here: with throttled writes, nulling between every test
			// makes the on-disk snapshot almost always catch the gap (showing no current test). Keep the
			// most-recently-started test as "current" until the run completes — during a run that is the
			// in-flight (or just-finished) test, which is what a watcher wants to see.

			var force = false;

			switch (status)
			{
				case TestStatus.Passed : _passed++;  break;
				case TestStatus.Skipped: _skipped++; break;
				case TestStatus.Failed :
					_failed++;
					force = true;
					if (_failures.Count < MaxRecentFailures)
						_failures.Add((fullName, Trim(message)));
					break;
				// Inconclusive / Warning — count as completed but neither pass nor fail bucket.
				default: break;
			}

			// For unfiltered runs total is exact, so completed reaching it marks the run done even if the
			// root-suite teardown ordering surprises us.
			if (!_done && _total > 0 && _completed >= _total)
			{
				_done    = true;
				_current = null;
				force    = true;
			}

			Publish(force);
		}

		void Publish(bool force)
		{
			_publish?.Invoke(force);
		}

		static string Trim(string? message)
		{
			if (string.IsNullOrEmpty(message))
				return "";

			return message!.Length > MaxMessageLength ? message.Substring(0, MaxMessageLength) : message;
		}

		sealed class DeferredUnit
		{
			public int        Depth   { get; set; }
			public bool       Sampled { get; set; }
			public TestStatus Status  { get; set; }
			public string?    Message { get; set; }
		}
	}
}
