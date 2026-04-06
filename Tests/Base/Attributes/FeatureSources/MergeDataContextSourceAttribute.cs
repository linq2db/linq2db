using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class MergeDataContextSourceAttribute : DataSourcesAttribute
	{
		public static List<string> Unsupported = new[]
			{
				TestProvName.AllAccess,
				ProviderName.SqlCe,
				ProviderName.Ydb,
				TestProvName.AllSQLite,
				TestProvName.AllSqlServer2005,
				TestProvName.AllClickHouse,
				TestProvName.AllPostgreSQL14Minus,
				TestProvName.AllMySql,
			}.SelectMany(_ => _.Split(',')).ToList();

		public MergeDataContextSourceAttribute(params string[] except)
			: base(true, Unsupported.Concat(except.SelectMany(_ => _.Split(','))).ToArray())
		{
		}

		public MergeDataContextSourceAttribute(bool includeLinqService, params string[] except)
			: base(includeLinqService, Unsupported.Concat(except.SelectMany(_ => _.Split(','))).ToArray())
		{
		}
	}
}
