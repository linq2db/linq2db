using System.Collections.Generic;

using JetBrains.Annotations;

using LinqToDB.Configuration;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Access;

namespace LinqToDB.Internal.DataProvider.Access
{
	[UsedImplicitly]
	sealed class AccessFactory : DataProviderFactoryBase
	{
		public override IDataProvider GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var provider = GetAssemblyName(attributes) switch
			{
				OleDbProviderAdapter.AssemblyName => AccessProvider.OleDb,
				OdbcProviderAdapter.AssemblyName  => AccessProvider.ODBC,
				_                                 => AccessProvider.AutoDetect
			};

			var version = GetVersion(attributes) switch
			{
				"JET" => AccessVersion.Jet,
				"ACE" => AccessVersion.Ace,
				_     => AccessVersion.AutoDetect
			};

			return AccessTools.GetDataProvider(version, provider);
		}
	}
}
