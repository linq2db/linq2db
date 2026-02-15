using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Firebird
{
	public partial class FirebirdSqlBuilder
	{
		// source subquery select list shouldn't contain parameters otherwise following error will be
		// generated:
		//
		// FirebirdSql.Data.Common.IscException : Dynamic SQL Error
		// SQL error code = -804
		//Data type unknown

		// VALUES(...) syntax not supported in MERGE source
		protected override bool IsValuesSyntaxSupported => false;

		protected override string FakeTable => "rdb$database";

		private readonly HashSet<(SqlValuesTable, int)> _typedColumns = [];

		protected override bool IsSqlValuesTableValueTypeRequired(SqlValuesTable source, IReadOnlyList<List<ISqlExpression>> rows, int row, int column)
		{
			if (row >= 0 && ConvertElement(rows[row][column]) is SqlParameter parameter && parameter.IsQueryParameter)
			{
				return true;
			}

			if (row == 0)
			{
				// without type Firebird with convert string values in column to CHAR(LENGTH_OF_BIGGEST_VALUE_IN_COLUMN) with
				// padding shorter values with spaces
				if (rows.Any(r => ConvertElement(r[column]) is SqlValue { Value: string }))
				{
					_typedColumns.Add((source, column));
					return rows[0][column] is SqlValue { Value: { } };
				}

				return rows[0][column] is SqlValue
				{
					Value: uint or long or ulong or float or double or decimal or null,
				};
			}

			return _typedColumns.Contains((source, column))
				&& ConvertElement(rows[row][column]) is SqlValue { Value: { } };
		}

		// available since FB5
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

		// available since FB5
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
