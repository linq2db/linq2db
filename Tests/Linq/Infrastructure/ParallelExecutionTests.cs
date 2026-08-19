using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

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
	}
}
