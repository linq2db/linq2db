namespace LinqToDB.DataProvider.Sybase
{
	public sealed class SybaseParametersNormalizer : UniqueParametersNormalizer
	{
		protected override int MaxLength => 26;
	}
}
