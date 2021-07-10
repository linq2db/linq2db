namespace LinqToDB.CodeGen.CodeModel
{
	public abstract class TypeBuilder<TBuilder, TType>
		where TBuilder : TypeBuilder<TBuilder, TType>
		where TType : CodeTypeBase
	{
		protected TypeBuilder(TType type)
		{
			Type = type;
		}

		public TType Type { get; }

		public AttributeBuilder AddAttribute(IType type)
		{
			var attr = new CodeAttribute(type);
			Type.CustomAttributes.Add(attr);
			return new AttributeBuilder(attr);
		}

		public XmlCommentBuilder XmlComment()
		{
			var xml = new CodeXmlComment();
			Type.XmlDoc = xml;
			return new XmlCommentBuilder(xml);
		}

		public TBuilder Public()
		{
			Type.Attributes |= MemberAttributes.Public;
			return (TBuilder)this;
		}
	}

}
