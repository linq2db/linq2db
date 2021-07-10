namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeBinaryExpression : ICodeExpression
	{
		public CodeBinaryExpression(ICodeExpression left, BinaryOperation operation, ICodeExpression right)
		{
			Left = left;
			Operation = operation;
			Right = right;
		}

		public ICodeExpression Left { get; }
		public ICodeExpression Right { get; }
		public BinaryOperation Operation { get; }

		public CodeElementType ElementType => CodeElementType.BinaryOperation;
	}
}
