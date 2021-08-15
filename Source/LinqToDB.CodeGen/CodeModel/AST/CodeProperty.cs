namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Class property declaration.
	/// </summary>
	public class CodeProperty : AttributeOwner, IGroupElement
	{
		public CodeProperty(CodeIdentifier name, IType type)
		{
			Name = name;
			Type = new (type);
		}

		/// <summary>
		/// Property name.
		/// </summary>
		public CodeIdentifier   Name            { get; }
		/// <summary>
		/// Property type.
		/// </summary>
		public CodeTypeToken    Type            { get; }
		/// <summary>
		/// Property attributes and modifiers.
		/// </summary>
		public Modifiers        Attributes      { get; internal set; }
		/// <summary>
		/// Indicates that property has getter.
		/// </summary>
		public bool             HasGetter       { get; internal set; }
		/// <summary>
		/// Getter body.
		/// </summary>
		public CodeBlock?       Getter          { get; internal set; }
		/// <summary>
		/// Indicates that property has setter.
		/// </summary>
		public bool             HasSetter       { get; internal set; }
		/// <summary>
		/// Setter body.
		/// </summary>
		public CodeBlock?       Setter          { get; internal set; }
		/// <summary>
		/// Optional trailing comment on same line as property.
		/// </summary>
		public CodeComment?     TrailingComment { get; internal set; }
		/// <summary>
		/// Xml-doc comment.
		/// </summary>
		public CodeXmlComment?  XmlDoc          { get; internal set; }
		/// <summary>
		/// Optional initializer.
		/// </summary>
		public ICodeExpression? Initializer     { get; internal set; }

		public override CodeElementType ElementType => CodeElementType.Property;
	}
}
