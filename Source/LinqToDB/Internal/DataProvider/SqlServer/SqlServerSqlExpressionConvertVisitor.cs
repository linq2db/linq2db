using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.SqlServer
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
								new SqlFunction(MappingSchema.GetDbDataType(typeof(byte[])), "Convert", SqlDataType.DbVarBinary, new SqlFunction(
									MappingSchema.GetDbDataType(typeof(string)), "LEFT", predicate.Expr1,
									new SqlFunction(MappingSchema.GetDbDataType(typeof(int)), "LEN", predicate.Expr2))),
								SqlPredicate.Operator.Equal,
								new SqlFunction(MappingSchema.GetDbDataType(typeof(byte[])), "Convert", SqlDataType.DbVarBinary, predicate.Expr2),
								null
							);

						break;
					}

					case SqlPredicate.SearchString.SearchKind.EndsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(MappingSchema.GetDbDataType(typeof(byte[])), "Convert", SqlDataType.DbVarBinary, new SqlFunction(
									MappingSchema.GetDbDataType(typeof(string)), "RIGHT", predicate.Expr1,
									new SqlFunction(MappingSchema.GetDbDataType(typeof(int)), "LEN", predicate.Expr2))),
								SqlPredicate.Operator.Equal,
								new SqlFunction(MappingSchema.GetDbDataType(typeof(byte[])), "Convert", SqlDataType.DbVarBinary, predicate.Expr2),
								null
							);

						break;
					}
					case SqlPredicate.SearchString.SearchKind.Contains:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(MappingSchema.GetDbDataType(typeof(int)), "CHARINDEX",
									new SqlFunction(MappingSchema.GetDbDataType(typeof(byte[])), "Convert", SqlDataType.DbVarBinary,
										predicate.Expr2),
									new SqlFunction(MappingSchema.GetDbDataType(typeof(byte[])), "Convert", SqlDataType.DbVarBinary,
										predicate.Expr1)),
								SqlPredicate.Operator.Greater,
								new SqlValue(0), null);

						break;
					}

				}

				if (subStrPredicate != null)
				{
					var result = new SqlSearchCondition(predicate.IsNot, canBeUnknown: null,
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
							new SqlFunction(MappingSchema.GetDbDataType(typeof(int)), "Convert", SqlDataType.Int32, element.Expr1),
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

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func.Name)
			{
				case PseudoFunctions.LENGTH:
				{
					/*
					 * LEN(value + ".") - 1
					 */

					var value     = func.Parameters[0];
					var valueType = Factory.GetDbDataType(value);
					var funcType  = Factory.GetDbDataType(value);

					var valueString = Factory.Add(valueType, value, Factory.Value(valueType, "."));
					var valueLength = Factory.Function(funcType, "LEN", valueString);

					return Factory.Sub(func.Type, valueLength, Factory.Value(func.Type, 1));
	}
}

			return base.ConvertSqlFunction(func);
		}

		protected override ISqlExpression WrapColumnExpression(ISqlExpression expr)
		{
			if (expr is SqlValue
				{
					Value: uint or long or ulong or float or double or decimal,
				} value)
			{
				expr = new SqlCastExpression(expr, value.ValueType, null, isMandatory: true);
			}

			if (expr is SqlParameter { IsQueryParameter: false } param)
			{
				var paramType = param.Type.SystemType.UnwrapNullableType();
				if (paramType == typeof(uint)
					|| paramType == typeof(long)
					|| paramType == typeof(ulong)
					|| paramType == typeof(float)
					|| paramType == typeof(double)
					|| paramType == typeof(decimal))
					expr = new SqlCastExpression(expr, param.Type, null, isMandatory: true);
			}

			return base.WrapColumnExpression(expr);
		}
	}
}
