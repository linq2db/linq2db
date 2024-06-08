using System;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// null-forgiving operator.
	/// </summary>
	public sealed class CodeSuppressNull : ICodeExpression
	{
		public CodeSuppressNull(ICodeExpression value)
		{
			if (!value.Type.IsNullable)
				throw new InvalidOperationException($"null-forgiving operator cannot be used with non-nullable value: {value.Type}");

			Value = value;
		}

		/// <summary>
		/// Value (expression) to apply operator.
		/// </summary>
		public ICodeExpression Value { get; }

		IType           ICodeExpression.Type        => Value.Type.IsValueType ? Value.Type : Value.Type.WithNullability(false);
		CodeElementType ICodeElement   .ElementType => CodeElementType.SuppressNull;
	}
}
