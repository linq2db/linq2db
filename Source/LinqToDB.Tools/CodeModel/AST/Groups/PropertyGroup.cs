using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Group of properties.
	/// </summary>
	public sealed class PropertyGroup : MemberGroup<CodeProperty>
	{
		public PropertyGroup(IEnumerable<CodeProperty>? members, bool tableLayout)
			: base(members)
		{
			TableLayout = tableLayout;
		}

		public PropertyGroup(bool tableLayout)
			: this(null, tableLayout)
		{
		}

		/// <summary>
		/// Prefered group rendering layout: as-is or table layout.
		/// </summary>
		public bool TableLayout { get; }

		public override CodeElementType ElementType => CodeElementType.PropertyGroup;

		public PropertyBuilder New(CodeIdentifier name, IType type)
		{
			return new PropertyBuilder(AddMember(new CodeProperty(name, type)));
		}
	}
}
