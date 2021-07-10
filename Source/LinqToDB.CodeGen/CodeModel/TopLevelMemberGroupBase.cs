using System.Collections.Generic;

namespace LinqToDB.CodeGen.CodeModel
{
	public abstract class TopLevelMemberGroupBase<TMember, TMemberBuilder> : IMemberGroup
		where TMember : IMemberElement
	{
		protected TopLevelMemberGroupBase(CodeIdentifier[]? @namespace, bool tableLayout)
		{
			Namespace = @namespace;
			TableLayout = tableLayout;
		}

		public bool TableLayout { get; }

		public CodeIdentifier[]? Namespace { get; }

		public List<TMember> Members { get; set; } = new();

		public virtual bool IsEmpty => Members.Count == 0;

		public abstract CodeElementType ElementType { get; }
	}
}
