using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Assignment statement.
	/// </summary>
	public sealed class CodeAssignmentStatement : CodeAssignmentBase, ICodeStatement
	{
		public CodeAssignmentStatement(ILValue lvalue, ICodeExpression rvalue, IEnumerable<SimpleTrivia>? beforeTrivia, IEnumerable<SimpleTrivia>? afterTrivia)
			: base(lvalue, rvalue)
		{
			_beforeTrivia = beforeTrivia?.ToList();
			_afterTrivia  = afterTrivia ?.ToList();
		}

		CodeElementType ICodeElement.ElementType => CodeElementType.AssignmentStatement;

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
