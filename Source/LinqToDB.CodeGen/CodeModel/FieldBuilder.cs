namespace LinqToDB.CodeGen.CodeModel
{
	public class FieldBuilder
	{
		public FieldBuilder(CodeField field)
		{
			Field = field;
		}

		public CodeField Field { get; }

		public FieldBuilder Public()
		{
			Field.Attributes |= MemberAttributes.Public;
			return this;
		}

		public FieldBuilder Private()
		{
			Field.Attributes |= MemberAttributes.Private;
			return this;
		}

		public FieldBuilder Static()
		{
			Field.Attributes |= MemberAttributes.Static;
			return this;
		}

		public FieldBuilder ReadOnly()
		{
			Field.Attributes |= MemberAttributes.ReadOnly;
			return this;
		}

		public FieldBuilder AddSetter(ICodeExpression setter)
		{
			Field.Setter = setter;
			return this;
		}
	}
}
