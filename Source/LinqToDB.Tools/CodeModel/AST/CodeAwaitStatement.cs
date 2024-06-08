using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Await statement.
	/// </summary>
	public sealed class CodeAwaitStatement : ICodeStatement
	{
		public CodeAwaitStatement(ICodeExpression task, IEnumerable<SimpleTrivia>? beforeTrivia, IEnumerable<SimpleTrivia>? afterTrivia)
		{
			Task = task;

			_beforeTrivia = beforeTrivia?.ToList();
			_afterTrivia  = afterTrivia ?.ToList();
		}

		public ICodeExpression Task { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.AwaitStatement;

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
