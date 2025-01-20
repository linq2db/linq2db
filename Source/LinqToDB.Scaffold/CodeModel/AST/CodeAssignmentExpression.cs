namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Assignment expression.
	/// </summary>
	public sealed class CodeAssignmentExpression : CodeAssignmentBase, ICodeExpression
	{
		public CodeAssignmentExpression(ILValue lvalue, ICodeExpression rvalue)
			: base(lvalue, rvalue)
		{
		}

		IType           ICodeExpression.Type        => RValue.Type;
		CodeElementType ICodeElement   .ElementType => CodeElementType.AssignmentExpression;
	}
}
