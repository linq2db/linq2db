using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Expression, describing new one-dimensional array declaration.
	/// </summary>
	public sealed class CodeNewArray : ICodeExpression
	{
		private readonly IType _arrayType;

		public CodeNewArray(CodeTypeToken type, bool valueTyped, IEnumerable<ICodeExpression> values, bool inline)
		{
			Type       = type;
			ValueTyped = valueTyped;
			Inline     = inline;
			Values     = values.ToArray();
			_arrayType = new ArrayType(type.Type, new int?[] { Values.Count == 0 ? null : Values.Count }, false);
		}

		public CodeNewArray(IType type, bool valueTyped, IEnumerable<ICodeExpression> values, bool inline)
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

		IType           ICodeExpression.Type        => _arrayType;
		CodeElementType ICodeElement   .ElementType => CodeElementType.Array;
	}
}
