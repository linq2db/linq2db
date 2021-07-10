using System;

namespace LinqToDB.CodeGen.CodeModel
{
	public class MethodBaseBuilder<TBuilder, TMethod>
		 where TBuilder : MethodBaseBuilder<TBuilder, TMethod>
		 where TMethod : CodeElementMethodBase
	{
		public MethodBaseBuilder(TMethod method)
		{
			Method = method;
		}

		public TMethod Method { get; }

		public TBuilder Public()
		{
			if (Method.ElementType == CodeElementType.Lambda)
				throw new InvalidOperationException();
			Method.Attributes |= MemberAttributes.Public;
			return (TBuilder)this;
		}

		public TBuilder Partial()
		{
			if (Method.ElementType == CodeElementType.Lambda)
				throw new InvalidOperationException();
			Method.Attributes |= MemberAttributes.Partial;
			return (TBuilder)this;
		}

		public CodeBlockBuilder Body()
		{
			if (Method.Body == null)
			{
				var builder = new CodeBlockBuilder(new CodeBlock());
				Method.Body = builder.Block;
				return builder;
			}

			return new CodeBlockBuilder(Method.Body);
		}

		public TBuilder Parameter(CodeParameter parameter)
		{
			Method.Parameters.Add(parameter);
			return (TBuilder)this;
		}

		public XmlCommentBuilder XmlComment()
		{
			var doc = new CodeXmlComment();
			Method.XmlDoc = doc;
			return new XmlCommentBuilder(doc);
		}

		public AttributeBuilder Attribute(IType type)
		{
			var attr = new CodeAttribute(type);
			Method.CustomAttributes.Add(attr);
			return new AttributeBuilder(attr);
		}
	}

}
