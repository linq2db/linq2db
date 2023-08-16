using System;

using LinqToDB.Extensions;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlProvider;
	using SqlQuery;

	public class SqlServerSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		readonly SqlServerVersion _sqlServerVersion;

		public SqlServerSqlExpressionConvertVisitor(bool allowModify, SqlServerVersion sqlServerVersion) : base(allowModify)
		{
			_sqlServerVersion = sqlServerVersion;
		}

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
									new SqlFunction(typeof(int), "Length", predicate.Expr2))),
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

		protected override ISqlExpression ConvertConversion(SqlFunction func)
		{
			var toType   = func.Parameters[0];
			var fromType = func.Parameters[1];
			var argument = func.Parameters[2];

			if (func.SystemType.ToUnderlying() == typeof(ulong) &&
			    argument.SystemType!.IsFloatType())
			{
				return new SqlFunction(
					func.SystemType,
					func.Name,
					false,
					func.Precedence,
					toType,
					fromType,
					new SqlFunction(func.SystemType, "Floor", argument));
			}

			if (Type.GetTypeCode(func.SystemType.ToUnderlying()) == TypeCode.DateTime)
			{
				var type1 = argument.SystemType!.ToUnderlying();

				if (IsTimeDataType(toType))
				{
					if (type1 == typeof(DateTimeOffset) || type1 == typeof(DateTime))
						if (_sqlServerVersion >= SqlServerVersion.v2008)
							return new SqlExpression(
								func.SystemType, "CAST({0} AS TIME)", Precedence.Primary, argument);
						else
							return new SqlExpression(
								func.SystemType, "Cast(Convert(Char, {0}, 114) as DateTime)", Precedence.Primary, argument);

					if (argument.SystemType == typeof(string))
						return argument;

					return new SqlExpression(
						func.SystemType, "Convert(Char, {0}, 114)", Precedence.Primary, argument);
				}

				if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
				{
					if (IsDateDataType(toType, "Datetime"))
						return new SqlExpression(
							func.SystemType, "Cast(Floor(Cast({0} as Float)) as DateTime)", Precedence.Primary, argument);
				}
			}

			return base.ConvertConversion(func);
		}
	}
}
