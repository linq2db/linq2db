namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Class field definition.
	/// </summary>
	public sealed class CodeField : IGroupElement, ITypedName
	{
		public CodeField(CodeIdentifier name, CodeTypeToken type, Modifiers attributes, ICodeExpression? initializer)
		{
			Name        = name;
			Type        = type;
			Attributes  = attributes;
			Initializer = initializer;

			Reference = new CodeReference(this);
		}

		public CodeField(CodeIdentifier name, IType type)
			: this(name, new CodeTypeToken(type), default, null)
		{
		}

		/// <summary>
		/// Field name.
		/// </summary>
		public CodeIdentifier   Name        { get; }
		/// <summary>
		/// Field type.
		/// </summary>
		public CodeTypeToken    Type        { get; }
		/// <summary>
		/// Field attributes and modifiers.
		/// </summary>
		public Modifiers        Attributes  { get; internal set; }
		/// <summary>
		/// Optional field initializer.
		/// </summary>
		public ICodeExpression? Initializer { get; internal set; }

		/// <summary>
		/// Simple reference to current field.
		/// </summary>
		public CodeReference    Reference   { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.Field;
	}
}
