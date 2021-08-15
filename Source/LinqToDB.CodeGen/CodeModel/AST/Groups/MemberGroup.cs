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
		/// <summary>
		/// Group members.
		/// </summary>
		public List<TMember> Members { get; set; } = new();

		public virtual bool IsEmpty => Members.Count == 0;

		public abstract CodeElementType ElementType { get; }
	}
}
