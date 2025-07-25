namespace LinqToDB.Internal.DataProvider.Sybase
{
	sealed class SybaseParametersNormalizer : UniqueParametersNormalizer
	{
		protected override int MaxLength => 26;
	}
}
