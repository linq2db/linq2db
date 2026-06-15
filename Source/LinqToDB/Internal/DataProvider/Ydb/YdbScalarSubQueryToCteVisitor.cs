using System;
using System.Collections.Generic;
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
	// - a scalar subquery in an IN/EXISTS operand  ->  a trivial `SELECT <col> FROM $cte` over a named CTE
	// - a scalar subquery in any other column / predicate / scalar-expression position  ->  the bare `$cte` reference
	//
	// The shape depends only on the position the subquery occupies. Rather than carry the parent element across the
	// recursion, we keep a single position flag: each element classifies the position it imposes on its children
	// (ClassifyChildren) the moment we descend into them, and the rewrite reads the position its parent imposed. It
	// runs in the finalize phase, bottom-up, offloading each subquery the moment it is reached - so no second pass
	// over rewritten nodes is needed. Structurally-identical subqueries reuse a single CTE (so a subquery duplicated
	// across clauses - e.g. a SELECT-column copy plus the rendered INSERT/UPDATE item - collapses to one named query).
	sealed class YdbScalarSubQueryToCteVisitor : QueryElementVisitor
	{
		internal static readonly ObjectPool<YdbScalarSubQueryToCteVisitor> Pool
			= new(() => new YdbScalarSubQueryToCteVisitor(), v => v.Cleanup(), 100);

		// Position a subquery occupies relative to the rewrite, imposed by its parent (see ClassifyChildren).
		enum Position
		{
			// A column / predicate / scalar-expression position -> a single-column subquery becomes a bare `$cte`
			// reference.
			Scalar,
			// An IN/EXISTS operand -> wrapped into a trivial `SELECT <col> FROM $cte` (these slots are hard-cast to
			// SelectQuery by the core visitors and cannot take a bare CTE-table reference).
			Wrapped,
			// A table source, the statement root, an equality-comparison operand (handled by the IN rewrite at the
			// comparison node), or any other non-expression slot -> left as-is, not turned into a `$cte` reference.
			Excluded,
		}

		SqlStatementWithQueryBase _statement     = default!;
		MappingSchema             _mappingSchema = default!;
		Position                  _position;
		// One CTE per structurally-identical scalar subquery (keyed by deep equality), shared across the per-statement
		// Convert calls so duplicated subqueries collapse to a single named query.
		Dictionary<ISqlExpression, CteClause>? _scalarCtes;

		public YdbScalarSubQueryToCteVisitor() : base(VisitMode.Modify)
		{
		}

		public T Convert<T>(SqlStatementWithQueryBase statement, MappingSchema mappingSchema, T element)
			where T : class, IQueryElement
		{
			_statement     = statement;
			_mappingSchema = mappingSchema;
			_position      = Position.Excluded;
			_scalarCtes  ??= new(ISqlExpressionEqualityComparer.Instance);

			return (T)Visit(element)!;
		}

		public override void Cleanup()
		{
			_statement     = default!;
			_mappingSchema = default!;
			_position      = Position.Excluded;
			_scalarCtes    = null;

			base.Cleanup();
		}

		[return: NotNullIfNotNull(nameof(element))]
		public override IQueryElement? Visit(IQueryElement? element)
		{
			if (element == null)
				return null;

			var position = _position;             // position my parent imposed on me

			_position   = ClassifyChildren(element); // position I impose on my children
			var visited = base.Visit(element)!;
			_position   = position;               // restore for my siblings

			return Rewrite(visited, position);
		}

		// The position a parent element imposes on the children it is about to visit. Mirrors the legal-shape rules:
		// only a column / predicate / scalar-expression / update-set slot hosts an inline scalar subquery (Scalar);
		// IN/EXISTS operands need the wrapped form; everything else (table source, set operator, ORDER BY / GROUP BY
		// item, statement root, equality-comparison operand) leaves the subquery untouched.
		static Position ClassifyChildren(IQueryElement parent) =>
			parent is (SqlSelectClause or ISqlPredicate or SqlExpressionBase or SqlSetExpression)
				and not ISqlTableSource
				and not SqlPredicate.ExprExpr { Operator: SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual }
				? parent is SqlPredicate.InSubQuery or SqlPredicate.Exists ? Position.Wrapped : Position.Scalar
				: Position.Excluded;

		IQueryElement Rewrite(IQueryElement element, Position position)
		{
			if (element is SqlPredicate.ExprExpr { Operator: SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual } cmp
				&& TryRewriteScalarComparisonToIn(cmp) is { } inPredicate)
			{
				return inPredicate;
			}

			if (position != Position.Excluded
				&& element is SelectQuery { Select.Columns: [var column] } subQuery
				&& !QueryHelper.IsDependsOnOuterSources(subQuery))
			{
				if (column.SystemType == null)
					throw new InvalidOperationException();

				if (position == Position.Wrapped)
					return WrapScalarSubQueryAsCte(subQuery);

				return new SqlCteTable(GetOrAddScalarCte(subQuery, column.SystemType), column.SystemType);
			}

			return element;
		}

		// Returns the CTE for a single-column scalar subquery, reusing an existing structurally-identical one so a
		// subquery duplicated across clauses maps to a single named query.
		CteClause GetOrAddScalarCte(SelectQuery subQuery, Type systemType)
		{
			if (!_scalarCtes!.TryGetValue(subQuery, out var cte))
			{
				cte = new CteClause(subQuery, systemType, false, null);
				(_statement.With ??= new()).Clauses.Add(cte);
				_scalarCtes[subQuery] = cte;
			}

			return cte;
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

			var dataType = QueryHelper.GetDbDataType(column.Expression, _mappingSchema);

			var cteField = new SqlCteField(dataType, alias) { Column = column };
			cte.Fields.Add(cteField);

			var cteTable   = new SqlCteTable(cte, column.SystemType!);
			var tableField = new SqlCteTableField(cteField);
			cteTable.Add(tableField);

			var wrap = new SelectQuery();
			wrap.From.Table(cteTable);
			wrap.Select.AddColumn(tableField);

			return wrap;
		}
	}
}
