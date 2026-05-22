using System.Collections.Generic;

using JetBrains.Annotations;

using LinqToDB.Configuration;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.DataProvider.Oracle;

namespace LinqToDB.DataProvider.Oracle
{
	[UsedImplicitly]
	sealed class OracleFactory : DataProviderFactoryBase
	{
		public override IDataProvider GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var dialect = GetVersion(attributes) switch
			{
				"11" => OracleVersion.v11,
				"12" => OracleVersion.v12,
				_    => OracleVersion.AutoDetect,
			};

			var provider = GetAssemblyName(attributes) switch
			{
				OracleProviderAdapter.DevartAssemblyName  => OracleProvider.Devart,
				OracleProviderAdapter.NativeAssemblyName  => OracleProvider.Native,
				OracleProviderAdapter.ManagedAssemblyName => OracleProvider.Managed,
				_                                         => OracleProvider.AutoDetect,
			};

			return OracleTools.GetDataProvider(dialect, provider);
		}
	}
}
