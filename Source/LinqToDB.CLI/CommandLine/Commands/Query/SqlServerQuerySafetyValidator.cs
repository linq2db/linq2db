using System.IO;
using System.Linq;

using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace LinqToDB.CommandLine
{
	internal static class SqlServerQuerySafetyValidator
	{
		public static QuerySafetyResult Validate(string sql)
		{
			var parser = new TSql160Parser(false);
			var fragment = parser.Parse(new StringReader(sql), out var errors);

			if (errors.Count > 0)
				return QuerySafetyResult.Unsafe($"Query is not valid T-SQL: {errors[0].Message}");

			if (fragment is not TSqlScript script)
				return QuerySafetyResult.Unsafe("Query is not valid T-SQL.");

			var statements = script.Batches.SelectMany(batch => batch.Statements).ToArray();

			if (statements.Length != 1)
				return QuerySafetyResult.Unsafe("Only single SELECT statement is allowed.");

			if (statements[0] is ExecuteStatement)
				return QuerySafetyResult.Unsafe("Query is not read-only: EXECUTE is not allowed.");

			if (statements[0] is not SelectStatement)
				return QuerySafetyResult.Unsafe($"Query is not read-only: {statements[0].GetType().Name} is not allowed.");

			return GenericQuerySafetyValidator.Validate(sql);
		}
	}
}
