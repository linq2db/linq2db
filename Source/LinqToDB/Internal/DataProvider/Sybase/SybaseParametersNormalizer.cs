using LinqToDB.DataProvider;

namespace LinqToDB.Internal.DataProvider.Sybase
{
	public sealed class SybaseParametersNormalizer : UniqueParametersNormalizer
	{
		protected override int MaxLength => 26;
	}
}
