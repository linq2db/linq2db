using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlSetExpression : IQueryElement, ISqlExpressionWalkable
	{
		private ISqlExpression?   column;
		private ISqlExpression[]? row;

		public SqlSetExpression(ISqlExpression column, ISqlExpression? expression)
		{
			this.column = column;
			Expression  = expression;

			if (expression is SqlParameter p)
			{
				if (column is SqlField field)
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
		}

		public SqlSetExpression(ISqlExpression[] row, ISqlExpression? expression)
		{
			Row        = row;
			Expression = expression;
			// TODO SqlRow: set parameters types inside the SqlRow expression...
		}

		// Most places (e.g. Insert) that use SqlSetExpression don't support the Row variant and access Column directly.
		// In those places, an invalid query that was built with SqlRow will throw LinqToDBException.
		public ISqlExpression Column 
		{
			get => this.column ?? throw new LinqToDBException("SET (SqlRow) not supported here.");
			set
			{
				if (this.row != null) throw new InvalidOperationException("Can't set both Row and Column.");
				this.column = value;
			}
		} 

		// Codepaths (e.g. Update) that support Row will first check whether this property is not null.
		public ISqlExpression[]? Row
		{ 
			get => this.row;
			set
			{
				if (this.column != null) throw new InvalidOperationException("Can't set both Row and Column.");
				this.row = value;
			}
		}

		public ISqlExpression? Expression { get; set; }

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
			if (row is {} fields)
			{
				for (int i = 0; i < fields.Length; i++)
					fields[i] = fields[i].Walk(options, context, func)!;
			}
			else
			{
				column = column?.Walk(options, context, func);			
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
				Column.ToString(sb, dic);

			sb.Append(" = ");
			Expression?.ToString(sb, dic);

			return sb;
		}

		#endregion
	}
}
