namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// How a function's result relates to the domain of its argument.
	/// </summary>
	/// <remarks>
	/// Read where a column descriptor is recovered from an expression. The descriptor answers two questions that
	/// come apart here: how the value is read back - its value converter, and the unit a duration is stored in -
	/// and how wide the column that holds it is declared. A result drawn from the argument's own values answers
	/// both, one merely of the same kind answers only the first.
	/// </remarks>
	public enum SqlArgumentDomain
	{
		/// <summary>
		/// The result is unrelated to the argument's domain: <c>COUNT</c> answers how many rows there are and
		/// <c>AVG</c> in a type of its own. Neither the reading nor the width of the argument describes it.
		/// </summary>
		None,

		/// <summary>
		/// The result is a value of the same kind as the argument, but not necessarily one of its values:
		/// <c>SUM</c> reads back the way the argument does and can exceed the width the argument is declared with.
		/// </summary>
		SameKind,

		/// <summary>
		/// The result is one of the argument's own values - <c>MIN</c> and <c>MAX</c> return a row's value
		/// unchanged - so the argument's column describes it completely, width included.
		/// </summary>
		Element,
	}
}
