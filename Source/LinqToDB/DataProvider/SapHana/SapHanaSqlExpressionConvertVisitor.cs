using System;

namespace LinqToDB.DataProvider.SapHana
{
	using LinqToDB.Extensions;
	using LinqToDB.SqlProvider;
	using LinqToDB.SqlQuery;

	public class SapHanaSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public SapHanaSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		#region LIKE

		// https://help.sap.com/viewer/4fe29514fd584807ac9f2a04f6754767/2.0.01/en-US/20fa17f375191014a4d8d8cbfddfe340.html
		protected static   string[] HanaLikeCharactersToEscape = { "%", "_" };
		public    override string[] LikeCharactersToEscape => HanaLikeCharactersToEscape;

		#endregion

		//this is for Tests.Linq.Common.CoalesceLike test
		static SqlFunction ConvertCase(SqlFunction? func, Type systemType, ISqlExpression[] parameters, int start)
		{
			var len  = parameters.Length - start;
			var cond = parameters[start];

			if (start == 0 && SqlExpression.NeedsEqual(cond))
			{
				cond = new SqlSearchCondition(
					new SqlCondition(
						false,
						new SqlPredicate.ExprExpr(cond, SqlPredicate.Operator.Equal, new SqlValue(1), null)));
			}

			const string name = "CASE";

			if (len == 2)
			{
				if (func != null && start == 0 && ReferenceEquals(parameters[start], cond))
					return func;

				return new (systemType, name, cond, parameters[start + 1]);
			}

			if (len == 3)
			{
				if (func != null && start == 0 && ReferenceEquals(parameters[start], cond))
					return func;

				return new (systemType, name, cond, parameters[start + 1], parameters[start + 2]);
			}

			return new SqlFunction(systemType, name,
				cond,
				parameters[start                                + 1],
				ConvertCase(null, systemType, parameters, start + 2));
		}

		//this is for Tests.Linq.Common.CoalesceLike test
		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func, false);
			switch (func.Name)
			{
				case "CASE": func = ConvertCase(func, func.SystemType, func.Parameters, 0);
					break;
			}

			return base.ConvertSqlFunction(func);
		}

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				case "%":
					return new SqlFunction(element.SystemType, "MOD", element.Expr1, element.Expr2);
				case "&":
					return new SqlFunction(element.SystemType, "BITAND", element.Expr1, element.Expr2);
				case "|":
					return Sub(
						Add(element.Expr1, element.Expr2, element.SystemType),
						new SqlFunction(element.SystemType, "BITAND", element.Expr1, element.Expr2),
						element.SystemType);
				case "^": // (a + b) - BITAND(a, b) * 2
					return Sub(
						Add(element.Expr1, element.Expr2, element.SystemType),
						Mul(new SqlFunction(element.SystemType, "BITAND", element.Expr1, element.Expr2), 2),
						element.SystemType);
				case "+":
					return element.SystemType == typeof(string) ?
						new SqlBinaryExpression(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence) :
						element;
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		protected override ISqlExpression ConvertConversion(SqlFunction func)
		{
			var ftype = func.SystemType.ToUnderlying();

			if (ftype == typeof(bool))
			{
				var ex = AlternativeConvertToBoolean(func, 2);
				if (ex != null)
					return ex;
			}
			return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, FloorBeforeConvert(func, func.Parameters[2]), func.Parameters[0]);
		}
	}
}
