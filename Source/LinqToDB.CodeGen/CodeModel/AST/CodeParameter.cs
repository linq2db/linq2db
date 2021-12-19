namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Method parameter.
	/// </summary>
	public sealed class CodeParameter : CodeTypedName, ICodeElement
	{
		internal CodeParameter(CodeTypeToken type, CodeIdentifier name, CodeParameterDirection direction)
			: base(name, type)
		{
			Direction = direction;
		}

		public CodeParameter(IType type, CodeIdentifier name, CodeParameterDirection direction)
			: this(new CodeTypeToken(type), name, direction)
		{
		}

		/// <summary>
		/// Parameter direction.
		/// </summary>
		public CodeParameterDirection Direction { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.Parameter;
	}
}
