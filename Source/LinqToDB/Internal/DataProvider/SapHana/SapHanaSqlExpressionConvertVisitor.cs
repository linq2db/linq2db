using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.DataProvider.SapHana
{
	public class SapHanaSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public SapHanaSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		protected override bool SupportsDistinctAsExistsIntersect => true;
		protected override bool ConcatRequiresExplicitStringCast  => false;

		/// <inheritdoc />
		public override bool CanLowerIntervalDifference => true;

		/// <summary>
		/// <c>NANO100_BETWEEN</c> counts hundred-nanosecond units, which is a tick and is also what a SAP HANA
		/// timestamp stores, so the elapsed count needs no scaling and loses nothing.
		/// </summary>
		protected override ISqlExpression? ElapsedTicks(SqlIntervalDifferenceExpression element)
		{
			return Factory.Function(Factory.GetDbDataType(typeof(long)), "Nano100_Between", element.Start, element.End);
		}

		#region LIKE

		// https://help.sap.com/viewer/4fe29514fd584807ac9f2a04f6754767/2.0.01/en-US/20fa17f375191014a4d8d8cbfddfe340.html
		static readonly string[] HanaLikeCharactersToEscape = { "%", "_" };
		public override string[] LikeCharactersToEscape => HanaLikeCharactersToEscape;

		#endregion

		public override ISqlExpression ConvertSqlUnaryExpression(SqlUnaryExpression element)
		{
			if (element.Operation is SqlUnaryOperation.BitwiseNegation)
				return new SqlFunction(element.Type, "BITNOT", element.Expr);

			return base.ConvertSqlUnaryExpression(element);
		}

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			return element.Operation switch
			{
				"%" => new SqlFunction(element.Type, "MOD", element.Expr1, element.Expr2),
				"&" => new SqlFunction(element.Type, "BITAND", element.Expr1, element.Expr2),

				"|" =>
					Sub(
						Add(element.Expr1, element.Expr2, element.SystemType),
						new SqlFunction(element.Type, "BITAND", element.Expr1, element.Expr2),
						element.SystemType
					),

				// (a + b) - BITAND(a, b) * 2
				"^" =>
					Sub(
						Add(element.Expr1, element.Expr2, element.SystemType),
						Mul(new SqlFunction(element.Type, "BITAND", element.Expr1, element.Expr2), 2),
						element.SystemType
					),

				_ => base.ConvertSqlBinaryExpression(element),
			};
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
				{
					expr = new SqlCastExpression(expr, param.Type, null, isMandatory : true);
				}
			}

			return base.WrapColumnExpression(expr);
		}

		/// <summary>
		/// <c>feature not supported: Function must have ORDER BY clause</c> - for every ranking function except
		/// <c>ROW_NUMBER</c>, which SAP HANA is happy to leave unordered, and for the four that read a neighbouring
		/// row. A frame needs one too (<c>Window functions must have ORDER BY clause</c>); an unframed aggregate
		/// does not.
		/// </summary>
		protected override bool IsWindowOrderByRequired(SqlExtendedFunction func)
			=> func.FrameClause != null
				|| IsOrderDependentWindowFunction(func.FunctionName)
				|| func.FunctionName is "NTILE" or "FIRST_VALUE" or "LAST_VALUE";
	}
}
