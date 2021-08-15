namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Type cast expression.
	/// </summary>
	public class CodeTypeCast : ICodeExpression
	{
		public CodeTypeCast(IType type, ICodeExpression value)
		{
			Type  = new (type);
			Value = value;
		}

		/// <summary>
		/// Target type.
		/// </summary>
		public CodeTypeToken   Type  { get; }
		/// <summary>
		/// Value (expression) to cast.
		/// </summary>
		public ICodeExpression Value { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.Cast;
	}
}
