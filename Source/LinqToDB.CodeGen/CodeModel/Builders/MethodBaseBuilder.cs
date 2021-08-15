using System;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Base class for method-like object builders.
	/// </summary>
	/// <typeparam name="TBuilder">Builder class type.</typeparam>
	/// <typeparam name="TMethod">Method like object type.</typeparam>
	public abstract class MethodBaseBuilder<TBuilder, TMethod>
		 where TBuilder : MethodBaseBuilder<TBuilder, TMethod>
		 where TMethod : MethodBase
	{
		protected MethodBaseBuilder(TMethod method)
		{
			Method = method;
		}

		/// <summary>
		/// Built method-like object.
		/// </summary>
		public TMethod Method { get; }

		/// <summary>
		/// Mark method as public.
		/// </summary>
		/// <returns>Builder instance.</returns>
		public TBuilder Public()
		{
			if (Method.ElementType == CodeElementType.Lambda)
				throw new InvalidOperationException();

			Method.Attributes |= Modifiers.Public;
			return (TBuilder)this;
		}

		/// <summary>
		/// Mark method as partial.
		/// </summary>
		/// <returns>Builder instance.</returns>
		public TBuilder Partial()
		{
			if (Method.ElementType == CodeElementType.Lambda)
				throw new InvalidOperationException();

			Method.Attributes |= Modifiers.Partial;
			return (TBuilder)this;
		}

		/// <summary>
		/// Create method body builder.
		/// </summary>
		/// <returns>Method body builder instance.</returns>
		public BlockBuilder Body()
		{
			if (Method.Body == null)
			{
				var builder = new BlockBuilder(new CodeBlock());
				Method.Body = builder.Block;
				return builder;
			}

			return new BlockBuilder(Method.Body);
		}

		/// <summary>
		/// Create method parameter builder.
		/// </summary>
		/// <returns>Builder instance.</returns>
		public TBuilder Parameter(CodeParameter parameter)
		{
			Method.Parameters.Add(parameter);
			return (TBuilder)this;
		}

		private XmlDocBuilder? _xmlComment;

		/// <summary>
		/// Create xml-doc comment builder (or get existing).
		/// </summary>
		/// <returns>Xml-doc comment builder instance.</returns>
		public XmlDocBuilder XmlComment()
		{
			if (_xmlComment == null)
			{
				var doc = new CodeXmlComment();
				Method.XmlDoc = doc;
				_xmlComment = new XmlDocBuilder(doc);
			}

			return _xmlComment;
		}

		/// <summary>
		/// Create custom attribute builder.
		/// </summary>
		/// <returns>Custom attribute builder instance.</returns>
		public AttributeBuilder Attribute(IType type)
		{
			var attr = new CodeAttribute(type);
			Method.CustomAttributes.Add(attr);
			return new AttributeBuilder(attr);
		}
	}
}
