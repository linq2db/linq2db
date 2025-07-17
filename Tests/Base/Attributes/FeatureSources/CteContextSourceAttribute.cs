using System;
using System.Linq;

using LinqToDB;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class CteContextSourceAttribute : IncludeDataSourcesAttribute
	{
		public static string[] CteSupportedProviders = new[]
			{
				TestProvName.AllSqlServer,
				TestProvName.AllFirebird,
				TestProvName.AllPostgreSQL,
				ProviderName.DB2,
				TestProvName.AllSQLite,
				TestProvName.AllOracle,
				TestProvName.AllClickHouse,
				TestProvName.AllMySqlWithCTE,
				TestProvName.AllInformix,
				TestProvName.AllSapHana,
			}.SelectMany(_ => _.Split(',')).ToArray();

		public CteContextSourceAttribute() : this(true)
		{
		}

		public CteContextSourceAttribute(bool includeLinqService)
			: base(includeLinqService, CteSupportedProviders)
		{
		}

		public CteContextSourceAttribute(params string[] excludedProviders)
			: base(CteSupportedProviders.Except(excludedProviders.SelectMany(_ => _.Split(','))).ToArray())
		{
		}

		public CteContextSourceAttribute(bool includeLinqService, params string[] excludedProviders)
			: base(includeLinqService, CteSupportedProviders.Except(excludedProviders.SelectMany(_ => _.Split(','))).ToArray())
		{
		}
	}
}
