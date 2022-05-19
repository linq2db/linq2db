namespace LinqToDB.CodeModel;

/// <summary>
/// Return statement.
/// </summary>
public sealed class CodeReturn : ICodeStatement
{
	public CodeReturn(ICodeExpression? expression)
	{
		Expression = expression;
	}

	/// <summary>
	/// Optional return value.
	/// </summary>
	public ICodeExpression? Expression { get; }

	CodeElementType ICodeElement.ElementType => CodeElementType.ReturnStatement;
}
