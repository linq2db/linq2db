using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Method call expression.
	/// </summary>
	public sealed class CodeCallExpression : CodeCallBase, ICodeExpression
	{
		internal CodeCallExpression(
			bool                           extension,
			ICodeExpression                callee,
			CodeIdentifier                 method,
			IReadOnlyList<CodeTypeToken>   genericArguments,
			IReadOnlyList<ICodeExpression> parameters,
			IType                          returnType)
			: base(extension, callee, method, genericArguments, parameters)
		{
			ReturnType = returnType;
		}

		public CodeCallExpression(
			bool                           extension,
			ICodeExpression                callee,
			CodeIdentifier                 method,
			IReadOnlyList<IType>           genericArguments,
			IReadOnlyList<ICodeExpression> parameters,
			IType                          returnType)
			: this(extension, callee, method, genericArguments.Count > 0 ? genericArguments.Select(t => new CodeTypeToken(t)).ToArray() : Array.Empty<CodeTypeToken>(), parameters, returnType)
		{
		}

		public IType ReturnType { get; }

		IType ICodeExpression.Type => ReturnType;

		CodeElementType ICodeElement.ElementType => CodeElementType.CallExpression;
	}
}
