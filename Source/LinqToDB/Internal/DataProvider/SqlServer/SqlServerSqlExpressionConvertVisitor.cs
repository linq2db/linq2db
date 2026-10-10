using System;

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

		/// <summary>
		/// On 2005 the measurement stops at the millisecond, so a component asked for below one is identically zero
		/// rather than merely imprecise - declined for the reason SQL CE and Sybase decline it.
		/// </summary>
		public override SqlIntervalUnit IntervalResolution =>
			_sqlServerVersion >= SqlServerVersion.v2008 ? SqlIntervalUnit.Tick : SqlIntervalUnit.Millisecond;

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

			var partName = Factory.NotNullExpression(Factory.GetDbDataType(typeof(string)), part);

			// DateDiff_Big, not DateDiff: the 32-bit form overflows at about 24 days in milliseconds. Counting
			// whole units keeps the number small, but the caller may ask for a fine unit over a long range.
			if (_sqlServerVersion >= SqlServerVersion.v2016)
				return Factory.Function(Factory.GetDbDataType(typeof(long)), "DateDiff_Big", partName, start, end);

			// Before 2016 there is only the 32-bit form, so every window counted here is kept short enough for its
			// unit - see ElapsedTicks. The count is widened because the arithmetic built on it is not: a day count
			// scaled to ticks is past the INT range long before the day count itself is.
			return Factory.Cast(
				Factory.Function(Factory.GetDbDataType(typeof(int)), "DateDiff", partName, start, end),
				Factory.GetDbDataType(typeof(long)), true);
		}

		/// <summary>
		/// Before 2016 the base decomposition cannot be used as it is once the finest unit is the nanosecond: it
		/// counts the remainder after whole days in that unit, and a day is 8.64e13 nanoseconds against the
		/// roughly 2.1e9 a 32-bit <c>DATEDIFF</c> holds. The remainder is split once more instead - whole seconds
		/// within it, then the nanoseconds within the last second, at most 1e9 - so every count stays in range and
		/// the result stays exact at the 100ns <c>datetime2</c> stores.
		/// </summary>
		/// <remarks>
		/// Each count is the raw boundary count, measured from exactly where the previous shift landed, so an
		/// overshoot at one step returns as a negative remainder at the next and the parts telescope, as in the base.
		/// That needs the second shift to be exact, which two storage types do not give: <c>DATEADD</c> refuses a
		/// second on a <c>date</c>, and rounds a <c>smalldatetime</c> to the minute. So the anchor is widened to
		/// <c>datetime2</c> first, which holds either of them exactly - whatever the mapping declares, since that is
		/// not what the column stores: a <c>date</c> column mapped as a plain <see cref="DateTime"/> is typed
		/// <c>datetime2</c> here.
		/// </remarks>
		protected override ISqlExpression? ElapsedTicks(SqlIntervalDifferenceExpression element)
		{
			if (_sqlServerVersion >= SqlServerVersion.v2016 || FinestDateUnit != SqlIntervalUnit.Nanosecond)
				return base.ElapsedTicks(element);

			var days = CountDateBoundaries(SqlIntervalUnit.Day, element.Start, element.End);
			if (days == null)
				return null;

			var dayAnchor = ShiftDate(SqlIntervalUnit.Day, days, element.Start);
			if (dayAnchor == null)
				return null;

			dayAnchor = AsDateTime2(dayAnchor);

			var seconds = CountDateBoundaries(SqlIntervalUnit.Second, dayAnchor, element.End);
			if (seconds == null)
				return null;

			var secondAnchor = ShiftDate(SqlIntervalUnit.Second, seconds, dayAnchor);
			if (secondAnchor == null)
				return null;

			var nanoseconds = CountDateBoundaries(SqlIntervalUnit.Nanosecond, secondAnchor, element.End);
			if (nanoseconds == null)
				return null;

			var longType = Factory.GetDbDataType(typeof(long));

			// Scaled in factors that each fit in 32 bits, for the reason the base gives: a wider literal may be read
			// as a decimal and take the whole expression with it.
			var dayTicks = Factory.Multiply(longType,
				Factory.Multiply(longType, days, (long)TimeSpan.TicksPerDay / TimeSpan.TicksPerSecond),
				TimeSpan.TicksPerSecond);

			var secondTicks = Factory.Multiply(longType, seconds, TimeSpan.TicksPerSecond);

			var remainderTicks = Factory.Div(longType, nanoseconds, Factory.Value(longType, 100L));

			return Factory.Add(longType, Factory.Add(longType, dayTicks, secondTicks), remainderTicks);
		}

		/// <summary>
		/// Shifts as the base does - days, then seconds, then the rest in the finest unit - over the value widened to
		/// <c>datetime2</c> first, or to <c>datetime</c> on 2005.
		/// </summary>
		/// <remarks>
		/// <c>DATEADD</c> refuses the nanosecond the last step adds on a <c>date</c>, a <c>smalldatetime</c> and a
		/// <c>datetime</c> - the common type before 2008, and what <c>GETDATE()</c> returns - and the first two would
		/// not hold the time of day it adds anyway. The widening is applied whatever the mapping declares, for the
		/// reason <see cref="ElapsedTicks"/> gives. 2005 has no <c>datetime2</c> and counts in milliseconds, which
		/// <c>DATEADD</c> takes on a <c>datetime</c>; a <c>smalldatetime</c> is still widened there, since it keeps
		/// no seconds and would round the result to the minute.
		/// </remarks>
		protected override ISqlExpression? LowerTemporalArithmetic(SqlTemporalArithmeticExpression element)
		{
			var widened = _sqlServerVersion >= SqlServerVersion.v2008
				? AsDateTime2(element.Temporal)
				: Widened(element.Temporal, DataType.DateTime);

			return base.LowerTemporalArithmetic(
				new SqlTemporalArithmeticExpression(widened, element.Interval, element.IsSubtract, element.Type));
		}

		/// <summary>
		/// A date/time value cast to <c>datetime2</c>, which holds every other SQL Server date/time type exactly; a
		/// zoned one as it is.
		/// </summary>
		ISqlExpression AsDateTime2(ISqlExpression value)
		{
			return Widened(value, DataType.DateTime2);
		}

		/// <summary>
		/// A date/time value cast to <paramref name="target"/>; a zoned one as it is.
		/// </summary>
		/// <remarks>
		/// The target is built from the CLR type alone, so no <c>DbType</c> of the mapping rides along and renders the
		/// cast as the type it was meant to leave. The cast is mandatory: the declared type is not what the column
		/// stores, and the optimizer would drop a cast it believes is a no-op. A <c>datetimeoffset</c> is not cast,
		/// because <c>DATEADD</c> is exact on it and a cast would lose the offset.
		/// </remarks>
		ISqlExpression Widened(ISqlExpression value, DataType target)
		{
			var type = QueryHelper.GetDbDataType(value, MappingSchema);

			if (type.DataType == DataType.DateTimeOffset || type.SystemType.ToUnderlying() != typeof(DateTime))
				return value;

			return Factory.Cast(value, new DbDataType(type.SystemType, target), true);
		}

		/// <summary>
		/// Every version counts elapsed time: 2016 and later through <c>DATEDIFF_BIG</c>, earlier ones through the
		/// 32-bit <c>DATEDIFF</c> over windows kept short enough for it.
		/// </summary>
		public override bool CanLowerIntervalDifference => true;

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

		public override ISqlExpression ConvertDefaultValue(SqlDefaultValueExpression expression)
		{
			if (LeastDate(expression.Type) is { } least && RaiseDefaultDate(expression, least) is { } raised)
			{
				// Every path that clamps writes a DateTimeOffset through LocalDateTime, which moves with the time zone.
				return raised.Value is DateTimeOffset dateTimeOffset
					? new SqlValue(expression.Type.WithSystemType(typeof(DateTime)), dateTimeOffset.DateTime)
					: raised;
			}

			return base.ConvertDefaultValue(expression);
		}

		// The least date of the type the mapping schema's converters write a date as, where it is later than 0001-01-01: a text
		// type is a string, date and datetime2 hold 0001-01-01 from 2008 on and so does the datetimeoffset an offset outside the
		// datetime family is written as, and everything else is written as datetime.
		DateTime? LeastDate(DbDataType type)
		{
			var v2008Plus = _sqlServerVersion >= SqlServerVersion.v2008;

			return type.DataType switch
			{
				DataType.Char or DataType.VarChar or DataType.Text or DataType.NChar or DataType.NVarChar or DataType.NText => null,

				DataType.SmallDateTime                                                             => new DateTime(1900, 1, 1),
				DataType.DateTime                                                                  => new DateTime(1753, 1, 1),
				DataType.Date or DataType.DateTime2 when v2008Plus                                 => null,
				_ when v2008Plus && type.SystemType.UnwrapNullableType() == typeof(DateTimeOffset) => null,
				_                                                                                  => new DateTime(1753, 1, 1),
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
					expr = new SqlCastExpression(expr, param.Type, null, isMandatory: true);
			}

			return base.WrapColumnExpression(expr);
		}

		/// <summary>
		/// Every ranking function and every one that reads a neighbouring row - <c>The function 'ROW_NUMBER' must
		/// have an OVER clause with ORDER BY</c> - and any frame at all, which is <c>Incorrect syntax near 'ROWS'</c>
		/// without one. An unframed aggregate is exempt: <c>SUM(x) OVER ()</c> is valid.
		/// </summary>
		protected override bool IsWindowOrderByRequired(SqlExtendedFunction func)
			=> base.IsWindowOrderByRequired(func)
				|| func.FrameClause != null
				|| IsOrderDependentWindowFunction(func.FunctionName)
				|| func.FunctionName is "NTILE" or "FIRST_VALUE" or "LAST_VALUE";
	}
}
