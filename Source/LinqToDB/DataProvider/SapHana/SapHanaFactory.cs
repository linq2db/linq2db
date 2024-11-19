using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SapHana
{
	using Configuration;

	[UsedImplicitly]
	sealed class SapHanaFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var assemblyName = attributes.FirstOrDefault(_ => _.Name == "assemblyName")?.Value;

			var provider = assemblyName switch
			{
				SapHanaProviderAdapter.UnmanagedAssemblyName => SapHanaProvider.Unmanaged,
				OdbcProviderAdapter.AssemblyName             => SapHanaProvider.ODBC,
				_                                            => SapHanaProvider.AutoDetect
			};

			return SapHanaTools.GetDataProvider(provider);
		}
	}
}
