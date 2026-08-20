using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;
using NUnit.Framework.Internal;
using NUnit.ParallelByResource;

using Shouldly;

namespace Tests.Infrastructure
{
	[TestFixture]
	public class ParallelExecutionTests
	{
		// GetProviders() is protected on DataSourcesBaseAttribute, so the two selection rules can only
		// be compared through derived probes.
		sealed class CreateDatabaseProbe : CreateDatabaseSourcesAttribute
		{
			public IEnumerable<string> Resolve() => GetProviders();
		}

		sealed class NorthwindProbe : NorthwindDataContextAttribute
		{
			public IEnumerable<string> Resolve() => GetProviders();
		}

		sealed class IncludeProbe : IncludeDataSourcesAttribute
		{
			public IncludeProbe(params string[] providers) : base(providers)
			{
			}

			public IEnumerable<string> Resolve() => GetProviders();
		}

		/// <summary>
		/// Under the parallel dispatcher every provider that reaches a test as an argument becomes a
		/// resource-lane key, and that lane's first test blocks on the readiness latch until the
		/// provider's <c>CreateDatabase</c> case signals it. <see cref="CreateDatabaseSourcesAttribute"/>
		/// narrows <see cref="TestConfiguration.UserProviders"/> through <see cref="TestConfiguration.Providers"/>
		/// while the <see cref="IncludeDataSourcesAttribute"/> family does not, so a provider outside that
		/// master list - the Northwind contexts, TestNoopProvider - is selectable as a lane key with
		/// nothing to signal it. Such a provider must be pre-marked ready, or its lane pays the full
		/// latch timeout on every run while holding the dispatcher's read lock.
		/// </summary>
		[Test]
		public void ProvidersWithoutCreateDatabaseArePreMarkedReady()
		{
			var signalled = new CreateDatabaseProbe().Resolve().ToHashSet(StringComparer.OrdinalIgnoreCase);

			var laneKeys = new NorthwindProbe().Resolve()
				.Concat(new IncludeProbe(TestProvName.NoopProvider).Resolve());

			var unsignalled = laneKeys
				.Where(p => !signalled.Contains(p))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			// Nothing to prove on a run that enables none of them.
			Assume.That(unsignalled, Is.Not.Empty, "no lane key without a CreateDatabase case is enabled for this run");

			foreach (var provider in unsignalled)
				CustomTestContext.IsDatabaseReady(provider).ShouldBeTrue(
					$"'{provider}' can become a resource-lane key but has no CreateDatabase case, so nothing will ever signal its readiness latch");
		}

		static LaneAssignment ClassifyCurrentTest()
		{
			var assignment = new DatabaseLaneStrategy().Classify(TestExecutionContext.CurrentContext.CurrentTest);

			assignment.ShouldNotBeNull();

			return assignment.Value;
		}

		/// <summary>
		/// A provider test goes to its provider's serial lane, and - the load-bearing part - a provider's
		/// remote (LinqService) variant maps to the <b>same</b> lane key as its direct variant, so the two
		/// never overlap on one database. Only the remote variant additionally takes the process-wide
		/// secondary mutex, because all LinqService tests share one in-process server.
		/// </summary>
		[Test]
		public void ProviderTestIsClassifiedToItsProviderLane([DataSources] string context)
		{
			var assignment = ClassifyCurrentTest();

			assignment.Disposition.ShouldBe(LaneDisposition.SerialLane);
			assignment.ResourceKey.ShouldNotBeNull();

			// The remote variant's context is the direct one plus a suffix, so a shared key shows up as the
			// key being a prefix of the context.
			context.ShouldStartWith(assignment.ResourceKey!);
			assignment.RequiresSecondaryMutex.ShouldBe(context != assignment.ResourceKey);
		}

		/// <summary>
		/// A test bound to no provider has no resource to serialize on, so it runs inline under the read
		/// gate - which still excludes it from the globally-exclusive lane.
		/// </summary>
		[Test]
		public void TestWithoutAProviderIsClassifiedGatedInline()
		{
			var assignment = ClassifyCurrentTest();

			assignment.Disposition.ShouldBe(LaneDisposition.GatedInline);
			assignment.ResourceKey.ShouldBeNull();
			assignment.RequiresSecondaryMutex.ShouldBeFalse();
		}

		/// <summary>
		/// Schema creation must be classified <see cref="LaneDisposition.Ungated"/>, keyed by its provider.
		/// This is the invariant the dispatcher's Ungated branch depends on: were a CreateDatabase case to
		/// classify as anything gated, it would run under the read gate and the provider's other tests
		/// could wait on a readiness latch nothing can reach. The key is required, since it selects the
		/// ungated lane.
		/// </summary>
		[Test]
		public void CreateDatabaseTestIsClassifiedUngated([CreateDatabaseSources] string context)
		{
			var assignment = ClassifyCurrentTest();

			assignment.Disposition.ShouldBe(LaneDisposition.Ungated);
			assignment.ResourceKey.ShouldBe(context);
		}

		/// <summary>
		/// The latch is what a provider's tests wait on while its schema is created, so a waiter must block
		/// until the preparing item signals and never after. <see cref="ResourceReadinessLatch.MarkReady"/>
		/// being idempotent matters because it is called from both the preparing item's teardown and the
		/// waiter's own timeout backstop.
		/// </summary>
		[Test]
		public void ReadinessLatchGatesUntilSignalled()
		{
			var latch = new ResourceReadinessLatch();

			latch.WaitReady("db", TimeSpan.Zero).ShouldBeFalse("an unsignalled key must not report ready");

			latch.MarkReady("db");
			latch.WaitReady("db", TimeSpan.Zero).ShouldBeTrue("a signalled key must report ready without waiting");

			latch.MarkReady("db");
			latch.WaitReady("db", TimeSpan.Zero).ShouldBeTrue("MarkReady must be idempotent");

			latch.WaitReady("other", TimeSpan.Zero).ShouldBeFalse("keys must be independent");
		}

		/// <summary>
		/// Keys are provider context names, which reach the latch from both the test arguments and the
		/// remote-suffix-stripped form, so the default comparer has to be case-insensitive.
		/// </summary>
		[Test]
		public void ReadinessLatchKeysAreCaseInsensitiveByDefault()
		{
			var latch = new ResourceReadinessLatch();

			latch.MarkReady("SQLite.MS");
			latch.WaitReady("sqlite.ms", TimeSpan.Zero).ShouldBeTrue();

			var ordinal = new ResourceReadinessLatch(StringComparer.Ordinal);

			ordinal.MarkReady("SQLite.MS");
			ordinal.WaitReady("sqlite.ms", TimeSpan.Zero).ShouldBeFalse("an explicit comparer must be honoured");
		}
	}
}
