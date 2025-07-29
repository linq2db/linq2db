namespace LinqToDB.Internal.DataProvider.Oracle
{
	public class Oracle11ParametersNormalizer : Oracle122ParametersNormalizer
	{
		protected override int MaxLength => 30;
	}
}
