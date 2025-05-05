namespace LinqToDB.CodeModel
{
	/// <summary>
	/// <c>this</c> reference.
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
