using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Group of properties.
	/// </summary>
	public class PropertyGroup : MemberGroup<CodeProperty>
	{
		public PropertyGroup(List<CodeProperty>? members, bool tableLayout)
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
			var property = new CodeProperty(name, type);
			Members.Add(property);
			return new PropertyBuilder(property);
		}
	}
}
