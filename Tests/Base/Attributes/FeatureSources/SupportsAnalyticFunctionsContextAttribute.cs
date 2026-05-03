using System;
using System.Linq;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class SupportsAnalyticFunctionsContextAttribute : IncludeDataSourcesAttribute
	{
		static readonly string[] SupportedProviders = new[]
			{
				TestProvName.AllSqlServer,
				TestProvName.AllOracle,
				TestProvName.AllClickHouse,
			}.SelectMany(_ => _.Split(',')).ToArray();

		public SupportsAnalyticFunctionsContextAttribute(bool includeLinqService = true, params string[] excludedProviders)
			: base(includeLinqService, SupportedProviders.Except(excludedProviders.SelectMany(_ => _.Split(','))).ToArray())
		{
		}
	}
}
