using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LinqToDB.DataProvider.Firebird
{
	using SqlQuery;
	using Common;

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

		private readonly ISet<Tuple<SqlValuesTable, int>> _typedColumns = new HashSet<Tuple<SqlValuesTable, int>>();

		protected override bool IsSqlValuesTableValueTypeRequired(SqlValuesTable source, IReadOnlyList<ISqlExpression[]> rows, int row, int column)
		{
			if (row >= 0 && ConvertElement(rows[row][column]) is SqlParameter parameter && parameter.IsQueryParameter)
			{
				return true;
			}

			if (row == 0)
			{
				// without type Firebird with convert string values in column to CHAR(LENGTH_OF_BIGGEST_VALUE_IN_COLUMN) with
				// padding shorter values with spaces
				if (rows.Any(r => ConvertElement(r[column]) is SqlValue value && value.Value is string))
				{
					_typedColumns.Add(Tuple.Create(source, column));
					return rows[0][column] is SqlValue val && val.Value != null;
				}

				return false;
			}

			return _typedColumns.Contains(Tuple.Create(source, column))
				&& ConvertElement(rows[row][column]) is SqlValue sqlValue && sqlValue.Value != null;
		}

		// FB 2.5 need to use small values to avoid error due to bad row size calculation
		// resulting it being bigger than that limit (64Kb)
		// limit is the same for newer versions, but only FB 2.5 fails
		protected virtual int NullCharSize => 1;

		protected override void BuildTypedExpression(DbDataType dataType, ISqlExpression value)
		{
			if (dataType.DbType == null && (dataType.DataType == DataType.NVarChar || dataType.DataType == DataType.NChar))
			{
				object? providerValue = null;
				var     typeRequired  = false;

				switch (value)
				{
					case SqlValue sqlValue:
						providerValue = sqlValue.Value;
						break;
					case SqlParameter param:
					{
						typeRequired = true;
						var paramValue = param.GetParameterValue(OptimizationContext.EvaluationContext.ParameterValues);
						providerValue = paramValue.ProviderValue;
						break;
					}
				}

				var length = providerValue switch
				{
					string strValue => Encoding.UTF8.GetByteCount(strValue),
					char charValue => Encoding.UTF8.GetByteCount(new[] { charValue }),
					_ => -1
				};

				if (length == 0)
					length = 1;

				typeRequired = typeRequired || length > 0;

				if (typeRequired && length < 0)
				{
					length = NullCharSize;
				}

				if (typeRequired)
					StringBuilder.Append("CAST(");

				BuildExpression(value);

				if (typeRequired)
				{
					if (dataType.DataType  == DataType.NChar)
						StringBuilder.Append(CultureInfo.InvariantCulture, $" AS CHAR({length}))");
					else
						StringBuilder.Append(CultureInfo.InvariantCulture, $" AS VARCHAR({length}))");
				}
			}
			else
				base.BuildTypedExpression(dataType, value);
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
