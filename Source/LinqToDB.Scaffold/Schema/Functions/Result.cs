namespace LinqToDB.Schema
{
	/// <summary>
	/// Base function result descriptor.
	/// </summary>
	/// <param name="Kind">Kind of result value.</param>
	public abstract record Result(ResultKind Kind);
}
