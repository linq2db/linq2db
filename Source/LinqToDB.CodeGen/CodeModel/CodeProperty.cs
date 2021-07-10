namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeProperty : AttributeOwner, IMemberElement
	{
		public CodeProperty(CodeIdentifier name, IType type)
		{
			Name = name;
			Type = new (type);
		}

		public CodeIdentifier Name { get; }
		public TypeToken Type { get; }

		public MemberAttributes Attributes { get; set; }
		public bool HasGetter { get; set; }
		public CodeBlock? Getter { get; set; }
		public bool HasSetter { get; set; }
		public CodeBlock? Setter { get; set; }
		public CodeElementComment? TrailingComment { get; set; }
		public CodeXmlComment? XmlDoc { get; set; }

		public override CodeElementType ElementType => CodeElementType.Property;
	}
}
