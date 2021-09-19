using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Code block statement.
	/// </summary>
	public sealed class CodeBlock : CodeElementList<ICodeStatement>
	{
		public CodeBlock(List<ICodeStatement>? items)
			: base(items)
		{
		}

		public CodeBlock()
			: this(null)
		{
		}
	}
}
