namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Group of properties.
	/// </summary>
	public class PropertyGroup : MemberGroup<CodeProperty>
	{
		public PropertyGroup(bool tableLayout)
		{
			TableLayout = tableLayout;
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
