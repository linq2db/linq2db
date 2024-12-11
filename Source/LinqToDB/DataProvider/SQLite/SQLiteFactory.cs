using System.Collections.Generic;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SQLite
{
	using Configuration;

	[UsedImplicitly]
	sealed class SQLiteFactory : DataProviderFactoryBase
	{
		public override IDataProvider GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var provider = GetAssemblyName(attributes) switch
			{
				SQLiteProviderAdapter.SystemDataSQLiteAssemblyName    => SQLiteProvider.System,
				SQLiteProviderAdapter.MicrosoftDataSQLiteAssemblyName => SQLiteProvider.Microsoft,
				_                                                     => SQLiteProvider.AutoDetect
			};

			return SQLiteTools.GetDataProvider(provider);
		}
	}
}
