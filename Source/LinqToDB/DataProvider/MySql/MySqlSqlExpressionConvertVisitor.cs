using System.Collections.Generic;

namespace LinqToDB.DataProvider.MySql
{
	using System;

	using Extensions;
	using SqlProvider;
	using SqlQuery;

	public class MySqlSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public MySqlSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		public override bool CanCompareSearchConditions => true;

		protected override ISqlExpression ConvertConversion(SqlFunction func)
		{
			var to = (SqlDataType)func.Parameters[0];

			return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, FloorBeforeConvert(func, func.Parameters[2]), to);
		}

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element)
			{
				case SqlBinaryExpression(var type, var ex1, "+", var ex2) when type == typeof(string) :
				{
					return ConvertFunc(new (type, "Concat", ex1, ex2));

					static SqlFunction ConvertFunc(SqlFunction func)
					{
						for (var i = 0; i < func.Parameters.Length; i++)
						{
							switch (func.Parameters[i])
							{
								case SqlBinaryExpression(var t, var e1, "+", var e2) when t == typeof(string) :
								{
									var ps = new List<ISqlExpression>(func.Parameters);

									ps.RemoveAt(i);
									ps.Insert(i,     e1);
									ps.Insert(i + 1, e2);

									return ConvertFunc(new (t, func.Name, ps.ToArray()));
								}

								case SqlFunction(var t, "Concat") f when t == typeof(string) :
								{
									var ps = new List<ISqlExpression>(func.Parameters);

									ps.RemoveAt(i);
									ps.InsertRange(i, f.Parameters);

									return ConvertFunc(new (t, func.Name, ps.ToArray()));
								}
							}
						}

						return func;
					}
				}
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func)
			{
				case SqlFunction(var type, "Convert"):
				{
					var ftype = type.ToUnderlying();

					if (ftype == typeof(bool))
					{
						var ex = AlternativeConvertToBoolean(func, 1);
						if (ex != null)
							return ex;
					}

					if ((ftype == typeof(double) || ftype == typeof(float)) && func.Parameters[1].SystemType!.ToUnderlying() == typeof(decimal))
						return func.Parameters[1];

					return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, FloorBeforeConvert(func), func.Parameters[0]);
				}

			}

			return base.ConvertSqlFunction(func);
		}

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var caseSensitive = predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext);

			if (caseSensitive == null || caseSensitive == false)
			{
				var searchExpr = predicate.Expr2;
				var dataExpr   = predicate.Expr1;

				if (caseSensitive == false)
				{
					searchExpr = PseudoFunctions.MakeToLower(searchExpr);
					dataExpr   = PseudoFunctions.MakeToLower(dataExpr);
				}

				ISqlPredicate? newPredicate = null;
				switch (predicate.Kind)
				{
					case SqlPredicate.SearchString.SearchKind.Contains:
					{
						newPredicate = new SqlPredicate.ExprExpr(
							new SqlFunction(typeof(int), "LOCATE", searchExpr, dataExpr), SqlPredicate.Operator.Greater,
							new SqlValue(0), null);
						break;
					}
				}

				if (newPredicate != null)
				{
					if (predicate.IsNot)
					{
						newPredicate = new SqlSearchCondition(new SqlCondition(true, newPredicate));
					}

					return newPredicate;
				}

				if (caseSensitive == false)
				{
					predicate = new SqlPredicate.SearchString(
						dataExpr,
						predicate.IsNot,
						searchExpr,
						predicate.Kind,
						new SqlValue(false));
				}
			}

			if (caseSensitive == true)
			{
				predicate = new SqlPredicate.SearchString(
					new SqlExpression(typeof(string), $"{{0}} COLLATE utf8_bin", Precedence.Primary, predicate.Expr1),
					predicate.IsNot,
					predicate.Expr2,
					predicate.Kind,
					new SqlValue(false));
			}

			return ConvertSearchStringPredicateViaLike(predicate);
		}

	}
}
