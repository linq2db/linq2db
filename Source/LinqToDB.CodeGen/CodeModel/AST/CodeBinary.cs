using System;

namespace LinqToDB.CodeGen.Model
{
	public sealed class CodeBinary : ICodeExpression
	{
		private readonly IType _type;

		public CodeBinary(ICodeExpression left, BinaryOperation operation, ICodeExpression right)
		{
			Left      = left;
			Operation = operation;
			Right     = right;

			switch (operation)
			{
				case BinaryOperation.Equal:
				case BinaryOperation.And  :
					_type = WellKnownTypes.Boolean;
					break;
				default:
					throw new NotImplementedException($"Type infer is not implemented for binary operation: {operation}");
			}
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

		IType ICodeExpression.Type => _type;

		CodeElementType ICodeElement.ElementType => CodeElementType.BinaryOperation;
	}
}
