namespace LinqToDB.CodeGen.Model
{
	public class CodeBinary : ICodeExpression
	{
		public CodeBinary(ICodeExpression left, BinaryOperation operation, ICodeExpression right)
		{
			Left      = left;
			Operation = operation;
			Right     = right;
		}

		/// <summary>
		/// Left-side operand.
		/// </summary>
		public ICodeExpression Left      { get; }
		/// <summary>
		/// Right-side operand.
		/// </summary>
		public ICodeExpression Right     { get; }
		/// <summary>
		/// Operation type.
		/// </summary>
		public BinaryOperation Operation { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.BinaryOperation;
	}
}
