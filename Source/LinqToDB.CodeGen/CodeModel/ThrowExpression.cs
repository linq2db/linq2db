namespace LinqToDB.CodeGen.CodeModel
{
	public class ThrowExpression : ICodeExpression, ICodeStatement
	{
		public ThrowExpression(ICodeExpression exception)
		{
			Exception = exception;
		}

		public ICodeExpression Exception { get; }

		public CodeElementType ElementType => CodeElementType.Throw;
	}
}
