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
				case "Convert"  :
				{
					var par0 = func.Parameters[0];
					var par1 = func.Parameters[1];

					var isNull = par1 is SqlValue sqlValue && sqlValue.Value == null;

					if (!isNull)
					{
						switch (Type.GetTypeCode(func.SystemType.ToUnderlying()))
						{
							case TypeCode.String   :
							{
								var stype = func.Parameters[1].SystemType!.ToUnderlying();
								if (stype == typeof(DateTime))
								{
									return new SqlFunction(func.SystemType, "To_Char", func.Parameters[1], new SqlValue("%Y-%m-%d %H:%M:%S.%F"));
								}
#if NET6_0_OR_GREATER
								else if (stype == typeof(DateOnly))
								{
									return new SqlFunction(func.SystemType, "To_Char", func.Parameters[1], new SqlValue("%Y-%m-%d"));
								}
#endif
								return new SqlFunction(func.SystemType, "To_Char", func.Parameters[1]);
							}

							case TypeCode.Boolean  :
							{
								var ex = AlternativeConvertToBoolean(func, 1);
								if (ex != null)
									return ex;
								break;
							}

							case TypeCode.UInt64   :
								if (func.Parameters[1].SystemType!.IsFloatType())
									par1 = new SqlFunction(func.SystemType, "Floor", func.Parameters[1]);
								break;

							case TypeCode.DateTime :
								if (IsDateDataType(func.Parameters[0], "Date"))
								{
									if (func.Parameters[1].SystemType == typeof(string))
									{
										return new SqlFunction(
											func.SystemType,
											"Date",
											new SqlFunction(func.SystemType, "To_Date", func.Parameters[1], new SqlValue("%Y-%m-%d")));
									}

									return new SqlFunction(func.SystemType, "Date", func.Parameters[1]);
								}

								if ((IsDateTime2Type(func.Parameters[0], "DateTime2")
										|| IsDateTimeType(func.Parameters[0], "DateTime")
										|| IsSmallDateTimeType(func.Parameters[0], "SmallDateTime"))
									&& func.Parameters[1].SystemType == typeof(string))
									return new SqlFunction(func.SystemType, "To_Date", func.Parameters[1], new SqlValue("%Y-%m-%d %H:%M:%S"));

								if (IsTimeDataType(func.Parameters[0]))
									return new SqlExpression(func.SystemType, "Cast(Extend({0}, hour to second) as Char(8))", Precedence.Primary, func.Parameters[1]);

								return new SqlFunction(func.SystemType, "To_Date", func.Parameters[1]);

							default:
								if (func.SystemType.ToUnderlying() == typeof(DateTimeOffset))
									goto case TypeCode.DateTime;
								break;
						}
					}

					return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, par1, par0);
				}
			}

			func = ConvertFunctionParameters(func, false);

			return base.ConvertSqlFunction(func);
		}
	}
}
