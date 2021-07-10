namespace LinqToDB.CodeGen.CodeModel
{
	public class PropertyBuilder
	{
		public PropertyBuilder(CodeProperty property)
		{
			Property = property;
		}

		public CodeProperty Property { get; }

		public PropertyBuilder Public()
		{
			Property.Attributes |= MemberAttributes.Public;
			return this;
		}

		public PropertyBuilder Default(bool hasSetter)
		{
			Property.HasGetter = true;
			Property.HasSetter = hasSetter;
			return this;
		}

		public CodeBlockBuilder AddGetter()
		{
			Property.HasGetter = true;
			var block = new CodeBlock();
			Property.Getter = block;
			return new CodeBlockBuilder(block);
		}

		public CodeBlockBuilder AddSetter()
		{
			Property.HasSetter = true;
			var block = new CodeBlock();
			Property.Setter = block;
			return new CodeBlockBuilder(block);
		}

		public AttributeBuilder AddAttribute(IType type)
		{
			var attr = new CodeAttribute(type);
			Property.CustomAttributes.Add(attr);
			return new AttributeBuilder(attr);
		}

		public PropertyBuilder TrailingComment(string comment)
		{
			Property.TrailingComment = new CodeElementComment(comment, false);
			return this;
		}

		public XmlCommentBuilder XmlComment()
		{
			var xml = new CodeXmlComment();
			Property.XmlDoc = xml;
			return new XmlCommentBuilder(xml);
		}
	}
}
