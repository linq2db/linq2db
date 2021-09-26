namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Base class for type builders.
	/// </summary>
	/// <typeparam name="TBuilder">Builder implementation type.</typeparam>
	/// <typeparam name="TType">Built type.</typeparam>
	public abstract class TypeBuilder<TBuilder, TType>
		where TBuilder : TypeBuilder<TBuilder, TType>
		where TType : TypeBase
	{
		protected TypeBuilder(TType type, ClassGroup group)
		{
			Type  = type;
			Group = group;
		}

		/// <summary>
		/// Class group, to which current class belongs.
		/// </summary>
		public ClassGroup Group { get; }

		/// <summary>
		/// Built type type descriptor.
		/// </summary>
		public TType Type { get; }

		/// <summary>
		/// Add custom attribute to type.
		/// </summary>
		/// <param name="type">Attribute type.</param>
		/// <returns>Custom attribute builder.</returns>
		public AttributeBuilder AddAttribute(IType type)
		{
			var attr = new CodeAttribute(type);
			Type.CustomAttributes.Add(attr);
			return new AttributeBuilder(attr);
		}

		/// <summary>
		/// Add xml-doc to type.
		/// </summary>
		/// <returns>Xml-doc builder.</returns>
		public XmlDocBuilder XmlComment()
		{
			var xml = new CodeXmlComment();
			Type.XmlDoc = xml;
			return new XmlDocBuilder(xml);
		}

		/// <summary>
		/// Mark type as public.
		/// </summary>
		/// <returns>Type builder.</returns>
		public TBuilder Public()
		{
			Type.Attributes |= Modifiers.Public;
			return (TBuilder)this;
		}
	}
}
