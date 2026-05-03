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

			_type = operation switch
			{
				BinaryOperation.Equal    or
				BinaryOperation.NotEqual or
				BinaryOperation.And      or
				BinaryOperation.Or       =>
					WellKnownTypes.System.Boolean,

				BinaryOperation.Add =>
					// this is not correct in general
					// but for now we will stick to it while it doesn't give issues
					left.Type,

				_ =>
					throw new NotImplementedException($"Type infer is not implemented for binary operation: {operation}"),
			};
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
