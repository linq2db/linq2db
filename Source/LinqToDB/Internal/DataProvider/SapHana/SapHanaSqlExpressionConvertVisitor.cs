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

		#region LIKE

		// https://help.sap.com/viewer/4fe29514fd584807ac9f2a04f6754767/2.0.01/en-US/20fa17f375191014a4d8d8cbfddfe340.html
		          static   string[] HanaLikeCharactersToEscape = { "%", "_" };
		public    override string[] LikeCharactersToEscape => HanaLikeCharactersToEscape;

		#endregion

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				case "%":
					return new SqlFunction(element.Type, "MOD", element.Expr1, element.Expr2);
				case "&":
					return new SqlFunction(element.Type, "BITAND", element.Expr1, element.Expr2);
				case "|":
					return Sub(
						Add(element.Expr1, element.Expr2, element.SystemType),
						new SqlFunction(element.Type, "BITAND", element.Expr1, element.Expr2),
						element.SystemType);
				case "^": // (a + b) - BITAND(a, b) * 2
					return Sub(
						Add(element.Expr1, element.Expr2, element.SystemType),
						Mul(new SqlFunction(element.Type, "BITAND", element.Expr1, element.Expr2), 2),
						element.SystemType);
				case "+":
					return element.SystemType == typeof(string) ?
						new SqlBinaryExpression(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence) :
						element;
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		protected override ISqlExpression WrapColumnExpression(ISqlExpression expr)
		{
			if (expr is SqlValue
				{
					Value: uint or long or ulong or float or double or decimal
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
