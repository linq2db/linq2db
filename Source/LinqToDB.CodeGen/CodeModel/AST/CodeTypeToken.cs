namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Type reference, used in type-only context.
	/// </summary>
	public class CodeTypeToken : ICodeElement
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
