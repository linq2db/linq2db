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
		/// <see cref="SqlFragmentStatement"/> per identity column
		/// (<c>ALTER TABLE … ALTER COLUMN … COUNTER(1, 1)</c>). Everything else uses the base scenario.
		/// </summary>
		public override SqlCommandScenario? BuildCommandScenario(SqlStatement statement, SqlProviderFlags flags, ISqlExpressionFactory factory)
		{
			if (statement is SqlTruncateTableStatement { ResetIdentity: true } truncate && truncate.Table!.IdentityFields.Count > 0)
			{
				var table = truncate.Table!;

				return PerFieldResetScenario(truncate, field => new SqlFragmentStatement(factory.Fragment(
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

			return false;
		}
	}
}
