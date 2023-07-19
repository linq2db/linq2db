using System;

namespace LinqToDB.DataProvider.SqlCe
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	public class SqlCeSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public SqlCeSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		#region LIKE

		private static string[] LikeSqlCeCharactersToEscape = { "_", "%" };

		public override string[] LikeCharactersToEscape => LikeSqlCeCharactersToEscape;

		#endregion

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				case "%":
					return element.Expr1.SystemType!.IsIntegerType()?
						element : 
						new SqlBinaryExpression(
							typeof(int),
							new SqlFunction(typeof(int), "Convert", SqlDataType.Int32, element.Expr1),
							element.Operation,
							element.Expr2,
							element.Precedence);
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func.Name)
			{
				case "Length":
				{
					return func.WithName("LEN");
				}
			}

			func = ConvertFunctionParameters(func, false);

			return base.ConvertSqlFunction(func);
		}

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var like = ConvertSearchStringPredicateViaLike(predicate);

			if (predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext) == true)
			{
				SqlPredicate.ExprExpr? subStrPredicate = null;

				switch (predicate.Kind)
				{
					case SqlPredicate.SearchString.SearchKind.StartsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary,
									new SqlFunction(typeof(string), "SUBSTRING",
										predicate.Expr1,
										new SqlValue(1),
										new SqlFunction(typeof(int), "Length", predicate.Expr2))),
								SqlPredicate.Operator.Equal,
								new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary, predicate.Expr2),
								null
							);
						break;
					}

					case SqlPredicate.SearchString.SearchKind.EndsWith:
					{
						var indexExpression = new SqlBinaryExpression(typeof(int),
							new SqlBinaryExpression(typeof(int),
								new SqlFunction(typeof(int), "Length", predicate.Expr1),
								"-",
								new SqlFunction(typeof(int), "Length", predicate.Expr2)),
							"+",
							new SqlValue(1));

						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary,
									new SqlFunction(typeof(string), "SUBSTRING",
										predicate.Expr1,
										indexExpression,
										new SqlFunction(typeof(int), "Length", predicate.Expr2))),
								SqlPredicate.Operator.Equal,
								new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary, predicate.Expr2),
								null
							);

						break;
					}
					case SqlPredicate.SearchString.SearchKind.Contains:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(int), "CHARINDEX",
									new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary,
										predicate.Expr2),
									new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary,
										predicate.Expr1)),
								SqlPredicate.Operator.Greater,
								new SqlValue(0), null);

						break;
					}

				}

				if (subStrPredicate != null)
				{
					var result = new SqlSearchCondition(
						new SqlCondition(false, like, predicate.IsNot),
						new SqlCondition(predicate.IsNot, subStrPredicate));

					return result;
				}
			}

			return like;
		}


		protected override ISqlExpression ConvertConversion(SqlFunction func)
		{
			var toType   = func.Parameters[0];
			var argument = func.Parameters[1];

			switch (Type.GetTypeCode(func.SystemType.ToUnderlying()))
			{
				case TypeCode.UInt64:
					if (argument.SystemType!.IsFloatType())
						return new SqlFunction(
							func.SystemType,
							func.Name,
							false,
							func.Precedence,
							toType,
							new SqlFunction(func.SystemType, "Floor", argument));

					break;

				case TypeCode.DateTime:
					var type1 = argument.SystemType!.ToUnderlying();

					if (IsTimeDataType(toType))
					{
						if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
							return new SqlExpression(
								func.SystemType, "Cast(Convert(NChar, {0}, 114) as DateTime)",
								Precedence.Primary, argument);

						if (argument.SystemType == typeof(string))
							return argument;

						return new SqlExpression(
							func.SystemType, "Convert(NChar, {0}, 114)", Precedence.Primary,
							argument);
					}

					if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
					{
						if (IsDateDataType(toType, "Datetime"))
							return new SqlExpression(
								func.SystemType, "Cast(Floor(Cast({0} as Float)) as DateTime)",
								Precedence.Primary, argument);
					}

					break;
			}

			return base.ConvertConversion(func);
		}
	}
}
