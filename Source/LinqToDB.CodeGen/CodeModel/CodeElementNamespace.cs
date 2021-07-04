using System.Collections.Generic;

namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeElementNamespace : ITopLevelCodeElement
	{
		public CodeElementNamespace(CodeIdentifier[] name)
		{
			Name = name;
		}

		public CodeIdentifier[] Name { get; }

		public List<IMemberGroup> Members { get; set; } = new();

		public CodeElementType ElementType => CodeElementType.Namespace;
	}
}
