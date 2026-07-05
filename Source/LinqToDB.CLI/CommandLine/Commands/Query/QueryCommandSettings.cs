namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Fully resolved query command settings passed to execution logic.
	/// </summary>
	/// <param name="Profile">Selected configuration profile name.</param>
	/// <param name="Provider">linq2db provider name.</param>
	/// <param name="ConnectionString">
	/// Final database connection string after command line/profile merge and user/password formatting.
	/// </param>
	/// <param name="CommandTimeout">Optional query command timeout in seconds.</param>
	/// <param name="LockTimeout">Optional provider-specific lock wait timeout in seconds.</param>
	/// <param name="MaxRows">Maximum number of result rows to read.</param>
	/// <param name="Output">Query output format.</param>
	/// <param name="OutputFile">Optional output file path. When not specified, output is written to stdout.</param>
	/// <param name="Overwrite">Allow replacing existing output file.</param>
	/// <param name="SqlSafety">Unsafe SQL execution policy resolved from configuration profiles.</param>
	/// <param name="AllowUnsafeSql">Command-line confirmation for unsafe SQL execution.</param>
	/// <param name="Sql">SQL query text from command line.</param>
	/// <param name="SqlFile">Path to command-line SQL file.</param>
	internal sealed record QueryCommandSettings(
		string             Profile,
		string             Provider,
		string             ConnectionString,
		int?               CommandTimeout,
		int?               LockTimeout,
		int                MaxRows,
		string             Output,
		string?            OutputFile,
		bool               Overwrite,
		QuerySqlSafetyMode SqlSafety,
		bool               AllowUnsafeSql,
		string?            Sql,
		string?            SqlFile);
}
