using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Method call statement.
	/// </summary>
	public sealed class CodeCallStatement : CodeCallBase, ICodeStatement
	{
		internal CodeCallStatement(
			bool                           extension,
			ICodeExpression                callee,
			CodeIdentifier                 method,
			IReadOnlyList<CodeTypeToken>   genericArguments,
			IReadOnlyList<ICodeExpression> parameters)
			: base(extension, callee, method, genericArguments, parameters)
		{
		}

		public CodeCallStatement(
			bool                           extension,
			ICodeExpression                callee,
			CodeIdentifier                 method,
			IReadOnlyList<IType>           genericArguments,
			IReadOnlyList<ICodeExpression> parameters)
			: base(extension, callee, method, genericArguments.Count > 0 ? genericArguments.Select(t => new CodeTypeToken(t)).ToArray() : Array.Empty<CodeTypeToken>(), parameters)
		{
		}

		CodeElementType ICodeElement.ElementType => CodeElementType.CallStatement;
	}
}
