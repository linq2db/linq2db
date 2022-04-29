﻿namespace LinqToDB.CodeModel
{
	/// <summary>
	/// <see cref="CodeProperty"/> object builder.
	/// </summary>
	public sealed class PropertyBuilder
	{
		internal PropertyBuilder(CodeProperty property)
		{
			Property = property;
		}

		/// <summary>
		/// Built property.
		/// </summary>
		public CodeProperty Property { get; }

		/// <summary>
		/// Mark property public.
		/// </summary>
		/// <returns>Builder instance.</returns>
		public PropertyBuilder Public()
		{
			Property.Attributes |= Modifiers.Public;
			return this;
		}

		/// <summary>
		/// Mark property static.
		/// </summary>
		/// <returns>Builder instance.</returns>
		public PropertyBuilder Static()
		{
			Property.Attributes |= Modifiers.Static;
			return this;
		}

		/// <summary>
		/// Mark property as having default implementation.
		/// </summary>
		/// <param name="hasSetter">Indicate that property has setter.</param>
		/// <returns>Builder instance.</returns>
		public PropertyBuilder Default(bool hasSetter)
		{
			Property.HasGetter = true;
			Property.HasSetter = hasSetter;
			return this;
		}

		/// <summary>
		/// Add getter implementation.
		/// </summary>
		/// <returns>Getter code block builder.</returns>
		public BlockBuilder AddGetter()
		{
			Property.HasGetter = true;
			var block = new CodeBlock();
			Property.Getter = block;
			return new BlockBuilder(block);
		}

		/// <summary>
		/// Add setter implementation.
		/// </summary>
		/// <returns>Setter code block builder.</returns>
		public BlockBuilder AddSetter()
		{
			Property.HasSetter = true;
			var block = new CodeBlock();
			Property.Setter = block;
			return new BlockBuilder(block);
		}

		/// <summary>
		/// Add custom attribute.
		/// </summary>
		/// <param name="type">Attribute type.</param>
		/// <returns>Custom attribute builder.</returns>
		public AttributeBuilder AddAttribute(IType type)
		{
			var attr = new CodeAttribute(type);
			Property.AddAttribute(attr);
			return new AttributeBuilder(attr);
		}

		/// <summary>
		/// Add trailing comment to property.
		/// </summary>
		/// <param name="comment">Commentaty text.</param>
		/// <returns>Builder instance.</returns>
		public PropertyBuilder TrailingComment(string comment)
		{
			Property.TrailingComment = new CodeComment(comment, false);
			return this;
		}

		/// <summary>
		/// Add property initializer.
		/// </summary>
		/// <param name="initializer">Initialization expression.</param>
		/// <returns>Builder instance.</returns>
		public PropertyBuilder SetInitializer(ICodeExpression initializer)
		{
			Property.Initializer = initializer;
			return this;
		}

		/// <summary>
		/// Add xml-doc comment.
		/// </summary>
		/// <returns>Xml-doc builder.</returns>
		public XmlDocBuilder XmlComment()
		{
			var xml = new CodeXmlComment();
			Property.XmlDoc = xml;
			return new XmlDocBuilder(xml);
		}
	}
}
