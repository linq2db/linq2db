namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeField : IMemberElement, ICodeElement
	{
		public CodeField(CodeIdentifier name, IType type)
		{
			Name = name;
			Type = new (type);
		}

		public CodeIdentifier Name { get; }
		public TypeToken Type { get; }

		public MemberAttributes Attributes { get; set; }
		public ICodeExpression? Setter { get; set; }

		public CodeElementType ElementType => CodeElementType.Field;
	}
}
