namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Indexed access expression.
	/// </summary>
	public class CodeIndex : ICodeExpression
	{
		public CodeIndex(ICodeExpression obj, ICodeExpression index)
		{
			Object = obj;
			Index  = index;
		}

		/// <summary>
		/// Indexed object or type.
		/// </summary>
		public ICodeExpression Object { get; }
		/// <summary>
		/// Index value.
		/// For now only one-parameter indexes supported.
		/// </summary>
		public ICodeExpression Index  { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.Index;
	}
}
