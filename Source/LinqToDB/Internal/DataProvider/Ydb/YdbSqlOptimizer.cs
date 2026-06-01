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
					if (elem is SelectQuery { Select.Columns: [var column] } subQuery
						&& !QueryHelper.IsDependsOnOuterSources(subQuery))
					{
						if (column.SystemType == null)
							throw new InvalidOperationException();

						if (visitor.Stack?.Count > 1
							// in column or predicate
							&& visitor.Stack[^2] is not ISqlTableSource
							&& visitor.Stack[^2] is SqlSelectClause
								or ISqlPredicate
								or SqlExpressionBase
								or SqlSetExpression)
						{
							var cte = new CteClause(subQuery, column.SystemType, false, null);
							(visitor.Context.Statement.With ??= new()).Clauses.Add(cte);

							// IN / EXISTS / set-operator operands are strongly-typed SelectQuery and cannot take a
							// bare CTE-table reference (the core visitors hard-cast them to SelectQuery). Wrap the
							// CTE into a trivial `SELECT <col> FROM $cte` so the operand stays a SelectQuery; the
							// offloaded query lives in the named CTE variable. Scalar positions keep the bare CTE
							// reference, which renders directly as the `$cte` named-query variable.
							if (visitor.Stack[^2] is SqlPredicate.InSubQuery or SqlPredicate.Exists or SqlSetOperator)
							{
								var alias = column.Alias ?? "cte_value";
								column.Alias = alias;

								var dataType  = QueryHelper.GetDbDataType(column.Expression, visitor.Context.MappingSchema);
								var canBeNull = column.Expression.CanBeNullable(NullabilityContext.GetContext(subQuery));

								cte.Fields.Add(new SqlField(dataType, alias, canBeNull) { PhysicalName = alias });

								var cteTable   = new SqlCteTable(cte, column.SystemType);
								var tableField = new SqlField(cte.Fields[0]);
								cteTable.Add(tableField);

								var wrap = new SelectQuery();
								wrap.From.Table(cteTable);
								wrap.Select.AddColumn(tableField);

								return wrap;
							}

							return new SqlCteTable(cte, column.SystemType);
						}
					}

					return elem;
				}, true);
			}
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
