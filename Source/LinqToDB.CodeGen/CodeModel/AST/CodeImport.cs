using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Import (using) statement.
	/// </summary>
	public sealed class CodeImport : ITopLevelElement
	{
		public CodeImport(IReadOnlyList<CodeIdentifier> @namespace)
		{
			Namespace = @namespace;
		}

		/// <summary>
		/// Imported namespace.
		/// </summary>
		public IReadOnlyList<CodeIdentifier> Namespace { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.Import;
	}
}
