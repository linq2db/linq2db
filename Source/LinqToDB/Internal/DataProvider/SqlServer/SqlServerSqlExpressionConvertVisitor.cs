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
		/// <summary>
		/// <c>DATEDIFF_BIG</c> counts nanoseconds, and <c>datetime2</c> stores 100ns, so the leftover of a total
		/// is exact. It is only ever applied to a sub-unit window, well inside the roughly 292 years at which the
		/// nanosecond form would overflow.
		/// </summary>
		/// <remarks>
		/// The nanosecond datepart arrived with <c>datetime2</c> in 2008 - <c>DATEADD</c> on 2005 answers <em>is not
		/// a recognized dateadd option</em> - so that version counts in milliseconds instead, which is as fine as its
		/// own <c>datetime</c> resolves anyway. Version-checked here rather than overridden on the 2005 visitor,
		/// because the 2008 one derives from it and would inherit the wrong answer.
		/// </remarks>
		protected override SqlIntervalUnit? FinestDateUnit =>
			_sqlServerVersion >= SqlServerVersion.v2008 ? SqlIntervalUnit.Nanosecond : SqlIntervalUnit.Millisecond;

		static string? DatePartName(SqlIntervalUnit unit)
		{
			return unit switch
			{
				SqlIntervalUnit.Nanosecond  => "nanosecond",
				SqlIntervalUnit.Day         => "day",
				SqlIntervalUnit.Hour        => "hour",
				SqlIntervalUnit.Minute      => "minute",
				SqlIntervalUnit.Second      => "second",
				SqlIntervalUnit.Millisecond => "millisecond",
				SqlIntervalUnit.Microsecond => "microsecond",
				_                           => null,
			};
		}

		protected override ISqlExpression? ShiftDate(SqlIntervalUnit unit, ISqlExpression amount, ISqlExpression date)
		{
			var part = DatePartName(unit);
			if (part == null)
				return null;

			// The amount is cast down: DATEADD takes a 32-bit number, and an amount computed from a tick count
			// arrives here as BIGINT even when its value is small - which SQL Server rejects as an overflow rather
			// than narrowing on its own.
			return Factory.Function(Factory.GetDbDataType(date), "DateAdd",
				Factory.NotNullExpression(Factory.GetDbDataType(typeof(string)), part),
				Factory.Cast(amount, Factory.GetDbDataType(typeof(int)), true),
				date);
		}

		protected override ISqlExpression? CountDateBoundaries(SqlIntervalUnit unit, ISqlExpression start, ISqlExpression end)
		{
			var part = DatePartName(unit);
			if (part == null)
				return null;

			// DateDiff_Big, not DateDiff: the 32-bit form overflows at about 24 days in milliseconds. Counting
			// whole units keeps the number small, but the caller may ask for a fine unit over a long range.
			return Factory.Function(Factory.GetDbDataType(typeof(long)), "DateDiff_Big",
				Factory.NotNullExpression(Factory.GetDbDataType(typeof(string)), part), start, end);
		}

		/// <summary>
		/// <c>DATEDIFF_BIG</c> arrived in 2016; earlier versions leave date subtraction to .NET.
		/// </summary>
		public override bool CanLowerIntervalDifference => _sqlServerVersion >= SqlServerVersion.v2016;

		readonly SqlServerVersion _sqlServerVersion;

		public SqlServerSqlExpressionConvertVisitor(bool allowModify, SqlServerVersion sqlServerVersion) : base(allowModify)
		{
			_sqlServerVersion = sqlServerVersion;
		}

		protected override bool SupportsDistinctAsExistsIntersect => _sqlServerVersion < SqlServerVersion.v2022;

		public override ISqlExpression ConvertConcat(SqlConcatExpression element)
		{
			// SQL Server's `+` (and 2025+ `||`) operator and `CONCAT(...)` function reject
			// `text`/`ntext` operands ("The data types nvarchar and ntext are incompatible
			// in the add operator"). These LOB types have been deprecated since 2005 and
			// cannot participate in string operations — cast them up to `[N]VARCHAR(MAX)`
			// before delegating to the base concat lowering.
			ISqlExpression[]? operands = null;

			for (var i = 0; i < element.Expressions.Length; i++)
			{
				var operand     = element.Expressions[i];
				var operandType = QueryHelper.GetDbDataType(operand, MappingSchema);

				var castTo = operandType.DataType switch
				{
					DataType.NText => new DbDataType(typeof(string), DataType.NVarChar),
					DataType.Text  => new DbDataType(typeof(string), DataType.VarChar),
					_              => default(DbDataType?),
				};

				if (castTo == null)
					continue;

				operands    ??= (ISqlExpression[])element.Expressions.Clone();
				operands[i] = PseudoFunctions.MakeCast(operand, castTo.Value);
			}

			if (operands != null)
				element = new SqlConcatExpression(element.PreserveNull, operands);

			return base.ConvertConcat(element);
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
						// Precedence stated so this reads like every other remainder. Left unstated it takes the
						// constructor default and the renderer brackets it, which is how a float % on this provider
						// came to be parenthesised while the generated ones no longer are.
						return new SqlBinaryExpression(
							element.Expr2.SystemType!,
							new SqlFunction(MappingSchema.GetDbDataType(typeof(int)), "Convert", SqlDataType.Int32, element.Expr1),
							element.Operation,
							element.Expr2,
							Precedence.Multiplicative);
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
					var funcType  = Factory.GetDbDataType(typeof(int));

					var valueString = Factory.Concat(value, Factory.Value(valueType, "."));
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
