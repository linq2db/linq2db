using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;
using NUnit.Framework.Interfaces;

using Shouldly;

namespace Tests.Infrastructure
{
	/// <summary>
	/// Accounting behind the <c>--test-progress</c> heartbeat (<see cref="TestProgressState"/>). The interesting
	/// cases come from result-rewriting wrappers such as <see cref="ThrowsWhenAttribute"/>, whose verdict is only
	/// known <em>after</em> the reporter action — which NUnit nests inside them — has sampled the outcome.
	/// </summary>
	[TestFixture]
	public class TestProgressStateTests : TestBase
	{
		const string Unit  = "Tests.Sample.Query(SQLite)";
		const string Other = "Tests.Sample.Other(SQLite)";

		const string RawMessage   = "LinqToDB.LinqToDBException : Correlated subqueries are not supported.";
		const string FinalMessage = "Expected a <LinqToDB.LinqToDBException> to be thrown, but found: 'System.InvalidOperationException : nope'";

		[Test]
		public void ExpectedThrowIsNeverPublishedAsFailure()
		{
			var rec = Recorder.Create();

			// The wrapper defers, the inner test throws (sampled Failed), the wrapper rewrites the verdict to Success.
			rec.State.BeginDeferred (Unit);
			rec.State.StartTest     (Unit);
			rec.State.CompleteTest  (Unit, TestStatus.Failed, RawMessage);
			rec.State.CommitDeferred(Unit, TestStatus.Passed, "Required exception was thrown:\n\n" + RawMessage);

			rec.State.Completed.ShouldBe(1);
			rec.State.Passed   .ShouldBe(1);
			rec.State.Failed   .ShouldBe(0);
			rec.State.Pending  .ShouldBe(0);
			rec.State.RecentFailures.ShouldBeEmpty();

			// No snapshot the heartbeat could have written ever showed the provisional failure, and the whole unit
			// cost no forced (throttle-bypassing) write at all.
			rec.Writes.ShouldAllBe(w => w.Failed == 0 && w.Failures == 0);
			rec.Writes.Count(w => w.Force).ShouldBe(0);
		}

		[Test]
		public void ExpectedThrowThatDidNotFireIsCountedAsFailure()
		{
			var rec = Recorder.Create();

			const string message = "Expected a <LinqToDB.LinqToDBException> to be thrown, but no exception was thrown";

			rec.State.BeginDeferred (Unit);
			rec.State.StartTest     (Unit);
			rec.State.CompleteTest  (Unit, TestStatus.Passed, null);
			rec.State.CommitDeferred(Unit, TestStatus.Failed, message);

			rec.State.Completed.ShouldBe(1);
			rec.State.Passed   .ShouldBe(0);
			rec.State.Failed   .ShouldBe(1);
			rec.State.RecentFailures.Count     .ShouldBe(1);
			rec.State.RecentFailures[0].Test   .ShouldBe(Unit);
			rec.State.RecentFailures[0].Message.ShouldBe(message);

			// The provisional pass was never published either, and the real failure forces exactly one write.
			rec.Writes.ShouldAllBe(w => w.Passed == 0);
			rec.Writes.Count(w => w.Force).ShouldBe(1);
		}

		[Test]
		public void WrongExceptionKeepsTheFinalizedMessage()
		{
			var rec = Recorder.Create();

			// Failed -> Failed: the wrapper keeps the failure but replaces the raw exception text with the one that
			// says *which* expectation was not met. The heartbeat has to carry the finalized message, not the raw one.
			rec.State.BeginDeferred (Unit);
			rec.State.StartTest     (Unit);
			rec.State.CompleteTest  (Unit, TestStatus.Failed, "System.InvalidOperationException : nope");
			rec.State.CommitDeferred(Unit, TestStatus.Failed, FinalMessage);

			rec.State.Completed.ShouldBe(1);
			rec.State.Failed   .ShouldBe(1);
			rec.State.RecentFailures.Count     .ShouldBe(1);
			rec.State.RecentFailures[0].Message.ShouldBe(FinalMessage);

			rec.Writes.Count(w => w.Force).ShouldBe(1);
		}

		[Test]
		public void ProvisionalFailureDoesNotConsumeARecentFailureSlot()
		{
			var state = new TestProgressState();

			for (var i = 0; i < TestProgressState.MaxRecentFailures - 1; i++)
			{
				Complete(state, $"Tests.Sample.Bulk{i}", TestStatus.Failed, "boom");
			}

			// An expected-throw unit is in flight (its provisional Failed would have taken the last free slot) while a
			// real failure completes. The real one must get the slot, and keep it after the expected throw resolves.
			state.BeginDeferred(Unit);
			state.StartTest    (Unit);
			state.CompleteTest (Unit, TestStatus.Failed, RawMessage);

			Complete(state, Other, TestStatus.Failed, "real failure");

			state.CommitDeferred(Unit, TestStatus.Passed, null);

			var failures = state.RecentFailures;

			failures.Count                   .ShouldBe(TestProgressState.MaxRecentFailures);
			failures[failures.Count - 1].Test.ShouldBe(Other);
			failures.ShouldNotContain(f => f.Test == Unit);
		}

		[Test]
		public void NestedWrappersBookOnceWithTheOutermostVerdict()
		{
			var rec = Recorder.Create();

			// Several ThrowsWhen-family attributes on one test nest their wrappers; only the outermost one sees the
			// final result, so only it may book the unit.
			rec.State.BeginDeferred (Unit);
			rec.State.BeginDeferred (Unit);
			rec.State.StartTest     (Unit);
			rec.State.CompleteTest  (Unit, TestStatus.Failed, RawMessage);
			rec.State.CommitDeferred(Unit, TestStatus.Failed, RawMessage);   // inner wrapper: did not rewrite
			rec.State.CommitDeferred(Unit, TestStatus.Passed, null);         // outer wrapper: rewrote to Success

			rec.State.Completed.ShouldBe(1);
			rec.State.Passed   .ShouldBe(1);
			rec.State.Failed   .ShouldBe(0);
			rec.State.Pending  .ShouldBe(0);
			rec.State.RecentFailures.ShouldBeEmpty();

			rec.Writes.ShouldAllBe(w => w.Failed == 0);
		}

		[Test]
		public void ReporterOutsideTheWrapperBooksImmediately()
		{
			var state = new TestProgressState();

			// Defensive against a change in NUnit's command ordering: if the reporter action ever ends up *outside*
			// the wrapper it already samples the final verdict, so the commit must not book a second unit.
			state.BeginDeferred (Unit);
			state.CommitDeferred(Unit, TestStatus.Passed, null);
			state.CompleteTest  (Unit, TestStatus.Passed, null);

			state.Completed.ShouldBe(1);
			state.Passed   .ShouldBe(1);
			state.Pending  .ShouldBe(0);
		}

		[Test]
		public void UncommittedDeferralIsFlushedWhenTheRunEnds()
		{
			var state = new TestProgressState();

			// An exception escaping between the sample and the commit must not drop the unit from the tally.
			state.BeginDeferred(Unit);
			state.StartTest    (Unit);
			state.CompleteTest (Unit, TestStatus.Failed, RawMessage);

			state.MarkDone();

			state.Completed.ShouldBe(1);
			state.Failed   .ShouldBe(1);
			state.Pending  .ShouldBe(0);
			state.Done     .ShouldBeTrue();
			state.Current  .ShouldBeNull();
			state.RecentFailures.Count     .ShouldBe(1);
			state.RecentFailures[0].Test   .ShouldBe(Unit);
			state.RecentFailures[0].Message.ShouldBe(RawMessage);
		}

		[Test]
		public void DoneLatchesWhenCompletedReachesTotal()
		{
			var state = new TestProgressState();

			state.SetTotal(2);

			Complete(state, Unit,  TestStatus.Passed, null);
			state.Done.ShouldBeFalse();

			Complete(state, Other, TestStatus.Passed, null);
			state.Done   .ShouldBeTrue();
			state.Current.ShouldBeNull();
		}

		[Test]
		public void ConcurrentUnitsKeepTheTallyConsistent()
		{
			const int units = 500;

			var state = new TestProgressState();

			Parallel.For(0, units, i =>
			{
				var name = $"Tests.Sample.Concurrent{i}";

				// Every third unit goes through the deferred (expected-throw) path.
				if (i % 3 == 0)
				{
					state.BeginDeferred (name);
					state.StartTest     (name);
					state.CompleteTest  (name, TestStatus.Failed, RawMessage);
					state.CommitDeferred(name, TestStatus.Passed, null);
				}
				else
				{
					Complete(state, name, i % 3 == 1 ? TestStatus.Passed : TestStatus.Failed, "boom");
				}
			});

			var failed = Enumerable.Range(0, units).Count(i => i % 3 == 2);

			state.Started  .ShouldBe(units);
			state.Completed.ShouldBe(units);
			state.Pending  .ShouldBe(0);
			(state.Passed + state.Failed + state.Skipped).ShouldBe(units);

			state.Failed.ShouldBe(failed);
			state.RecentFailures.Count.ShouldBe(TestProgressState.MaxRecentFailures);
			state.RecentFailures.ShouldAllBe(f => f.Test != null && f.Message == "boom");
		}

		[Test]
		public void RepeatIterationsBookOneUnitWithTheLatestVerdict()
		{
			var rec = Recorder.Create();

			// [Repeat] / [Retry] wrappers sit *outside* the reporter action, so it runs once per iteration for a
			// single test case while the run total counts that case once. The unit must be booked once, with the
			// last verdict, and a withdrawn failure must not leave its recentFailures entry behind.
			rec.State.StartTest   (Unit);
			rec.State.CompleteTest(Unit, TestStatus.Failed, "first attempt");
			rec.State.StartTest   (Unit);
			rec.State.CompleteTest(Unit, TestStatus.Passed, null);

			rec.State.Started  .ShouldBe(1);
			rec.State.Completed.ShouldBe(1);
			rec.State.Passed   .ShouldBe(1);
			rec.State.Failed   .ShouldBe(0);
			rec.State.RecentFailures.ShouldBeEmpty();
		}

		[Test]
		public void LongFailureMessagesAreTruncated()
		{
			var state = new TestProgressState();

			Complete(state, Unit, TestStatus.Failed, new string('x', 900));

			state.RecentFailures[0].Message.Length.ShouldBe(500);
		}

		static void Complete(TestProgressState state, string test, TestStatus status, string? message)
		{
			state.StartTest   (test);
			state.CompleteTest(test, status, message);
		}

		/// <summary>
		/// A state plus every snapshot the heartbeat would have written from it — the publish callback fires under the
		/// state lock, so what it reads is exactly what a watcher could have observed on disk.
		/// </summary>
		sealed class Recorder
		{
			public TestProgressState State { get; private set; } = null!;

			public List<(long Passed, long Failed, long Skipped, long Completed, int Failures, bool Force)> Writes { get; } = new();

			public static Recorder Create()
			{
				var rec = new Recorder();

				rec.State = new TestProgressState(force => rec.Writes.Add(
					(rec.State.Passed, rec.State.Failed, rec.State.Skipped, rec.State.Completed, rec.State.RecentFailures.Count, force)));

				return rec;
			}
		}
	}
}
