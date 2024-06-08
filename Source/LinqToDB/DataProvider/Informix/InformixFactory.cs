using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Informix
{
	using Configuration;
	using DB2;

	[UsedImplicitly]
	sealed class InformixFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var assemblyName = attributes.FirstOrDefault(_ => _.Name == "assemblyName")?.Value;

			var provider = assemblyName switch
			{
				InformixProviderAdapter.IfxAssemblyName                          => InformixProvider.Informix,
				DB2ProviderAdapter.AssemblyName                                  => InformixProvider.DB2,
#if !NETFRAMEWORK
				DB2ProviderAdapter.AssemblyNameOld when assemblyName is not null => InformixProvider.DB2,
#endif
				_                                                                => InformixProvider.AutoDetect
			};

			return InformixTools.GetDataProvider(provider);
		}
	}
}
