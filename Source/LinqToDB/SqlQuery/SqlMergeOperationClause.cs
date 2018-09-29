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

		public List<SqlSetExpression> Items { get; } = new List<SqlSetExpression>();

		public SqlSearchCondition Where { get; } = new SqlSearchCondition();

		public SqlSearchCondition WhereDelete { get; } = new SqlSearchCondition();

		public MergeOperationType OperationType { get; }

		QueryElementType IQueryElement.ElementType => throw new NotImplementedException();

		ICloneableElement ICloneableElement.Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			throw new NotImplementedException();
		}

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

					sb.AppendLine("THEN DELETE");

					break;
				case MergeOperationType.DeleteBySource:
					sb.Append("WHEN NOT MATCHED BY SOURCE");
					if (Where != null)
					{
						sb.Append(" AND ");
						((IQueryElement)Where).ToString(sb, dic);
					}

					sb.AppendLine("THEN DELETE");

					break;
				case MergeOperationType.Insert:
					sb.Append("WHEN NOT MATCHED");
					if (Where != null)
					{
						sb.Append(" AND ");
						((IQueryElement)Where).ToString(sb, dic);
					}

					sb.AppendLine("THEN INSERT");
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

					sb.AppendLine("THEN UPDATE");
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

					sb.AppendLine("THEN UPDATE");
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

		ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func)
		{
			((ISqlExpressionWalkable)Where)?.Walk(skipColumns, func);

			foreach (var t in Items)
				((ISqlExpressionWalkable)t).Walk(skipColumns, func);

			((ISqlExpressionWalkable)WhereDelete)?.Walk(skipColumns, func);

			return null;
		}
	}
}
