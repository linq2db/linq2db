using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Code block statement.
	/// </summary>
	public sealed class CodeBlock : CodeElementList<ICodeStatement>
	{
		public CodeBlock(IEnumerable<ICodeStatement>? items)
			: base(items)
		{
		}

		public CodeBlock()
			: this(null)
		{
		}
	}
}
