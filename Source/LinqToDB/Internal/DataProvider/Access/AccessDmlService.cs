using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Access
{
	public class AccessDmlService : DmlServiceBase
	{
		/// <summary>
		/// Access DML mechanics. An identity insert is split into the insert (non-query) plus a scalar
		/// <c>SELECT @@IDENTITY</c>; a truncate-with-reset becomes the truncate plus one
		/// <see cref="SqlRawStatement"/> per identity column
		/// (<c>ALTER TABLE … ALTER COLUMN … COUNTER(1, 1)</c>). Everything else uses the base scenario.
		/// </summary>
		public override SqlCommandScenario? BuildCommandScenario(SqlStatement statement, SqlProviderFlags flags, ISqlExpressionFactory factory)
		{
			if (statement is SqlTruncateTableStatement { ResetIdentity: true } truncate && truncate.Table!.IdentityFields.Count > 0)
			{
				var table = truncate.Table!;

				return PerFieldResetScenario(truncate, field => new SqlRawStatement(factory.Fragment(
					"ALTER TABLE {0} ALTER COLUMN {1} COUNTER(1, 1)",
					new SqlObjectNameExpression(table.TableName, ConvertType.NameToQueryTable, table.TableOptions),
					new SqlObjectNameExpression(new SqlObjectName(field.PhysicalName), ConvertType.NameToQueryField))));
			}

			if (statement.NeedsIdentity)
			{
				var idType = factory.GetDbDataType(typeof(long));

				return IdentitySelectScenario(statement, factory.Expression(idType, "@@IDENTITY"));
			}

			return base.BuildCommandScenario(statement, flags, factory);
		}

		// DB_E_NOTABLE — "The specified table does not exist."
		const int DB_E_NOTABLE = unchecked((int)0x80040E37);

		protected override bool IsTableNotFoundExceptionCore(Exception exception)
		{
			// Access via OleDb — HResult is populated correctly.
			if (TypeOrMessageContains(exception, "OleDbException"))
			{
				return HResultMatches(exception, DB_E_NOTABLE)
					|| exception.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase);
			}

			// Access via ODBC — SQLSTATE 42S02 = "Base table or view not found".
			if (TypeOrMessageContains(exception, "OdbcException"))
			{
				return exception.Message.Contains("42S02",          StringComparison.Ordinal)
					|| exception.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase);
			}

			// Access via LibRed — no error number, and the shape differs per statement:
			// SELECT raises SqlBindException, DROP TABLE a plain InvalidOperationException.
			if (TypeOrMessageContains(exception, "SqlBindException"))
				return exception.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase);

			// DROP TABLE says "no such table" up to 11.0.0-alpha.4 and "Table 'X' does not exist." from beta.1;
			// the latter is matched on the table wording only, so a missing database file is not swallowed
			if (TypeOrMessageContains(exception, "InvalidOperationException"))
				return exception.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase)
					|| exception.Message.Contains("no such procedure", StringComparison.OrdinalIgnoreCase)
					|| IsMissingTableMessage(exception.Message);

			return false;
		}

		static bool IsMissingTableMessage(string message)
		{
			var start = message.IndexOf("Table '", StringComparison.Ordinal);

			return start >= 0 && message.IndexOf("' does not exist", start, StringComparison.Ordinal) > start;
		}
	}
}
