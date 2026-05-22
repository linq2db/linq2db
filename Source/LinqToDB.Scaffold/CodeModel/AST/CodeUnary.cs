using System;

namespace LinqToDB.CodeModel
{
	public sealed class CodeUnary : ICodeExpression
	{
		private readonly IType _type;

		public CodeUnary(ICodeExpression argument, UnaryOperation operation)
		{
			Argument  = argument;
			Operation = operation;

			_type = operation switch
			{
				UnaryOperation.Not => WellKnownTypes.System.Boolean,
				_ => throw new NotImplementedException($"Type infer is not implemented for unary operation: {operation}"),
			};
		}

		/// <summary>
		/// Operand.
		/// </summary>
		public ICodeExpression Argument { get; }
		/// <summary>
		/// Operation type.
		/// </summary>
		public UnaryOperation  Operation { get; }

		IType           ICodeExpression.Type        => _type;
		CodeElementType ICodeElement   .ElementType => CodeElementType.UnaryOperation;
	}
}
