using System;
using System.Linq;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class IdentityInsertMergeDataContextSourceAttribute : IncludeDataSourcesAttribute
	{
		static string[] Supported = new[]
			{
				TestProvName.AllSybase,
				TestProvName.AllSqlServer2008Plus,
				TestProvName.AllPostgreSQL15Plus,
			}.SelectMany(_ => _.Split(',')).ToArray();

		public IdentityInsertMergeDataContextSourceAttribute(params string[] except)
			: base(true, Supported.Except(except.SelectMany(_ => _.Split(','))).ToArray())
		{
		}

		public IdentityInsertMergeDataContextSourceAttribute(bool includeLinqService, params string[] except)
			: base(includeLinqService, Supported.Except(except.SelectMany(_ => _.Split(','))).ToArray())
		{
		}
	}
}
