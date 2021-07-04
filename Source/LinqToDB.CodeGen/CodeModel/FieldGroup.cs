namespace LinqToDB.CodeGen.CodeModel
{
	public class FieldGroup : MemberGroup<CodeField>
	{
		public FieldGroup(bool tableLayout)
		{
			TableLayout = tableLayout;
		}

		public bool TableLayout { get; }

		public override CodeElementType ElementType => CodeElementType.FieldGroup;

		public FieldBuilder New(CodeIdentifier name, IType type)
		{
			var field = new CodeField(name, type);
			Members.Add(field);
			return new FieldBuilder(field);
		}
	}

	public class PragmaGroup : MemberGroup<CodeElementPragma>
	{
		public PragmaGroup()
		{
		}

		public override CodeElementType ElementType => CodeElementType.PragmaGroup;

		public PragmaGroup Add(CodeElementPragma pragma)
		{
			Members.Add(pragma);
			return this;
		}
	}
}
