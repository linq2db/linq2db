using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Group of methods.
	/// </summary>
	public class MethodGroup : MemberGroup<CodeMethod>
	{
		public MethodGroup(List<CodeMethod>? members, bool tableLayout)
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
			var method = new CodeMethod(name);
			Members.Add(method);
			return new MethodBuilder(method);
		}
	}
}
