namespace LinqToDB.Internal.DataProvider
{
	/// <summary>
	/// Interface, implemented by query parameter name normalization policy for specific provider/database.
	/// </summary>
	public interface IQueryParametersNormalizer
	{
		/// <summary>
		/// Normalize parameter name and return normalized name.
		/// </summary>
		/// <param name="originalName">Original parameter name.</param>
		/// <returns>Normalized parameter name.</returns>
		string? Normalize(string? originalName);
	}
}
