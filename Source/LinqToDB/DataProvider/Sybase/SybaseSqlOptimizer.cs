namespace LinqToDB.DataProvider.Sybase
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using LinqToDB.Common;
	using LinqToDB.Extensions;
	using LinqToDB.Tools;
	using SqlProvider;
	using SqlQuery;

	class SybaseSqlOptimizer : BasicSqlOptimizer
	{
		public SybaseSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		protected static string[] SybaseCharactersToEscape = {"_", "%", "[", "]", "^"};

		public override string[] LikeCharactersToEscape => SybaseCharactersToEscape;

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

				// remove last digit as max precision Sybase supports is 6 for bigdatetime
				// in 10^-6 seconds
				var increment = ts.Value.Ticks / 10;

				if (isSubstraction)
					increment = -increment;

				var maxValue = increment < 0 ? int.MinValue : int.MaxValue;

				if (increment % 1000 != 0)
				{
					var interval = (int)(increment % maxValue);
					increment   -= interval;
					expr1        = CreateDateAdd(dbType1.SystemType!, "us", interval, expr1, expr2);
				}

				// in milliseconds
				increment /= 1000;

				if (increment % 1000 != 0)
				{
					var interval = (int)(increment % maxValue);
					increment   -= interval;
					expr1        = CreateDateAdd(dbType1.SystemType!, "ms", interval, expr1, expr2);
				}

				// in seconds
				increment /= 1000;

				if (increment % 60 != 0)
				{
					var interval = (int)(increment % maxValue);
					increment   -= interval;
					expr1        = CreateDateAdd(dbType1.SystemType!, "ss", interval, expr1, expr2);
				}

				// in minutes
				increment /= 60;

				if (increment % 60 != 0)
				{
					var interval = (int)(increment % maxValue);
					increment   -= interval;
					expr1        = CreateDateAdd(dbType1.SystemType!, "mi", interval, expr1, expr2);
				}

				// in hours (hours is last interval, as max interval will fit into it)
				increment /= 60;

				if (increment != 0)
				{
					var interval = (int)increment;
					expr1        = CreateDateAdd(dbType1.SystemType!, "hh", interval, expr1, expr2);
				}

				generated = expr1;

				return true;
			}

			generated = null;
			return false;

			static ISqlExpression CreateDateAdd(Type resultType, string interval, int increment, ISqlExpression dateTime, ISqlExpression intervalSource)
			{
				return new SqlFunction(
						resultType,
						"DateAdd",
						false,
						true,
						new SqlExpression(typeof(string), interval, Precedence.Primary),
						CreateSqlValue(increment, new DbDataType(typeof(int)), intervalSource),
						dateTime)
				{ CanBeNull = dateTime.CanBeNull || intervalSource.CanBeNull };
			}
		}

		protected override ISqlExpression ConvertFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func, false);

			switch (func.Name)
			{
				case "$Replace$": return new SqlFunction(func.SystemType, "Str_Replace", func.IsAggregate, func.IsPure, func.Precedence, func.Parameters);

				case "CharIndex":
				{
					if (func.Parameters.Length == 3)
						return Add<int>(
							new SqlFunction(func.SystemType, "CharIndex",
								func.Parameters[0],
								new SqlFunction(typeof(string), "Substring",
									func.Parameters[1],
									func.Parameters[2],
									new SqlFunction(typeof(int), "Len", func.Parameters[1]))),
							Sub(func.Parameters[2], 1));
					break;
				}

				case "Stuff":
				{
					if (func.Parameters[3] is SqlValue value)
					{
						if (value.Value is string @string && string.IsNullOrEmpty(@string))
							return new SqlFunction(
								func.SystemType,
								func.Name,
								false,
								func.Precedence,
								func.Parameters[0],
								func.Parameters[1],
								func.Parameters[1],
								new SqlValue(value.ValueType, null));
					}

					break;
				}
			}

			return base.ConvertFunction(func);
		}

		public override ISqlExpression ConvertExpressionImpl(ISqlExpression expr, ConvertVisitor visitor, EvaluationContext context)
		{
			expr = base.ConvertExpressionImpl(expr, visitor, context);

			switch (expr.ElementType)
			{
				case QueryElementType.SqlBinaryExpression:
				{
					var be = (SqlBinaryExpression)expr;

					switch (be.Operation)
					{
						case "+":
						{
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

					break;
				}

			}

			return expr;
		}
	}
}
