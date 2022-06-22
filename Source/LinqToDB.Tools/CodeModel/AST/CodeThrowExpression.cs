namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Throw expression.
	/// </summary>
	public sealed class CodeThrowExpression : CodeThrowBase, ICodeExpression
	{
		private readonly IType _targetType;

		public CodeThrowExpression(ICodeExpression exception, IType targetType)
			: base(exception)
		{
			_targetType = targetType;
		}

		IType           ICodeExpression.Type        => _targetType;
		CodeElementType ICodeElement   .ElementType => CodeElementType.ThrowExpression;
	}
}
