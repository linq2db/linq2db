using System.Collections.Generic;

namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeMethod : CodeElementMethodBase, IMemberElement
	{
		public CodeMethod(CodeIdentifier name)
		{
			Name = name;
		}

		public CodeIdentifier Name { get; }

		public TypeToken? ReturnType { get; set; }

		public List<TypeToken> TypeParameters { get; } = new ();

		public override CodeElementType ElementType => CodeElementType.Method;

		// TODO: add type parameters count check to overloads detection logic when type parameters added to method model
	}
}
