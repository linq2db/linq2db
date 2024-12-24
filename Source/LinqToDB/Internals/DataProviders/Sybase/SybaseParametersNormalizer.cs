using LinqToDB.DataProvider;

namespace LinqToDB.Internals.DataProviders.Sybase
{
	public sealed class SybaseParametersNormalizer : UniqueParametersNormalizer
	{
		protected override int MaxLength => 26;
	}
}
