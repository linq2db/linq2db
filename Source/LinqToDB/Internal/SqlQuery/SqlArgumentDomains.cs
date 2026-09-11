namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// The domain an aggregate's result draws from, by the name the function is built with.
	/// </summary>
	/// <remarks>
	/// Shared because the same three names are built on two surfaces - an ordinary aggregate and a window function -
	/// and a name that answered differently depending on which asked would put two domains on one function. That is
	/// not merely untidy: <see cref="SqlExtendedFunction"/> compares its domain, so two nodes alike in everything
	/// else would stop being interchangeable.
	/// </remarks>
	public static class SqlArgumentDomains
	{
		/// <summary>
		/// The domain for <paramref name="functionName"/>, or <see cref="SqlArgumentDomain.None"/> where the
		/// argument does not describe the result.
		/// </summary>
		/// <param name="functionName">Name the function is built with.</param>
		public static SqlArgumentDomain ForAggregate(string functionName)
		{
			return functionName switch
			{
				"MIN" or "MAX" => SqlArgumentDomain.Element,
				"SUM"          => SqlArgumentDomain.SameKind,
				_              => SqlArgumentDomain.None,
			};
		}
	}
}
