using System.IO;
using System.Linq;

using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace LinqToDB.CommandLine
{
	internal static class SqlServerReadOnlySqlGuard
	{
		public static SqlGuardResult Validate(string sql)
		{
			var scriptResult = ParseScript(sql, out var script);
			if (!scriptResult.IsAllowed)
				return scriptResult;

			var singleStatementResult = ValidateSingleStatement(script!, out var statements);
			if (!singleStatementResult.IsAllowed)
				return singleStatementResult;

			if (statements[0] is ExecuteStatement)
				return SqlGuardResult.Rejected("Query is not read-only: EXECUTE is not allowed.");

			if (statements[0] is not SelectStatement selectStatement)
				return SqlGuardResult.Rejected($"Query is not read-only: {statements[0].GetType().Name} is not allowed.");

			if (HasSelectInto(selectStatement))
				return SqlGuardResult.Rejected("Query is not read-only: SELECT INTO is not allowed.");

			return SqlGuardResult.Allowed;
		}

		public static SqlGuardResult ValidateSingleStatement(string sql)
		{
			var scriptResult = ParseScript(sql, out var script);
			if (!scriptResult.IsAllowed)
				return scriptResult;

			return ValidateSingleStatement(script!, out _);
		}

		private static SqlGuardResult ParseScript(string sql, out TSqlScript? script)
		{
			var parser = new TSql180Parser(false);
			var fragment = parser.Parse(new StringReader(sql), out var errors);

			if (errors.Count > 0)
			{
				script = null;
				return SqlGuardResult.Rejected($"Query is not valid T-SQL: {errors[0].Message}");
			}

			if (fragment is not TSqlScript parsedScript)
			{
				script = null;
				return SqlGuardResult.Rejected("Query is not valid T-SQL.");
			}

			script = parsedScript;
			return SqlGuardResult.Allowed;
		}

		private static SqlGuardResult ValidateSingleStatement(TSqlScript script, out TSqlStatement[] statements)
		{
			statements = script.Batches.SelectMany(batch => batch.Statements).ToArray();

			if (statements.Length != 1)
				return SqlGuardResult.Rejected("Only single SQL statement is allowed.");

			return SqlGuardResult.Allowed;
		}

		private static bool HasSelectInto(SelectStatement selectStatement)
		{
			return selectStatement.ScriptTokenStream
				.Skip(selectStatement.FirstTokenIndex)
				.Take(selectStatement.LastTokenIndex - selectStatement.FirstTokenIndex + 1)
				.Any(token => token.TokenType == TSqlTokenType.Into);
		}
	}
}
