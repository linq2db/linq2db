using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Sybase
{
	using Configuration;

	[UsedImplicitly]
	class SybaseFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var assemblyName = attributes.FirstOrDefault(_ => _.Name == "assemblyName");
			return SybaseTools.GetDataProvider(null, assemblyName?.Value);
		}
	}
}
