namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Typed named entity (parameter, variable, field or property).
	/// </summary>
	public abstract class CodeTypedName : ITypedName
	{
		protected CodeTypedName(CodeIdentifier name, CodeTypeToken type)
		{
			Name      = name;
			Type      = type;
			Reference = new CodeReference(this);
		}

		/// <summary>
		/// Name.
		/// </summary>
		public CodeIdentifier Name     { get; }
		/// <summary>
		/// Type.
		/// </summary>
		public CodeTypeToken  Type     { get; }

		/// <summary>
		/// Reference to current parameter/variable.
		/// </summary>
		public CodeReference Reference { get; }
	}
}
