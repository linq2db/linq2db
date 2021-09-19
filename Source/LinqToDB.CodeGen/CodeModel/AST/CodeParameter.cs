namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Method parameter.
	/// </summary>
	public sealed class CodeParameter : CodeTypedName, ICodeElement
	{
		internal CodeParameter(CodeTypeToken type, CodeIdentifier name, ParameterDirection direction)
			: base(name, type)
		{
			Direction = direction;
		}

		public CodeParameter(IType type, CodeIdentifier name, ParameterDirection direction)
			: this(new CodeTypeToken(type), name, direction)
		{
		}

		/// <summary>
		/// Parameter direction.
		/// </summary>
		public ParameterDirection Direction { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.Parameter;
	}
}
