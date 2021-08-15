namespace LinqToDB.CodeGen.Schema
{
	/// <summary>
	/// Void return type descriptor.
	/// </summary>
	public record VoidResult() : Result(ResultKind.Void)
	{
		public override string ToString() => "void";
	}
}
