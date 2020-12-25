using System;

namespace LinqToDB.DataProvider.SapHana
{
	using System.Diagnostics.CodeAnalysis;
	using Extensions;
	using LinqToDB.Common;
	using LinqToDB.Tools;
	using SqlProvider;
	using SqlQuery;

	class SapHanaSqlOptimizer : BasicSqlOptimizer
	{
		public SapHanaSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{

		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			switch (statement.QueryType)
			{
				case QueryType.Delete: statement = GetAlternativeDelete((SqlDeleteStatement) statement); break;
				case QueryType.Update: statement = GetAlternativeUpdate((SqlUpdateStatement) statement); break;
			}
			return statement;
		}

		static bool GenerateDateAdd(ISqlExpression expr1, ISqlExpression expr2, bool isSubstraction, EvaluationContext context,
			[MaybeNullWhen(false)] out ISqlExpression generated)
		{
			var dbType1 = expr1.GetExpressionType();
			var dbType2 = expr2.GetExpressionType();

			if (dbType1.SystemType.ToNullableUnderlying().In(typeof(DateTime), typeof(DateTimeOffset))
				&& dbType2.SystemType.ToNullableUnderlying() == typeof(TimeSpan)
				&& expr2.TryEvaluateExpression(context, out var value))
			{
				var ts = value as TimeSpan?;

				if (ts == null)
				{
					generated = new SqlValue(dbType1, null);
					return true;
				}

				var increment = ts.Value.Ticks;

				if (isSubstraction)
					increment = -increment;

				generated = new SqlFunction(
						dbType1.SystemType!,
						"ADD_NANO100",
						false,
						true,
						expr1,
						CreateSqlValue(increment, new DbDataType(typeof(long)), expr2))
					{ CanBeNull = expr1.CanBeNull || expr2.CanBeNull };

				return true;
			}

			generated = null;
			return false;
		}

		public override ISqlExpression ConvertExpressionImpl(ISqlExpression expr, ConvertVisitor visitor,
			EvaluationContext context)
		{
			expr = base.ConvertExpressionImpl(expr, visitor, context);

			if (expr is SqlFunction func)
			{
				if (func.Name == "Convert")
				{
					var ftype = func.SystemType.ToUnderlying();

					if (ftype == typeof(bool))
					{
						var ex = AlternativeConvertToBoolean(func, 1);
						if (ex != null)
							return ex;
					}
					return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, FloorBeforeConvert(func), func.Parameters[0]);
				}
			}
			else if (expr is SqlBinaryExpression be)
			{
				switch (be.Operation)
				{
					case "%":
						return new SqlFunction(be.SystemType, "MOD", be.Expr1, be.Expr2);
					case "&": 
						return new SqlFunction(be.SystemType, "BITAND", be.Expr1, be.Expr2);
					case "|":
						return Sub(
							Add(be.Expr1, be.Expr2, be.SystemType),
							new SqlFunction(be.SystemType, "BITAND", be.Expr1, be.Expr2),
							be.SystemType);
					case "^": // (a + b) - BITAND(a, b) * 2
						return Sub(
							Add(be.Expr1, be.Expr2, be.SystemType),
							Mul(new SqlFunction(be.SystemType, "BITAND", be.Expr1, be.Expr2), 2),
							be.SystemType);
					case "+":
					{
						if (be.SystemType == typeof(string))
							return new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence);

						if (GenerateDateAdd(be.Expr1, be.Expr2, false, context, out var generated))
							return generated;

						if (GenerateDateAdd(be.Expr2, be.Expr1, false, context, out generated))
							return generated;

						break;
					}
					case "-":
					{
						if (GenerateDateAdd(be.Expr1, be.Expr2, true, context, out var generated))
							return generated;

						break;
					}
				}
			}

			return expr;
		}

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

			if (len == 3)
			{
				if (func != null && start == 0 && ReferenceEquals(parameters[start], cond))
				{
					return func;
				}
				return new SqlFunction(systemType, name, cond, parameters[start + 1], parameters[start + 2]);
			}
			
			return new SqlFunction(systemType, name,
				cond,
				parameters[start + 1],
				ConvertCase(null, systemType, parameters, start + 2));
		}

		//this is for Tests.Linq.Common.CoalesceLike test
		protected override ISqlExpression ConvertFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func, false);
			switch (func.Name)
			{
				case "CASE": func = ConvertCase(func, func.SystemType, func.Parameters, 0);
					break;
			}
			return base.ConvertFunction(func);
		}

		// https://help.sap.com/viewer/4fe29514fd584807ac9f2a04f6754767/2.0.01/en-US/20fa17f375191014a4d8d8cbfddfe340.html
		protected static   string[] HanaLikeCharactersToEscape = { "%", "_" };
		public    override string[] LikeCharactersToEscape     => HanaLikeCharactersToEscape;
	}
}
