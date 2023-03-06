using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Sybase
{
	using Configuration;

	[UsedImplicitly]
	sealed class SybaseFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var assemblyName = attributes.FirstOrDefault(_ => _.Name == "assemblyName")?.Value;

			var provider = assemblyName switch
			{
				SybaseProviderAdapter.NativeAssemblyName  => SybaseProvider.Unmanaged,
				SybaseProviderAdapter.ManagedAssemblyName => SybaseProvider.DataAction,
				_                                         => SybaseProvider.AutoDetect
			};

			return SybaseTools.GetDataProvider(provider);
		}
	}
}
