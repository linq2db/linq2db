namespace LinqToDB.CodeGen.Schema
{
	/// <summary>
	/// Dynamic return type descriptor.
	/// </summary>
	public record DynamicResult() : Result(ResultKind.Dynamic)
	{
		public override string ToString() => "dynamic";
	}
}
