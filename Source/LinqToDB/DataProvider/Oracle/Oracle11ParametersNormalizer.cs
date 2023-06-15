namespace LinqToDB.DataProvider.Oracle
{
	public sealed class Oracle11ParametersNormalizer : Oracle12ParametersNormalizer
	{
		protected override int MaxLength => 30;
	}
}
