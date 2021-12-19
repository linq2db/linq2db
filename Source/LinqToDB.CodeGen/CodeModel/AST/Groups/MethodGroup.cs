using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Group of methods.
	/// </summary>
	public sealed class MethodGroup : MemberGroup<CodeMethod>
	{
		public MethodGroup(IEnumerable<CodeMethod>? members, bool tableLayout)
			: base(members)
		{
			TableLayout = tableLayout;
		}

		public MethodGroup(bool tableLayout)
			: this(null, tableLayout)
		{
		}

		/// <summary>
		/// Prefered group rendering layout: as-is or table layout.
		/// </summary>
		public bool TableLayout { get; }

		public override CodeElementType ElementType => CodeElementType.MethodGroup;

		public MethodBuilder New(CodeIdentifier name)
		{
			return new MethodBuilder(AddMember(new CodeMethod(name)));
		}
	}
}
