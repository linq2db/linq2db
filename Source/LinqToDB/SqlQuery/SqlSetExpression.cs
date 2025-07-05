using System;

namespace LinqToDB.SqlQuery
{
	public class SqlSetExpression : IQueryElement
	{
		// These are both nullable refs, but by construction either _column or _row is set.

		public SqlSetExpression(ISqlExpression column, ISqlExpression? expression)
		{
			Column    = column;
			Expression = expression;

			ValidateColumnExpression(column, expression);
		}

		private void ValidateColumnExpression(ISqlExpression column, ISqlExpression? expression)
		{
			if (column is SqlRowExpression row)
			{
				// The length-checks _should_ never failed thanks to C# type-checking.
				// We do them in case someone attempts to build invalid expressions with unsafe casts or similar.

				if (expression is SelectQuery subquery)
				{
					var columns = subquery.Select.Columns;
					if (columns.Count != row.Values.Length)
						throw new LinqToDBException("Arity of row expression and subquery do not match.");
					for (int i = 0; i < row.Values.Length; i++)
						RefineDbParameter(row.Values[i], columns[i].Expression);
				}
				else if (expression is SqlRowExpression sqlRow)
				{
					var values = sqlRow.Values;
					if (values.Length != row.Values.Length)
						throw new LinqToDBException("Arity of row expressions do not match.");
					for (int i = 0; i < values.Length; i++)
						RefineDbParameter(values[i], values[i]);
				}
				else if (expression != null)
				{
					//throw new ArgumentException("An array of expressions can only be SET to a subquery or row expression", nameof(expression));
				}
			}
			else
			{
				RefineDbParameter(column, expression);
			}
		}

		public ISqlExpression  Column     { get; set; }
		public ISqlExpression? Expression { get; set; }

		private static void RefineDbParameter(ISqlExpression column, ISqlExpression? value)
		{
			if (value is SqlParameter p && column is SqlField field)
			{
				if (field.ColumnDescriptor != null && p.Type.SystemType != typeof(object))
				{
					if (field.ColumnDescriptor.DataType  != DataType.Undefined && p.Type.DataType == DataType.Undefined)
						p.Type = p.Type.WithDataType(field.ColumnDescriptor.DataType);

					if (field.ColumnDescriptor.DbType    != null && p.Type.DbType == null)
						p.Type = p.Type.WithDbType(field.ColumnDescriptor.DbType);
					if (field.ColumnDescriptor.Length    != null && p.Type.Length == null)
						p.Type = p.Type.WithLength(field.ColumnDescriptor.Length);
					if (field.ColumnDescriptor.Precision != null && p.Type.Precision == null)
						p.Type = p.Type.WithPrecision(field.ColumnDescriptor.Precision);
					if (field.ColumnDescriptor.Scale     != null && p.Type.Scale == null)
						p.Type = p.Type.WithScale(field.ColumnDescriptor.Scale);
				}
			}
		}

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

		#endregion

		#region IQueryElement Members

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif
		public QueryElementType ElementType => QueryElementType.SetExpression;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
		{
			writer
				.AppendElement(Column)
				.Append(" = ")
				.AppendElement(Expression);

			return writer;
		}

		public int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(Column.GetElementHashCode());
			hash.Add(Expression?.GetElementHashCode());
			return hash.ToHashCode();
		}

		#endregion
	}
}
