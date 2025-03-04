namespace LinqToDB.Internal.DataProvider
{
	/// <summary>
	/// No-op query parameter normalization policy.
	/// Could be used for providers with positional nameless parameters or providers without database support.
	/// </summary>
	public sealed class NoopQueryParametersNormalizer : IQueryParametersNormalizer
	{
		public static readonly IQueryParametersNormalizer Instance = new NoopQueryParametersNormalizer();

		private NoopQueryParametersNormalizer()
		{
		}

		public string? Normalize(string? originalName) => originalName;
	}
}
