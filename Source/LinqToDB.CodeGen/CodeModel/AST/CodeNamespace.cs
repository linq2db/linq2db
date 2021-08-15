using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Namespace declaration.
	/// </summary>
	public class CodeNamespace : ITopLevelElement
	{
		public CodeNamespace(CodeIdentifier[] name)
		{
			Name = name;
		}

		/// <summary>
		/// Namespace name.
		/// </summary>
		public CodeIdentifier[]   Name    { get; }
		/// <summary>
		/// Namespace members (in groups).
		/// </summary>
		public List<IMemberGroup> Members { get; set; } = new();

		CodeElementType ICodeElement.ElementType => CodeElementType.Namespace;
	}
}
