using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Custom attribute declaration.
	/// </summary>
	public class CodeAttribute : ITopLevelElement
	{
		public CodeAttribute(IType type)
		{
			Type = new (type);
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
