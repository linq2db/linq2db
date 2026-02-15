namespace LinqToDB.DataProvider.DB2
{
	/// <summary>
	/// Identifier quotation logic for SQL generation.
	/// </summary>
	public enum DB2IdentifierQuoteMode
	{
		/// <summary>
		/// Never quote identifiers.
		/// </summary>
		None,
		/// <summary>
		/// Allways quote identifiers.
		/// </summary>
		Quote,
		/// <summary>
		/// Quote identifiers only when it is required according to DB2 identifier quotation rules:
		/// <list type="bullet">
		/// <item>identifier starts with underscore: <c>'_'</c></item>
		/// <item>identifier contains whitespace character(s)</item>
		/// <item>identifier contains lowercase letter(s)</item>
		/// </list>
		/// </summary>
		Auto,
	}
}
