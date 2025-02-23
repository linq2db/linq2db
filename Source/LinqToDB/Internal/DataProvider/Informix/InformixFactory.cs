using System.Collections.Generic;

using JetBrains.Annotations;

using LinqToDB.Configuration;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Informix;
using LinqToDB.Internal.DataProvider.DB2;

namespace LinqToDB.Internal.DataProvider.Informix
{
	[UsedImplicitly]
	sealed class InformixFactory : DataProviderFactoryBase
	{
		public override IDataProvider GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var provider = GetAssemblyName(attributes) switch
			{
				InformixProviderAdapter.IfxAssemblyName => InformixProvider.Informix,
				DB2ProviderAdapter.AssemblyName         => InformixProvider.DB2,
#if !NETFRAMEWORK
				DB2ProviderAdapter.AssemblyNameOld      => InformixProvider.DB2,
#endif
				_                                       => InformixProvider.AutoDetect
			};

			return InformixTools.GetDataProvider(provider);
		}
	}
}
