namespace LinqToDB.DataProvider.Firebird
{
	using LinqToDB.SqlQuery;
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text;

	public partial class FirebirdSqlBuilder
	{
		// source subquery select list shouldn't contain parameters otherwise following error will be
		// generated:
		//
		// FirebirdSql.Data.Common.IscException : Dynamic SQL Error
		// SQL error code = -804
		//Data type unknown

		// VALUES(...) syntax not supported in MERGE source
		protected override bool MergeSupportsSourceDirectValues => false;

		protected override string FakeTable => "rdb$database";

		private readonly ISet<Tuple<SqlValuesTable, int>> _typedColumns = new HashSet<Tuple<SqlValuesTable, int>>();

		protected override bool MergeSourceValueTypeRequired(SqlValuesTable sourceEnumerable, int row, int column)
		{
			if (row == 0)
			{
				// without type Firebird with convert string values in column to CHAR(LENGTH_OF_BIGGEST_VALUE_IN_COLUMN) with
				// padding shorter values with spaces
				if (sourceEnumerable.Rows.Any(r => r[column] is SqlValue value && value.Value is string))
				{
					_typedColumns.Add(Tuple.Create(sourceEnumerable, column));
					return sourceEnumerable.Rows[0][column] is SqlValue val && val.Value != null;
				}

				return false;
			}

			return _typedColumns.Contains(Tuple.Create(sourceEnumerable, column))
				&& sourceEnumerable.Rows[row][column] is SqlValue sqlValue && sqlValue.Value != null;
		}

		protected override void BuildTypedExpression(SqlDataType dataType, ISqlExpression value)
		{
			if (dataType.DbType == null && dataType.DataType == DataType.NVarChar)
			{
				var length = 0;
				var typeRequired = false;
				if (value is SqlValue sqlValue && sqlValue.Value is string stringValue)
				{
					typeRequired = true;
					length = Encoding.UTF8.GetByteCount(stringValue);
					if (length == 0)
						length = 1;
				}

				if (typeRequired)
					StringBuilder.Append("CAST(");

				BuildExpression(value);

				if (typeRequired)
					StringBuilder.Append($" AS VARCHAR({length.ToString(CultureInfo.InvariantCulture)}))");
			}
			else
				base.BuildTypedExpression(dataType, value);
		}
	}
}
