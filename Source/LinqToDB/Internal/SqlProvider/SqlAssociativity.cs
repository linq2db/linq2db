namespace LinqToDB.Internal.SqlProvider
{
	/// <summary>
	/// How an operator groups operands of its own level.
	/// </summary>
	public enum SqlAssociativity
	{
		/// <summary>Evaluated left to right: an equal-level left operand needs no parentheses.</summary>
		Left,
		/// <summary>Not associative: an equal-level operand on either side is parenthesized.</summary>
		None,
	}
}
