﻿using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlSetExpression : IQueryElement, ISqlExpressionWalkable
	{
		public SqlSetExpression(ISqlExpression column, ISqlExpression? expression)
		{
			Column     = column;
			Expression = expression;

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

		public ISqlExpression  Column     { get; set; }
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

		ISqlExpression? ISqlExpressionWalkable.Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
		{
			Column     = Column.     Walk(options, func)!;
			Expression = Expression?.Walk(options, func);
			return null;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.SetExpression;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			Column.ToString(sb, dic);
			sb.Append(" = ");
			Expression?.ToString(sb, dic);

			return sb;
		}

		#endregion
	}
}
