using System;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Type convert expression using <see langword="as"/> operator.
	/// </summary>
	public sealed class CodeAsOperator : ICodeExpression
	{
		public CodeAsOperator(CodeTypeToken type, ICodeExpression value)
		{
			if (type.Type.IsValueType && !type.Type.IsNullable)
				throw new InvalidOperationException($"as operator cannot be used with non-nullable value type: {type.Type}");

			Type  = type;
			Value = value;
		}

		public CodeAsOperator(IType type, ICodeExpression value)
			: this(new CodeTypeToken(type), value)
		{
		}

		/// <summary>
		/// Target type.
		/// </summary>
		public CodeTypeToken   Type  { get; }
		/// <summary>
		/// Value (expression) to convert.
		/// </summary>
		public ICodeExpression Value { get; }

		IType           ICodeExpression.Type        => Type.Type.WithNullability(true);
		CodeElementType ICodeElement   .ElementType => CodeElementType.AsOperator;
	}
}
