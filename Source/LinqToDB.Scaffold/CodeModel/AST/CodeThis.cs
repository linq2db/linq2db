namespace LinqToDB.CodeModel
{
	/// <summary>
	/// <see langword="this"/> reference.
	/// </summary>
	public sealed class CodeThis : ICodeExpression
	{
		public CodeThis(CodeClass @class)
		{
			Class = @class;
		}

		public CodeClass Class { get; }

		IType           ICodeExpression.Type        => Class.Type;
		CodeElementType ICodeElement   .ElementType => CodeElementType.This;
	}
}
