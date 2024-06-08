namespace LinqToDB.Schema
{
	/// <summary>
	/// Scalar return value descriptor.
	/// </summary>
	/// <param name="Name">Optional name for return value parameter.</param>
	/// <param name="Type">Type of return value.</param>
	/// <param name="Nullable">Return value could contain <c>NULL</c>.</param>
	public sealed record ScalarResult(string? Name, DatabaseType Type, bool Nullable) : Result(ResultKind.Scalar)
	{
		public override string ToString() => $"{(Name != null ? Name + " " : null)}{Type}";
	}
}
