using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Base class for node groups.
	/// </summary>
	/// <typeparam name="TMember">Type of node in group.</typeparam>
	public abstract class MemberGroup<TMember> : IMemberGroup
		where TMember : IGroupElement
	{
		private readonly List<TMember> _members;

		protected MemberGroup(IEnumerable<TMember>? members)
		{
			_members = new List<TMember>(members ?? []);
		}

		/// <summary>
		/// Group members.
		/// </summary>
		public IReadOnlyList<TMember> Members => _members;

		/// <summary>
		/// Add new member to members list.
		/// </summary>
		/// <param name="member">New group element.</param>
		/// <returns>Added element.</returns>
		protected TMember AddMember(TMember member)
		{
			_members.Add(member);
			return member;
		}

		public virtual bool IsEmpty => Members.Count == 0;

		public abstract CodeElementType ElementType { get; }
	}
}
