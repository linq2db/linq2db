using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlSetExpression : IQueryElement, ISqlExpressionWalkable
	{
		// These are both nullable refs, but by construction either _column or _row is set.
		private ISqlExpression?   _column;
		private ISqlExpression[]? _row;

		public SqlSetExpression(ISqlExpression column, ISqlExpression? expression)
		{
			_column = column;
			Expression  = expression;
			RefineDbParameter(column, expression);
		}

		public SqlSetExpression(ISqlExpression[] row, ISqlExpression? expression)
		{
			Row        = row;
			Expression = expression;

			// The length-checks _should_ never failed thanks to C# type-checking.
			// We do them in case someone attempts to build invalid expressions with unsafe casts or similar.

			if (expression is SelectQuery subquery)
			{
				var columns = subquery.Select.Columns;
				if (columns.Count != row.Length)
					throw new LinqToDBException("Arity of row expression and subquery do not match.");
				for (int i = 0; i < row.Length; i++)				
					RefineDbParameter(row[i], columns[i].Expression);
			}
			else if (expression is SqlRow sqlRow)
			{
				var values = sqlRow.Values;
				if (values.Length != row.Length)
					throw new LinqToDBException("Arity of row expressions do not match.");
				for (int i = 0; i < values.Length; i++)
					RefineDbParameter(values[i], values[i]);
			}
			else if (expression != null)
			{
				throw new ArgumentException("An array of expressions can only be SET to a subquery or row expression", nameof(expression));
			}
		}

		// Most places (e.g. Insert) that use SqlSetExpression don't support the Row variant and access Column directly.
		// In those places, an invalid query that was built with SqlRow will throw LinqToDBException.
		// Codepaths that support Row (e.g. Update) will first check whether the `Row` property below is not null.
		public ISqlExpression Column 
		{
			get => _column ?? throw new LinqToDBException("SqlRow is not supported in this statement");
			set
			{
				if (_row != null) throw new InvalidOperationException("Can't set both Row and Column.");
				_column = value;
			}
		} 
		
		public ISqlExpression[]? Row
		{ 
			get => _row;
			set
			{
				if (_column != null) throw new InvalidOperationException("Can't set both Row and Column.");
				_row = value;
			}
		}

		public ISqlExpression? Expression { get; set; }

		private void RefineDbParameter(ISqlExpression column, ISqlExpression? value)
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
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression? ISqlExpressionWalkable.Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			if (_row is {} fields)
			{
				for (int i = 0; i < fields.Length; i++)
					fields[i] = fields[i].Walk(options, context, func)!;
			}
			else
			{
				_column = _column?.Walk(options, context, func);			
			}
			Expression = Expression?.Walk(options, context, func);
			return null;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.SetExpression;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			if (Row is {} fields)
			{
				sb.Append('(');
				foreach (var f in fields)
					f.ToString(sb, dic).Append(", ");
				if (fields.Length > 0)
					sb.Length -= 2;
				sb.Append(')');
			}
			else
				Column!.ToString(sb, dic);

			sb.Append(" = ");
			Expression?.ToString(sb, dic);

			return sb;
		}

		#endregion
	}
}
