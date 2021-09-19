using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Namespace declaration.
	/// </summary>
	public sealed class CodeNamespace : ITopLevelElement
	{
		internal CodeNamespace(IReadOnlyList<CodeIdentifier> name, List<IMemberGroup>? members)
		{
			Name    = name;
			Members = members ?? new ();
		}

		public CodeNamespace(IReadOnlyList<CodeIdentifier> name)
			: this(name, null)
		{
		}

		/// <summary>
		/// Namespace name.
		/// </summary>
		public IReadOnlyList<CodeIdentifier> Name    { get; }
		/// <summary>
		/// Namespace members (in groups).
		/// </summary>
		public List<IMemberGroup>            Members { get; set; }

		CodeElementType ICodeElement.ElementType => CodeElementType.Namespace;
	}
}
