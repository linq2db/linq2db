using System.Collections.Generic;

using JetBrains.Annotations;

using LinqToDB.Configuration;
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
				"9.2"                          => PostgreSQLVersion.v92,
				"9.3" or "9.4"                 => PostgreSQLVersion.v93,
				 "9.5" or "10"                 => PostgreSQLVersion.v95,
				 "11"                          => PostgreSQLVersion.v11,
				 "12"                          => PostgreSQLVersion.v12,
				 "13" or "14"                  => PostgreSQLVersion.v13,
				 "15" or "16" or "17"          => PostgreSQLVersion.v15,
				 "18"                          => PostgreSQLVersion.v18,
				 "19"                          => PostgreSQLVersion.v19,
				_                              => PostgreSQLVersion.AutoDetect,
			};

			return PostgreSQLTools.GetDataProvider(version);
		}
	}
}
