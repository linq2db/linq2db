namespace LinqToDB.CodeGen.CodeModel
{
	public class AssignExpression : ICodeExpression, ICodeStatement
	{
		public AssignExpression(ILValue lvalue, ICodeExpression rvalue)
		{
			LValue = lvalue;
			RValue = rvalue;
		}

		public ILValue LValue;
		public ICodeExpression RValue;

		public CodeElementType ElementType => CodeElementType.Assignment;
	}
}
