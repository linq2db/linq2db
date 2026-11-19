using System.Globalization;

using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.SqlProvider
{
	/// <summary>
	/// Keys of the non-binary entries of a <see cref="SqlPrecedenceTable"/>. A binary operator is keyed by the text
	/// <see cref="Internal.SqlQuery.SqlBinaryExpression.Operation"/> renders.
	/// </summary>
	public static class SqlPrecedenceKeys
	{
		/// <summary>Prefix minus.</summary>
		public const string Negation        = "unary -";
		/// <summary>Prefix bitwise not.</summary>
		public const string BitwiseNegation = "unary ~";
		/// <summary>A literal or inlined value whose text starts with a minus sign.</summary>
		public const string NegativeLiteral = "negative literal";
		/// <summary>A string concatenation rendered as an operator chain.</summary>
		public const string Concatenation   = "concat";
		/// <summary>A typed expression (cast).</summary>
		public const string Cast            = "cast";
		/// <summary>A power function rendered as an infix operator.</summary>
		public const string Power           = "power";

		/// <summary>
		/// Operands of <c>BETWEEN</c>. Other predicate operands resolve through the predicate's
		/// <see cref="Precedence"/> class (<see cref="Class"/>).
		/// </summary>
		public const string Between         = "predicate between";

		/// <summary>Key of the template class a declared <see cref="Precedence"/> constant maps to.</summary>
		public static string Class(int precedence) => "class " + precedence.ToString(CultureInfo.InvariantCulture);
	}
}
