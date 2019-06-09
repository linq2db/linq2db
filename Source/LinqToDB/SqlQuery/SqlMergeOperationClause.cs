using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlMergeOperationClause : IQueryElement, ISqlExpressionWalkable, ICloneableElement
	{
		public SqlMergeOperationClause(MergeOperationType type)
		{
			OperationType = type;
		}

		internal SqlMergeOperationClause(
			MergeOperationType type,
			SqlSearchCondition where,
			SqlSearchCondition whereDelete,
			IEnumerable<SqlSetExpression> items)
		{
			OperationType = type;
			Where = where;
			WhereDelete = whereDelete;

			foreach (var item in items)
				Items.Add(item);
		}

		public MergeOperationType     OperationType { get; }

		public SqlSearchCondition     Where         { get; internal set; }

		public SqlSearchCondition     WhereDelete   { get; internal set; }

		public List<SqlSetExpression> Items         { get; } = new List<SqlSetExpression>();


		#region ISqlExpressionWalkable

		ISqlExpression ISqlExpressionWalkable.Walk(WalkOptions options, Func<ISqlExpression, ISqlExpression> func)
		{
			((ISqlExpressionWalkable)Where)?.Walk(options, func);

			foreach (var t in Items)
				((ISqlExpressionWalkable)t).Walk(options, func);

			((ISqlExpressionWalkable)WhereDelete)?.Walk(options, func);

			return null;
		}

		#endregion

		#region IQueryElement

		QueryElementType IQueryElement.ElementType => QueryElementType.MergeOperationClause;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			switch (OperationType)
			{
				case MergeOperationType.Delete:
					sb.Append("WHEN MATCHED");

					if (Where != null)
					{
						sb.Append(" AND ");
						((IQueryElement)Where).ToString(sb, dic);
					}

					sb.AppendLine(" THEN DELETE");

					break;

				case MergeOperationType.DeleteBySource:
					sb.Append("WHEN NOT MATCHED BY SOURCE");

					if (Where != null)
					{
						sb.Append(" AND ");
						((IQueryElement)Where).ToString(sb, dic);
					}

					sb.AppendLine(" THEN DELETE");

					break;

				case MergeOperationType.Insert:
					sb.Append("WHEN NOT MATCHED");

					if (Where != null)
					{
						sb.Append(" AND ");
						((IQueryElement)Where).ToString(sb, dic);
					}

					sb.AppendLine(" THEN INSERT");

					foreach (var item in Items)
					{
						sb.Append("\t");
						((IQueryElement)item).ToString(sb, dic);
						sb.AppendLine();
					}

					break;

				case MergeOperationType.Update:
					sb.Append("WHEN MATCHED");

					if (Where != null)
					{
						sb.Append(" AND ");
						((IQueryElement)Where).ToString(sb, dic);
					}

					sb.AppendLine(" THEN UPDATE");

					foreach (var item in Items)
					{
						sb.Append("\t");
						((IQueryElement)item).ToString(sb, dic);
						sb.AppendLine();
					}

					break;

				case MergeOperationType.UpdateBySource:
					sb.Append("WHEN NOT MATCHED BY SOURCE");

					if (Where != null)
					{
						sb.Append(" AND ");
						((IQueryElement)Where).ToString(sb, dic);
					}

					sb.AppendLine(" THEN UPDATE");

					foreach (var item in Items)
					{
						sb.Append("\t");
						((IQueryElement)item).ToString(sb, dic);
						sb.AppendLine();
					}

					break;

				case MergeOperationType.UpdateWithDelete:
					sb.Append("WHEN MATCHED THEN UPDATE");

					if (Where != null)
					{
						sb.Append(" WHERE ");
						((IQueryElement)Where).ToString(sb, dic);
					}

					if (WhereDelete != null)
					{
						sb.Append(" DELETE WHERE ");
						((IQueryElement)Where).ToString(sb, dic);
					}

					break;
			}

			return sb;
		}

		#endregion

		#region ICloneableElement

		ICloneableElement ICloneableElement.Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
