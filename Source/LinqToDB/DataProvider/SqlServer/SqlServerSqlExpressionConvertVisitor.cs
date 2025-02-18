using LinqToDB.Extensions;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.SqlServer
{
	public class SqlServerSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		readonly SqlServerVersion _sqlServerVersion;

		public SqlServerSqlExpressionConvertVisitor(bool allowModify, SqlServerVersion sqlServerVersion) : base(allowModify)
		{
			_sqlServerVersion = sqlServerVersion;
		}

		protected override bool SupportsDistinctAsExistsIntersect => _sqlServerVersion < SqlServerVersion.v2022;

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var like = base.ConvertSearchStringPredicate(predicate);

			if (predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext) == true)
			{
				SqlPredicate.ExprExpr? subStrPredicate = null;

				switch (predicate.Kind)
				{
					case SqlPredicate.SearchString.SearchKind.StartsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary, new SqlFunction(
									typeof(string), "LEFT", predicate.Expr1,
									new SqlFunction(typeof(int), "LEN", predicate.Expr2))),
								SqlPredicate.Operator.Equal,
								new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary, predicate.Expr2),
								null
							);

						break;
					}

					case SqlPredicate.SearchString.SearchKind.EndsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary, new SqlFunction(
									typeof(string), "RIGHT", predicate.Expr1,
									new SqlFunction(typeof(int), "LEN", predicate.Expr2))),
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
					var result = new SqlSearchCondition(predicate.IsNot,
						like,
						subStrPredicate.MakeNot(predicate.IsNot));

					return result;
				}
			}

			return like;
		}

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				case "%":
				{
					var type1 = element.Expr1.SystemType!.ToUnderlying();

					if (type1 == typeof(double) || type1 == typeof(float))
					{
						return new SqlBinaryExpression(
							element.Expr2.SystemType!,
							new SqlFunction(typeof(int), "Convert", SqlDataType.Int32, element.Expr1),
							element.Operation,
							element.Expr2);
					}

					break;
				}
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			cast = FloorBeforeConvert(cast);

			if (cast.ToType.DataType == DataType.Decimal)
			{
				if (cast.ToType.Precision == null && cast.ToType.Scale == null)
				{
					cast = cast.WithToType(cast.ToType.WithPrecisionScale(38, 17));
				}
			}

			return base.ConvertConversion(cast);
		}
	}
}
