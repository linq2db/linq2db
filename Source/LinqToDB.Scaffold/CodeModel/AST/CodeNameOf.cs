namespace LinqToDB.CodeModel
{
	/// <summary>
	/// <c>nameof(...)</c> expression.
	/// </summary>
	public sealed class CodeNameOf : ICodeExpression
	{
		public CodeNameOf(ICodeExpression expression)
		{
			Expression = expression;
		}

		// TODO: makes sense to introduce new marker interface (e.g. INameOfArgument) to limit allowed expressions
		/// <summary>
		/// <c>nameof</c> argument.
		/// </summary>
		public ICodeExpression Expression { get; }

		IType           ICodeExpression.Type        => WellKnownTypes.System.String;
		CodeElementType ICodeElement   .ElementType => CodeElementType.NameOf;
	}
}
