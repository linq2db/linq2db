using System;

namespace LinqToDB.DataProvider.SqlServer
{
	public static class SqlServerConfiguration
	{
		[Obsolete("Use SqlServerOptions.Default.GenerateScopeIdentity instead.")]
		public static bool GenerateScopeIdentity
		{
			get => SqlServerOptions.Default.GenerateScopeIdentity;
			set => SqlServerOptions.Default = new SqlServerOptions(SqlServerOptions.Default) { GenerateScopeIdentity = value };
		}
	}
}
