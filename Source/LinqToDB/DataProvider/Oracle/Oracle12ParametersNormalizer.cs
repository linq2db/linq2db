using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Oracle
{
	public class Oracle12ParametersNormalizer : UniqueParametersNormalizer
	{
		protected override bool IsReserved(string name) => ReservedWords.IsReserved(name, ProviderName.Oracle);
	}
}
