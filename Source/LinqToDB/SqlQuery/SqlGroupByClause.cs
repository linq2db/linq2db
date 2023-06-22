using System;
using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	public enum GroupingType
	{
		Default,
		GroupBySets,
		Rollup,
		Cube
	}

	public class SqlGroupByClause : ClauseBase, IQueryElement, ISqlExpressionWalkable
	{

		internal SqlGroupByClause(SelectQuery selectQuery) : base(selectQuery)
		{
		}

		internal SqlGroupByClause(GroupingType groupingType, IEnumerable<ISqlExpression> items) : base(null)
		{
			GroupingType = groupingType;
			Items.AddRange(items);
		}

		public SqlGroupByClause Expr(ISqlExpression expr)
		{
			Add(expr);
			return this;
		}

		public SqlGroupByClause Field(SqlField field)
		{
			return Expr(field);
		}

		void Add(ISqlExpression expr)
		{
			foreach (var e in Items)
				if (e.Equals(expr))
					return;

			Items.Add(expr);
		}

		public GroupingType GroupingType  { get; set; } = GroupingType.Default;

		// Note: List is used in Visitor to modify elements, by replacing List by ReadOnly collection,
		// Visitor should be corrected and appropriate Modify function updated.
		public List<ISqlExpression> Items { get; } = new List<ISqlExpression>();

		public bool IsEmpty => Items.Count == 0;

		public IEnumerable<ISqlExpression> EnumItems()
		{
			foreach (var item in Items)
			{
				if (item is SqlGroupingSet groupingSet)
				{
					foreach (var gropingSetItem in groupingSet.Items)
					{
						yield return gropingSetItem;
					}
				}
				else
				{
					yield return item;
				}
			}
		}

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString(SelectQuery);
		}

#endif

		#region ISqlExpressionWalkable Members

		ISqlExpression? ISqlExpressionWalkable.Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			for (var i = 0; i < Items.Count; i++)
				Items[i] = Items[i].Walk(options, context, func)!;

			return null;
		}

		#endregion

		#region IQueryElement Members

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif
		public QueryElementType ElementType => QueryElementType.GroupByClause;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
		{
			if (Items.Count == 0)
				return writer;

			writer
				.AppendLine()
				.AppendLine(" GROUP BY");

			switch (GroupingType)
			{
				case GroupingType.Default:
					break;
				case GroupingType.GroupBySets:
					writer.AppendLine(" GROUPING SETS (");
					break;
				case GroupingType.Rollup:
					writer.AppendLine(" ROLLUP (");
					break;
				case GroupingType.Cube:
					writer.AppendLine(" CUBE (");
					break;
				default:
					throw new InvalidOperationException($"Unexpected grouping type: {GroupingType}");
			}

			using(writer.WithScope())
			{
				for (var index = 0; index < Items.Count; index++)
				{
					var item = Items[index];
					writer.AppendElement(item);
					if (index < Items.Count - 1)
						writer.AppendLine(',');
				}
			}


			if (GroupingType != GroupingType.Default)
				writer.Append(')');

			return writer;
		}

		#endregion
	}
}
