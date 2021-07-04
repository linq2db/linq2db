using System.Collections.Generic;

namespace LinqToDB.CodeGen.CodeModel
{
	public abstract class AttributeOwner : ICodeElement
	{
		public List<CodeAttribute> CustomAttributes { get; set; } = new();

		public abstract CodeElementType ElementType { get; }
	}
}
