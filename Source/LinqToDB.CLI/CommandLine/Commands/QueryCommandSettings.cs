namespace LinqToDB.CommandLine
{
	internal sealed record QueryCommandSettings(
		string  Profile,
		string  Provider,
		string  ConnectionString,
		string  Output,
		string? OutputFile,
		string? Sql,
		string? SqlFile);
}
