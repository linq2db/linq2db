using System.Collections.Generic;

using JetBrains.Annotations;

using LinqToDB.Configuration;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.Internal.DataProvider;

namespace LinqToDB.DataProvider.PostgreSQL
{
	[UsedImplicitly]
	sealed class PostgreSQLFactory : DataProviderFactoryBase
	{
		public override IDataProvider GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var version = GetVersion(attributes) switch
			{
				"9.2"           => PostgreSQLVersion.v92,
				"9.3" or "9.4"  => PostgreSQLVersion.v93,
				"9.5"           => PostgreSQLVersion.v95,
				"10" or "11" or "12" or "13" or "14" or "15" or "16" or "17" 
				                => PostgreSQLVersion.v15,
				_               => PostgreSQLVersion.AutoDetect,
			};

			return PostgreSQLTools.GetDataProvider();
		}
	}
}
