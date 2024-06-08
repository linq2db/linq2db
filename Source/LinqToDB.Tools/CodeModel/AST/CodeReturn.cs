using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Return statement.
	/// </summary>
	public sealed class CodeReturn : ICodeStatement
	{
		public CodeReturn(ICodeExpression? expression, IEnumerable<SimpleTrivia>? beforeTrivia, IEnumerable<SimpleTrivia>? afterTrivia)
		{
			Expression = expression;

			_beforeTrivia = beforeTrivia?.ToList();
			_afterTrivia  = afterTrivia ?.ToList();
		}

		/// <summary>
		/// Optional return value.
		/// </summary>
		public ICodeExpression? Expression { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.ReturnStatement;

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
