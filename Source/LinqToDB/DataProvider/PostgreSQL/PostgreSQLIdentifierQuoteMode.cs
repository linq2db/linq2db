namespace LinqToDB.DataProvider.PostgreSQL
{
	/// <summary>
	/// Identifier quotation logic for SQL generation.
	/// </summary>
	public enum PostgreSQLIdentifierQuoteMode
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
		/// Quote identifiers only when it is required according to PostgreSQL identifier quotation rules:
		/// <list type="bullet">
		/// <item>identifier use reserved word</item>
		/// <item>identifier contains whitespace character(s)</item>
		/// </list>
		/// </summary>
		Needed,
		/// <summary>
		/// Quote identifiers only when it is required according to PostgreSQL identifier quotation rules:
		/// <list type="bullet">
		/// <item>identifier use reserved word</item>
		/// <item>identifier constains whitespace character(s)</item>
		/// </list>
		/// or when identifier starts with upper-case letter.
		/// </summary>
		Auto
	}
}
