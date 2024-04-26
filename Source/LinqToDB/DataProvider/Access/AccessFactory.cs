using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Access
{
	using Configuration;

	[UsedImplicitly]
	sealed class AccessFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var assemblyName = attributes.FirstOrDefault(_ => _.Name == "assemblyName")?.Value;

			var provider = assemblyName switch
			{
				OleDbProviderAdapter.AssemblyName => AccessProvider.OleDb,
				OdbcProviderAdapter.AssemblyName  => AccessProvider.ODBC,
				_                                 => AccessProvider.AutoDetect
			};

			return AccessTools.GetDataProvider(provider);
		}
	}
}
