namespace LinqToDB.DataProvider.Oracle
{
	using SqlQuery;

	public class Oracle122ParametersNormalizer : UniqueParametersNormalizer
	{
		protected override bool IsReserved(string name) => ReservedWords.IsReserved(name, ProviderName.Oracle);
	}
}
