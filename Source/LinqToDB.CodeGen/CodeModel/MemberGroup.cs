using System.Collections.Generic;

namespace LinqToDB.CodeGen.CodeModel
{
	public abstract class MemberGroup<TMember> : IMemberGroup
		where TMember : IMemberElement
	{
		public List<TMember> Members { get; set; } = new();

		public virtual bool IsEmpty => Members.Count == 0;

		public abstract CodeElementType ElementType { get; }
	}
}
