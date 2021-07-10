namespace LinqToDB.CodeGen.CodeModel
{
	public class IndexExpression : ICodeExpression
	{
		public IndexExpression(ICodeExpression obj, ICodeExpression index)
		{
			Object = obj;
			Index = index;
		}

		public ICodeExpression Object { get; }
		public ICodeExpression Index { get; }

		public CodeElementType ElementType => CodeElementType.Index;
	}
}
