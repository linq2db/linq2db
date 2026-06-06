using System;

using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	public class YdbSqlOptimizer : BasicSqlOptimizer
	{
		public YdbSqlOptimizer(SqlProviderFlags sqlProviderFlags)
			: base(sqlProviderFlags) { }

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
			=> new YdbSqlExpressionConvertVisitor(allowModify);

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = base.TransformStatement(statement, dataOptions, mappingSchema);

			switch (statement.QueryType)
			{
				case QueryType.Delete:
					// disable table alias
					statement = GetAlternativeDelete((SqlDeleteStatement)statement);
					statement.SelectQuery!.From.Tables[0].Alias = "$";
					break;
				case QueryType.Update:
					// disable table alias
					statement.SelectQuery!.From.Tables[0].Alias = "$";
					statement = GetAlternativeUpdate((SqlUpdateStatement)statement, dataOptions, mappingSchema);
					break;
				case QueryType.Insert:
					statement = CorrectInsertStatement((SqlInsertStatement)statement);
					break;
			}

			return statement;
		}

		private SqlStatement CorrectInsertStatement(SqlInsertStatement statement)
		{
			if (statement.SelectQuery != null
				&& statement.SelectQuery.Select.Columns.Count == statement.Insert.Items.Count)
			{
				for (var i = 0; i < statement.Insert.Items.Count; i++)
				{
					statement.SelectQuery.Select.Columns[i].Alias = ((SqlField)statement.Insert.Items[i].Column).Name;
				}

				statement.SelectQuery.DoNotSetAliases = true;
			}

			return statement;
		}

		public override SqlStatement Finalize(MappingSchema mappingSchema, SqlStatement statement, DataOptions dataOptions)
		{
			statement.VisitAll(SetQueryParameter);

			statement = base.Finalize(mappingSchema, statement, dataOptions);

			if (MoveScalarSubQueriesToCte(statement, mappingSchema))
				FinalizeCte(statement);

			statement.VisitAll(ReplaceTableAll);

			return statement;
		}

		private bool MoveScalarSubQueriesToCte(SqlStatement statement, MappingSchema mappingSchema)
		{
			if (statement is not SqlStatementWithQueryBase withStatement)
				return false;

			var cteCount = withStatement.With?.Clauses.Count ?? 0;

			var context = (Statement: withStatement, MappingSchema: mappingSchema);

			if (statement.SelectQuery != null && statement.QueryType != QueryType.Merge)
				statement.SelectQuery = ConvertToCte(statement.SelectQuery, context);

			if (statement is SqlInsertStatement insert)
				insert.Insert = ConvertToCte(insert.Insert, context);

			return withStatement.With?.Clauses.Count > cteCount;

			static T ConvertToCte<T>(T statement, (SqlStatementWithQueryBase Statement, MappingSchema MappingSchema) context)
				where T: class, IQueryElement
			{
				return statement.Convert(context, static (visitor, elem) =>
				{
					// YQL has no inline scalar subquery in a comparison ("(SELECT ...) = x" is a parse/type
					// error). Rewrite `subquery = x` / `subquery <> x` (single-column, non-correlated subquery)
					// to `x [NOT] IN (SELECT <col> FROM $cte)` - the IN form is YQL-legal (see IN/EXISTS below).
					// The scalar SelectQuery branch below skips ExprExpr operands so they reach this rewrite intact.
					if (elem is SqlPredicate.ExprExpr { Operator: SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual } cmp)
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
						{
							// Rewrite to `value [NOT] IN (subquery)`. The IN-operand handling below offloads the
							// subquery to a single named CTE - no extra `SELECT <col> FROM $cte` wrapping layer.
							return new SqlPredicate.InSubQuery(value!, cmp.Operator == SqlPredicate.Operator.NotEqual, sub, false);
						}
					}

					if (elem is SelectQuery { Select.Columns: [var column] } subQuery
						&& !QueryHelper.IsDependsOnOuterSources(subQuery))
					{
						if (column.SystemType == null)
							throw new InvalidOperationException();

						if (visitor.Stack?.Count > 1
							// in column or predicate
							&& visitor.Stack[^2] is not ISqlTableSource
							// equality/inequality comparisons are rewritten to IN above; leave the operand intact
							// here. Other comparison operators (>, <, ...) still take the bare CTE reference.
							&& visitor.Stack[^2] is not SqlPredicate.ExprExpr { Operator: SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual }
							&& visitor.Stack[^2] is SqlSelectClause
								or ISqlPredicate
								or SqlExpressionBase
								or SqlSetExpression)
						{
							// IN / EXISTS / set-operator operands are strongly-typed SelectQuery and cannot take a
							// bare CTE-table reference (the core visitors hard-cast them to SelectQuery). Wrap the
							// CTE into a trivial `SELECT <col> FROM $cte` so the operand stays a SelectQuery; the
							// offloaded query lives in the named CTE variable. Scalar positions keep the bare CTE
							// reference, which renders directly as the `$cte` named-query variable.
							if (visitor.Stack[^2] is SqlPredicate.InSubQuery or SqlPredicate.Exists or SqlSetOperator)
								return WrapScalarSubQueryAsCte(visitor.Context.Statement, visitor.Context.MappingSchema, subQuery);

							var cte = new CteClause(subQuery, column.SystemType, false, null);
							(visitor.Context.Statement.With ??= new()).Clauses.Add(cte);

							return new SqlCteTable(cte, column.SystemType);
						}
					}

					return elem;
				}, true);
			}
		}

		// Offloads a single-column subquery to a named CTE and returns a trivial `SELECT <col> FROM $cte`
		// SelectQuery referencing it (a relation for IN/EXISTS operands, a single-row source for `x IN (...)`).
		static SelectQuery WrapScalarSubQueryAsCte(SqlStatementWithQueryBase statement, MappingSchema mappingSchema, SelectQuery subQuery)
		{
			var column = subQuery.Select.Columns[0];

			var cte = new CteClause(subQuery, column.SystemType!, false, null);
			(statement.With ??= new()).Clauses.Add(cte);

			var alias = column.Alias ?? "cte_value";
			column.Alias = alias;

			var dataType  = QueryHelper.GetDbDataType(column.Expression, mappingSchema);
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

		static void SetQueryParameter(IQueryElement element)
		{
			// Following parameters not supported by provider and should be literals:
			// - Date32 mapped to raw int
			if (element is SqlParameter p)
			{
				if ((p.Type.SystemType.UnwrapNullableType() == typeof(int) && p.Type.DataType is DataType.Date32)
					|| (p.Type.SystemType.UnwrapNullableType() == typeof(long) && p.Type.DataType is DataType.DateTime64 or DataType.Timestamp64 or DataType.Interval64))
					p.IsQueryParameter = false;
			}
		}

		static void ReplaceTableAll(IQueryElement element)
		{
			// "SELECT *" could fail if there are columns with same name. E.g. from joined tables
			if (element is SqlPredicate.Exists predicate)
			{
				predicate.SubQuery.Select.Columns.Clear();
				predicate.SubQuery.Select.Columns.Add(new SqlColumn(predicate.SubQuery, new SqlValue(1)));
			}
		}
	}
}
