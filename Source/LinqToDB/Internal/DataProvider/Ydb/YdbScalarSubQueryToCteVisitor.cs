using System;

using LinqToDB.Internal.SqlQuery;
using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	// YQL has no inline scalar subquery: "(SELECT ...) = x" in a comparison and a bare "(SELECT ...)" in a
	// scalar / set-operand position are both parse/type errors. This visitor walks a finalized query and
	// rewrites single-column, non-correlated scalar subqueries into a YQL-legal form:
	// - `subquery = x` / `subquery <> x`  ->  `x [NOT] IN (SELECT <col> FROM $cte)`
	// - a scalar subquery in an IN/EXISTS/set operand  ->  a trivial `SELECT <col> FROM $cte` over a named CTE
	// - a scalar subquery in any other column/predicate position  ->  the bare `$cte` named-query reference
	//
	// It runs in the finalize phase (after the statement is otherwise complete), in Transform mode, mirroring
	// the standalone Convert pass it replaces. Using a dedicated visitor keeps the per-node context check on the
	// semantic ParentElement rather than a raw two-frame Stack peek.
	sealed class YdbScalarSubQueryToCteVisitor : SqlQueryConvertVisitorBase
	{
		readonly SqlStatementWithQueryBase _statement;
		readonly MappingSchema             _mappingSchema;

		public YdbScalarSubQueryToCteVisitor(SqlStatementWithQueryBase statement, MappingSchema mappingSchema)
			: base(allowMutation: false, transformationInfo: null)
		{
			_statement     = statement;
			_mappingSchema = mappingSchema;
			WithStack      = true;
		}

		public T Convert<T>(T element)
			where T : class, IQueryElement
		{
			Stack?.Clear();

			return (T)PerformConvert(element);
		}

		public override IQueryElement ConvertElement(IQueryElement element)
		{
			if (element is SqlPredicate.ExprExpr { Operator: SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual } cmp
				&& TryRewriteScalarComparisonToIn(cmp) is { } inPredicate)
			{
				return inPredicate;
			}

			if (element is SelectQuery { Select.Columns: [var column] } subQuery
				&& !QueryHelper.IsDependsOnOuterSources(subQuery))
			{
				if (column.SystemType == null)
					throw new InvalidOperationException();

				if (TryOffloadScalarSubQuery(subQuery, column.SystemType) is { } offloaded)
					return offloaded;
			}

			return element;
		}

		// `subquery = x` / `subquery <> x` for a single-column, non-correlated subquery -> `x [NOT] IN (subquery)`.
		// The IN-operand handling below offloads the subquery to a single named CTE - no extra
		// `SELECT <col> FROM $cte` wrapping layer. ExprExpr operands are intentionally left intact by the scalar
		// SelectQuery branch so they reach this rewrite first.
		SqlPredicate.InSubQuery? TryRewriteScalarComparisonToIn(SqlPredicate.ExprExpr cmp)
		{
			SelectQuery?    sub   = null;
			ISqlExpression? value = null;

			if (QueryHelper.UnwrapNullablity(cmp.Expr1) is SelectQuery { Select.Columns.Count: 1 } s1
				&& !QueryHelper.IsDependsOnOuterSources(s1))
			{
				sub   = s1;
				value = cmp.Expr2;
			}
			else if (QueryHelper.UnwrapNullablity(cmp.Expr2) is SelectQuery { Select.Columns.Count: 1 } s2
				&& !QueryHelper.IsDependsOnOuterSources(s2))
			{
				sub   = s2;
				value = cmp.Expr1;
			}

			if (sub != null && sub.Select.Columns[0].SystemType != null)
				return new SqlPredicate.InSubQuery(value!, cmp.Operator == SqlPredicate.Operator.NotEqual, sub, false);

			return null;
		}

		// A single-column, non-correlated scalar subquery in a column/predicate position. The parent element
		// selects the legal YQL shape:
		// - IN / EXISTS / set-operator operands are strongly-typed SelectQuery and cannot take a bare CTE-table
		//   reference (the core visitors hard-cast them to SelectQuery), so wrap the CTE into a trivial
		//   `SELECT <col> FROM $cte`.
		// - other scalar positions keep the bare CTE reference, which renders as the `$cte` named-query variable.
		// equality/inequality comparisons are handled by TryRewriteScalarComparisonToIn above; leave their operand
		// intact here. Other comparison operators (>, <, ...) still take the bare CTE reference.
		IQueryElement? TryOffloadScalarSubQuery(SelectQuery subQuery, Type systemType)
		{
			if (ParentElement is (SqlSelectClause or ISqlPredicate or SqlExpressionBase or SqlSetExpression)
				and not ISqlTableSource
				and not SqlPredicate.ExprExpr { Operator: SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual })
			{
				if (ParentElement is SqlPredicate.InSubQuery or SqlPredicate.Exists or SqlSetOperator)
					return WrapScalarSubQueryAsCte(subQuery);

				var cte = new CteClause(subQuery, systemType, false, null);
				(_statement.With ??= new()).Clauses.Add(cte);

				return new SqlCteTable(cte, systemType);
			}

			return null;
		}

		// Offloads a single-column subquery to a named CTE and returns a trivial `SELECT <col> FROM $cte`
		// SelectQuery referencing it (a relation for IN/EXISTS operands, a single-row source for `x IN (...)`).
		SelectQuery WrapScalarSubQueryAsCte(SelectQuery subQuery)
		{
			var column = subQuery.Select.Columns[0];

			var cte = new CteClause(subQuery, column.SystemType!, false, null);
			(_statement.With ??= new()).Clauses.Add(cte);

			var alias = column.Alias ?? "cte_value";
			column.Alias = alias;

			var dataType  = QueryHelper.GetDbDataType(column.Expression, _mappingSchema);
			var canBeNull = column.Expression.CanBeNullable(NullabilityContext.GetContext(subQuery));

			cte.Fields.Add(new SqlField(dataType, alias, canBeNull) { PhysicalName = alias });

			var cteTable   = new SqlCteTable(cte, column.SystemType!);
			var tableField = new SqlField(cte.Fields[0]);
			cteTable.Add(tableField);

			var wrap = new SelectQuery();
			wrap.From.Table(cteTable);
			wrap.Select.AddColumn(tableField);

			return wrap;
		}
	}
}
