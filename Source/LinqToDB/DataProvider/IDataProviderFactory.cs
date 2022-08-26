namespace LinqToDB.DataProvider
{
	using Configuration;

	public interface IDataProviderFactory
	{
		IDataProvider GetDataProvider (IEnumerable<NamedValue> attributes);
	}
}
