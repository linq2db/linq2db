namespace LinqToDB.CodeModel
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
		public TType      Type  { get; }

		/// <summary>
		/// Add custom attribute to type.
		/// </summary>
		/// <param name="type">Attribute type.</param>
		/// <returns>Custom attribute builder.</returns>
		public AttributeBuilder AddAttribute(IType type)
		{
			var attr = new CodeAttribute(type);
			Type.AddAttribute(attr);
			return new AttributeBuilder(attr);
		}

		/// <summary>
		/// Add custom attribute to type.
		/// </summary>
		/// <param name="attribute">Custom attribute.</param>
		/// <returns>Type builder.</returns>
		public TBuilder AddAttribute(CodeAttribute attribute)
		{
			Type.AddAttribute(attribute);
			return (TBuilder)this;
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
		/// Set modifiers to type. Replaces old value.
		/// </summary>
		/// <returns>Type builder.</returns>
		public TBuilder SetModifiers(Modifiers modifiers)
		{
			Type.Attributes = modifiers;
			return (TBuilder)this;
		}
	}
}
