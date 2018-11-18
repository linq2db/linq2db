using System;

using LinqToDB;

namespace Tests
{
	public class SQLiteDataSourcesAttribute : IncludeDataSourcesAttribute
	{
		public SQLiteDataSourcesAttribute(bool includeLinqService = false) : base(includeLinqService,
			ProviderName.SQLiteClassic, ProviderName.SQLite, ProviderName.SQLiteMS)
		{
		}
	}
}
