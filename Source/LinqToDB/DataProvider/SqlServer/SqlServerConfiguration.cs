using System;

namespace LinqToDB.DataProvider.SqlServer
{
	public static class SqlServerConfiguration
	{
		[Obsolete("Use SqlServerOptions.Default.BulkCopyType instead.")]
		public static bool GenerateScopeIdentity
		{
			get => SqlServerOptions.Default.GenerateScopeIdentity;
			set => SqlServerOptions.Default = SqlServerOptions.Default with { GenerateScopeIdentity = value };
		}

	}
}
