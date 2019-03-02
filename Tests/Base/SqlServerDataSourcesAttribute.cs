using System;

using LinqToDB;

namespace Tests
{
	public class SqlServerDataSourcesAttribute : IncludeDataSourcesAttribute
	{
		public SqlServerDataSourcesAttribute(bool includeLinqService = false)
			: base(
				  includeLinqService,
				  ProviderName.SqlServer2000,
				  ProviderName.SqlServer2005,
				  ProviderName.SqlServer2008,
				  ProviderName.SqlServer2012,
				  ProviderName.SqlServer2014)
		{
		}
	}
}
