using System.Collections.Generic;

using JetBrains.Annotations;

using LinqToDB.Configuration;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Firebird;

namespace LinqToDB.Internal.DataProvider.Firebird
{
	[UsedImplicitly]
	sealed class FirebirdFactory : DataProviderFactoryBase
	{
		public override IDataProvider GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var version = GetVersion(attributes) switch
			{
				"2.5" => FirebirdVersion.v25,
				"3"   => FirebirdVersion.v3,
				"4"   => FirebirdVersion.v4,
				"5"   => FirebirdVersion.v5,
				_     => FirebirdVersion.AutoDetect,
			};

			return FirebirdTools.GetDataProvider(version);
		}
	}
}
