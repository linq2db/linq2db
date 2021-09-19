using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Custom attribute declaration.
	/// </summary>
	public sealed class CodeAttribute : ITopLevelElement
	{
		internal CodeAttribute(
			CodeTypeToken                                           type,
			List<ICodeExpression>?                                  parameters,
			List<(CodeIdentifier property, ICodeExpression value)>? namedParameters)
		{
			Type            = type;
			Parameters      = parameters      ?? new ();
			NamedParameters = namedParameters ?? new ();
		}

		public CodeAttribute(IType type)
			: this(new (type), null, null)
		{
		}

		/// <summary>
		/// Attribute type.
		/// </summary>
		public CodeTypeToken                                          Type            { get; }
		/// <summary>
		/// Positional attribute parameters.
		/// </summary>
		public List<ICodeExpression>                                  Parameters      { get; } = new ();
		/// <summary>
		/// Named attribute parameters.
		/// </summary>
		public List<(CodeIdentifier property, ICodeExpression value)> NamedParameters { get; } = new ();

		CodeElementType ICodeElement.ElementType => CodeElementType.Attribute;
	}
}
