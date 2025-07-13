using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using LinqToDB.Configuration;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.DataProvider.SapHana;

namespace LinqToDB.DataProvider.SapHana
{
	[UsedImplicitly]
	sealed class SapHanaFactory : DataProviderFactoryBase
	{
		public override IDataProvider GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var assemblyName = GetAssemblyName(attributes);

			var provider = SapHanaProvider.AutoDetect;

			if (SapHanaProviderAdapter.UnmanagedAssemblyNames.Any(an => an == assemblyName))
				provider = SapHanaProvider.Unmanaged;
			else if (assemblyName == OdbcProviderAdapter.AssemblyName)
				provider = SapHanaProvider.ODBC;

			return SapHanaTools.GetDataProvider(provider);
		}
	}
}
