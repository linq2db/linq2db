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
		// Keyed by test full name — unique per case by project convention (the baseline files are keyed by the same
		// fully-qualified name), so a duplicate is a test-authoring bug to fix, not a case this protocol handles.
		readonly Dictionary<string, DeferredUnit>    _deferred = new(StringComparer.Ordinal);
		// Verdict already booked per test case, so a repeat/retry wrapper - which sits *outside* the reporter action
		// and re-enters it once per iteration - books one unit rather than one per iteration. See Book.
		readonly Dictionary<string, TestStatus>      _booked   = new(StringComparer.Ordinal);
		readonly Action<bool>?                       _publish;

		long    _total;
		long    _started;
		long    _completed;
		long    _passed;
		long    _failed;
		long    _skipped;
		long    _inconclusive;
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
		/// <summary>
		/// Tests booked as inconclusive — the bucket <see cref="ActiveIssueNewAttribute"/> rewrites a still-failing
		/// known issue into. Tracked separately because the platform summary folds it into <c>skipped</c>, which
		/// makes a known-issue case indistinguishable from a genuinely skipped one.
		/// </summary>
		public long Inconclusive { get { lock (_sync) return _inconclusive; } }
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
				// A repeat/retry iteration re-enters this for an already-booked case; it is not a new unit.
				if (!_booked.ContainsKey(fullName))
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
			// [Repeat] / [Retry] wrappers sit outside the reporter action, so it runs once per iteration for the same
			// test case while the run total counts the case once. Book the unit on first sight and afterwards only
			// move it between buckets, keeping the latest verdict - which is what the platform summary reports.
			if (_booked.TryGetValue(fullName, out var booked))
				Unbook(fullName, booked);
			else
				_completed++;

			_booked[fullName] = status;

			// Deliberately do NOT clear _current here: with throttled writes, nulling between every test
			// makes the on-disk snapshot almost always catch the gap (showing no current test). Keep the
			// most-recently-started test as "current" until the run completes — during a run that is the
			// in-flight (or just-finished) test, which is what a watcher wants to see.

			var force = false;

			switch (status)
			{
				case TestStatus.Passed      : _passed++;       break;
				case TestStatus.Skipped     : _skipped++;      break;
				case TestStatus.Inconclusive: _inconclusive++; break;
				case TestStatus.Failed :
					_failed++;
					force = true;
					if (_failures.Count < MaxRecentFailures)
						_failures.Add((fullName, Trim(message)));
					break;
				// Warning — count as completed but in no verdict bucket.
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

		// Withdraws a previously booked verdict, so a re-booked case does not double-count.
		void Unbook(string fullName, TestStatus booked)
		{
			switch (booked)
			{
				case TestStatus.Passed      : _passed--;       break;
				case TestStatus.Skipped     : _skipped--;      break;
				case TestStatus.Inconclusive: _inconclusive--; break;
				case TestStatus.Failed :
					_failed--;

					// Only the entry this case added, not every entry sharing its name.
					var idx = _failures.FindLastIndex(f => f.Test == fullName);

					if (idx >= 0)
						_failures.RemoveAt(idx);

					break;
				default: break;
			}
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
