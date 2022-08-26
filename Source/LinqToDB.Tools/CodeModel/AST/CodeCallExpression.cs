namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Method call expression.
	/// </summary>
	public sealed class CodeCallExpression : CodeCallBase, ICodeExpression
	{
		internal CodeCallExpression(
			bool                         extension,
			ICodeExpression              callee,
			CodeIdentifier               method,
			IEnumerable<CodeTypeToken>   genericArguments,
			bool                         skipTypeArguments,
			IEnumerable<ICodeExpression> parameters,
			IType                        returnType)
			: base(extension, callee, method, genericArguments, skipTypeArguments, parameters)
		{
			ReturnType = returnType;
		}

		public CodeCallExpression(
			bool                         extension,
			ICodeExpression              callee,
			CodeIdentifier               method,
			IEnumerable<IType>           genericArguments,
			bool                         skipTypeArguments,
			IEnumerable<ICodeExpression> parameters,
			IType                        returnType)
			: this(extension, callee, method, genericArguments.Select(static t => new CodeTypeToken(t)), skipTypeArguments, parameters, returnType)
		{
		}

		/// <summary>
		/// Gets return type of call expression.
		/// </summary>
		public IType ReturnType { get; }

		IType           ICodeExpression.Type        => ReturnType;
		CodeElementType ICodeElement   .ElementType => CodeElementType.CallExpression;
	}
}
