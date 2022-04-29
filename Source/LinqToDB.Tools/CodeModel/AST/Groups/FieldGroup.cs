using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Group of fields.
	/// </summary>
	public sealed class FieldGroup : MemberGroup<CodeField>
	{
		public FieldGroup(IEnumerable<CodeField>? members, bool tableLayout)
			: base(members)
		{
			TableLayout = tableLayout;
		}

		public FieldGroup(bool tableLayout)
			: this(null, tableLayout)
		{
		}

		/// <summary>
		/// Prefered group rendering layout: as-is or table layout.
		/// </summary>
		public bool TableLayout { get; }

		public override CodeElementType ElementType => CodeElementType.FieldGroup;

		public FieldBuilder New(CodeIdentifier name, IType type)
		{
			return new FieldBuilder(AddMember(new CodeField(name, type)));
		}
	}
}
