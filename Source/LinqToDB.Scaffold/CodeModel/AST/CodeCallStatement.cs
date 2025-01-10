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
			bool                         skipTypeArguments,
			IEnumerable<ICodeExpression> parameters,
			IEnumerable<SimpleTrivia>?   wrapTrivia,
			IEnumerable<SimpleTrivia>?   beforeTrivia,
			IEnumerable<SimpleTrivia>?   afterTrivia)
			: base(extension, callee, method, genericArguments, skipTypeArguments, parameters, wrapTrivia)
		{
			_beforeTrivia = beforeTrivia?.ToList();
			_afterTrivia  = afterTrivia ?.ToList();
		}

		public CodeCallStatement(
			bool                         extension,
			ICodeExpression              callee,
			CodeIdentifier               method,
			IEnumerable<IType>           genericArguments,
			bool                         skipTypeArguments,
			IEnumerable<ICodeExpression> parameters,
			IEnumerable<SimpleTrivia>?   wrapTrivia)
			: base(extension, callee, method, genericArguments.Select(static t => new CodeTypeToken(t)), skipTypeArguments, parameters, wrapTrivia)
		{
		}

		CodeElementType ICodeElement.ElementType => CodeElementType.CallStatement;

		private List<SimpleTrivia>? _beforeTrivia;
		private List<SimpleTrivia>? _afterTrivia;
		IReadOnlyList<SimpleTrivia>? ICodeStatement.Before => _beforeTrivia;
		IReadOnlyList<SimpleTrivia>? ICodeStatement.After  => _afterTrivia;

		void ICodeStatement.AddSimpleTrivia(SimpleTrivia trivia, bool after)
		{
			if (after)
				(_afterTrivia  ??= new()).Add(trivia);
			else
				(_beforeTrivia ??= new()).Add(trivia);
		}
	}
}
