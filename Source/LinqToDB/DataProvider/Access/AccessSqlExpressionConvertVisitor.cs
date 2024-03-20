using System;

namespace LinqToDB.DataProvider.Access
{
	using Extensions;
	using Linq;
	using SqlProvider;
	using SqlQuery;

	public class AccessSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public AccessSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		protected static string[] AccessLikeCharactersToEscape = {"_", "?", "*", "%", "#", "-", "!"};

		public override bool LikeIsEscapeSupported => false;

		public override string[] LikeCharactersToEscape => AccessLikeCharactersToEscape;

		public override ISqlPredicate ConvertLikePredicate(SqlPredicate.Like predicate)
		{
			if (predicate.Escape != null)
			{
				return new SqlPredicate.Like(predicate.Expr1, predicate.IsNot, predicate.Expr2, null);
			}

			return base.ConvertLikePredicate(predicate);
		}

		protected override string EscapeLikePattern(string str)
		{
			var newStr = DataTools.EscapeUnterminatedBracket(str);
			if (newStr == str)
				newStr = newStr.Replace("[", "[[]");

			foreach (var s in LikeCharactersToEscape)
				newStr = newStr.Replace(s, "[" + s + "]");

			return newStr;
		}

		public override ISqlExpression EscapeLikeCharacters(ISqlExpression expression, ref ISqlExpression? escape)
		{
			throw new LinqException("Access does not support `Replace` function which is required for such query.");
		}

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var like   = ConvertSearchStringPredicateViaLike(predicate);
			var result = like;

			if (predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext) == true)
			{
				SqlPredicate.ExprExpr? subStrPredicate = null;

				switch (predicate.Kind)
				{
					case SqlPredicate.SearchString.SearchKind.StartsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(int), "InStr",
									new SqlValue(1),
									predicate.Expr1,
									predicate.Expr2,
									new SqlValue(0)),
								SqlPredicate.Operator.Equal,
								new SqlValue(1), null);

						break;
					}

					case SqlPredicate.SearchString.SearchKind.EndsWith:
					{
						var indexExpr = new SqlBinaryExpression(typeof(int),
							new SqlBinaryExpression(typeof(int),
								new SqlFunction(typeof(int), "Length", predicate.Expr1), "-",
								new SqlFunction(typeof(int), "Length", predicate.Expr2)), "+",
							new SqlValue(1));

						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(int), "InStr",
									indexExpr,
									predicate.Expr1,
									predicate.Expr2,
									new SqlValue(0)),
								SqlPredicate.Operator.Equal,
								indexExpr, null);

						break;
					}
					case SqlPredicate.SearchString.SearchKind.Contains:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(int), "InStr",
									new SqlValue(1),
									predicate.Expr1,
									predicate.Expr2,
									new SqlValue(0)),
								SqlPredicate.Operator.GreaterOrEqual,
								new SqlValue(1), null);
						break;
					}

				}

				if (subStrPredicate != null)
				{
					result = new SqlSearchCondition(predicate.IsNot, like, subStrPredicate.MakeNot(predicate.IsNot));
				}
			}

			return result;
		}

		SqlFunction ConvertCase(Type systemType, ISqlExpression[] parameters, int start)
		{
			var len = parameters.Length - start;

			if (len < 2)
				throw new SqlException("CASE statement is not supported by the {0}.", GetType().Name);

			return new SqlFunction(systemType, "IIF",
				parameters[start],
				parameters[start + 1],
				len switch
				{
					2 => parameters[start                          + 1],
					3 => parameters[start                          + 2],
					_ => ConvertCase(systemType, parameters, start + 2)
				});
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func.Name)
			{
				case PseudoFunctions.TO_LOWER: return func.WithName("LCase");
				case PseudoFunctions.TO_UPPER: return func.WithName("UCase");
				case "Length"                : return func.WithName("Len");
				case PseudoFunctions.COALESCE  :
				{
					if (func.Parameters.Length > 2)
					{
						var parms = new ISqlExpression[func.Parameters.Length - 1];

						Array.Copy(func.Parameters, 1, parms, 0, parms.Length);
						return new SqlFunction(func.SystemType, func.Name, func.Parameters[0], new SqlFunction(func.SystemType, func.Name, parms));
					}

					var sc = new SqlSearchCondition();

					sc.Predicates.Add(new SqlPredicate.IsNull(func.Parameters[0], false));

					return new SqlFunction(func.SystemType, "IIF", sc, func.Parameters[1], func.Parameters[0]);
				}
				case "CASE"      : return ConvertCase(func.SystemType, func.Parameters, 0); 
				case "CharIndex" :
					return func.Parameters.Length == 2?
						new SqlFunction(func.SystemType, "InStr", new SqlValue(1),    func.Parameters[1], func.Parameters[0], new SqlValue(1)):
						new SqlFunction(func.SystemType, "InStr", func.Parameters[2], func.Parameters[1], func.Parameters[0], new SqlValue(1));
			}

			return base.ConvertSqlFunction(func);
		}

		protected override ISqlExpression ConvertConversion(SqlFunction func)
		{
			var expression = func.Parameters[2];
			var funcName   = string.Empty;

			switch (func.SystemType.ToUnderlying().GetTypeCodeEx())
			{
				case TypeCode.String   : funcName = "CStr";  break;
				case TypeCode.Boolean  : funcName = "CBool"; break;
				case TypeCode.DateTime :
					if (IsDateDataType(func.Parameters[0], "Date"))
						funcName = "DateValue";
					else if (IsTimeDataType(func.Parameters[0]))
						funcName = "TimeValue";
					else
						funcName = "CDate";
					break;

				default:
					if (func.SystemType == typeof(DateTime))
						goto case TypeCode.DateTime;

					return expression;
			}

			if (!string.IsNullOrEmpty(funcName))
			{
				return new SqlFunction(func.SystemType, funcName, expression); 
			}

			return expression;
		}

	}
}
