namespace LinqToDB.Internal.SqlQuery.Visitors
{
	/// <summary>
	/// Lowers <see cref="Sql.NullsPosition"/> on regular <c>ORDER BY</c> items into an explicit
	/// <c>CASE WHEN &lt;expr&gt; IS NULL THEN … END</c> sort key for providers that lack native
	/// <c>NULLS FIRST</c> / <c>NULLS LAST</c> support.
	/// <para>
	/// Run before the query optimizer so the emulation key is an ordinary derived order expression and the existing
	/// DISTINCT / set-operation / sub-query handling (column promotion, sub-query wrapping) treats it correctly.
	/// Window <c>OVER(ORDER BY …)</c> ordering is emulated separately at SQL-build time (it has no select-list
	/// constraint), so it is intentionally not touched here.
	/// </para>
	/// </summary>
	sealed class SqlNullsOrderingLoweringVisitor : QueryElementVisitor
	{
		public SqlNullsOrderingLoweringVisitor() : base(VisitMode.Modify)
		{
		}

		public IQueryElement LowerNullsOrdering(IQueryElement element)
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

				if (item.NullsPosition == Sql.NullsPosition.None)
					continue;

				// A non-nullable ordering key has no NULLs, so the requested position is a no-op: drop it instead of
				// emitting a constant CASE key (mirrors SqlExpressionOptimizerVisitor.VisitSqlOrderByItem, which does
				// the same for providers with native NULLS support).
				if (!nullability.IsEmpty && !item.Expression.CanBeNullable(nullability))
				{
					element.Items[i] = new SqlOrderByItem(item.Expression, item.IsDescending, item.IsPositioned, Sql.NullsPosition.None);
					continue;
				}

				// NULLS LAST  -> nulls sort after  (1 for null, 0 otherwise)
				// NULLS FIRST -> nulls sort before (0 for null, 1 otherwise)
				var nullsLast = item.NullsPosition == Sql.NullsPosition.Last;

				var nullsKey = new SqlConditionExpression(
					new SqlPredicate.IsNull(item.Expression, false),
					new SqlValue(nullsLast ? 1 : 0),
					new SqlValue(nullsLast ? 0 : 1));

				// Replace the NULLS-positioned item with a leading ascending CASE key + the plain item.
				element.Items[i] = new SqlOrderByItem(nullsKey, false, false, Sql.NullsPosition.None);
				element.Items.Insert(i + 1, new SqlOrderByItem(item.Expression, item.IsDescending, item.IsPositioned, Sql.NullsPosition.None));

				i++; // skip the inserted plain item
			}

			return element;
		}
	}
}
