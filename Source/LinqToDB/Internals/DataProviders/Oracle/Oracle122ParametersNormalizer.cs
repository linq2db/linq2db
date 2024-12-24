using LinqToDB.DataProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internals.DataProviders.Oracle
{
	public class Oracle122ParametersNormalizer : UniqueParametersNormalizer
	{
		protected override bool IsReserved(string name) => ReservedWords.IsReserved(name, ProviderName.Oracle);
	}
}
