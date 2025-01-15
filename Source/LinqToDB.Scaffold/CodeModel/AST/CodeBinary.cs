using System;

namespace LinqToDB.CodeModel
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
				case BinaryOperation.Equal   :
				case BinaryOperation.NotEqual:
				case BinaryOperation.And     :
				case BinaryOperation.Or      :
					_type = WellKnownTypes.System.Boolean;
					break;
				case BinaryOperation.Add  :
					// this is not correct in general
					// but for now we will stick to it while it doesn't give issues
					_type = left.Type;
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

		IType           ICodeExpression.Type        => _type;
		CodeElementType ICodeElement   .ElementType => CodeElementType.BinaryOperation;
	}
}
