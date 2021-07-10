using System.Collections.Generic;

namespace LinqToDB.CodeGen.CodeModel
{
	public abstract class CodeElementMethodBase : CodeElementBase
	{
		public MemberAttributes Attributes { get; set; }

		public CodeBlock? Body { get; set; }

		public CodeXmlComment? XmlDoc { get; set; }

		public List<CodeParameter> Parameters { get; } = new ();
		public List<CodeAttribute> CustomAttributes { get; } = new ();
	}
}
