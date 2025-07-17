using System;
using System.Linq;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class AllJoinsSourceAttribute : IncludeDataSourcesAttribute
	{
		private static readonly string[] SupportedProviders = new[]
			{
				TestProvName.AllSqlServer,
				TestProvName.AllOracle,
				TestProvName.AllFirebird,
				TestProvName.AllPostgreSQL,
				TestProvName.AllClickHouse
			}.SelectMany(_ => _.Split(',')).ToArray();

		public AllJoinsSourceAttribute(params string[] excludedProviders)
			: base(SupportedProviders.Except(excludedProviders.SelectMany(_ => _.Split(','))).ToArray())
		{
		}
	}
}
