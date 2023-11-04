using System;

namespace LinqToDB.DataProvider.Firebird
{
	using Mapping;
	using SqlProvider;
	using SqlQuery;
	using SqlQuery.Visitors;

	public class FirebirdSqlOptimizer : BasicSqlOptimizer
	{
		public FirebirdSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new FirebirdSqlExpressionConvertVisitor(allowModify);
		}

		public override SqlStatement Finalize(MappingSchema mappingSchema, SqlStatement statement, DataOptions dataOptions)
		{
			CheckAliases(statement, int.MaxValue);

			statement = base.Finalize(mappingSchema, statement, dataOptions);

			return statement;
		}

		public override bool IsParameterDependedElement(NullabilityContext nullability, IQueryElement element)
		{
			var result = base.IsParameterDependedElement(nullability, element);
			if (result)
				return true;

			switch (element.ElementType)
			{
				case QueryElementType.LikePredicate:
				{
					var like = (SqlPredicate.Like)element;
					if (like.Expr1.ElementType != QueryElementType.SqlValue ||
					    like.Expr2.ElementType != QueryElementType.SqlValue)
						return true;
					break;
				}

				case QueryElementType.SearchStringPredicate:
				{
					var containsPredicate = (SqlPredicate.SearchString)element;
					if (containsPredicate.Expr1.ElementType != QueryElementType.SqlValue || containsPredicate.Expr2.ElementType != QueryElementType.SqlValue)
						return true;

					return false;
				}

			}

			return false;
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions)
		{
			return statement.QueryType switch
			{
				QueryType.Delete => GetAlternativeDelete((SqlDeleteStatement)statement, dataOptions),
				QueryType.Update => GetAlternativeUpdate((SqlUpdateStatement)statement, dataOptions),
				_                => statement,
			};
		}

		public override SqlStatement FinalizeStatement(SqlStatement statement, EvaluationContext context, DataOptions dataOptions)
		{
			statement = base.FinalizeStatement(statement, context, dataOptions);
			statement = WrapParameters(statement, context);
			return statement;
		}

		class WrapParametersVisitor : SqlQueryVisitor
		{
			bool _needCast;
			bool _inModifier;

			public WrapParametersVisitor(VisitMode visitMode) : base(visitMode)
			{
			}

			readonly struct NeedCastScope : IDisposable
			{
				readonly WrapParametersVisitor _visitor;
				readonly bool                  _saveValue;

				public NeedCastScope(WrapParametersVisitor visitor, bool needCast)
				{
					_visitor           = visitor;
					_saveValue         = visitor._needCast;
					visitor._needCast  = needCast;
				}

				public void Dispose()
				{
					_visitor._needCast = _saveValue;
				}
			}

			NeedCastScope NeedCast(bool needCast)
			{
				return new NeedCastScope(this, needCast);
			}

			protected override IQueryElement VisitSqlSelectClause(SqlSelectClause element)
			{
				var save  = _inModifier;

				using var scope = NeedCast(false);
				{
					_inModifier = true;

					element.TakeValue = (ISqlExpression?)Visit(element.TakeValue);
					element.SkipValue = (ISqlExpression?)Visit(element.SkipValue);

					_inModifier = save;
				}

				foreach (var column in element.Columns)
				{
					column.Expression = VisitSqlColumnExpression(column, column.Expression);
				}

				return element;
			}

			protected override ISqlExpression VisitSqlColumnExpression(SqlColumn column, ISqlExpression expression)
			{
				using var scope = NeedCast(true);
				return base.VisitSqlColumnExpression(column, expression);
			}

			protected override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
			{
				using var scope = NeedCast(false);
				return base.VisitSqlQuery(selectQuery);
			}

			protected override IQueryElement VisitSqlFunction(SqlFunction element)
			{
				if (element.Name == PseudoFunctions.CONVERT)
				{
					var newElement = base.VisitSqlFunction(element);

					if (newElement is SqlFunction func)
					{
						var foundParam = false;
						foreach (var param in func.Parameters)
						{
							if (param is SqlParameter sqlParam)
							{
								foundParam = true;
								sqlParam.NeedsCast = false;
							}
						}

						if (foundParam)
							func.DoNotOptimize = true;
					}

					return newElement;
				}

				return base.VisitSqlFunction(element);
			}

			protected override IQueryElement VisitSqlUpdateStatement(SqlUpdateStatement element)
			{
				using var scope = NeedCast(true);
				return base.VisitSqlUpdateStatement(element);
			}

			protected override IQueryElement VisitSqlBinaryExpression(SqlBinaryExpression element)
			{
				using var scope = NeedCast(!_inModifier);
				return base.VisitSqlBinaryExpression(element);
			}

			protected override IQueryElement VisitSqlParameter(SqlParameter sqlParameter)
			{
				if (_needCast)
				{
					if (!sqlParameter.NeedsCast)
					{
						sqlParameter.NeedsCast = true;
					}
				}

				return base.VisitSqlParameter(sqlParameter);
			}

			protected override IQueryElement VisitSqlInsertOrUpdateStatement(SqlInsertOrUpdateStatement element)
			{
				using var scope = NeedCast(true);
				return base.VisitSqlInsertOrUpdateStatement(element);
			}

			protected override IQueryElement VisitSqlOutputClause(SqlOutputClause element)
			{
				using var scope = NeedCast(true);
				return base.VisitSqlOutputClause(element);
			}

			protected override IQueryElement VisitSqlMergeOperationClause(SqlMergeOperationClause element)
			{
				using var scope = NeedCast(true);
				return base.VisitSqlMergeOperationClause(element);
			}
		}

		#region Wrap Parameters
		private static SqlStatement WrapParameters(SqlStatement statement, EvaluationContext context)
		{
			// for some reason Firebird doesn't use parameter type information (not supported?) is some places, so
			// we need to wrap parameter into CAST() to add type information explicitly
			// As it is not clear when type CAST needed, below we should document observations on current behavior.
			//
			// When CAST is not needed:
			// - parameter already in CAST from original query
			// - parameter used as direct inserted/updated value in insert/update queries (including merge)
			//
			// When CAST is needed:
			// - in select column expression at any position (except nested subquery): select, subquery, merge source
			// - in composite expression in insert or update setter: insert, update, merge (not always, in some cases it works)

			var visitor = new WrapParametersVisitor(VisitMode.Modify);

			statement = (SqlStatement)visitor.ProcessElement(statement);

			return statement;
		}

		#endregion
	}
}
