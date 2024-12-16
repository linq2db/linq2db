using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Compiler pragma directive.
	/// </summary>
	public sealed class CodePragma : ITopLevelElement, IGroupElement
	{
		public CodePragma(PragmaType type, IEnumerable<string> parameters)
		{
			PragmaType = type;
			Parameters = parameters.ToArray();
		}

		/// <summary>
		/// Directive type.
		/// </summary>
		public PragmaType            PragmaType { get; }
		/// <summary>
		/// Directive parameters.
		/// </summary>
		public IReadOnlyList<string> Parameters { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.Pragma;
	}
}
