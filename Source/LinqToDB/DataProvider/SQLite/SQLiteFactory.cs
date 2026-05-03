using System.Collections.Generic;

using JetBrains.Annotations;

using LinqToDB.Configuration;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.DataProvider.SQLite;

namespace LinqToDB.DataProvider.SQLite
{
	[UsedImplicitly]
	sealed class SQLiteFactory : DataProviderFactoryBase
	{
		public override IDataProvider GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var provider = GetAssemblyName(attributes) switch
			{
				SQLiteProviderAdapter.SystemDataSQLiteAssemblyName    => SQLiteProvider.System,
				SQLiteProviderAdapter.MicrosoftDataSQLiteAssemblyName => SQLiteProvider.Microsoft,
				_                                                     => SQLiteProvider.AutoDetect,
			};

			return SQLiteTools.GetDataProvider(provider);
		}
	}
}
