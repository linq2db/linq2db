using System.Collections.Generic;

namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeConstructor : CodeElementMethodBase, IMemberElement
	{
		public CodeConstructor(CodeClass type)
		{
			Type = type;
		}

		public override CodeElementType ElementType => CodeElementType.Constructor;
		public List<ICodeExpression> BaseArguments { get; } = new();

		public CodeClass Type { get; }
		public bool ThisCall { get; set; }
	}
}
