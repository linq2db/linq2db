using System.Collections.Generic;

using JetBrains.Annotations;

using LinqToDB.Configuration;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Sybase;

namespace LinqToDB.Internal.DataProvider.Sybase
{
	[UsedImplicitly]
	sealed class SybaseFactory : DataProviderFactoryBase
	{
		public override IDataProvider GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var provider = GetAssemblyName(attributes) switch
			{
				SybaseProviderAdapter.NativeAssemblyName  => SybaseProvider.Unmanaged,
				SybaseProviderAdapter.ManagedAssemblyName => SybaseProvider.DataAction,
				_                                         => SybaseProvider.AutoDetect
			};

			return SybaseTools.GetDataProvider(provider);
		}
	}
}
