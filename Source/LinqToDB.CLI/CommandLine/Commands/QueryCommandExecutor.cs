using System;
using System.Threading.Tasks;

namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Query command execution logic.
	/// </summary>
	internal sealed class QueryCommandExecutor
	{
		private readonly ICliEnvironment       _environment;
		private readonly QueryCommandSettings _settings;

		public QueryCommandExecutor(ICliEnvironment environment, QueryCommandSettings settings)
		{
			_environment = environment;
			_settings     = settings;
		}

		public ValueTask<int> Execute()
		{
			_ = _settings;
			_environment.Error.WriteLine("Query command execution is not implemented yet.");
			return new(StatusCodes.EXPECTED_ERROR);
		}
	}

	internal sealed record QueryCommandSettings(
		string  Profile,
		string  Provider,
		string  ConnectionString,
		string  Output,
		string? OutputFile,
		string? Sql,
		string? SqlFile);
}
