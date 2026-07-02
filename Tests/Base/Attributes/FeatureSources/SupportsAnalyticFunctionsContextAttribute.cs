using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class SupportsAnalyticFunctionsContextAttribute : DataSourcesAttribute
	{
		// Providers whose IsWindowFunctionsSupported => false (see WindowFunctions.FeatureMatrix.md §1): they reject
		// every window/analytic function at translate time. Excluded so these tests run only where the feature exists,
		// and any newly-added context is included automatically. SQL Server 2005/2008 are kept (the ranking functions
		// work there); per-feature gaps are still asserted per test via [ThrowsForProvider].
		public static List<string> Unsupported = new List<string>
		{
			TestProvName.AllMySql57,
			TestProvName.AllAccess,
			TestProvName.AllSqlCe,
			TestProvName.AllSybase,
			TestProvName.AllFirebirdLess3,
		}.SelectMany(_ => _.Split(',')).ToList();

		public SupportsAnalyticFunctionsContextAttribute(params string[] excludedProviders)
			: base(true, Unsupported.Concat(excludedProviders.SelectMany(_ => _.Split(','))).ToArray())
		{
		}

		public SupportsAnalyticFunctionsContextAttribute(bool includeLinqService, params string[] excludedProviders)
			: base(includeLinqService, Unsupported.Concat(excludedProviders.SelectMany(_ => _.Split(','))).ToArray())
		{
		}
	}
}
