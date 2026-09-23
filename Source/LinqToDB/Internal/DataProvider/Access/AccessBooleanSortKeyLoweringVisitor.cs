using LinqToDB.Internal.SqlQuery;
using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Access
{
	/// <summary>
	/// Access stores True as -1, so a boolean sort key orders True before False. Lowers such a key to a 1/0 one.
	/// </summary>
	/// <remarks>
	/// Run before the query optimizer, as <c>SqlNullsOrderingLoweringVisitor</c> is, so DISTINCT / set-operation /
	/// sub-query handling sees the derived key and keeps the sub-query that puts it outside the DISTINCT.
	/// </remarks>
	sealed class AccessBooleanSortKeyLoweringVisitor : QueryElementVisitor
	{
		public AccessBooleanSortKeyLoweringVisitor() : base(VisitMode.Modify)
		{
		}

		public IQueryElement LowerBooleanSortKeys(IQueryElement element)
		{
			return Visit(element);
		}

		protected internal override IQueryElement VisitSqlOrderByClause(SqlOrderByClause element)
		{
			base.VisitSqlOrderByClause(element);

			var nullability = NullabilityContext.GetContext(element.SelectQuery);

			for (var i = 0; i < element.Items.Count; i++)
			{
				var item = element.Items[i];

				if (item.IsPositioned)
					continue;

				var key = ToSortKey(item.Expression, nullability);

				if (!ReferenceEquals(key, item.Expression))
					element.Items[i] = new SqlOrderByItem(key, item.IsDescending, item.IsPositioned, item.NullsPosition);
			}

			return element;
		}

		protected internal override IQueryElement VisitSqlWindowOrderItem(SqlWindowOrderItem element)
		{
			base.VisitSqlWindowOrderItem(element);

			var key = ToSortKey(element.Expression, NullabilityContext.NonQuery);

			if (!ReferenceEquals(key, element.Expression))
				element.Modify(key);

			return element;
		}

		static ISqlExpression ToSortKey(ISqlExpression expr, NullabilityContext nullability)
		{
			if (expr.SystemType != typeof(bool) && expr.SystemType != typeof(bool?))
				return expr;

			var unwrapped = QueryHelper.UnwrapNullablity(expr);

			if (unwrapped is SqlValue or SqlParameter)
				return expr;

			ISqlPredicate predicate = unwrapped as ISqlPredicate
				?? new SqlPredicate.IsTrue(expr, new SqlValue(true), new SqlValue(false), null, false);

			var one  = new SqlValue(1);
			var zero = new SqlValue(0);

			return expr.CanBeNullable(nullability)
				? new SqlConditionExpression(predicate, one, new SqlConditionExpression(new SqlPredicate.Not(predicate), zero, new SqlValue(typeof(int?), null)))
				: new SqlConditionExpression(predicate, one, zero);
		}
	}
}
