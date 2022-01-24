namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Type reference, used in type-only context.
	/// </summary>
	public sealed class CodeTypeToken : ICodeElement
	{
		public CodeTypeToken(IType type)
		{
			Type = type;
		}

		/// <summary>
		/// Type definition.
		/// </summary>
		public IType Type { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.TypeToken;
	}
}
