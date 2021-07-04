using System.Collections.Generic;

namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeClass : CodeTypeBase, IMemberElement
	{
		public CodeClass(CodeIdentifier[]? @namespace, CodeIdentifier name)
		{
			Name = name;
			Type = new RegularType(@namespace, name, false, false, false, false);
		}

		public CodeClass(CodeClass parent, CodeIdentifier name)
		{
			Name = name;
			Parent = parent;
			Type = new RegularType(parent.Type, name, false, false, false, false);
		}

		public CodeClass? Parent { get; }

		public CodeIdentifier Name { get; }

		public TypeToken? Inherits { get; set; }

		public List<TypeToken> Implements { get; set; } = new();
		
		public List<IMemberGroup> Members { get; set; } = new();

		public override CodeElementType ElementType => CodeElementType.Class;
		public override IType Type { get; }
	}
}
