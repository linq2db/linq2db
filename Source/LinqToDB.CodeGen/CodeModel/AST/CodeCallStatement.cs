using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Method call statement.
	/// </summary>
	public sealed class CodeCallStatement : CodeCallBase, ICodeStatement
	{
		internal CodeCallStatement(
			bool                         extension,
			ICodeExpression              callee,
			CodeIdentifier               method,
			IEnumerable<CodeTypeToken>   genericArguments,
			IEnumerable<ICodeExpression> parameters)
			: base(extension, callee, method, genericArguments, parameters)
		{
		}

		public CodeCallStatement(
			bool                         extension,
			ICodeExpression              callee,
			CodeIdentifier               method,
			IEnumerable<IType>           genericArguments,
			IEnumerable<ICodeExpression> parameters)
			: base(extension, callee, method, genericArguments.Select(static t => new CodeTypeToken(t)), parameters)
		{
		}

		CodeElementType ICodeElement.ElementType => CodeElementType.CallStatement;
	}
}
