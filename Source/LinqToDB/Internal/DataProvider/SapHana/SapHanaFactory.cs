using System.Collections.Generic;

using JetBrains.Annotations;

using LinqToDB.Configuration;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SapHana;

namespace LinqToDB.Internal.DataProvider.SapHana
{
	[UsedImplicitly]
	sealed class SapHanaFactory : DataProviderFactoryBase
	{
		public override IDataProvider GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var provider = GetAssemblyName(attributes) switch
			{
				SapHanaProviderAdapter.UnmanagedAssemblyName => SapHanaProvider.Unmanaged,
				OdbcProviderAdapter.AssemblyName             => SapHanaProvider.ODBC,
				_                                            => SapHanaProvider.AutoDetect
			};

			return SapHanaTools.GetDataProvider(provider);
		}
	}
}
