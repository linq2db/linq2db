using System;
using System.Linq;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class MergeNotMatchedBySourceDataContextSourceAttribute : IncludeDataSourcesAttribute
	{
		static string[] Supported = new[]
			{
				TestProvName.AllFirebird5Plus,
				TestProvName.AllSqlServer2008Plus,
				TestProvName.AllPostgreSQL17Plus,
			}.SelectMany(_ => _.Split(',')).ToArray();

		public MergeNotMatchedBySourceDataContextSourceAttribute(params string[] except)
			: base(true, Supported.Except(except.SelectMany(_ => _.Split(','))).ToArray())
		{
		}

		public MergeNotMatchedBySourceDataContextSourceAttribute(bool excludeLinqService, params string[] except)
			: base(!excludeLinqService, Supported.Except(except.SelectMany(_ => _.Split(','))).ToArray())
		{
		}
	}
}
