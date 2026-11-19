using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.SqlProvider
{
	/// <summary>
	/// Precedence of one rendered SQL operator in one dialect.
	/// </summary>
	/// <param name="Level">Binding strength on the <see cref="Precedence"/> scale; higher binds tighter.</param>
	/// <param name="Associativity">How equal-level operands group.</param>
	/// <param name="Associative">Whether a right operand that is the same operator may stay bare (<c>a AND (b AND c)</c>).</param>
	/// <param name="Groups">
	/// Ordering groups: two entries are ordered relative to each other only when they share a group, or are the same
	/// operator. An entry unordered with its parent is always parenthesized.
	/// </param>
	public readonly record struct SqlOperatorPrecedence(int Level, SqlAssociativity Associativity, bool Associative, int Groups)
	{
		/// <summary>An operand that is never parenthesized: a column, a literal, a function call.</summary>
		public static readonly SqlOperatorPrecedence Primary = new(Precedence.Primary, SqlAssociativity.Left, false, SqlPrecedenceTable.AllGroups);

		/// <summary>An operand of unknown shape: always parenthesized, and it wraps every non-primary operand.</summary>
		public static readonly SqlOperatorPrecedence Unknown = new(Precedence.Unknown, SqlAssociativity.None, false, 0);
	}
}
