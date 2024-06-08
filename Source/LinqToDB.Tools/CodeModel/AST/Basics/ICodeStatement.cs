using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Marker interface for statement nodes.
	/// </summary>
	public interface ICodeStatement : ICodeElement
	{
		public IReadOnlyList<SimpleTrivia>? Before { get; }
		public IReadOnlyList<SimpleTrivia>? After  { get; }

		void AddSimpleTrivia(SimpleTrivia trivia, bool after);
	}
}
