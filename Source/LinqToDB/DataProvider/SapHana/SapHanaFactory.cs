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
			var version      = attributes.FirstOrDefault(_ => _.Name == "version")?.Value;
			var assemblyName = attributes.FirstOrDefault(_ => _.Name == "assemblyName")?.Value;

			return version switch
			{
				"1"      => SapHanaTools.GetDataProvider(assemblyName: assemblyName, version: SapHanaVersion.SapHana1),
				"2SPS04" => SapHanaTools.GetDataProvider(assemblyName: assemblyName, version: SapHanaVersion.SapHana2sps04),
				_        => SapHanaTools.GetDataProvider(assemblyName: assemblyName),
			};
		}
	}
}
