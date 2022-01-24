namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Indexed access expression.
	/// </summary>
	public sealed class CodeIndex : ICodeExpression
	{
		public CodeIndex(ICodeExpression @object, ICodeExpression index, IType returnType)
		{
			Object     = @object;
			Index      = index;
			ReturnType = returnType;
		}

		/// <summary>
		/// Indexed object or type.
		/// </summary>
		public ICodeExpression Object     { get; }
		/// <summary>
		/// Index value.
		/// For now only one-parameter indexes supported.
		/// </summary>
		public ICodeExpression Index      { get; }
		/// <summary>
		/// Type of returned value.
		/// </summary>
		public IType           ReturnType { get; }

		IType           ICodeExpression.Type        => ReturnType;
		CodeElementType ICodeElement   .ElementType => CodeElementType.Index;
	}
}
