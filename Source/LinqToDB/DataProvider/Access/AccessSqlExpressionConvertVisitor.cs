﻿using System;

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

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func)
			{ 
				case { Name: PseudoFunctions.TO_LOWER }:
					return func.WithName("LCase");
				case { Name: PseudoFunctions.TO_UPPER }:
					return func.WithName("UCase");
				case { Name: "Length" }:
					return func.WithName("Len");

				case {
					Name: PseudoFunctions.COALESCE,
					Parameters: [var p0, var p1],
					SystemType: var type,
				}:
					return new SqlFunction(type, "IIF", new SqlSearchCondition { Predicates = { new SqlPredicate.IsNull(p0, false) } }, p1, p0);

				case {
					Name: PseudoFunctions.COALESCE,
					Parameters: [var p0, ..] parms,
					SystemType: var type,
				}:
					return new SqlFunction(type, PseudoFunctions.COALESCE, p0, new SqlFunction(type, PseudoFunctions.COALESCE, GetSubArray(parms)));

				case {
					Name: "CharIndex",
					Parameters: [var p0, var p1],
					SystemType: var type,
				}:
					return new SqlFunction(type, "InStr", new SqlValue(1), p1, p0, new SqlValue(1));

				case {
					Name: "CharIndex",
					Parameters: [var p0, var p1, var p2],
					SystemType: var type,
				}:
					return new SqlFunction(type, "InStr", p2, p1, p0, new SqlValue(1));

				default:
					return base.ConvertSqlFunction(func);
			};

			static ISqlExpression[] GetSubArray(ISqlExpression[] array)
			{
				var parms = new ISqlExpression[array.Length - 1];
				Array.Copy(array, 1, parms, 0, parms.Length);
				return parms;
			}
		}

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			var expression = cast.Expression;
			var funcName   = string.Empty;

			switch (cast.SystemType.ToUnderlying().GetTypeCodeEx())
			{
				case TypeCode.String   : funcName = "CStr";  break;
				case TypeCode.Boolean  : funcName = "CBool"; break;
				case TypeCode.DateTime :
					if (IsDateDataType(cast.ToType, "Date"))
						funcName = "DateValue";
					else if (IsTimeDataType(cast.ToType))
						funcName = "TimeValue";
					else
						funcName = "CDate";
					break;

				default:
					if (cast.SystemType == typeof(DateTime))
						goto case TypeCode.DateTime;

					return expression;
			}

			if (!string.IsNullOrEmpty(funcName))
			{
				return new SqlFunction(cast.SystemType, funcName, expression); 
			}

			return expression;
		}

	}
}
