namespace LinqToDB.Schema
{
	/// <summary>
	/// Void return type descriptor.
	/// </summary>
	public sealed record VoidResult() : Result(ResultKind.Void)
	{
		public override string ToString() => "void";
	}
}
