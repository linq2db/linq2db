using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Group of compiler pragmas.
	/// </summary>
	public class PragmaGroup : MemberGroup<CodePragma>
	{
		public PragmaGroup(List<CodePragma>? members)
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
			Members.Add(pragma);
			return this;
		}
	}
}
