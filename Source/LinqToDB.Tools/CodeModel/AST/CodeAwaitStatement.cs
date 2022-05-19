namespace LinqToDB.CodeModel;

/// <summary>
/// Await statement.
/// </summary>
public sealed class CodeAwaitStatement : ICodeStatement
{
	public CodeAwaitStatement(ICodeExpression task)
	{
		Task = task;
	}

	public ICodeExpression Task { get; }

	CodeElementType ICodeElement.ElementType => CodeElementType.AwaitStatement;
}
