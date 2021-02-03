using System;
using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.Firebird
{
	using System.Linq;
	using Extensions;
	using SqlQuery;
	using SqlProvider;

	public class FirebirdSqlOptimizer : BasicSqlOptimizer
	{
		public FirebirdSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement Finalize(SqlStatement statement)
		{
			CheckAliases(statement, int.MaxValue);

			statement = base.Finalize(statement);

			return statement;
		}

		protected static string[] LikeFirebirdEscapeSymbosl = { "_", "%" };

		public override string[] LikeCharactersToEscape => LikeFirebirdEscapeSymbosl;


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


		public override ISqlPredicate ConvertSearchStringPredicate(MappingSchema mappingSchema, SqlPredicate.SearchString predicate,
			ConvertVisitor visitor,
			OptimizationContext optimizationContext)
		{
			if (!predicate.IgnoreCase)
				return ConvertSearchStringPredicateViaLike(mappingSchema, predicate, visitor, optimizationContext);

			ISqlExpression expr;
			switch (predicate.Kind)
			{
				case SqlPredicate.SearchString.SearchKind.EndsWith:
				{
					predicate = new SqlPredicate.SearchString(
						new SqlFunction(typeof(string), "$ToLower$", predicate.Expr1),
						predicate.IsNot,
						new SqlFunction(typeof(string), "$ToLower$", predicate.Expr2), predicate.Kind,
						predicate.IgnoreCase);

					return ConvertSearchStringPredicateViaLike(mappingSchema, predicate, visitor, optimizationContext);
				}	
				case SqlPredicate.SearchString.SearchKind.StartsWith:
				{
					expr = new SqlExpression(typeof(bool),
						predicate.IsNot ? "{0} NOT STARTING WITH {1}" : "{0} STARTING WITH {1}", 
						Precedence.Comparison,
						TryConvertToValue(predicate.Expr1, optimizationContext.Context), TryConvertToValue(predicate.Expr2, optimizationContext.Context)) { CanBeNull = false };
					break;
				}	
				case SqlPredicate.SearchString.SearchKind.Contains:
					expr = new SqlExpression(typeof(bool),
						predicate.IsNot ? "{0} NOT CONTAINING {1}" : "{0} CONTAINING {1}", 
						Precedence.Comparison,
						TryConvertToValue(predicate.Expr1, optimizationContext.Context), TryConvertToValue(predicate.Expr2, optimizationContext.Context)) { CanBeNull = false };
					break;
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

		public override ISqlExpression OptimizeExpression(ISqlExpression expression, ConvertVisitor convertVisitor,
			EvaluationContext context)
		{
			var newExpr = base.OptimizeExpression(expression, convertVisitor, context);

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
						case "$Convert$":
						{
							if (func.SystemType.ToUnderlying() == typeof(bool))
							{
								var ex = AlternativeConvertToBoolean(func, 2);
								if (ex != null)
									return ex;
							}
							break;
						}
					}

					break;

				}
			}

			return newExpr;
		}

		public override ISqlExpression ConvertExpressionImpl(ISqlExpression expression, ConvertVisitor visitor,
			EvaluationContext context)
		{
			expression = base.ConvertExpressionImpl(expression, visitor, context);

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

			statement = ConvertVisitor.Convert(statement, (visitor, e) =>
			{
				if (e is SqlParameter p && p.IsQueryParameter)
				{
					var paramValue = p.GetParameterValue(context.ParameterValues);

					// Don't cast in cast
					if (visitor.ParentElement is SqlFunction convertFunc && convertFunc.Name == "$Convert$")
						return e;

					if (paramValue.DbDataType.SystemType == typeof(bool) && visitor.ParentElement is SqlFunction func && func.Name == "CASE")
						return e;

					var replace = false;
					for (var i = visitor.Stack.Count - 1; i >= 0; i--)
					{
						// went outside of subquery, mission abort
						if (visitor.Stack[i] is SelectQuery)
							return e;

						// part of select column
						if (visitor.Stack[i] is SqlColumn)
						{
							replace = true;
							break;
						}

						// insert or update keys used in merge source select query
						if (visitor.Stack[i] is SqlSetExpression set
							&& i == 2
							&& visitor.Stack[i - 1] is SqlInsertClause
							&& visitor.Stack[i - 2] is SqlInsertOrUpdateStatement insertOrUpdate
							&& insertOrUpdate.Update.Keys.Any(k => k.Expression == set.Expression))
						{
							replace = true;
							break;
						}

						// enumerable merge source
						if (visitor.Stack[i] is SqlValuesTable)
						{
							replace = true;
							break;
						}

						// complex insert/update statement, including merge
						if (visitor.Stack[i] is SqlSetExpression
							&& i >= 2
							&& i < visitor.Stack.Count - 1 // not just parameter setter
							&& (visitor.Stack[i - 1] is SqlUpdateClause
								|| visitor.Stack[i - 1] is SqlInsertClause
								|| visitor.Stack[i - 1] is SqlMergeOperationClause))
						{
							replace = true;
							break;
						}
					}

					if (!replace)
						return e;

					return new SqlExpression(paramValue.DbDataType.SystemType, CASTEXPR, Precedence.Primary, p, new SqlDataType(paramValue.DbDataType));
				}

				return e;
			});

			return statement;
		}

		private const string CASTEXPR = "Cast({0} as {1})";
		#endregion
	}
}
