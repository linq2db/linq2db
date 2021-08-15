namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Expression, describing new one-dimensional array declaration.
	/// </summary>
	public class CodeNewArray : ICodeExpression
	{
		public CodeNewArray(IType type, bool valueTyped, ICodeExpression[] values, bool inline)
		{
			Type       = new (type);
			ValueTyped = valueTyped;
			Inline     = inline;
			Values     = values;
		}

		/// <summary>
		/// Array element type.
		/// </summary>
		public CodeTypeToken     Type       { get; }
		/// <summary>
		/// Array type could be infered from values.
		/// </summary>
		public bool              ValueTyped { get; }
		/// <summary>
		/// Generate array declaration in single code line if possible.
		/// </summary>
		public bool              Inline     { get; }
		/// <summary>
		/// Array elements.
		/// </summary>
		public ICodeExpression[] Values     { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.Array;
	}
}
