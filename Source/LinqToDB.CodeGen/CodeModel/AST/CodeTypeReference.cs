namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Type, used in expression context.
	/// </summary>
	public sealed class CodeTypeReference : ICodeExpression
	{
		public CodeTypeReference(IType type)
		{
			Type = type;
		}

		/// <summary>
		/// Type definition.
		/// </summary>
		public IType Type { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.TypeReference;
	}
}
