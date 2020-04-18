using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Oracle
{
	using System.Collections.Generic;
	using System.Linq;
	using Configuration;

	[UsedImplicitly]
	class OracleFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var assemblyName = attributes.FirstOrDefault(_ => _.Name == "assemblyName");
			return OracleTools.GetDataProvider(null, assemblyName?.Value);
		}
	}
}
