using System;

namespace LinqToDB.DataProvider.Informix
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	public class InformixSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public InformixSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		public override ISqlPredicate ConvertLikePredicate(SqlPredicate.Like predicate)
		{
			//Informix cannot process parameter in Like template (only Informix provider, not InformixDB2)
			//
			if (EvaluationContext.ParameterValues != null)
			{
				var exp2 = TryConvertToValue(predicate.Expr2, EvaluationContext);

				if (!ReferenceEquals(exp2, predicate.Expr2))
				{
					predicate = new SqlPredicate.Like(predicate.Expr1, predicate.IsNot, exp2, predicate.Escape);
				}
			}

			return predicate;
		}

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				case "%": return new SqlFunction(element.SystemType, "Mod", element.Expr1, element.Expr2);
				case "&": return new SqlFunction(element.SystemType, "BitAnd", element.Expr1, element.Expr2);
				case "|": return new SqlFunction(element.SystemType, "BitOr", element.Expr1, element.Expr2);
				case "^": return new SqlFunction(element.SystemType, "BitXor", element.Expr1, element.Expr2);
				case "+": return element.SystemType == typeof(string) ? new SqlBinaryExpression(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence) : element;
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func.Name)
			{
				// passing parameter to NVL will result in "A syntax error has occurred." error from server
				case "Coalesce" : return ConvertCoalesceToBinaryFunc(func, "Nvl", supportsParameters: false);
			}

			func = ConvertFunctionParameters(func, false);

			return base.ConvertSqlFunction(func);
		}

		protected override ISqlExpression ConvertConversion(SqlFunction func)
		{
			var toType   = func.Parameters[0];
			var argument = func.Parameters[2];

			var isNull = argument is SqlValue sqlValue && sqlValue.Value == null;

			if (!isNull)
			{
				switch (Type.GetTypeCode(func.SystemType.ToUnderlying()))
				{
					case TypeCode.String   :
					{
						var stype = argument.SystemType!.ToUnderlying();
						if (stype == typeof(DateTime))
						{
							return new SqlFunction(func.SystemType, "To_Char", argument, new SqlValue("%Y-%m-%d %H:%M:%S.%F"));
						}
#if NET6_0_OR_GREATER
						else if (stype == typeof(DateOnly))
						{
							return new SqlFunction(func.SystemType, "To_Char", argument, new SqlValue("%Y-%m-%d"));
						}
#endif
						return new SqlFunction(func.SystemType, "To_Char", argument);
					}

					case TypeCode.Boolean  :
					{
						var ex = AlternativeConvertToBoolean(func, 2);
						if (ex != null)
							return ex;
						break;
					}

					case TypeCode.UInt64   :
						if (argument.SystemType!.IsFloatType())
							argument = new SqlFunction(func.SystemType, "Floor", argument);
						break;

					case TypeCode.DateTime :
						if (IsDateDataType(toType, "Date"))
						{
							if (argument.SystemType == typeof(string))
							{
								return new SqlFunction(
									func.SystemType,
									"Date",
									new SqlFunction(func.SystemType, "To_Date", argument, new SqlValue("%Y-%m-%d")));
							}

							return new SqlFunction(func.SystemType, "Date", argument);
						}

						if ((IsDateTime2Type(func.Parameters[0], "DateTime2")
								|| IsDateTimeType(func.Parameters[0], "DateTime")
								|| IsSmallDateTimeType(func.Parameters[0], "SmallDateTime"))
							&& argument.SystemType == typeof(string))
							return new SqlFunction(func.SystemType, "To_Date", argument, new SqlValue("%Y-%m-%d %H:%M:%S"));

						if (IsTimeDataType(func.Parameters[0]))
							return new SqlExpression(func.SystemType, "Cast(Extend({0}, hour to second) as Char(8))", Precedence.Primary, argument);

						return new SqlFunction(func.SystemType, "To_Date", argument);

					default:
						if (func.SystemType.ToUnderlying() == typeof(DateTimeOffset))
							goto case TypeCode.DateTime;
						break;
				}
			}

			return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, argument, toType);
		}
	}
}
