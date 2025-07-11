using System;
using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	public sealed class SqlMergeOperationClause : QueryElement
	{
		public SqlMergeOperationClause(MergeOperationType type)
		{
			OperationType = type;
		}

		internal SqlMergeOperationClause(
			MergeOperationType            type,
			SqlSearchCondition?           where,
			SqlSearchCondition?           whereDelete,
			IEnumerable<SqlSetExpression> items)
		{
			OperationType = type;
			Where         = where;
			WhereDelete   = whereDelete;

			foreach (var item in items)
				Items.Add(item);
		}

		public MergeOperationType     OperationType { get; }

		public SqlSearchCondition?    Where         { get; internal set; }

		public SqlSearchCondition?    WhereDelete   { get; internal set; }

		public List<SqlSetExpression> Items         { get; } = new List<SqlSetExpression>();

		#region IQueryElement

		public override QueryElementType ElementType => QueryElementType.MergeOperationClause;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			switch (OperationType)
			{
				case MergeOperationType.Delete:
					writer.Append("WHEN MATCHED");

					if (Where != null)
					{
						writer
							.Append(" AND ")
							.AppendElement(Where);
					}

					writer.AppendLine(" THEN DELETE");

					break;

				case MergeOperationType.DeleteBySource:
					writer.Append("WHEN NOT MATCHED BY SOURCE");

					if (Where != null)
					{
						writer
							.Append(" AND ")
							.AppendElement(Where);
					}

					writer.AppendLine(" THEN DELETE");

					break;

				case MergeOperationType.Insert:
					writer.Append("WHEN NOT MATCHED");

					if (Where != null)
					{
						writer.Append(" AND ");
						((IQueryElement)Where).ToString(writer);
					}

					writer.AppendLine(" THEN INSERT");

					using (writer.IndentScope())
						for (var index = 0; index < Items.Count; index++)
						{
							var item = Items[index];
							writer.AppendElement(item);
							if (index < Items.Count - 1)
								writer.AppendLine();
						}

					break;

				case MergeOperationType.Update:
					writer.Append("WHEN MATCHED");

					if (Where != null)
					{
						writer
							.Append(" AND ")
							.AppendElement(Where);
					}

					writer.AppendLine(" THEN UPDATE");

					using (writer.IndentScope())
						for (var index = 0; index < Items.Count; index++)
						{
							var item = Items[index];
							writer.AppendElement(item);
							if (index < Items.Count - 1)
								writer.AppendLine();
						}

					break;

				case MergeOperationType.UpdateBySource:
					writer.Append("WHEN NOT MATCHED BY SOURCE");

					if (Where != null)
					{
						writer
							.Append(" AND ")
							.AppendElement(Where);
					}

					writer.AppendLine(" THEN UPDATE");

					using (writer.IndentScope())
						for (var index = 0; index < Items.Count; index++)
						{
							var item = Items[index];
							writer.AppendElement(item);
							if (index < Items.Count - 1)
								writer.AppendLine();
						}

					break;

				case MergeOperationType.UpdateWithDelete:
					writer.Append("WHEN MATCHED THEN UPDATE");

					if (Where != null)
					{
						writer
							.Append(" WHERE ")
							.AppendElement(Where);
					}

					if (WhereDelete != null)
					{
						writer
							.Append(" DELETE WHERE ")
							.AppendElement(WhereDelete);
					}

					break;
			}

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(OperationType);
			hash.Add(Where?.GetElementHashCode());
			hash.Add(WhereDelete?.GetElementHashCode());

			foreach (var item in Items)
				hash.Add(item.GetElementHashCode());

			return hash.ToHashCode();
		}

		#endregion
	}
}
