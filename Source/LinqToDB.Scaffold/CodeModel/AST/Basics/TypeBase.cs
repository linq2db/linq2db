using System;
using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Base class for types.
	/// </summary>
	public abstract class TypeBase : AttributeOwner, ITopLevelElement
	{
		protected TypeBase(
			IEnumerable<CodeAttribute>? customAttributes,
			Modifiers                   attributes,
			CodeXmlComment?             xmlDoc,
			IType                       type,
			CodeIdentifier              name)
			: base(customAttributes)
		{
			Attributes = attributes;
			XmlDoc     = xmlDoc;
			Type       = type;
			Name       = name;

			Type.SetNameChangeHandler(_ => ChangeHandler?.Invoke(this));
		}

		/// <summary>
		/// Type modifiers and attributes.
		/// </summary>
		public Modifiers       Attributes { get; internal set; }
		/// <summary>
		/// Xml-doc comment.
		/// </summary>
		public CodeXmlComment? XmlDoc     { get; internal set; }
		/// <summary>
		/// Type definition.
		/// </summary>
		public IType           Type       { get; }
		/// <summary>
		/// Type name.
		/// </summary>
		public CodeIdentifier  Name       { get; }

		/// <summary>
		/// Internal change-tracking infrastructure. Single action instance is enough.
		/// </summary>
		internal Action<TypeBase>? ChangeHandler { get; set; }
	}
}
