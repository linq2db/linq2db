using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Throw statement.
	/// </summary>
	public sealed class CodeThrowStatement : CodeThrowBase, ICodeStatement
	{
		public CodeThrowStatement(ICodeExpression exception, IEnumerable<SimpleTrivia>? beforeTrivia, IEnumerable<SimpleTrivia>? afterTrivia)
			: base(exception)
		{
			_beforeTrivia = beforeTrivia?.ToList();
			_afterTrivia  = afterTrivia ?.ToList();
		}

		CodeElementType ICodeElement.ElementType => CodeElementType.ThrowStatement;

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
