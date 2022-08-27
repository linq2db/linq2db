﻿namespace Tests
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public class InsertWithIdentityDataSourcesAttribute : DataSourcesAttribute
	{
		public static List<string> Unsupported = new List<string>
		{
			TestProvName.AllClickHouse
		}.SelectMany(_ => _.Split(',')).ToList();

		public InsertWithIdentityDataSourcesAttribute(params string[] except)
			: base(true, Unsupported.Concat(except.SelectMany(_ => _.Split(','))).ToArray())
		{
		}

		public InsertWithIdentityDataSourcesAttribute(bool includeLinqService, params string[] except)
			: base(includeLinqService, Unsupported.Concat(except.SelectMany(_ => _.Split(','))).ToArray())
		{
		}
	}
}
