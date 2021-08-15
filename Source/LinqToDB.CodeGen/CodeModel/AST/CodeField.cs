namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Class field definition.
	/// </summary>
	public class CodeField : IGroupElement
	{
		public CodeField(CodeIdentifier name, IType type)
		{
			Name = name;
			Type = new (type);
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
		public ICodeExpression? Initializer { get; set; }

		CodeElementType ICodeElement.ElementType => CodeElementType.Field;
	}
}
