using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SapHana
{
	using System.Collections.Generic;
	using System.Linq;
	using Configuration;

	[UsedImplicitly]
	class SapHanaFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var assemblyName = attributes.FirstOrDefault(_ => _.Name == "assemblyName");
			return SapHanaTools.GetDataProvider(null, assemblyName?.Value);
		}
	}
}
