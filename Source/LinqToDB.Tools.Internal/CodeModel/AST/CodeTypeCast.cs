namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Type cast expression.
	/// </summary>
	public sealed class CodeTypeCast : ICodeExpression
	{
		public CodeTypeCast(CodeTypeToken type, ICodeExpression value)
		{
			Type  = type;
			Value = value;
		}

		public CodeTypeCast(IType type, ICodeExpression value)
			: this(new CodeTypeToken(type), value)
		{
		}

		/// <summary>
		/// Target type.
		/// </summary>
		public CodeTypeToken   Type  { get; }
		/// <summary>
		/// Value (expression) to cast.
		/// </summary>
		public ICodeExpression Value { get; }

		IType           ICodeExpression.Type        => Type.Type;
		CodeElementType ICodeElement   .ElementType => CodeElementType.Cast;
	}
}
