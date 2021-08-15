namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Method parameter.
	/// </summary>
	public class CodeParameter : ICodeElement
	{
		public CodeParameter(IType? type, CodeIdentifier name, ParameterDirection direction)
		{
			Type      = type == null ? null : new (type);
			Name      = name;
			Direction = direction;
		}

		/// <summary>
		/// Parameter type. Could be missing for lambda-methods.
		/// </summary>
		public CodeTypeToken?     Type      { get; }
		/// <summary>
		/// Parameter name.
		/// </summary>
		public CodeIdentifier     Name      { get; }
		/// <summary>
		/// Parameter direction.
		/// </summary>
		public ParameterDirection Direction { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.Parameter;
	}

}
