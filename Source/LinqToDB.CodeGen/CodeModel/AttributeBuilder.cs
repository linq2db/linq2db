namespace LinqToDB.CodeGen.CodeModel
{
	public class AttributeBuilder
	{
		public AttributeBuilder(CodeAttribute attribute)
		{
			Attribute = attribute;
		}

		public CodeAttribute Attribute { get; }

		public AttributeBuilder Parameter(ICodeExpression value)
		{
			Attribute.Parameters.Add(value);
			return this;
		}

		public AttributeBuilder Parameter(CodeIdentifier property, ICodeExpression value)
		{
			Attribute.NamedParameters.Add((property, value));
			return this;
		}
	}

}
