namespace LinqToDB.Schema
{
	/// <summary>
	/// Function or procedure parameter descriptor.
	/// </summary>
	/// <param name="Name">Parameter name.</param>
	/// <param name="Description">Optional parameter description.</param>
	/// <param name="Type">Parameter type.</param>
	/// <param name="Nullable">Parameter allows <c>NULL</c> value.</param>
	/// <param name="Direction">Parameter direction.</param>
	public sealed record Parameter(
		string             Name,
		string?            Description,
		DatabaseType       Type,
		bool               Nullable,
		ParameterDirection Direction);
}
