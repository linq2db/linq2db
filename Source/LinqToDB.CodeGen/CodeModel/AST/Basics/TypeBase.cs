namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Base class for types.
	/// </summary>
	public abstract class TypeBase : AttributeOwner, ITopLevelElement
	{
		/// <summary>
		/// Type modifiers and attributes.
		/// </summary>
		public Modifiers       Attributes { get; set; }
		/// <summary>
		/// Xml-doc comment.
		/// </summary>
		public CodeXmlComment? XmlDoc     { get; set; }
		/// <summary>
		/// Type definition.
		/// </summary>
		public IType           Type       { get; protected set; } = null!;
		/// <summary>
		/// Type name.
		/// </summary>
		public CodeIdentifier  Name       { get; protected set; } = null!;
	}
}
