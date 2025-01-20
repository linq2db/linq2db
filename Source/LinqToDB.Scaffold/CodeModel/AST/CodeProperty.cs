using System;
using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Class property declaration.
	/// </summary>
	public sealed class CodeProperty : AttributeOwner, IGroupElement, ITypedName
	{
		public CodeProperty(
			IEnumerable<CodeAttribute>? customAttributes,
			CodeIdentifier              name,
			CodeTypeToken               type,
			Modifiers                   attributes,
			bool                        hasGetter,
			CodeBlock?                  getter,
			bool                        hasSetter,
			Modifiers                   setterModifiers,
			CodeBlock?                  setter,
			CodeComment?                trailingComment,
			CodeXmlComment?             xmlDoc,
			ICodeExpression?            initializer)
			: base(customAttributes)
		{
			Name            = name;
			Type            = type;
			Attributes      = attributes;
			HasGetter       = hasGetter;
			Getter          = getter;
			HasSetter       = hasSetter;
			SetterModifiers = setterModifiers;
			Setter          = setter;
			TrailingComment = trailingComment;
			XmlDoc          = xmlDoc;
			Initializer     = initializer;

			Reference = new CodeReference(this);

			Name.OnChange += _ => ChangeHandler?.Invoke(this);
			Type.Type.SetNameChangeHandler(_ => ChangeHandler?.Invoke(this));
		}

		public CodeProperty(CodeIdentifier name, IType type)
			: this(null, name, new CodeTypeToken(type), default, default, null, default, default, null, null, null, null)
		{
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
		/// Setter modifiers.
		/// </summary>
		public Modifiers        SetterModifiers { get; internal set; }
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

		/// <summary>
		/// Simple reference to current property.
		/// </summary>
		public CodeReference    Reference       { get; }

		public override CodeElementType ElementType => CodeElementType.Property;

		/// <summary>
		/// Internal change-tracking infrastructure. Single action instance is enough.
		/// </summary>
		internal Action<CodeProperty>? ChangeHandler { get; set; }
	}
}
