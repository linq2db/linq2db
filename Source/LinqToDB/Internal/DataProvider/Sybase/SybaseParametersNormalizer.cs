namespace LinqToDB.Internal.DataProvider.Sybase
{
	public class SybaseParametersNormalizer : UniqueParametersNormalizer
	{
		protected override int MaxLength => 26;
	}
}
