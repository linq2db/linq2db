using System.Collections.Generic;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SapHana
{
	using Configuration;

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
