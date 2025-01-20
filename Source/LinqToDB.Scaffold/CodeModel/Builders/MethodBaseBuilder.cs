using System;

namespace LinqToDB.CodeModel
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
		private BlockBuilder?  _body;
		private XmlDocBuilder? _xmlComment;

		protected MethodBaseBuilder(TMethod method)
		{
			Method = method;
		}

		/// <summary>
		/// Built method-like object.
		/// </summary>
		public TMethod Method { get; }

		/// <summary>
		/// Set modifiers to method. Replaces old value.
		/// </summary>
		/// <returns>Builder instance.</returns>
		public TBuilder SetModifiers(Modifiers modifiers)
		{
			Method.Attributes = modifiers;
			return (TBuilder)this;
		}

		/// <summary>
		/// Create method body builder.
		/// </summary>
		/// <returns>Method body builder instance.</returns>
		public BlockBuilder Body()
		{
			if (_body == null)
			{
				_body       = new BlockBuilder(new CodeBlock());
				Method.Body = _body.Block;
			}

			return _body;
		}

		/// <summary>
		/// Create method parameter builder.
		/// </summary>
		/// <returns>Builder instance.</returns>
		public TBuilder Parameter(CodeParameter parameter)
		{
			Method.AddParameter(parameter);
			return (TBuilder)this;
		}

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
			Method.AddAttribute(attr);
			return new AttributeBuilder(attr);
		}
	}
}
