namespace LinqToDB.SqlQuery
{
	// TODO: precedence requires total refactoring, as it db-specific
	public sealed class Precedence
	{
		public const int Primary            = 100; // (x) x.y f(x) a[x] x++ x-- new typeof sizeof checked unchecked
		public const int Unary              =  90; // + - ! ++x --x (T)x
		public const int Multiplicative     =  80; // * / %
		public const int Subtraction        =  70; // -
		public const int Additive           =  60; // +
		public const int Comparison         =  50; // ANY ALL SOME EXISTS, IS [NOT], IN, BETWEEN, LIKE, < > <= >=, == !=
		public const int Bitwise            =  40; // ^
		public const int LogicalNegation    =  30; // NOT
		public const int LogicalConjunction =  20; // AND
		public const int LogicalDisjunction =  10; // OR
		/// <summary>
		/// Conservative low-binding precedence for SQL concat (<c>||</c> / <c>CONCAT(...)</c>).
		/// Real per-provider <c>||</c> precedence varies (e.g. SQLite documents it between unary and
		/// multiplicative; Oracle places it at additive level). Setting it below every other operator
		/// forces parentheses around concat chains whenever they're nested in another operator —
		/// defensive over that variance.
		/// </summary>
		public const int Concatenate        =   5; // ||  / CONCAT(...)
		public const int Unknown            =   0;
	}
}
