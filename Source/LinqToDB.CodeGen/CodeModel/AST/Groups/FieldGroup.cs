namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Group of fields.
	/// </summary>
	public class FieldGroup : MemberGroup<CodeField>
	{
		public FieldGroup(bool tableLayout)
		{
			TableLayout = tableLayout;
		}

		/// <summary>
		/// Prefered group rendering layout: as-is or table layout.
		/// </summary>
		public bool TableLayout { get; }

		public override CodeElementType ElementType => CodeElementType.FieldGroup;

		public FieldBuilder New(CodeIdentifier name, IType type)
		{
			var field = new CodeField(name, type);
			Members.Add(field);
			return new FieldBuilder(field);
		}
	}
}
