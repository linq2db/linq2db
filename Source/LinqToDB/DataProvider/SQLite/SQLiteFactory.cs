using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SQLite
{
	using Configuration;

	[UsedImplicitly]
	sealed class SQLiteFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var assemblyName = attributes.FirstOrDefault(_ => _.Name == "assemblyName")?.Value;

			var provider = assemblyName switch
			{
				SQLiteProviderAdapter.SystemDataSQLiteAssemblyName    => SQLiteProvider.System,
				SQLiteProviderAdapter.MicrosoftDataSQLiteAssemblyName => SQLiteProvider.Microsoft,
				_                                                     => SQLiteProvider.AutoDetect
			};

			return SQLiteTools.GetDataProvider(provider);
		}
	}
}
