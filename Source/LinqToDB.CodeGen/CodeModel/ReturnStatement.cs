namespace LinqToDB.CodeGen.CodeModel
{
	public class ReturnStatement : ICodeStatement
	{
		public ReturnStatement(ICodeExpression? expression)
		{
			Expression = expression;
		}

		public ICodeExpression? Expression { get; }

		public CodeElementType ElementType => CodeElementType.ReturnStatement;
	}
}
