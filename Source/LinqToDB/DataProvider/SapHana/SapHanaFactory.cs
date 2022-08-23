namespace LinqToDB.DataProvider.SapHana
{
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
