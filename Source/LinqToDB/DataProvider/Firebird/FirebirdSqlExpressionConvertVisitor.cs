using System;

using LinqToDB.Extensions;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Firebird
{
	public class FirebirdSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		const string CASTEXPR = "Cast({0} as {1})";

		public FirebirdSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		#region LIKE

		protected static string[] LikeFirebirdEscapeSymbols = { "_", "%" };

		public override string[] LikeCharactersToEscape    => LikeFirebirdEscapeSymbols;
		public override bool     LikeValueParameterSupport => false;

		#endregion

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				case "%": return new SqlFunction(element.SystemType, "Mod", element.Expr1, element.Expr2);
				case "&": return new SqlFunction(element.SystemType, "Bin_And", element.Expr1, element.Expr2);
				case "|": return new SqlFunction(element.SystemType, "Bin_Or", element.Expr1, element.Expr2);
				case "^": return new SqlFunction(element.SystemType, "Bin_Xor", element.Expr1, element.Expr2);
				case "+": return element.SystemType == typeof(string) ? new SqlBinaryExpression(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence) : element;
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func.Name)
			{
				case "Convert" :
				{
					if (func.SystemType.ToUnderlying() == typeof(bool))
					{
						var ex = AlternativeConvertToBoolean(func, 1);
						if (ex != null)
							return ex;
					}

					return new SqlExpression(func.SystemType, CASTEXPR, Precedence.Primary, FloorBeforeConvert(func),
						func.Parameters[0]);
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

					var convert = new SqlExpression(func.SystemType, CASTEXPR, Precedence.Primary,
						FloorBeforeConvert(func, func.Parameters[2]), func.Parameters[0]);

					return convert;
				}

			}

			func = ConvertFunctionParameters(func, false);

			return base.ConvertSqlFunction(func);
		}

		public override IQueryElement VisitSqlParameter(SqlParameter sqlParameter)
		{
			/*if (EvaluationContext.ParameterValues != null && sqlParameter.NeedsCast)
			{
				var paramValue = sqlParameter.GetParameterValue(EvaluationContext.ParameterValues);

				// TODO: temporary guard against cast to unknown type (Variant)
				if (paramValue.DbDataType.DataType == DataType.Undefined && paramValue.DbDataType.SystemType == typeof(object))
					return sqlParameter;

				return new SqlExpression(
					paramValue.DbDataType.SystemType, 
					PseudoFunctions.CONVERT,
					Precedence.Primary, 
					new SqlDataType(paramValue.DbDataType),
					new SqlDataType(paramValue.DbDataType), 
					sqlParameter)
				{
					CanBeNull = sqlParameter.CanBeNull
				};
			}*/


			return base.VisitSqlParameter(sqlParameter);
		}

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			ISqlExpression expr;

			var caseSensitive = predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext);

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

					return ConvertSearchStringPredicateViaLike(predicate);
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
							EvaluationContext),
						TryConvertToValue(
							caseSensitive == false
								? PseudoFunctions.MakeToLower(predicate.Expr2)
								: predicate.Expr2, EvaluationContext)) {CanBeNull = false};
					break;
				}
				case SqlPredicate.SearchString.SearchKind.Contains:
				{
					if (caseSensitive == false)
					{
						expr = new SqlExpression(typeof(bool),
							predicate.IsNot ? "{0} NOT CONTAINING {1}" : "{0} CONTAINING {1}",
							Precedence.Comparison,
							TryConvertToValue(predicate.Expr1, EvaluationContext),
							TryConvertToValue(predicate.Expr2, EvaluationContext)) {CanBeNull = false};
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

						return ConvertSearchStringPredicateViaLike(predicate);
					}
					break;
				}
				default:
					throw new InvalidOperationException($"Unexpected predicate: {predicate.Kind}");
			}

			return new SqlSearchCondition(new SqlCondition(false, new SqlPredicate.Expr(expr)));
		}

	}
}
