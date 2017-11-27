using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlSetExpression : IQueryElement, ISqlExpressionWalkable, ICloneableElement
	{
		public SqlSetExpression(ISqlExpression column, ISqlExpression expression)
		{
			Column     = column;
			Expression = expression;

			if (expression is SqlParameter p)
			{
				if (column is SqlField field)
				{
					if (field.ColumnDescriptor != null)
					{
						if (field.ColumnDescriptor.DataType != DataType.Undefined && p.DataType == DataType.Undefined)
							p.DataType = field.ColumnDescriptor.DataType;
//							if (field.ColumnDescriptorptor.MapMemberInfo.IsDbTypeSet)
//								p.DbType = field.ColumnDescriptorptor.MapMemberInfo.DbType;
//
//							if (field.ColumnDescriptorptor.MapMemberInfo.IsDbSizeSet)
//								p.DbSize = field.ColumnDescriptor.MapMemberInfo.DbSize;
					}
				}
			}
		}

		public ISqlExpression Column     { get; set; }
		public ISqlExpression Expression { get; set; }

		#region Overrides

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

		#endregion

		#region ICloneableElement Members

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			if (!objectTree.TryGetValue(this, out var clone))
			{
				objectTree.Add(this, clone = new SqlSetExpression(
					(ISqlExpression)Column.    Clone(objectTree, doClone),
					(ISqlExpression)Expression.Clone(objectTree, doClone)));
			}

			return clone;
		}

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
			Column     = Column.    Walk(skipColumns, func);
			Expression = Expression.Walk(skipColumns, func);
			return null;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.SetExpression;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			Column.ToString(sb, dic);
			sb.Append(" = ");
			Expression.ToString(sb, dic);

			return sb;
		}

		#endregion
	}
}
