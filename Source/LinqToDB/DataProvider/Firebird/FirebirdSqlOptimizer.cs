using System;

namespace LinqToDB.DataProvider.Firebird
{
	using Extensions;
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	public class FirebirdSqlOptimizer : BasicSqlOptimizer
	{
		public FirebirdSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement Finalize(MappingSchema mappingSchema, SqlStatement statement)
		{
			CheckAliases(statement, int.MaxValue);

			statement = base.Finalize(mappingSchema, statement);

			return statement;
		}

		protected static string[] LikeFirebirdEscapeSymbols = { "_", "%" };

		public override string[] LikeCharactersToEscape    => LikeFirebirdEscapeSymbols;
		public override bool     LikeValueParameterSupport => false;


		public override bool IsParameterDependedElement(IQueryElement element)
		{
			var result = base.IsParameterDependedElement(element);
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

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate, ConvertVisitor<RunOptimizationContext> visitor)
		{
			ISqlExpression expr;

			var caseSensitive = predicate.CaseSensitive.EvaluateBoolExpression(visitor.Context.OptimizationContext.Context);

			// for explicit case-sensitive search we apply "CAST({0} AS BLOB)" to searched string as COLLATE's collation is character set-dependent
			switch (predicate.Kind)
			{
				case SqlPredicate.SearchString.SearchKind.EndsWith:
				{
					if (caseSensitive == false)
					{
						predicate = new SqlPredicate.SearchString(
							PseudoFunctions.MakeToLower(predicate.Expr1),
							predicate.IsNot,
							PseudoFunctions.MakeToLower(predicate.Expr2), predicate.Kind,
							predicate.CaseSensitive);
					}
					else if (caseSensitive == true)
					{
						predicate = new SqlPredicate.SearchString(
							new SqlExpression(typeof(string), "CAST({0} AS BLOB)", Precedence.Primary, predicate.Expr1),
							predicate.IsNot,
							predicate.Expr2,
							predicate.Kind,
							predicate.CaseSensitive);
					}

					return ConvertSearchStringPredicateViaLike(predicate, visitor);
				}	
				case SqlPredicate.SearchString.SearchKind.StartsWith:
				{
					expr = new SqlExpression(typeof(bool),
						predicate.IsNot ? "{0} NOT STARTING WITH {1}" : "{0} STARTING WITH {1}",
						Precedence.Comparison,
						TryConvertToValue(
							caseSensitive == false
								? PseudoFunctions.MakeToLower(predicate.Expr1)
								: caseSensitive == true
									? new SqlExpression(typeof(string), "CAST({0} AS BLOB)", Precedence.Primary, predicate.Expr1)
									: predicate.Expr1,
							visitor.Context.OptimizationContext.Context),
						TryConvertToValue(
							caseSensitive == false
								? PseudoFunctions.MakeToLower(predicate.Expr2)
								: predicate.Expr2, visitor.Context.OptimizationContext.Context)) {CanBeNull = false};
					break;
				}	
				case SqlPredicate.SearchString.SearchKind.Contains:
				{
					if (caseSensitive == false)
					{
						expr = new SqlExpression(typeof(bool),
							predicate.IsNot ? "{0} NOT CONTAINING {1}" : "{0} CONTAINING {1}",
							Precedence.Comparison,
							TryConvertToValue(predicate.Expr1, visitor.Context.OptimizationContext.Context),
							TryConvertToValue(predicate.Expr2, visitor.Context.OptimizationContext.Context)) {CanBeNull = false};
					}
					else
					{
						if (caseSensitive == true)
						{
							predicate = new SqlPredicate.SearchString(
								new SqlExpression(typeof(string), "CAST({0} AS BLOB)", Precedence.Primary, predicate.Expr1),
								predicate.IsNot,
								predicate.Expr2,
								predicate.Kind,
								new SqlValue(false));
						}

						return ConvertSearchStringPredicateViaLike(predicate, visitor);
					}
					break;
				}	
				default:
					throw new InvalidOperationException($"Unexpected predicate: {predicate.Kind}");
			}

			return new SqlSearchCondition(new SqlCondition(false, new SqlPredicate.Expr(expr)));
		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			return statement.QueryType switch
			{
				QueryType.Delete => GetAlternativeDelete((SqlDeleteStatement)statement),
				QueryType.Update => GetAlternativeUpdate((SqlUpdateStatement)statement),
				_                => statement,
			};
		}

		public override ISqlExpression OptimizeExpression(ISqlExpression expression, ConvertVisitor<RunOptimizationContext> convertVisitor)
		{
			var newExpr = base.OptimizeExpression(expression, convertVisitor);

			switch (newExpr.ElementType)
			{
				case QueryElementType.SqlFunction:
				{
					var func = (SqlFunction)newExpr;

					switch (func.Name)
					{
						case "Convert":
						{
							if (func.SystemType.ToUnderlying() == typeof(bool))
							{
								var ex = AlternativeConvertToBoolean(func, 1);
								if (ex != null)
									return ex;
							}
							break;
						}
						case PseudoFunctions.CONVERT:
						{
							if (func.SystemType.ToUnderlying() == typeof(bool))
							{
								var ex = AlternativeConvertToBoolean(func, 2);
								if (ex != null)
									return ex;
							}
							else  if (func.SystemType.ToUnderlying() == typeof(string) && func.Parameters[2].SystemType?.ToUnderlying() == typeof(Guid))
								return new SqlFunction(func.SystemType, "UUID_TO_CHAR", false, true, func.Parameters[2])
								{
									CanBeNull = func.CanBeNull
								};
							else if (func.SystemType.ToUnderlying() == typeof(Guid) && func.Parameters[2].SystemType?.ToUnderlying() == typeof(string))
								return new SqlFunction(func.SystemType, "CHAR_TO_UUID", false, true, func.Parameters[2])
								{
									CanBeNull = func.CanBeNull
								};
							break;
						}
					}

					break;
				}
			}

			return newExpr;
		}

		public override ISqlExpression ConvertExpressionImpl(ISqlExpression expression, ConvertVisitor<RunOptimizationContext> visitor)
		{
			expression = base.ConvertExpressionImpl(expression, visitor);

			if (expression is SqlBinaryExpression be)
			{
				switch (be.Operation)
				{
					case "%": return new SqlFunction(be.SystemType, "Mod", be.Expr1, be.Expr2);
					case "&": return new SqlFunction(be.SystemType, "Bin_And", be.Expr1, be.Expr2);
					case "|": return new SqlFunction(be.SystemType, "Bin_Or", be.Expr1, be.Expr2);
					case "^": return new SqlFunction(be.SystemType, "Bin_Xor", be.Expr1, be.Expr2);
					case "+": return be.SystemType == typeof(string) ? new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence) : expression;
				}
			}
			else if (expression is SqlFunction func)
			{
				switch (func.Name)
				{
					case "Convert" :
						return new SqlExpression(func.SystemType, CASTEXPR, Precedence.Primary, FloorBeforeConvert(func), func.Parameters[0]);
				}
			}

			return expression;
		}

		protected override ISqlExpression ConvertFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func, false);
			
			return base.ConvertFunction(func);
		}

		public override SqlStatement FinalizeStatement(SqlStatement statement, EvaluationContext context)
		{
			statement = base.FinalizeStatement(statement, context);
			statement = WrapParameters(statement, context);
			return statement;
		}

		#region Wrap Parameters
		private SqlStatement WrapParameters(SqlStatement statement, EvaluationContext context)
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

			statement = statement.Convert(context, static (visitor, e) =>
			{
				if (e is SqlParameter p && p.IsQueryParameter)
				{
					var paramValue = p.GetParameterValue(visitor.Context.ParameterValues);

					// Don't cast in cast
					if (visitor.ParentElement is SqlFunction convertFunc && convertFunc.Name == PseudoFunctions.CONVERT)
					{
						// prevent removal by ConvertConvertion
						if (!convertFunc.DoNotOptimize)
							convertFunc.DoNotOptimize = CastRequired(visitor.Stack);
						return e;
					}

					if (paramValue.DbDataType.SystemType == typeof(bool) && visitor.ParentElement is SqlFunction func && func.Name == "CASE")
						return e;

					if (!CastRequired(visitor.Stack))
						return e;

					// TODO: temporary guard against cast to unknown type (Variant)
					if (paramValue.DbDataType.DataType == DataType.Undefined && paramValue.DbDataType.SystemType == typeof(object))
						return e;

					return new SqlExpression(paramValue.DbDataType.SystemType, CASTEXPR, Precedence.Primary, p, new SqlDataType(paramValue.DbDataType))
					{
						CanBeNull = p.CanBeNull
					};
				}

				return e;
			}, withStack: true);

			return statement;
		}

		static bool CastRequired(IReadOnlyList<IQueryElement> parents)
		{
			for (var i = parents.Count - 1; i >= 0; i--)
			{
				// went outside of subquery, mission abort
				if (parents[i] is SelectQuery)
					return false;

				// part of select column
				if (parents[i] is SqlColumn)
					return true;

				// part of output clause
				if (parents[i] is SqlOutputClause)
					return true;

				// insert or update keys used in merge source select query
				if (parents[i] is SqlSetExpression set
				    && i == 2
				    && (parents[1] is SqlInsertClause || parents[1] is SqlUpdateClause)
				    && parents[0] is SqlInsertOrUpdateStatement insertOrUpdate
				    && insertOrUpdate.Update.Keys.Any(k => k.Expression == set.Expression))
					return true;

				// enumerable merge source
				if (parents[i] is SqlValuesTable)
					return true;

				// complex insert/update statement, including merge
				if (parents[i] is SqlSetExpression
				    && i >= 2
				    && i < parents.Count - 1 // not just parameter setter
				    && (parents[i    - 1] is SqlUpdateClause
				        || parents[i - 1] is SqlInsertClause
				        || parents[i - 1] is SqlMergeOperationClause))
					return true;
			}

			return false;
		}

		const string CASTEXPR = "Cast({0} as {1})";
		#endregion
	}
}
