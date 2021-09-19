using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Base class for types.
	/// </summary>
	public abstract class TypeBase : AttributeOwner, ITopLevelElement
	{
		protected TypeBase(
			List<CodeAttribute>? customAttributes,
			Modifiers            attributes,
			CodeXmlComment?      xmlDoc,
			IType                type,
			CodeIdentifier       name)
			: base(customAttributes)
		{
			Attributes = attributes;
			XmlDoc     = xmlDoc;
			Type       = type;
			Name       = name;
		}

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
		public IType           Type       { get; protected set; }
		/// <summary>
		/// Type name.
		/// </summary>
		public CodeIdentifier  Name       { get; protected set; }
	}
}
