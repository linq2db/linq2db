namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Await expression.
	/// </summary>
	public sealed class CodeAwaitExpression : ICodeExpression
	{
		public CodeAwaitExpression(ICodeExpression task)
		{
			Task = task;
		}

		public ICodeExpression Task { get; }

		IType           ICodeExpression.Type        => Task.Type.TypeArguments![0];
		CodeElementType ICodeElement   .ElementType => CodeElementType.AwaitExpression;
	}
}
