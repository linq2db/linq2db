using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Expression, describing new one-dimensional array declaration.
	/// </summary>
	public sealed class CodeNewArray : ICodeExpression
	{
		private readonly IType _arrayType;

		public CodeNewArray(CodeTypeToken type, bool valueTyped, IReadOnlyList<ICodeExpression> values, bool inline)
		{
			Type       = type;
			ValueTyped = valueTyped;
			Inline     = inline;
			Values     = values;

			_arrayType = new ArrayType(type.Type, new int?[] { values.Count == 0 ? null : values.Count }, false);
		}

		public CodeNewArray(IType type, bool valueTyped, IReadOnlyList<ICodeExpression> values, bool inline)
			: this(new CodeTypeToken(type), valueTyped, values, inline)
		{
		}

		/// <summary>
		/// Array element type.
		/// </summary>
		public CodeTypeToken     Type                { get; }
		/// <summary>
		/// Array type could be infered from values.
		/// </summary>
		public bool              ValueTyped          { get; }
		/// <summary>
		/// Generate array declaration in single code line if possible.
		/// </summary>
		public bool              Inline              { get; }
		/// <summary>
		/// Array elements.
		/// </summary>
		public IReadOnlyList<ICodeExpression> Values { get; }

		IType ICodeExpression.Type => _arrayType;

		CodeElementType ICodeElement.ElementType => CodeElementType.Array;
	}
}
