using System.Collections.Generic;
using System.Linq;

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
			IEnumerable<SimpleTrivia>?   wrapTrivia,
			IType                        returnType)
			: base(extension, callee, method, genericArguments, skipTypeArguments, parameters, wrapTrivia)
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
			IEnumerable<SimpleTrivia>?   wrapTrivia,
			IType                        returnType)
			: this(extension, callee, method, genericArguments.Select(static t => new CodeTypeToken(t)), skipTypeArguments, parameters, wrapTrivia, returnType)
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
