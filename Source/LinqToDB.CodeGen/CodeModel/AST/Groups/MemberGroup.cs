using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Base class for node groups.
	/// </summary>
	/// <typeparam name="TMember">Type of node in group.</typeparam>
	public abstract class MemberGroup<TMember> : IMemberGroup
		where TMember : IGroupElement
	{
		protected MemberGroup(List<TMember>? members)
		{
			Members = members ?? new List<TMember>();
		}

		/// <summary>
		/// Group members.
		/// </summary>
		public List<TMember> Members { get; set; }

		public virtual bool IsEmpty => Members.Count == 0;

		public abstract CodeElementType ElementType { get; }
	}
}
