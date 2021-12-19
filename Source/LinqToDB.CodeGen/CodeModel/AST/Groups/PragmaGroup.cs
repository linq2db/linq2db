using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Group of compiler pragmas.
	/// </summary>
	public sealed class PragmaGroup : MemberGroup<CodePragma>
	{
		public PragmaGroup(IEnumerable<CodePragma>? members)
			: base(members)
		{
		}

		public PragmaGroup()
			: this(null)
		{
		}

		public override CodeElementType ElementType => CodeElementType.PragmaGroup;

		/// <summary>
		/// Add compiler pragma to group.
		/// </summary>
		/// <param name="pragma">New pragma to add.</param>
		/// <returns>Current group instance.</returns>
		public PragmaGroup Add(CodePragma pragma)
		{
			AddMember(pragma);
			return this;
		}
	}
}
