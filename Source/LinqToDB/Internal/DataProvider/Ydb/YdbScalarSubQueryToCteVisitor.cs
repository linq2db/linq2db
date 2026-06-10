using System;
using System.Diagnostics.CodeAnalysis;

using LinqToDB.Internal.Common;
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
	// The rewrite is position-dependent, so it keys on the parent element (tracked across the recursion) rather
	// than the node alone. It runs in the finalize phase, bottom-up, offloading each subquery the moment it is
	// reached - so no second pass over rewritten nodes is needed.
	sealed class YdbScalarSubQueryToCteVisitor : QueryElementVisitor
	{
		internal static readonly ObjectPool<YdbScalarSubQueryToCteVisitor> Pool
			= new(() => new YdbScalarSubQueryToCteVisitor(), v => v.Cleanup(), 100);

		SqlStatementWithQueryBase _statement     = default!;
		MappingSchema             _mappingSchema = default!;
		IQueryElement?            _parent;

		public YdbScalarSubQueryToCteVisitor() : base(VisitMode.Modify)
		{
		}

		public T Convert<T>(SqlStatementWithQueryBase statement, MappingSchema mappingSchema, T element)
			where T : class, IQueryElement
		{
			_statement     = statement;
			_mappingSchema = mappingSchema;
			_parent        = null;

			return (T)Visit(element)!;
		}

		public override void Cleanup()
		{
			_statement     = default!;
			_mappingSchema = default!;
			_parent        = null;

			base.Cleanup();
		}

		[return: NotNullIfNotNull(nameof(element))]
		public override IQueryElement? Visit(IQueryElement? element)
		{
			if (element == null)
				return null;

			var parent = _parent;

			_parent     = element;
			var visited = base.Visit(element)!;
			_parent     = parent;

			return Rewrite(visited, parent);
		}

		IQueryElement Rewrite(IQueryElement element, IQueryElement? parent)
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

				// in a column / predicate / scalar-expression / set-expression position, but not a table source
				// and not an equality operand (those become IN above and keep the operand intact)
				if (parent is (SqlSelectClause or ISqlPredicate or SqlExpressionBase or SqlSetExpression)
					and not ISqlTableSource
					and not SqlPredicate.ExprExpr { Operator: SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual })
				{
					// IN / EXISTS / set-operator operands are strongly-typed SelectQuery and cannot take a bare
					// CTE-table reference (the core visitors hard-cast them to SelectQuery), so wrap the CTE into a
					// trivial `SELECT <col> FROM $cte`. Other scalar positions keep the bare CTE reference, which
					// renders as the `$cte` named-query variable.
					if (parent is SqlPredicate.InSubQuery or SqlPredicate.Exists or SqlSetOperator)
						return WrapScalarSubQueryAsCte(subQuery);

					var cte = new CteClause(subQuery, column.SystemType, false, null);
					(_statement.With ??= new()).Clauses.Add(cte);

					return new SqlCteTable(cte, column.SystemType);
				}
			}

			return element;
		}

		// `subquery = x` / `subquery <> x` for a single-column, non-correlated subquery -> `x [NOT] IN (subquery)`.
		// The subquery is an IN operand, so it is offloaded right away (WrapScalarSubQueryAsCte) instead of being
		// left for a second pass.
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
				return new SqlPredicate.InSubQuery(value!, cmp.Operator == SqlPredicate.Operator.NotEqual, WrapScalarSubQueryAsCte(sub), false);

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
