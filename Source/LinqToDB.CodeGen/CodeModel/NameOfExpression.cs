namespace LinqToDB.CodeGen.CodeModel
{
	public class NameOfExpression : ICodeExpression
	{
		public NameOfExpression(ICodeExpression expression)
		{
			Expression = expression;
		}

		public ICodeExpression Expression { get; }

		public CodeElementType ElementType => CodeElementType.NameOf;
	}
}
