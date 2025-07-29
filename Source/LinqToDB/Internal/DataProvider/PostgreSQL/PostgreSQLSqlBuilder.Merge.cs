using System.Collections.Generic;
using System.Linq;

using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.PostgreSQL
{
	partial class PostgreSQLSqlBuilder
	{
		// we enable MERGE in base pgsql builder class intentionally
		// this will allow users to use older dialects with merge at the same time
		// (e.g. to use non-merge insertorreplace implementation)

		protected override bool IsSqlValuesTableValueTypeRequired(SqlValuesTable source,
			IReadOnlyList<List<ISqlExpression>>                                      rows, int row, int column)
		{
			if (row == 0)
			{
				if (rows[0][column] is SqlValue
					{
						Value: long or float or double or decimal
					})
				{
					return true;
				}
			}

			return row < 0
				|| (row == 0
					&& (
						// if column contains NULL in all rows, pgsql will type is as "text"
						rows.All(r => r[column] is SqlValue value && value.Value == null)
						// json(b) should be typed explicitly or it will be typed as text
						|| PostgreSQLSqlExpressionConvertVisitor.IsJson(source.Fields[column].Type, out _)
				));
		}

		// available since PGSQL17
		protected override void BuildMergeOperationDeleteBySource(NullabilityContext nullability, SqlMergeOperationClause operation)
		{
			StringBuilder
				.AppendLine()
				.Append("WHEN NOT MATCHED BY SOURCE");

			if (operation.Where != null)
			{
				StringBuilder.Append(" AND ");
				BuildSearchCondition(Precedence.Unknown, operation.Where, wrapCondition: true);
			}

			StringBuilder.AppendLine(" THEN DELETE");
		}

		// available since PGSQL17
		protected override void BuildMergeOperationUpdateBySource(NullabilityContext nullability, SqlMergeOperationClause operation)
		{
			StringBuilder
				.AppendLine()
				.Append("WHEN NOT MATCHED BY SOURCE");

			if (operation.Where != null)
			{
				StringBuilder.Append(" AND ");
				BuildSearchCondition(Precedence.Unknown, operation.Where, wrapCondition: true);
			}

			StringBuilder.AppendLine(" THEN UPDATE");

			var update = new SqlUpdateClause();
			update.Items.AddRange(operation.Items);
			BuildUpdateSet(null, update);
		}
	}
}
