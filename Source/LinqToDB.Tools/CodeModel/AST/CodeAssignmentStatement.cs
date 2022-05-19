namespace LinqToDB.CodeModel;

/// <summary>
/// Assignment statement.
/// </summary>
public sealed class CodeAssignmentStatement : CodeAssignmentBase, ICodeStatement
{
	public CodeAssignmentStatement(ILValue lvalue, ICodeExpression rvalue)
		: base(lvalue, rvalue)
	{
	}

	CodeElementType ICodeElement.ElementType => CodeElementType.AssignmentStatement;
}
