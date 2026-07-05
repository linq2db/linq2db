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
	/// <param name="Output">Query output format.</param>
	/// <param name="OutputFile">Optional output file path. When not specified, output is written to stdout.</param>
	/// <param name="Sql">SQL query text from command line.</param>
	/// <param name="SqlFile">Path to command-line SQL file.</param>
	internal sealed record QueryCommandSettings(
		string  Profile,
		string  Provider,
		string  ConnectionString,
		string  Output,
		string? OutputFile,
		string? Sql,
		string? SqlFile);
}
