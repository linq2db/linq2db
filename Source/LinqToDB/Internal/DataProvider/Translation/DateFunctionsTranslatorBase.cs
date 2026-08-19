using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public abstract class DateFunctionsTranslatorBase : MemberTranslatorBase
	{
		protected DateFunctionsTranslatorBase()
		{
			RegisterDateTime();
			RegisterDateTimeOffset();
			RegisterTimeSpan();

#if SUPPORTS_DATEONLY
			RegisterDateOnly();
#endif

			Registration.RegisterMethod((int? year, int? month, int? day) => Sql.MakeDateTime(year, month, day), TranslateMakeDateTime);
			Registration.RegisterMethod((int year, int month, int day, int hour, int minute, int second) => Sql.MakeDateTime(year, month, day, hour, minute, second), TranslateMakeDateTime);
		}

		void RegisterDateTime()
		{
			Registration.RegisterConstructor((int year, int month, int day) 
				=> new DateTime(year, month, day), TranslateDateTimeConstructor);
			Registration.RegisterConstructor((int year, int month, int day, int hour, int minute, int second) 
				=> new DateTime(year, month, day, hour, minute, second), TranslateDateTimeConstructor);
			Registration.RegisterConstructor((int year, int month, int day, int hour, int minute, int second, int millisecond) 
				=> new DateTime(year, month, day, hour, minute, second, millisecond), TranslateDateTimeConstructor);

			Registration.RegisterMethod((int year, int month, int day, int hour, int minute, int second)
				=> Sql.MakeDateTime(year, month, day, hour, minute, second), TranslateMakeDateTimeMethod);

			Registration.RegisterMember((DateTime dt) => dt.Year,        (tc, me, tf) => TranslateDateTimeMember(tc, me, tf, Sql.DateParts.Year));
			Registration.RegisterMember((DateTime dt) => dt.Month,       (tc, me, tf) => TranslateDateTimeMember(tc, me, tf, Sql.DateParts.Month));
			Registration.RegisterMember((DateTime dt) => dt.Day,         (tc, me, tf) => TranslateDateTimeMember(tc, me, tf, Sql.DateParts.Day));
			Registration.RegisterMember((DateTime dt) => dt.Hour,        (tc, me, tf) => TranslateDateTimeMember(tc, me, tf, Sql.DateParts.Hour));
			Registration.RegisterMember((DateTime dt) => dt.Minute,      (tc, me, tf) => TranslateDateTimeMember(tc, me, tf, Sql.DateParts.Minute));
			Registration.RegisterMember((DateTime dt) => dt.Second,      (tc, me, tf) => TranslateDateTimeMember(tc, me, tf, Sql.DateParts.Second));
			Registration.RegisterMember((DateTime dt) => dt.Millisecond, (tc, me, tf) => TranslateDateTimeMember(tc, me, tf, Sql.DateParts.Millisecond));
			Registration.RegisterMember((DateTime dt) => dt.DayOfYear,   (tc, me, tf) => TranslateDateTimeMember(tc, me, tf, Sql.DateParts.DayOfYear));
			Registration.RegisterMember((DateTime dt) => dt.DayOfWeek,   (tc, me, tf) => TranslateDateTimeMember(tc, me, tf, Sql.DateParts.WeekDay));
			Registration.RegisterMember((DateTime dt) => dt.Date, TranslateDateTimeTruncationToDate);

			Registration.RegisterMember((DateTime dt) => dt.TimeOfDay, TranslateDateTimeTruncationToTime);

			Registration.RegisterMethod((DateTime dt) => Sql.DateAdd(Sql.DateParts.Year, 0, dt), TranslateDateTimeDateAdd);

			Registration.RegisterMethod((DateTime dt) => dt.AddYears(0),        (tc, mc, tf) => TranslateDateTimeAddMember(tc, mc, tf, Sql.DateParts.Year));
			Registration.RegisterMethod((DateTime dt) => dt.AddMonths(0),       (tc, mc, tf) => TranslateDateTimeAddMember(tc, mc, tf, Sql.DateParts.Month));
			Registration.RegisterMethod((DateTime dt) => dt.AddDays(0),         (tc, mc, tf) => TranslateDateTimeAddMember(tc, mc, tf, Sql.DateParts.Day));
			Registration.RegisterMethod((DateTime dt) => dt.AddHours(0),        (tc, mc, tf) => TranslateDateTimeAddMember(tc, mc, tf, Sql.DateParts.Hour));
			Registration.RegisterMethod((DateTime dt) => dt.AddMinutes(0),      (tc, mc, tf) => TranslateDateTimeAddMember(tc, mc, tf, Sql.DateParts.Minute));
			Registration.RegisterMethod((DateTime dt) => dt.AddSeconds(0),      (tc, mc, tf) => TranslateDateTimeAddMember(tc, mc, tf, Sql.DateParts.Second));
			Registration.RegisterMethod((DateTime dt) => dt.AddMilliseconds(0), (tc, mc, tf) => TranslateDateTimeAddMember(tc, mc, tf, Sql.DateParts.Millisecond));

			Registration.RegisterMethod((DateTime dt) => Sql.DatePart(Sql.DateParts.Year, dt), TranslateDateTimeSqlDatepart);

			Registration.RegisterMethod(() => Sql.GetDate(),           TranslateSqlGetDate);
			Registration.RegisterMember(() => Sql.CurrentTimestamp,    TranslateServerNow);
			Registration.RegisterMember(() => Sql.CurrentTimestamp2,   TranslateServerNow);
			Registration.RegisterMember(() => DateTime.Now,            TranslateNow);
			Registration.RegisterMember(() => DateTime.UtcNow,         TranslateUtcNow);
			Registration.RegisterMember(() => Sql.CurrentTimestampUtc, TranslateUtcNow);
		}

		void RegisterDateTimeOffset()
		{
			Registration.RegisterMember(() => DateTimeOffset.Now,      TranslateZonedNow);
			Registration.RegisterMember(() => DateTimeOffset.UtcNow,   TranslateZonedUtcNow);

			Registration.RegisterMember((DateTimeOffset dt) => dt.Year, (tc,        me, tf) => TranslateDateTimeOffsetMember(tc, me, tf, Sql.DateParts.Year));
			Registration.RegisterMember((DateTimeOffset dt) => dt.Month, (tc,       me, tf) => TranslateDateTimeOffsetMember(tc, me, tf, Sql.DateParts.Month));
			Registration.RegisterMember((DateTimeOffset dt) => dt.Day, (tc,         me, tf) => TranslateDateTimeOffsetMember(tc, me, tf, Sql.DateParts.Day));
			Registration.RegisterMember((DateTimeOffset dt) => dt.Hour, (tc,        me, tf) => TranslateDateTimeOffsetMember(tc, me, tf, Sql.DateParts.Hour));
			Registration.RegisterMember((DateTimeOffset dt) => dt.Minute, (tc,      me, tf) => TranslateDateTimeOffsetMember(tc, me, tf, Sql.DateParts.Minute));
			Registration.RegisterMember((DateTimeOffset dt) => dt.Second, (tc,      me, tf) => TranslateDateTimeOffsetMember(tc, me, tf, Sql.DateParts.Second));
			Registration.RegisterMember((DateTimeOffset dt) => dt.Millisecond, (tc, me, tf) => TranslateDateTimeOffsetMember(tc, me, tf, Sql.DateParts.Millisecond));
			Registration.RegisterMember((DateTimeOffset dt) => dt.DayOfYear, (tc,   me, tf) => TranslateDateTimeOffsetMember(tc, me, tf, Sql.DateParts.DayOfYear));
			Registration.RegisterMember((DateTimeOffset dt) => dt.DayOfWeek, (tc,   me, tf) => TranslateDateTimeOffsetMember(tc, me, tf, Sql.DateParts.WeekDay));
			Registration.RegisterMember((DateTimeOffset dt) => dt.Date, TranslateDateTimeOffsetTruncationToDate);

			Registration.RegisterMember((DateTimeOffset dt) => dt.TimeOfDay, TranslateDateTimeOffsetTruncationToTime);

			Registration.RegisterMethod((DateTimeOffset dt) => Sql.DateAdd(Sql.DateParts.Year, 0, dt), TranslateDateTimeOffsetDateAdd);

			Registration.RegisterMethod((DateTimeOffset dt) => dt.AddYears(0), (tc,        mc, tf) => TranslateDateTimeOffsetAddMember(tc, mc, tf, Sql.DateParts.Year));
			Registration.RegisterMethod((DateTimeOffset dt) => dt.AddMonths(0), (tc,       mc, tf) => TranslateDateTimeOffsetAddMember(tc, mc, tf, Sql.DateParts.Month));
			Registration.RegisterMethod((DateTimeOffset dt) => dt.AddDays(0), (tc,         mc, tf) => TranslateDateTimeOffsetAddMember(tc, mc, tf, Sql.DateParts.Day));
			Registration.RegisterMethod((DateTimeOffset dt) => dt.AddHours(0), (tc,        mc, tf) => TranslateDateTimeOffsetAddMember(tc, mc, tf, Sql.DateParts.Hour));
			Registration.RegisterMethod((DateTimeOffset dt) => dt.AddMinutes(0), (tc,      mc, tf) => TranslateDateTimeOffsetAddMember(tc, mc, tf, Sql.DateParts.Minute));
			Registration.RegisterMethod((DateTimeOffset dt) => dt.AddSeconds(0), (tc,      mc, tf) => TranslateDateTimeOffsetAddMember(tc, mc, tf, Sql.DateParts.Second));
			Registration.RegisterMethod((DateTimeOffset dt) => dt.AddMilliseconds(0), (tc, mc, tf) => TranslateDateTimeOffsetAddMember(tc, mc, tf, Sql.DateParts.Millisecond));

			Registration.RegisterMethod((DateTimeOffset dt) => Sql.DatePart(Sql.DateParts.Year, dt), TranslateDateTimeOffsetSqlDatepart);
		}

#if SUPPORTS_DATEONLY
		void RegisterDateOnly()
		{
			Registration.RegisterMethod((int year, int month, int day) => Sql.MakeDateOnly(year, month, day), TranslateMakeDateOnlyMethod);

			Registration.RegisterConstructor((int year, int month, int day) => new DateOnly(year, month, day), TranslateDateOnlyConstructor);
			Registration.RegisterMember((DateOnly dt) => dt.Year, (tc,      me, tf) => TranslateDateOnlyMember(tc, me, tf, Sql.DateParts.Year));
			Registration.RegisterMember((DateOnly dt) => dt.Month, (tc,     me, tf) => TranslateDateOnlyMember(tc, me, tf, Sql.DateParts.Month));
			Registration.RegisterMember((DateOnly dt) => dt.Day, (tc,       me, tf) => TranslateDateOnlyMember(tc, me, tf, Sql.DateParts.Day));
			Registration.RegisterMember((DateOnly dt) => dt.DayOfYear, (tc, me, tf) => TranslateDateOnlyMember(tc, me, tf, Sql.DateParts.DayOfYear));
			Registration.RegisterMember((DateOnly dt) => dt.DayOfWeek, (tc, me, tf) => TranslateDateOnlyMember(tc, me, tf, Sql.DateParts.WeekDay));

			Registration.RegisterMethod((DateOnly dt) => Sql.DateAdd(Sql.DateParts.Year, 0, dt), TranslateDateOnlyDateAdd);

			Registration.RegisterMethod((DateOnly dt) => dt.AddYears(0), (tc,  mc, tf) => TranslateDateOnlyAddMember(tc, mc, tf, Sql.DateParts.Year));
			Registration.RegisterMethod((DateOnly dt) => dt.AddMonths(0), (tc, mc, tf) => TranslateDateOnlyAddMember(tc, mc, tf, Sql.DateParts.Month));
			Registration.RegisterMethod((DateOnly dt) => dt.AddDays(0), (tc,   mc, tf) => TranslateDateOnlyAddMember(tc, mc, tf, Sql.DateParts.Day));

			Registration.RegisterMethod((DateOnly dt) => Sql.DatePart(Sql.DateParts.Year, dt), TranslateDateOnlySqlDatepart);
		}
#endif

		Expression? TranslateDateTimeConstructor(ITranslationContext translationContext, Expression expression, TranslationFlags translationFlags)
		{
			if (expression is not NewExpression newExpression)
				return null;

			if (newExpression.Arguments.Count < 3)
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			if (!translationContext.TranslateToSqlExpression(newExpression.Arguments[0], out var year)  ||
				!translationContext.TranslateToSqlExpression(newExpression.Arguments[1], out var month) ||
				!translationContext.TranslateToSqlExpression(newExpression.Arguments[2], out var day))
			{
				return null;
			}

			ISqlExpression? hour        = null;
			ISqlExpression? minute      = null;
			ISqlExpression? second      = null;
			ISqlExpression? millisecond = null;

			if (newExpression.Arguments.Count > 3)
			{
				if (!translationContext.TranslateToSqlExpression(newExpression.Arguments[3], out hour)   ||
				    !translationContext.TranslateToSqlExpression(newExpression.Arguments[4], out minute) ||
				    !translationContext.TranslateToSqlExpression(newExpression.Arguments[5], out second))
				{
					return null;
				}
			}

			if (newExpression.Arguments.Count > 6)
			{
				if (!translationContext.TranslateToSqlExpression(newExpression.Arguments[6], out millisecond))
					return null;
			}

			var makeExpression = TranslateMakeDateTime(translationContext, translationContext.ExpressionFactory.GetDbDataType(expression.Type), year, month, day, hour, minute, second, millisecond);

			if (makeExpression == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, makeExpression, newExpression);
		}

		Expression? TranslateMakeDateTimeMethod(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (methodCall.Arguments.Count < 6)
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0].UnwrapConvert(), out var year)   ||
				!translationContext.TranslateToSqlExpression(methodCall.Arguments[1].UnwrapConvert(), out var month)  ||
				!translationContext.TranslateToSqlExpression(methodCall.Arguments[2].UnwrapConvert(), out var day)    ||
				!translationContext.TranslateToSqlExpression(methodCall.Arguments[3].UnwrapConvert(), out var hour)   ||
				!translationContext.TranslateToSqlExpression(methodCall.Arguments[4].UnwrapConvert(), out var minute) ||
				!translationContext.TranslateToSqlExpression(methodCall.Arguments[5].UnwrapConvert(), out var second))
			{
				return null;
			}

			var makeExpression = TranslateMakeDateTime(translationContext, translationContext.ExpressionFactory.GetDbDataType(methodCall.Type), year, month, day, hour, minute, second, null);

			if (makeExpression == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, makeExpression, methodCall);
		}

		Expression? TranslateMakeDateTime(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (methodCall.Arguments.Count < 3)
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0].UnwrapConvert(), out var year)  ||
			    !translationContext.TranslateToSqlExpression(methodCall.Arguments[1].UnwrapConvert(), out var month) ||
			    !translationContext.TranslateToSqlExpression(methodCall.Arguments[2].UnwrapConvert(), out var day))
			{
				return null;
			}

			ISqlExpression? hour   = null;
			ISqlExpression? minute = null;
			ISqlExpression? second = null;

			if (methodCall.Arguments.Count > 3)
			{
				if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[3].UnwrapConvert(), out hour)   ||
				    !translationContext.TranslateToSqlExpression(methodCall.Arguments[4].UnwrapConvert(), out minute) ||
				    !translationContext.TranslateToSqlExpression(methodCall.Arguments[5].UnwrapConvert(), out second))
				{
					return null;
				}
			}

			var makeExpression = TranslateMakeDateTime(translationContext, translationContext.ExpressionFactory.GetDbDataType(methodCall.Type.UnwrapNullableType()), year, month, day, hour, minute, second, null);

			if (makeExpression == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, makeExpression, methodCall);
		}

		Expression? TranslateSqlGetDate(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var translated = TranslateServerNow(translationContext, translationFlags);
			if (translated == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translated, methodCall);
		}

		void RegisterTimeSpan()
		{
			// A Component truncates toward zero within the unit named last - stated rather than implied, because
			// the same unit reads differently depending on what encloses it. Total* keeps the whole interval and
			// its fraction and has nothing enclosing it, Ticks among them: it is a Total, not a Component.
			Registration.RegisterMember((TimeSpan ts) => ts.Days,              (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, SqlIntervalUnit.Day,         SqlIntervalPartKind.Component, null));
			Registration.RegisterMember((TimeSpan ts) => ts.Hours,             (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, SqlIntervalUnit.Hour,        SqlIntervalPartKind.Component, SqlIntervalUnit.Day));
			Registration.RegisterMember((TimeSpan ts) => ts.Minutes,           (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, SqlIntervalUnit.Minute,      SqlIntervalPartKind.Component, SqlIntervalUnit.Hour));
			Registration.RegisterMember((TimeSpan ts) => ts.Seconds,           (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, SqlIntervalUnit.Second,      SqlIntervalPartKind.Component, SqlIntervalUnit.Minute));
			Registration.RegisterMember((TimeSpan ts) => ts.Milliseconds,      (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, SqlIntervalUnit.Millisecond, SqlIntervalPartKind.Component, SqlIntervalUnit.Second));

			Registration.RegisterMember((TimeSpan ts) => ts.Ticks,             (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, SqlIntervalUnit.Tick,        SqlIntervalPartKind.Total,     null));
			Registration.RegisterMember((TimeSpan ts) => ts.TotalDays,         (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, SqlIntervalUnit.Day,         SqlIntervalPartKind.Total,     null));
			Registration.RegisterMember((TimeSpan ts) => ts.TotalHours,        (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, SqlIntervalUnit.Hour,        SqlIntervalPartKind.Total,     null));
			Registration.RegisterMember((TimeSpan ts) => ts.TotalMinutes,      (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, SqlIntervalUnit.Minute,      SqlIntervalPartKind.Total,     null));
			Registration.RegisterMember((TimeSpan ts) => ts.TotalSeconds,      (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, SqlIntervalUnit.Second,      SqlIntervalPartKind.Total,     null));
			Registration.RegisterMember((TimeSpan ts) => ts.TotalMilliseconds, (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, SqlIntervalUnit.Millisecond, SqlIntervalPartKind.Total,     null));

#if NET8_0_OR_GREATER
			Registration.RegisterMember((TimeSpan ts) => ts.Microseconds,      (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, SqlIntervalUnit.Microsecond, SqlIntervalPartKind.Component, SqlIntervalUnit.Millisecond));
			Registration.RegisterMember((TimeSpan ts) => ts.Nanoseconds,       (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, SqlIntervalUnit.Nanosecond,  SqlIntervalPartKind.Component, SqlIntervalUnit.Microsecond));
			Registration.RegisterMember((TimeSpan ts) => ts.TotalMicroseconds, (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, SqlIntervalUnit.Microsecond, SqlIntervalPartKind.Total,     null));
			Registration.RegisterMember((TimeSpan ts) => ts.TotalNanoseconds,  (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, SqlIntervalUnit.Nanosecond,  SqlIntervalPartKind.Total,     null));
#endif

			Registration.RegisterUnaryInternal(ExpressionType.Negate, typeof(TimeSpan),  TranslateTimeSpanNegate);
			Registration.RegisterUnaryInternal(ExpressionType.Negate, typeof(TimeSpan?), TranslateTimeSpanNegate);

			// Comparisons need a handler of their own: without one the two sides are compared as the numbers they
			// are stored as, which is only right when both were declared in the same unit.
			foreach (var comparison in new[]
			{
				ExpressionType.Equal, ExpressionType.NotEqual,
				ExpressionType.GreaterThan, ExpressionType.GreaterThanOrEqual,
				ExpressionType.LessThan, ExpressionType.LessThanOrEqual,
			})
			{
				Registration.RegisterBinaryInternal(comparison, typeof(TimeSpan),  typeof(TimeSpan),  TranslateIntervalComparison);
				Registration.RegisterBinaryInternal(comparison, typeof(TimeSpan?), typeof(TimeSpan?), TranslateIntervalComparison);
			}

			// Adding or subtracting two durations needs a handler for the reason the comparisons above give: without
			// one the two stored numbers are combined as they stand, which is only right when both were declared in
			// the same unit.
			foreach (var interval in new[] { typeof(TimeSpan), typeof(TimeSpan?) })
			{
				Registration.RegisterBinaryInternal(ExpressionType.Add,      interval, interval, TranslateIntervalArithmetic);
				Registration.RegisterBinaryInternal(ExpressionType.Subtract, interval, interval, TranslateIntervalArithmetic);
			}

			Registration.RegisterBinaryInternal(ExpressionType.Subtract, typeof(DateTime),        typeof(DateTime),        TranslateDateTimeDifference);
			Registration.RegisterBinaryInternal(ExpressionType.Subtract, typeof(DateTime?),       typeof(DateTime?),       TranslateDateTimeDifference);
			Registration.RegisterBinaryInternal(ExpressionType.Subtract, typeof(DateTimeOffset),  typeof(DateTimeOffset),  TranslateDateTimeDifference);
			Registration.RegisterBinaryInternal(ExpressionType.Subtract, typeof(DateTimeOffset?), typeof(DateTimeOffset?), TranslateDateTimeDifference);

			foreach (var temporal in new[] { typeof(DateTime), typeof(DateTime?), typeof(DateTimeOffset), typeof(DateTimeOffset?) })
			{
				foreach (var interval in new[] { typeof(TimeSpan), typeof(TimeSpan?) })
				{
					Registration.RegisterBinaryInternal(ExpressionType.Add,      temporal, interval, TranslateTemporalArithmetic);
					Registration.RegisterBinaryInternal(ExpressionType.Subtract, temporal, interval, TranslateTemporalArithmetic);
				}
			}
		}

		/// <summary>
		/// Adds or subtracts two durations, bringing them to a common unit first.
		/// </summary>
		/// <remarks>
		/// A duration lowers to the number it is stored as, and the unit that number is in lives in the column
		/// descriptor rather than in the expression. Two durations declared in different units therefore arrive here
		/// as plain numbers that are not commensurable - ninety minutes held as 1800 seconds and as 18000000000 ticks
		/// - and combining them as they stand answers with the sum of two unrelated counts, read back through
		/// whichever descriptor the walk reaches first.
		/// <para>
		/// Both sides are taken to ticks instead, which is the representation every duration shares and the one a
		/// computed difference already carries. The result is marked as a tick count rather than left bare, so the
		/// reader builds a <see cref="TimeSpan"/> from it through the constructor rather than looking for a column
		/// descriptor that a computed value does not have.
		/// </para>
		/// <para>
		/// Operands already agreeing on a unit are left to the generic binary handling. Their stored numbers are
		/// commensurable as they are, and taking them to ticks would put a multiplication on the column and lose
		/// whatever index it has - the same reason the comparison path converts the other side rather than the
		/// column when it can.
		/// </para>
		/// </remarks>
		Expression? TranslateIntervalArithmetic(ITranslationContext translationContext, BinaryExpression binaryExpression, TranslationFlags translationFlags)
		{
			// Dropped for the reason the comparison gives: what this sends is a tick count, and the column the
			// result is assigned to has nothing to say about how one is written.
			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			var left  = TranslateIntervalOperand(translationContext, binaryExpression.Left,  translationFlags);
			var right = TranslateIntervalOperand(translationContext, binaryExpression.Right, translationFlags);

			if (left == null && right == null)
				return null;

			var leftUnit  = left  == null ? null : IntervalResolutionOf(left);
			var rightUnit = right == null ? null : IntervalResolutionOf(right);

			// An interval whose unit cannot be read is not something to reconcile against anything.
			if (left != null && leftUnit == null || right != null && rightUnit == null)
				return null;

			// Both sides declared and already counting in the same unit: their stored numbers are commensurable as
			// they stand, so the generic handling adds them without putting arithmetic on either column.
			if (left != null && right != null && leftUnit == rightUnit)
				return null;

			// One side is an ordinary TimeSpan rather than a stored duration, and what happens to it depends on what
			// it is beside. Against a declared column the generic handling already writes it through that column's
			// converter - the value goes into the column's unit and the column stays bare, which is both right and
			// the cheaper shape - so that pairing is left alone.
			//
			// Against a computed difference there is no column and no converter to be written through, and the value
			// reaches the statement as whatever the provider maps a bare TimeSpan to, beside a tick count. Providers
			// that cannot compare the two say so - SQL Server calls it an operand type clash - which is loud rather
			// than wrong, but the query is an ordinary one and it should answer. So the value is counted in ticks
			// here, the way the comparison path counts one.
			if (left == null || right == null)
			{
				// A stored duration that never declared its unit is a different case again, and the only one of the
				// three with nothing to work with. Its number counts whatever a hand-written converter decided, and
				// nothing in the model says what - so it cannot be brought onto the other side's terms, and adding
				// the two raw numbers is a wrong duration rather than a refused one. Refused by name, which also
				// leaves a projection free to answer it in .NET, where both sides are real durations again.
				var undeclared = left == null ? binaryExpression.Left : binaryExpression.Right;

				if (IsUndeclaredStorage(translationContext, undeclared, translationFlags))
					return translationContext.CreateErrorExpression(binaryExpression, ErrorHelper.Error_Interval_UndeclaredOperand);

				var declared = left ?? right!;

				if (declared is not SqlIntervalDifferenceExpression)
					return null;
			}

			// The reconciled value is a tick count, and it has to come back as one: nothing turns a fractional number
			// into a TimeSpan, so a storage that cannot hold a tick count as a whole number leaves the arithmetic
			// right and the read impossible. Access is the case - it has no 64-bit integer, so a duration is kept as
			// money there and the sum arrives as a decimal the reader cannot make a duration of.
			//
			// Refused by name rather than left to the generic handling, which is what produced the wrong answer this
			// exists to stop. It joins the tick-count refusals that provider already declares for every other path.
			if (left != null && !CarriesWholeTicks(left) || right != null && !CarriesWholeTicks(right))
				return translationContext.CreateErrorExpression(binaryExpression, ErrorHelper.Error_Interval_Operation);

			var factory  = translationContext.ExpressionFactory;
			var tickType = factory.GetDbDataType(typeof(long));

			var leftTicks = left != null
				? new SqlIntervalPartExpression(left, SqlIntervalUnit.Tick, SqlIntervalPartKind.Total, tickType)
				: ValueIn(translationContext, binaryExpression.Left, translationFlags, 1, roundUp: false);

			var rightTicks = right != null
				? new SqlIntervalPartExpression(right, SqlIntervalUnit.Tick, SqlIntervalPartKind.Total, tickType)
				: ValueIn(translationContext, binaryExpression.Right, translationFlags, 1, roundUp: false);

			if (leftTicks == null || rightTicks == null)
				return null;

			var combined = binaryExpression.NodeType == ExpressionType.Add
				? factory.Add(tickType, leftTicks, rightTicks)
				: factory.Sub(tickType, leftTicks, rightTicks);

			// Pinned to the tick type rather than left to whatever the provider's own arithmetic produces. Adding two
			// integers is not integer addition everywhere - Firebird answers a wider integer than it was given and
			// Informix a decimal - and the only thing that turns the result back into a TimeSpan is a whole tick
			// count, so the type has to be stated rather than assumed.
			combined = factory.Cast(combined, tickType, true);

			var resultType = factory.GetDbDataType(typeof(TimeSpan)).WithDataType(DataType.Int64);

			return translationContext.CreatePlaceholder(
				translationContext.CurrentSelectQuery,
				new SqlIntervalExpression(combined, resultType, SqlIntervalType.ClrTimeSpan),
				binaryExpression);
		}

		/// <summary>
		/// Whether an operand is a stored value that carries no declared unit.
		/// </summary>
		/// <remarks>
		/// The question separates the two things that reach here having failed to become an interval. A literal or a
		/// parameter is a real <see cref="TimeSpan"/> and can be counted into any unit asked of it. A column mapped
		/// through a hand-written value converter cannot: what is stored is a number the converter chose, and only a
		/// <see cref="LinqToDB.Mapping.DurationAttribute"/> would say what it counts.
		/// </remarks>
		static bool IsUndeclaredStorage(ITranslationContext translationContext, Expression operand, TranslationFlags translationFlags)
		{
			if (translationContext.Translate(operand, translationFlags) is not SqlPlaceholderExpression placeholder)
				return false;

			return QueryHelper.GetColumnDescriptor(placeholder.Sql) is { DurationUnit: null, ValueConverter: not null };
		}

		/// <summary>
		/// Whether an operand's storage can hold a tick count as a whole number.
		/// </summary>
		/// <remarks>
		/// A computed difference is a tick count already and needs nothing. A declared duration is whatever its
		/// column is, and only an integral column can carry the reconciled result back - a duration kept as money,
		/// which is how a provider without a 64-bit integer keeps one, produces a decimal that no conversion turns
		/// into a <see cref="TimeSpan"/>.
		/// </remarks>
		static bool CarriesWholeTicks(ISqlExpression expression)
		{
			if (expression is not SqlIntervalExpression interval)
				return true;

			return interval.Type.DataType is
				DataType.Int64  or DataType.Int32  or DataType.Int16  or DataType.SByte or
				DataType.UInt64 or DataType.UInt32 or DataType.UInt16 or DataType.Byte;
		}

		/// <summary>
		/// The unit an interval operand counts in, whichever of the two interval shapes it is.
		/// </summary>
		static SqlIntervalUnit? IntervalResolutionOf(ISqlExpression expression)
		{
			return expression switch
			{
				SqlIntervalExpression           interval   => interval.IntervalType.Resolution,
				SqlIntervalDifferenceExpression difference => difference.IntervalType.Resolution,
				_                                          => null,
			};
		}

		/// <summary>
		/// Translates <c>date + interval</c> and <c>date - interval</c> into a node that keeps the shift visible.
		/// </summary>
		/// <remarks>
		/// Registered so that it is not left to the generic binary handling, which builds a plain <c>+</c> between
		/// the date and whatever the interval lowered to - a tick count on most providers - and a database
		/// evaluates that without complaint: SQLite reads the date as text, coerces it to a number and answers
		/// with something that still looks like a date. With the shift carried as a node the provider can lower it
		/// properly, and the optimizer can cancel it against the difference it came from.
		/// </remarks>
		Expression? TranslateTemporalArithmetic(ITranslationContext translationContext, BinaryExpression binaryExpression, TranslationFlags translationFlags)
		{
			// Parameters are taken here, unlike most translations: shifting a fixed date by a computed interval is
			// the ordinary case, and skipping it would hand the expression back to the generic binary handling -
			// which is what produced a raw plus between a date and a tick count.
			var temporal = TranslateNoRequiredExpression(translationContext, binaryExpression.Left, translationFlags, skipIfParameter: false);
			if (temporal == null)
				return null;

			// Without dropping the ambient descriptor the interval is built against the column the whole shift is
			// assigned to, which is a date - and a TimeSpan parameter typed as a DateTime throws outright rather
			// than producing a wrong value.
			SqlPlaceholderExpression? interval;

			using (translationContext.UsingColumnDescriptor(null))
			{
				interval = TranslateNoRequiredExpression(translationContext, binaryExpression.Right, translationFlags, skipIfParameter: false);
			}

			if (interval == null)
				return null;

			// Only a declared duration - or a computed difference, which is one by construction - becomes a shift
			// node, because only then is the amount a number whose unit is known. A bare CLR TimeSpan has no
			// declaration and keeps the handling it had: providers with a native interval type map it to one and
			// add it directly, which is why this test passes there and is gated everywhere else.
			var amount = TryMakeInterval(translationContext, interval.Sql);
			if (amount == null)
				return null;

			var factory = translationContext.ExpressionFactory;

			// A declared duration is a number in a unit of its own choosing, so it is asked for its tick total here
			// rather than passed on as it stands. The lowering spends the amount through the provider's own date
			// arithmetic, which has no way to learn that unit - handed 5400 from a column declared in seconds it
			// would spend 5400 ticks, a wrong date rather than a refused one.
			//
			// A difference needs no such step and must not get one: it has no unit of its own, and each provider
			// lowers one into whatever form its own date arithmetic takes - a tick count on most, a native interval
			// on PostgreSQL.
			if (amount is SqlIntervalExpression)
			{
				// A declared duration is a real amount and nothing later removes it, so a provider that cannot spend
				// one says so here rather than building a node the builder will refuse. Refused there it fails the
				// whole query - this was the one interval path that did, while a member and a comparison both
				// degrade to a client-side answer.
				//
				// Asked in this branch only. A shift by a computed difference is built regardless, because
				// start + (end - start) cancels against the difference it came from and asks the provider for
				// nothing at all - declining that here would sink a query that works everywhere today.
				if (!translationContext.ProviderFlags.CanLowerIntervalShift)
					return translationContext.CreateErrorExpression(binaryExpression, ErrorHelper.Error_Interval_Shift);

				amount = new SqlIntervalPartExpression(
					amount,
					SqlIntervalUnit.Tick,
					SqlIntervalPartKind.Total,
					factory.GetDbDataType(typeof(long)));
			}

			var shifted = new SqlTemporalArithmeticExpression(
				temporal.Sql,
				amount,
				binaryExpression.NodeType == ExpressionType.Subtract,
				factory.GetDbDataType(binaryExpression.Type));

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, shifted, binaryExpression);
		}

		/// <summary>
		/// Translates <c>end - start</c> between two date/time values into an elapsed interval.
		/// </summary>
		/// <remarks>
		/// Elapsed time, not a boundary count: <c>Sql.DateDiff</c> answers a different question and would report
		/// one hour between 10:59 and 11:01, where this reports two minutes. The existing per-provider
		/// <c>DateDiffBuilder</c> family implements the boundary contract and deliberately is not reused here.
		/// </remarks>
		Expression? TranslateDateTimeDifference(ITranslationContext translationContext, BinaryExpression binaryExpression, TranslationFlags translationFlags)
		{
			// Left untranslated rather than reported as an error by name, though the message would be better: an
			// error expression is built here, before the optimizer sees the statement, so it would also sink the
			// shapes that never need the difference at all - start + (end - start) cancels to end and asks nothing
			// of the provider. Those must keep working where the difference itself cannot be lowered.
			if (!translationContext.ProviderFlags.CanLowerIntervalDifference)
				return null;

			var difference = MakeDateDifference(translationContext, binaryExpression, translationFlags);

			return difference == null
				? null
				: translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, difference, binaryExpression);
		}

		/// <summary>
		/// The interval a member is taken from.
		/// </summary>
		/// <remarks>
		/// A date difference is built here rather than left to the registered subtraction, because a provider may
		/// be able to lower a member without being able to lower the bare difference.
		/// </remarks>
		ISqlExpression? TranslateIntervalOperand(ITranslationContext translationContext, Expression? operand, TranslationFlags translationFlags)
		{
			if (operand == null)
				return null;

			if (translationContext.ProviderFlags.CanLowerIntervalPart)
			{
				// A difference that went through a projection arrives as a reference into the anonymous type the
				// projection built, so the shape has to be expanded before it can be recognised at all.
				var subtraction = AsDateDifference(operand)
					?? AsDateDifference(translationContext.Translate(operand, TranslationFlags.Expand));

				if (subtraction != null)
				{
					var difference = MakeDateDifference(translationContext, subtraction, translationFlags);
					if (difference != null)
						return difference;
				}
			}

			var placeholder = TranslateNoRequiredExpression(translationContext, operand, translationFlags);

			return placeholder == null ? null : TryMakeInterval(translationContext, placeholder.Sql);
		}

		static readonly MethodInfo _countInOptional = ((Func<TimeSpan?, long, bool, long?>)CountIn).Method;

		/// <summary>
		/// A duration operand counted in units of <paramref name="perUnit"/> ticks rather than in ticks, rounded the
		/// way the comparison needs.
		/// </summary>
		/// <remarks>
		/// Taking both sides to ticks makes a comparison mean the same thing on both, but it puts the arithmetic on
		/// the column - <c>col * 10000000 &gt; …</c> - and a column under arithmetic cannot use its index. The same
		/// question is asked of the bare column by converting the other side instead, and the conversion is done
		/// where the value is: in the expression, before it ever becomes a parameter. Nothing is written into the
		/// statement that was not going there anyway, so a query differing only by its duration still asks its own
		/// question.
		/// <para>
		/// A duration that lands between two representable values is not simply truncated - that would answer a
		/// different question. Which way the bound moves is what the operator decides: a column counting whole
		/// seconds is above 1.5 exactly when it is above 1, but is at least 1.5 only when it is at least 2. So
		/// <c>&gt;</c> and <c>&lt;=</c> take the floor and <c>&gt;=</c> and <c>&lt;</c> take the ceiling, and each
		/// is exact for a representable duration too, where the two agree.
		/// </para>
		/// </remarks>
		static Expression? BoundIn(ITranslationContext translationContext, Expression duration, long perUnit, bool roundUp)
		{
			// Everything below is written to be run rather than rendered, and the count for a duration that may be
			// absent is a call, which has no rendering at all. So what reaches here has to be a duration that can be
			// worked out without asking the database - and that is not only a value: an operand this translator
			// cannot settle arrives too, a member of a set operation whose branches disagree among them, and it has
			// no count to give. The question is asked before anything is built, and asked without answering it.
			if (!translationContext.CanBeEvaluated(duration))
				return null;

			return ExpressionHelpers.MoveValueMarkerOutside(duration, value =>
			{
				// One that may be absent is counted by a call instead. Written out, the parts it needs - asking
				// whether it is there, and choosing between two answers - are themselves things this translator is
				// registered for, and the conversion would arrive back at itself. A duration that cannot vary loses
				// nothing by it, since one that may be absent is a value looked up rather than written down.
				if (value.Type == typeof(TimeSpan?))
					return Expression.Call(_countInOptional, value, Expression.Constant(perUnit), Expression.Constant(roundUp));

				var ticks = Expression.Property(value, nameof(TimeSpan.Ticks));

				if (perUnit == 1)
					return ticks;

				var quotient = Expression.Divide(
					Expression.Convert(ticks, typeof(decimal)),
					Expression.Constant((decimal)perUnit));

				var rounded = Expression.Call(typeof(Math), roundUp ? nameof(Math.Ceiling) : nameof(Math.Floor), null, quotient);

				return Expression.Convert(rounded, typeof(long));
			});
		}

		/// <summary>
		/// The whole number of <paramref name="perUnit"/>-tick units a duration is above or below, according to
		/// <paramref name="roundUp"/>.
		/// </summary>
		/// <remarks>
		/// A call rather than the arithmetic written into the expression, so that what reaches the statement is one
		/// value worked out beside the duration it came from. Written out, the parts a duration that may be absent
		/// needs - asking whether it is there, and choosing between two answers - are themselves things this
		/// translator is registered for, and the conversion would arrive back at itself.
		/// <para>
		/// Decimal rather than double for the division: a tick count reaches nineteen digits and a double stops
		/// being able to tell two of them apart at sixteen.
		/// </para>
		/// </remarks>
		static long CountIn(TimeSpan duration, long perUnit, bool roundUp)
		{
			if (perUnit == 1)
				return duration.Ticks;

			var quotient = (decimal)duration.Ticks / perUnit;

			return (long)(roundUp ? Math.Ceiling(quotient) : Math.Floor(quotient));
		}

		/// <summary>
		/// The same count for a duration that may be absent, which counts to nothing when it is.
		/// </summary>
		/// <remarks>
		/// A comparison whose bound is absent has nothing to hold a row against and answers no to every one of them,
		/// which is the answer the language gives it.
		/// </remarks>
		static long? CountIn(TimeSpan? duration, long perUnit, bool roundUp)
		{
			return duration is { } present ? CountIn(present, perUnit, roundUp) : null;
		}

		/// <summary>
		/// A <see cref="TimeSpan"/> operand, or one that may be absent, as a whole number of
		/// <paramref name="perUnit"/>-tick units.
		/// </summary>
		/// <remarks>
		/// An operand that could not be worked out leaves the comparison untranslated, which is how it is refused
		/// rather than quietly answered.
		/// <para>
		/// One that may be absent is counted here too, and that is not a convenience. Refused, it would fall back on
		/// the conversion the column carries for reading and writing, which divides and keeps the whole part - right
		/// for storing a duration finer than the column, wrong for asking about one, since it would put the question
		/// to a duration the caller did not ask about.
		/// </para>
		/// </remarks>
		ISqlExpression? ValueIn(ITranslationContext translationContext, Expression operand, TranslationFlags translationFlags, long perUnit, bool roundUp)
		{
			var unwrapped = operand.UnwrapConvert();

			if (unwrapped.Type != typeof(TimeSpan) && unwrapped.Type != typeof(TimeSpan?))
				return null;

			var bound = BoundIn(translationContext, unwrapped, perUnit, roundUp);

			if (bound == null)
				return null;

			// The count is asked for as an expression rather than worked out here, so how the value travels stays
			// the ordinary decision - it becomes a parameter, as an integer in the same position already does.
			// Deciding it here would settle that by accident: the number would be written into the statement, and a
			// query differing only by its duration would ask the previous one's question.
			return translationContext.Translate(bound, translationFlags) is SqlPlaceholderExpression placeholder
				? placeholder.Sql
				: null;
		}

		/// <summary>
		/// A comparison asked of the bare declared column, or <see langword="null"/> when the operands are not a
		/// declared duration against a value, or when the operator cannot be asked that way.
		/// </summary>
		/// <remarks>
		/// Equality is asked as the two comparisons it is made of - at least the duration, and at most it - rather
		/// than built here. Those two round in the directions equality needs without being told to: at least takes
		/// the representable value above and at most the one below, so a duration the column can hold makes them
		/// meet on it, and one it cannot makes them cross over and hold nothing. That is what equality means for a
		/// column that cannot represent the duration asked about, and it is said by asking, not by deciding.
		/// <para>
		/// A duration that may itself be absent gets the same range written in equalities instead, because a range
		/// spelled with bounds has no ends when the duration is absent and so matches nothing - where the language
		/// has an absent duration equal to a column that is absent too.
		/// </para>
		/// <para>
		/// Inequality is not asked this way. It is the complement of that pair, which is a disjunction - and one no
		/// index is going to walk anyway, so the column is left to scale into ticks and the comparison stays a single
		/// predicate.
		/// </para>
		/// </remarks>
		ISqlPredicate? InDeclaredUnit(
			ITranslationContext   translationContext,
			ISqlExpression?       interval,
			Expression            column,
			Expression            other,
			TranslationFlags      translationFlags,
			SqlPredicate.Operator comparison,
			bool                  mirrored)
		{
			if (interval is not SqlIntervalExpression declared)
				return null;

			if (!SqlIntervalUnits.TryGetTicksRatio(declared.IntervalType.Resolution, out var perUnit, out var denominator) || denominator != 1)
				return null;

			// A value on the left asks the mirrored question, so the operator turns with it before the rounding is
			// chosen from it.
			var op = mirrored
				? comparison switch
				{
					SqlPredicate.Operator.Greater        => SqlPredicate.Operator.Less,
					SqlPredicate.Operator.GreaterOrEqual => SqlPredicate.Operator.LessOrEqual,
					SqlPredicate.Operator.Less           => SqlPredicate.Operator.Greater,
					SqlPredicate.Operator.LessOrEqual    => SqlPredicate.Operator.GreaterOrEqual,
					_                                    => comparison,
				}
				: comparison;

			var stored = declared.Value;

			// Above a duration means above everything at or below it, so the bound falls to the representable value
			// underneath. At least a duration means at or above the representable value on top of it.
			ISqlPredicate? Bounded(bool roundUp)
			{
				var bound = ValueIn(translationContext, other, translationFlags, perUnit, roundUp);

				return bound == null ? null : Compare(translationContext, stored, op, bound);
			}

			switch (op)
			{
				case SqlPredicate.Operator.Greater:
				case SqlPredicate.Operator.LessOrEqual:
					return Bounded(roundUp: false);

				case SqlPredicate.Operator.GreaterOrEqual:
				case SqlPredicate.Operator.Less:
					return Bounded(roundUp: true);

				// A column counting ticks represents every duration there is, so the two comparisons would be one
				// written twice.
				case SqlPredicate.Operator.Equal when perUnit == 1:
					return Bounded(roundUp: false);

				case SqlPredicate.Operator.Equal when IsPresentDuration(translationContext, other):
				{
					var pair = Expression.AndAlso(
						Expression.MakeBinary(ExpressionType.GreaterThanOrEqual, column, other),
						Expression.MakeBinary(ExpressionType.LessThanOrEqual,    column, other));

					return translationContext.Translate(pair, translationFlags) is SqlPlaceholderExpression placeholder
						&& placeholder.Sql is ISqlPredicate asked
							? asked
							: null;
				}

				case SqlPredicate.Operator.Equal:
				{
					// The same range, written in equalities. A duration that may be absent needs it written that way:
					// an equality is the only comparison whose absent operand the engine reads as asking for absence,
					// so a range spelled with bounds would lose the row whose duration is absent, which the language
					// has equal to an absent duration. Read as two conditions on one column the pair looks like a
					// contradiction - it is the same two ends as above, and says the same three things: they meet on
					// a duration the column can hold, fall either side of one it cannot, and both go absent together.
					var atLeast = ValueIn(translationContext, other, translationFlags, perUnit, roundUp: true);
					var atMost  = ValueIn(translationContext, other, translationFlags, perUnit, roundUp: false);

					if (atLeast == null || atMost == null)
						return null;

					return translationContext.ExpressionFactory.SearchCondition()
						.Add(Compare(translationContext, stored, SqlPredicate.Operator.Equal, atLeast))
						.Add(Compare(translationContext, stored, SqlPredicate.Operator.Equal, atMost));
				}

				default:
					return null;
			}
		}

		/// <summary>
		/// Whether the operand is a duration whose value is settled before the statement runs and cannot turn out to
		/// be absent.
		/// </summary>
		/// <remarks>
		/// Only such a duration can have its equality written as a range. One that may be absent is equal to a column
		/// that is absent too, and a range spelled with bounds cannot say that.
		/// </remarks>
		static bool IsPresentDuration(ITranslationContext translationContext, Expression operand)
		{
			return translationContext.CanBeEvaluated(operand) && operand.UnwrapConvert().Type == typeof(TimeSpan);
		}

		/// <summary>
		/// Every expression the registries did not claim passes here, which is where a comparison against a
		/// duration's total can be caught.
		/// </summary>
		protected override Expression? TranslateOverrideHandler(ITranslationContext translationContext, Expression memberExpression, TranslationFlags translationFlags)
		{
			if (memberExpression is BinaryExpression binaryExpression
				&& TranslateTotalComparison(translationContext, binaryExpression, translationFlags) is { } translated)
			{
				return translated;
			}

			return base.TranslateOverrideHandler(translationContext, memberExpression, translationFlags);
		}

		/// <summary>
		/// The duration unit a <c>Total*</c> member counts in, when the member is one.
		/// </summary>
		static bool TryGetTotalUnit(MemberInfo member, out SqlIntervalUnit unit)
		{
			unit = default;

			if (member.DeclaringType != typeof(TimeSpan))
				return false;

			switch (member.Name)
			{
				case nameof(TimeSpan.TotalDays)         : unit = SqlIntervalUnit.Day;         return true;
				case nameof(TimeSpan.TotalHours)        : unit = SqlIntervalUnit.Hour;        return true;
				case nameof(TimeSpan.TotalMinutes)      : unit = SqlIntervalUnit.Minute;      return true;
				case nameof(TimeSpan.TotalSeconds)      : unit = SqlIntervalUnit.Second;      return true;
				case nameof(TimeSpan.TotalMilliseconds) : unit = SqlIntervalUnit.Millisecond; return true;
#if NET8_0_OR_GREATER
				case nameof(TimeSpan.TotalMicroseconds) : unit = SqlIntervalUnit.Microsecond; return true;
				case nameof(TimeSpan.TotalNanoseconds)  : unit = SqlIntervalUnit.Nanosecond;  return true;
#endif
				default                                 :                                     return false;
			}
		}

		/// <summary>
		/// A bound written as a count of one unit, counted in another - the column's - and rounded the way the
		/// comparison needs.
		/// </summary>
		static Expression BoundInUnit(Expression total, long perMember, long perUnit, bool roundUp)
		{
			return ExpressionHelpers.MoveValueMarkerOutside(total, value =>
			{
				var scaled = Expression.Divide(
					Expression.Multiply(Expression.Convert(value, typeof(decimal)), Expression.Constant((decimal)perMember)),
					Expression.Constant((decimal)perUnit));

				var rounded = Expression.Call(typeof(Math), roundUp ? nameof(Math.Ceiling) : nameof(Math.Floor), null, scaled);

				return Expression.Convert(rounded, typeof(long));
			});
		}

		/// <summary>
		/// A comparison between a duration's total in some unit and a number, asked of the bare declared column.
		/// </summary>
		Expression? TranslateTotalComparison(ITranslationContext translationContext, BinaryExpression binaryExpression, TranslationFlags translationFlags)
		{
			var comparison = binaryExpression.NodeType switch
			{
				ExpressionType.GreaterThan        => SqlPredicate.Operator.Greater,
				ExpressionType.GreaterThanOrEqual => SqlPredicate.Operator.GreaterOrEqual,
				ExpressionType.LessThan           => SqlPredicate.Operator.Less,
				ExpressionType.LessThanOrEqual    => SqlPredicate.Operator.LessOrEqual,
				_                                 => (SqlPredicate.Operator?)null,
			};

			if (comparison == null)
				return null;

			if (!IsDurationTotal(binaryExpression.Left) && !IsDurationTotal(binaryExpression.Right))
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			var direct =
				TotalInDeclaredUnit(translationContext, binaryExpression.Left, binaryExpression.Right, translationFlags, comparison.Value, mirrored: false)
				?? TotalInDeclaredUnit(translationContext, binaryExpression.Right, binaryExpression.Left, translationFlags, comparison.Value, mirrored: true);

			if (direct == null)
				return null;

			return translationContext.CreatePlaceholder(
				translationContext.CurrentSelectQuery,
				translationContext.ExpressionFactory.SearchCondition().Add(direct),
				binaryExpression);

			static bool IsDurationTotal(Expression operand)
			{
				return operand.UnwrapConvert() is MemberExpression { Expression: not null } member && TryGetTotalUnit(member.Member, out _);
			}
		}

		/// <summary>
		/// The comparison asked of the column the total was read from, or <see langword="null"/> when the operands
		/// are not a declared duration's total against a number.
		/// </summary>
		ISqlPredicate? TotalInDeclaredUnit(
			ITranslationContext   translationContext,
			Expression            total,
			Expression            other,
			TranslationFlags      translationFlags,
			SqlPredicate.Operator comparison,
			bool                  mirrored)
		{
			if (total.UnwrapConvert() is not MemberExpression { Expression: { } duration } member
				|| !TryGetTotalUnit(member.Member, out var memberUnit))
			{
				return null;
			}

			var unwrapped = other.UnwrapConvert();

			if (unwrapped.Type != typeof(double) || !translationContext.CanBeEvaluated(unwrapped))
				return null;

			if (TranslateIntervalOperand(translationContext, duration, translationFlags) is not SqlIntervalExpression declared)
				return null;

			if (!SqlIntervalUnits.TryGetTicksRatio(declared.IntervalType.Resolution, out var perUnit, out var storedDenominator) || storedDenominator != 1)
				return null;

			if (!SqlIntervalUnits.TryGetTicksRatio(memberUnit, out var perMember, out var memberDenominator) || memberDenominator != 1)
				return null;

			var op = mirrored
				? comparison switch
				{
					SqlPredicate.Operator.Greater        => SqlPredicate.Operator.Less,
					SqlPredicate.Operator.GreaterOrEqual => SqlPredicate.Operator.LessOrEqual,
					SqlPredicate.Operator.Less           => SqlPredicate.Operator.Greater,
					SqlPredicate.Operator.LessOrEqual    => SqlPredicate.Operator.GreaterOrEqual,
					_                                    => comparison,
				}
				: comparison;

			var roundUp = op is SqlPredicate.Operator.GreaterOrEqual or SqlPredicate.Operator.Less;

			var bound = translationContext.Translate(BoundInUnit(unwrapped, perMember, perUnit, roundUp), translationFlags) is SqlPlaceholderExpression placeholder
				? placeholder.Sql
				: null;

			return bound == null ? null : Compare(translationContext, declared.Value, op, bound);
		}

		/// <summary>
		/// A comparison between two durations, reading one that is absent the way the language reads it.
		/// </summary>
		/// <remarks>
		/// Every comparison here is written as a filter, and a duration that is not there is neither above nor below
		/// anything - which is what the language says of it, and what the pair standing in for an equality has to say
		/// too: read the other way, an absent duration would come out equal to every duration asked about. The
		/// factory's shorter overloads read <c>&gt;=</c> and <c>&lt;=</c> the other way, for callers putting the
		/// comparison in a <c>CASE</c>, so the reading is stated here rather than taken.
		/// </remarks>
		static ISqlPredicate Compare(ITranslationContext translationContext, ISqlExpression left, SqlPredicate.Operator op, ISqlExpression right)
		{
			var factory = translationContext.ExpressionFactory;

			// The absence of a duration answers no, unless nothing is being read into absence at all.
			var unknownValue = factory.DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? false : (bool?)null;

			return op switch
			{
				SqlPredicate.Operator.Equal          => factory.Equal(left, right, unknownValue),
				SqlPredicate.Operator.NotEqual       => factory.NotEqual(left, right, unknownValue),
				SqlPredicate.Operator.Greater        => factory.Greater(left, right, unknownValue),
				SqlPredicate.Operator.GreaterOrEqual => factory.GreaterOrEqual(left, right, unknownValue),
				SqlPredicate.Operator.Less           => factory.Less(left, right, unknownValue),
				SqlPredicate.Operator.LessOrEqual    => factory.LessOrEqual(left, right, unknownValue),
				_                                    => throw new InvalidOperationException($"Cannot compare durations with {op}."),
			};
		}

		/// <summary>
		/// Compares two durations by their tick counts rather than by the numbers they happen to be stored as.
		/// </summary>
		/// <remarks>
		/// A duration lowers to its stored amount and nothing more - the unit is put back by the read path, through
		/// the column descriptor. That works for a projection and for nothing else: two durations meeting in a
		/// comparison arrive as bare numbers in whatever units they were declared with, so ninety minutes held as
		/// 1800 seconds and as 18000000000 ticks compare unequal.
		/// <para>
		/// Both sides are taken to ticks instead, which is the one representation every duration has: a declared
		/// column converts through its unit, and an elapsed difference is a tick count already. On a provider with
		/// a native interval type this also stops the two sides having different SQL types.
		/// </para>
		/// </remarks>
		Expression? TranslateIntervalComparison(ITranslationContext translationContext, BinaryExpression binaryExpression, TranslationFlags translationFlags)
		{
			// What the comparison sends is a tick count, and the column whose value the comparison is being built as
			// - the flag it is assigned to, the column it is compared against - has nothing to say about how a tick
			// count is written. Left in scope its descriptor would be asked anyway, so it is dropped here, as every
			// other translation in this file drops it.
			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			var factory  = translationContext.ExpressionFactory;
			var tickType = factory.GetDbDataType(typeof(long));

			var leftInterval  = TranslateIntervalOperand(translationContext, binaryExpression.Left,  translationFlags);
			var rightInterval = TranslateIntervalOperand(translationContext, binaryExpression.Right, translationFlags);

			SqlPredicate.Operator? comparison = binaryExpression.NodeType switch
			{
				ExpressionType.Equal              => SqlPredicate.Operator.Equal,
				ExpressionType.NotEqual           => SqlPredicate.Operator.NotEqual,
				ExpressionType.GreaterThan        => SqlPredicate.Operator.Greater,
				ExpressionType.GreaterThanOrEqual => SqlPredicate.Operator.GreaterOrEqual,
				ExpressionType.LessThan           => SqlPredicate.Operator.Less,
				ExpressionType.LessThanOrEqual    => SqlPredicate.Operator.LessOrEqual,
				_                                 => null,
			};

			if (comparison == null)
				return null;

			// A declared duration against an ordinary value: the value goes into the column's unit rather than the
			// column into ticks, so the column stays bare and whatever index it has is still reachable.
			var direct =
				InDeclaredUnit(translationContext, leftInterval, binaryExpression.Left, binaryExpression.Right, translationFlags, comparison.Value, mirrored: false)
				?? InDeclaredUnit(translationContext, rightInterval, binaryExpression.Right, binaryExpression.Left, translationFlags, comparison.Value, mirrored: true);

			if (direct != null)
			{
				return translationContext.CreatePlaceholder(
					translationContext.CurrentSelectQuery,
					factory.SearchCondition().Add(direct),
					binaryExpression);
			}

			var leftTicks  = leftInterval  != null ? new SqlIntervalPartExpression(leftInterval,  SqlIntervalUnit.Tick, SqlIntervalPartKind.Total, tickType) : ValueIn(translationContext, binaryExpression.Left,  translationFlags, 1, roundUp: false);
			var rightTicks = rightInterval != null ? new SqlIntervalPartExpression(rightInterval, SqlIntervalUnit.Tick, SqlIntervalPartKind.Total, tickType) : ValueIn(translationContext, binaryExpression.Right, translationFlags, 1, roundUp: false);

			if (leftTicks == null || rightTicks == null)
			{
				// The same refusal a member of an unlowerable difference gets, for the same reason: a comparison
				// has to be made by the database, so there is no client-side answer to fall back to, and saying
				// which operation is missing beats reporting that some expression could not be converted.
				if (!translationContext.ProviderFlags.CanLowerIntervalPart
					&& (IsDateDifference(translationContext, binaryExpression.Left) || IsDateDifference(translationContext, binaryExpression.Right)))
				{
					return translationContext.CreateErrorExpression(binaryExpression, ErrorHelper.Error_Interval_Difference);
				}

				return null;
			}

			return translationContext.CreatePlaceholder(
				translationContext.CurrentSelectQuery,
				factory.SearchCondition().Add(Compare(translationContext, leftTicks, comparison.Value, rightTicks)),
				binaryExpression);
		}

		/// <summary>
		/// Whether an operand is the difference between two date/time values, in either the shape it was written
		/// in or the one a projection leaves behind.
		/// </summary>
		static bool IsDateDifference(ITranslationContext translationContext, Expression? operand)
		{
			if (operand == null)
				return false;

			return AsDateDifference(operand) != null
				|| AsDateDifference(translationContext.Translate(operand, TranslationFlags.Expand)) != null;
		}

		/// <summary>
		/// The expression as a subtraction of two date/time values of the same type, or <see langword="null"/>.
		/// </summary>
		static BinaryExpression? AsDateDifference(Expression? expression)
		{
			if (expression is not BinaryExpression { NodeType: ExpressionType.Subtract } subtraction)
				return null;

			var left  = subtraction.Left.Type.ToUnderlying();
			var right = subtraction.Right.Type.ToUnderlying();

			return left == right && (left == typeof(DateTime) || left == typeof(DateTimeOffset)) ? subtraction : null;
		}

		SqlIntervalDifferenceExpression? MakeDateDifference(ITranslationContext translationContext, BinaryExpression binaryExpression, TranslationFlags translationFlags)
		{
			var left = TranslateNoRequiredExpression(translationContext, binaryExpression.Left, translationFlags);
			if (left == null)
				return null;

			var right = TranslateNoRequiredExpression(translationContext, binaryExpression.Right, translationFlags);
			if (right == null)
				return null;

			var factory = translationContext.ExpressionFactory;
			var type    = factory.GetDbDataType(typeof(TimeSpan)).WithDataType(DataType.Int64);

			return new SqlIntervalDifferenceExpression(right.Sql, left.Sql, type, SqlIntervalType.ClrTimeSpan);
		}

		Expression? TranslateTimeSpanNegate(ITranslationContext translationContext, UnaryExpression unaryExpression, TranslationFlags translationFlags)
		{
			var placeholder = TranslateNoRequiredExpression(translationContext, unaryExpression.Operand, translationFlags);
			if (placeholder == null)
				return null;

			var factory  = translationContext.ExpressionFactory;
			var interval = TryMakeInterval(translationContext, placeholder.Sql);

			ISqlExpression negated;

			switch (interval)
			{
				// Negating the stored amount negates the interval whatever the unit, as long as the storage is
				// signed. An unsigned storage cannot hold the result, so leave it untranslated.
				case SqlIntervalExpression stored when stored.IntervalType.IsSigned:
					negated = new SqlIntervalExpression(
						factory.Negate(stored.Type, stored.Value),
						stored.Type,
						stored.IntervalType);
					break;

				// -(End - Start) is Start - End exactly, with no arithmetic and so no chance of overflow.
				case SqlIntervalDifferenceExpression difference:
					negated = new SqlIntervalDifferenceExpression(
						difference.End,
						difference.Start,
						difference.Type,
						difference.IntervalType);
					break;

				default:
					return null;
			}

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, negated, unaryExpression);
		}

		/// <summary>
		/// Wraps a translated expression as an interval, provided the mapping says what unit it is stored in.
		/// </summary>
		/// <remarks>
		/// Returning <see langword="null"/> when no unit is declared is what keeps duration support opt-in: an
		/// undeclared <see cref="TimeSpan"/> column falls back to whatever it means today rather than being
		/// reinterpreted as a tick count.
		/// </remarks>
		private protected static ISqlExpression? TryMakeInterval(ITranslationContext translationContext, ISqlExpression expression)
		{
			// Already an interval: a mapped duration wrapped earlier, or a computed difference, which is an
			// interval by construction and needs no unit of its own - the provider lowers it to ticks.
			if (expression is SqlIntervalExpression or SqlIntervalDifferenceExpression)
				return expression;

			var descriptor = QueryHelper.GetColumnDescriptor(expression);
			var unit       = descriptor?.DurationUnit;
			if (unit == null)
				return null;

			// The type comes from the same descriptor that supplies the unit, so the node carries the *model*
			// type (TimeSpan) with the storage DataType. Carrying the storage system type instead would make
			// QueryHelper.GetColumnDescriptor drop this expression when walking back to the column - its binary
			// branch requires the expression's SystemType to still match the column's - and the read path would
			// then miss the value converter and read the amount as raw ticks.
			var type = descriptor!.GetDbDataType(true);

			return new SqlIntervalExpression(expression, type, SqlIntervalType.ForDuration(unit.Value));
		}

		Expression? TranslateTimeSpanMember(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags, SqlIntervalUnit unit, SqlIntervalPartKind kind, SqlIntervalUnit? within)
		{
			// A component the provider's measurement cannot distinguish is not an imprecise answer but a constant
			// zero: the count it is taken from was rounded first, so nothing is left inside the unit the component
			// is counted within.
			//
			// The test is against that enclosing unit, not against the component's own. Nanoseconds is finer than
			// a tick and still meaningful - a tick is a hundred nanoseconds, so the component runs 0, 100, ... 900
			// within a microsecond - while microseconds against a millisecond measurement really is always zero.
			//
			// Reported as an error expression rather than by returning null, because the two are not the same
			// here. An error only propagates where SQL is actually required - a projection still falls back to
			// .NET, which holds both dates and answers exactly - so this keeps the correct answer where one is
			// possible and replaces the generic "could not be converted" with the reason where it is not.
			//
			// Totals are left alone: they scale the whole count rather than reading inside a coarser unit, so a
			// coarser measurement makes them quantised but still meaningful.
			//
			// Asked of the resolved operand rather than up front, because the limit belongs to measuring elapsed
			// time and not to durations in general: a column declared in ticks holds its own sub-second part
			// exactly, whatever the provider's own difference would have rounded to.

			var interval = TranslateIntervalOperand(translationContext, memberExpression.Expression, translationFlags);

			if (interval == null)
			{
				// Asking a member of a difference is a point where SQL is genuinely wanted, so a provider that
				// cannot measure one says so by name here rather than leaving the whole expression to fail as
				// something that could not be converted. Named at the member and not at the difference itself
				// because that would be too early: the forms that cancel before any provider is asked - start +
				// (end - start) is end - would be sunk along with it, and they need nothing from the provider.
				//
				// An error only propagates where SQL is required, so a projection still falls back to .NET and
				// answers exactly, which is what these providers do for a difference they can compute on the row.
				if (!translationContext.ProviderFlags.CanLowerIntervalPart && IsDateDifference(translationContext, memberExpression.Expression))
					return translationContext.CreateErrorExpression(memberExpression, ErrorHelper.Error_Interval_Difference);

				return null;
			}

			// A duration stored below a tick has no member lowering anywhere: reaching a tick count from it needs a
			// division, and neither path that serves a member chooses a rounding rule for one, because providers
			// disagree about which way a negative value goes.
			//
			// Left untranslated rather than refused by name, and that choice is what decides where the member still
			// works. Untranslated, a projection reads the column and computes the member in .NET, which answers
			// exactly; an error expression would take that away and refuse everywhere. What remains refused is the
			// use that genuinely requires SQL - a predicate, an ordering, an explicit Sql.AsSql - and it is refused
			// as an expression that could not be converted rather than as a limit of the current provider, which
			// this is not.
			if (QueryHelper.UnwrapNullablity(interval) is SqlIntervalExpression { IntervalType.Domain: SqlIntervalDomain.Duration } stored
				&& (!SqlIntervalUnits.TryGetTicksRatio(stored.IntervalType.Resolution, out _, out var storedDenominator) || storedDenominator != 1))
			{
				return null;
			}

			var resolution = translationContext.ProviderFlags.IntervalResolution;

			if (kind == SqlIntervalPartKind.Component
				&& within is { } enclosing
				&& QueryHelper.UnwrapNullablity(interval) is SqlIntervalDifferenceExpression
				&& !SqlIntervalUnits.IsFinerThan(resolution, enclosing))
			{
				return translationContext.CreateErrorExpression(
					memberExpression,
					string.Format(
						CultureInfo.InvariantCulture,
						ErrorHelper.Error_Interval_ComponentBelowResolution,
						unit.ToString().ToLowerInvariant(),
						resolution.ToString().ToLowerInvariant()));
			}

			// A total below the resolution is quantised rather than meaningless, so it is built - except where the
			// provider cannot reach one at all. Without a tick count to divide there is nothing to scale and no date
			// part fine enough to name, so every lowering declines and the node reaches the SQL builder, whose only
			// answer is to throw.
			//
			// Which of the two refusals it is depends on what the caller asked for, because an error expression
			// refuses everywhere - measured, not assumed. Where SQL is genuinely required the member cannot be
			// answered at all, so it is refused by name. Where it is not, returning null leaves the member readable
			// and a projection computes it in .NET from both dates, exactly.
			if (kind == SqlIntervalPartKind.Total
				&& !translationContext.ProviderFlags.CanMeasureDifferenceInTicks
				&& QueryHelper.UnwrapNullablity(interval) is SqlIntervalDifferenceExpression
				&& SqlIntervalUnits.IsFinerThan(unit, resolution))
			{
				return translationFlags.HasFlag(TranslationFlags.Sql)
					? translationContext.CreateErrorExpression(memberExpression, ErrorHelper.Error_Interval_Member)
					: null;
			}

			var resultType = translationContext.ExpressionFactory.GetDbDataType(memberExpression.Type);
			var part       = new SqlIntervalPartExpression(interval, unit, kind, resultType, within);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, part, memberExpression);
		}

		Expression? TranslateDateTimeMember(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags, Sql.DateParts datepart)
		{
			var placeholder = TranslateNoRequiredExpression(translationContext, memberExpression.Expression, translationFlags);
			if (placeholder == null)
				return null;

			var converted = TranslateDateTimeDatePart(translationContext, translationFlags, placeholder.Sql, datepart);
			if (converted == null)
				return null;

			//TODO: Why?
			if (datepart == Sql.DateParts.WeekDay)
				converted = translationContext.ExpressionFactory.Decrement(converted);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, memberExpression);
		}

		Expression? TranslateDateTimeOffsetMember(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags, Sql.DateParts datepart)
		{
			var placeholder = TranslateNoRequiredExpression(translationContext, memberExpression.Expression, translationFlags);
			if (placeholder == null)
				return null;

			var converted = TranslateDateTimeOffsetDatePart(translationContext, translationFlags, placeholder.Sql, datepart);
			if (converted == null)
				return null;

			//TODO: Why?
			if (datepart == Sql.DateParts.WeekDay)
				converted = translationContext.ExpressionFactory.Decrement(converted);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, memberExpression);
		}

		Expression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var placeholder = TranslateNoRequiredExpression(translationContext, memberExpression.Expression, translationFlags);
			if (placeholder == null)
				return null;

			var converted = TranslateDateTimeTruncationToDate(translationContext, placeholder.Sql, translationFlags);
			if (converted == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, memberExpression);
		}

		Expression? TranslateDateTimeOffsetTruncationToDate(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var placeholder = TranslateNoRequiredExpression(translationContext, memberExpression.Expression, translationFlags);
			if (placeholder == null)
				return null;

			var converted = TranslateDateTimeOffsetTruncationToDate(translationContext, placeholder.Sql, translationFlags);
			if (converted == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, memberExpression);
		}

		Expression? TranslateDateTimeTruncationToTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var placeholder = TranslateNoRequiredExpression(translationContext, memberExpression.Expression, translationFlags);
			if (placeholder == null)
				return null;

			var converted = TranslateDateTimeTruncationToTime(translationContext, placeholder.Sql, translationFlags);
			if (converted == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, memberExpression);
		}

		Expression? TranslateDateTimeOffsetTruncationToTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var placeholder = TranslateNoRequiredExpression(translationContext, memberExpression.Expression, translationFlags);
			if (placeholder == null)
				return null;

			var converted = TranslateDateTimeOffsetTruncationToTime(translationContext, placeholder.Sql, translationFlags);
			if (converted == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, memberExpression);
		}

		Expression? TranslateDateTimeSqlDatepart(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<Sql.DateParts>(methodCall.Arguments[0], out var datePart))
				return null;

			var dateExpr = translationContext.Translate(methodCall.Arguments[1]);

			if (dateExpr is not SqlPlaceholderExpression datePlaceholder)
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			var converted = TranslateDateTimeDatePart(translationContext, translationFlags, datePlaceholder.Sql, datePart);
			if (converted == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, methodCall);
		}

		Expression? TranslateDateTimeOffsetSqlDatepart(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<Sql.DateParts>(methodCall.Arguments[0], out var datePart))
				return null;

			var dateExpr = translationContext.Translate(methodCall.Arguments[1]);

			if (dateExpr is not SqlPlaceholderExpression datePlaceholder)
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			var converted = TranslateDateTimeOffsetDatePart(translationContext, translationFlags, datePlaceholder.Sql, datePart);
			if (converted == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, methodCall);
		}

		Expression? TranslateDateTimeAddMember(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, Sql.DateParts datepart)
		{
			if (methodCall.Object != null && translationContext.CanBeEvaluatedOnClient(methodCall.Object) && translationContext.CanBeEvaluatedOnClient(methodCall.Arguments[0]))
				return null;

			var datePlaceholder = TranslateNoRequiredExpression(translationContext, methodCall.Object, translationFlags, false);
			if (datePlaceholder == null)
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			var incrementPlaceholder = TranslateNoRequiredExpression(translationContext, methodCall.Arguments[0].UnwrapConvert(), translationFlags, false);
			if (incrementPlaceholder == null)
				return null;

			// Can be evaluated on client side
			if (datePlaceholder.Sql is SqlParameter && incrementPlaceholder.Sql is SqlParameter)
				return null;

			var converted = TranslateDateTimeDateAdd(translationContext, translationFlags, datePlaceholder.Sql, incrementPlaceholder.Sql, datepart);
			if (converted == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, methodCall);
		}

		Expression? TranslateDateTimeOffsetAddMember(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, Sql.DateParts datepart)
		{
			if (methodCall.Object != null && translationContext.CanBeEvaluatedOnClient(methodCall.Object) && translationContext.CanBeEvaluatedOnClient(methodCall.Arguments[0]))
				return null;

			var datePlaceholder = TranslateNoRequiredExpression(translationContext, methodCall.Object, translationFlags, false);
			if (datePlaceholder == null)
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			var incrementPlaceholder = TranslateNoRequiredExpression(translationContext, methodCall.Arguments[0].UnwrapConvert(), translationFlags, false);
			if (incrementPlaceholder == null)
				return null;

			// Can be evaluated on client side
			if (datePlaceholder.Sql is SqlParameter && incrementPlaceholder.Sql is SqlParameter)
				return null;

			var converted = TranslateDateTimeOffsetDateAdd(translationContext, translationFlags, datePlaceholder.Sql, incrementPlaceholder.Sql, datepart);
			if (converted == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, methodCall);
		}

#if NET8_0_OR_GREATER
		Expression? TranslateDateOnlyAddMember(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, Sql.DateParts datepart)
		{
			var datePlaceholder = TranslateNoRequiredExpression(translationContext, methodCall.Object, translationFlags, false);
			if (datePlaceholder == null)
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			var incrementPlaceholder = TranslateNoRequiredExpression(translationContext, methodCall.Arguments[0].UnwrapConvert(), translationFlags, false);
			if (incrementPlaceholder == null)
				return null;

			// Can be evaluated on client side
			if (datePlaceholder.Sql is SqlParameter && incrementPlaceholder.Sql is SqlParameter)
				return null;

			var converted = TranslateDateOnlyDateAdd(translationContext, translationFlags, datePlaceholder.Sql, incrementPlaceholder.Sql, datepart);
			if (converted == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, methodCall);
		}
#endif

		Expression? TranslateDateTimeDateAdd(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<Sql.DateParts>(methodCall.Arguments[0], out var datepart))
				return null;

			var datePlaceholder = TranslateNoRequiredExpression(translationContext, methodCall.Arguments[2].UnwrapConvert(), translationFlags, false);
			if (datePlaceholder == null)
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			var incrementPlaceholder = TranslateNoRequiredExpression(translationContext, methodCall.Arguments[1].UnwrapConvert(), translationFlags, false);
			if (incrementPlaceholder == null)
				return null;

			// Can be evaluated on client side
			if (datePlaceholder.Sql is SqlParameter && incrementPlaceholder.Sql is SqlParameter)
				return null;

			var converted = TranslateDateTimeDateAdd(translationContext, translationFlags, datePlaceholder.Sql, incrementPlaceholder.Sql, datepart);
			if (converted == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, methodCall);
		}

		Expression? TranslateDateTimeOffsetDateAdd(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<Sql.DateParts>(methodCall.Arguments[0], out var datepart))
				return null;

			var datePlaceholder = TranslateNoRequiredExpression(translationContext, methodCall.Arguments[2].UnwrapConvert(), translationFlags, false);
			if (datePlaceholder == null)
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			var incrementPlaceholder = TranslateNoRequiredExpression(translationContext, methodCall.Arguments[1].UnwrapConvert(), translationFlags, false);
			if (incrementPlaceholder == null)
				return null;

			// Can be evaluated on client side
			if (datePlaceholder.Sql is SqlParameter && incrementPlaceholder.Sql is SqlParameter)
				return null;

			var converted = TranslateDateTimeOffsetDateAdd(translationContext, translationFlags, datePlaceholder.Sql, incrementPlaceholder.Sql, datepart);
			if (converted == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, methodCall);
		}

#if SUPPORTS_DATEONLY
		Expression? TranslateDateOnlyConstructor(ITranslationContext translationContext, Expression expression, TranslationFlags translationFlags)
		{
			if (expression is not NewExpression newExpression)
				return null;

			if (newExpression.Arguments.Count < 3)
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			if (!translationContext.TranslateToSqlExpression(newExpression.Arguments[0], out var year)  ||
			    !translationContext.TranslateToSqlExpression(newExpression.Arguments[1], out var month) ||
			    !translationContext.TranslateToSqlExpression(newExpression.Arguments[2], out var day))
			{
				return null;
			}

			var makeExpression = TranslateMakeDateOnly(translationContext, translationContext.ExpressionFactory.GetDbDataType(expression.Type), year, month, day);

			if (makeExpression == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, makeExpression, newExpression);
		}

		Expression? TranslateMakeDateOnlyMethod(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (methodCall.Arguments.Count < 3)
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0].UnwrapConvert(), out var year)   ||
			    !translationContext.TranslateToSqlExpression(methodCall.Arguments[1].UnwrapConvert(), out var month)  ||
			    !translationContext.TranslateToSqlExpression(methodCall.Arguments[2].UnwrapConvert(), out var day)    )
			{
				return null;
			}

			var makeExpression = TranslateMakeDateOnly(translationContext, translationContext.ExpressionFactory.GetDbDataType(methodCall.Type), year, month, day);

			if (makeExpression == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, makeExpression, methodCall);
		}

		Expression? TranslateDateOnlyMember(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags, Sql.DateParts datepart)
		{
			var placeholder = TranslateNoRequiredExpression(translationContext, memberExpression.Expression, translationFlags);
			if (placeholder == null)
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			var converted = TranslateDateOnlyDatePart(translationContext, translationFlags, placeholder.Sql, datepart);
			if (converted == null)
				return null;

			//TODO: Why?
			if (datepart == Sql.DateParts.WeekDay)
				converted = translationContext.ExpressionFactory.Decrement(converted);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, memberExpression);
		}

		Expression? TranslateDateOnlySqlDatepart(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<Sql.DateParts>(methodCall.Arguments[0], out var datePart))
				return null;

			var dateExpr = translationContext.Translate(methodCall.Arguments[1]);

			if (dateExpr is not SqlPlaceholderExpression datePlaceholder)
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			var converted = TranslateDateOnlyDatePart(translationContext, translationFlags, datePlaceholder.Sql, datePart);
			if (converted == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, methodCall);
		}

		Expression? TranslateDateOnlyDateAdd(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<Sql.DateParts>(methodCall.Arguments[0], out var datepart))
				return null;

			var datePlaceholder = TranslateNoRequiredExpression(translationContext, methodCall.Arguments[2].UnwrapConvert(), translationFlags, false);
			if (datePlaceholder == null)
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			var incrementPlaceholder = TranslateNoRequiredExpression(translationContext, methodCall.Arguments[1].UnwrapConvert(), translationFlags, false);
			if (incrementPlaceholder == null)
				return null;

			// Can be evaluated on client side
			if (datePlaceholder.Sql is SqlParameter && incrementPlaceholder.Sql is SqlParameter)
				return null;

			var converted = TranslateDateOnlyDateAdd(translationContext, translationFlags, datePlaceholder.Sql, incrementPlaceholder.Sql, datepart);
			if (converted == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, methodCall);
		}
#endif

		Expression? TranslateServerNow(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var translated = TranslateServerNow(translationContext, translationFlags);
			if (translated == null)
				return SqlErrorExpression.EnsureError(memberExpression);
			return translationContext.CreatePlaceholder(translated, memberExpression);
		}

		Expression? TranslateNow(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var translated = TranslateNow(translationContext, translationFlags);
			if (translated == null)
				return null;
			return translationContext.CreatePlaceholder(translated, memberExpression);
		}

		Expression? TranslateUtcNow(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var translated = TranslateUtcNow(translationContext, translationFlags);
			if (translated == null)
				return null;
			return translationContext.CreatePlaceholder(translated, memberExpression);
		}

		Expression? TranslateZonedNow(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var dbType = translationContext.CurrentColumnDescriptor?.GetDbDataType(true) ?? translationContext.ExpressionFactory.GetDbDataType(memberExpression.Type);

			var translated = TranslateZonedNow(translationContext, dbType, translationFlags);
			if (translated == null)
				return null;
			return translationContext.CreatePlaceholder(translated, memberExpression);
		}

		Expression? TranslateZonedUtcNow(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var dbType = translationContext.CurrentColumnDescriptor?.GetDbDataType(true) ?? translationContext.ExpressionFactory.GetDbDataType(memberExpression.Type);

			var translated = TranslateZonedUtcNow(translationContext, dbType, translationFlags);
			if (translated == null)
				return null;
			return translationContext.CreatePlaceholder(translated, memberExpression);
		}

		#region Methods to override

		protected virtual ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
		{
			return null;
		}

		protected virtual ISqlExpression? TranslateDateTimeOffsetDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
		{
			return null;
		}

		protected virtual ISqlExpression? TranslateDateOnlyDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
		{
			return TranslateDateTimeDatePart(translationContext, translationFlag, dateTimeExpression, datepart);
		}

		protected virtual ISqlExpression? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment, Sql.DateParts datepart)
		{
			return null;
		}

		protected virtual ISqlExpression? TranslateDateTimeOffsetDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment, Sql.DateParts datepart)
		{
			return TranslateDateTimeDateAdd(translationContext, translationFlag, dateTimeExpression, increment, datepart);
		}

		protected virtual ISqlExpression? TranslateDateOnlyDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment, Sql.DateParts datepart)
		{
			return TranslateDateTimeDateAdd(translationContext, translationFlag, dateTimeExpression, increment, datepart);
		}

		protected virtual ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
		{
			return null;
		}

		protected virtual ISqlExpression? TranslateDateTimeOffsetTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
		{
			return null;
		}

		protected virtual ISqlExpression? TranslateDateTimeTruncationToTime(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;
			var cast    = factory.Cast(dateExpression, factory.GetDbDataType(typeof(TimeSpan)).WithDataType(DataType.Time), true);

			return cast;
		}

		protected virtual ISqlExpression? TranslateDateTimeOffsetTruncationToTime(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;
			var cast    = factory.Cast(dateExpression, factory.GetDbDataType(typeof(TimeSpan)).WithDataType(DataType.Time), true);

			return cast;
		}

		protected virtual ISqlExpression? TranslateServerNow(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;
			var currentTimeStamp = factory.NotNullExpression(factory.GetDbDataType(typeof(DateTime)), "CURRENT_TIMESTAMP");
			return currentTimeStamp;
		}

		protected virtual ISqlExpression? TranslateNow(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;
			var currentTimeStamp = factory.NotNullExpression(factory.GetDbDataType(typeof(DateTime)), "CURRENT_TIMESTAMP");
			return currentTimeStamp;
		}

		protected virtual ISqlExpression? TranslateUtcNow(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			return null;
		}

		protected virtual ISqlExpression? TranslateZonedNow(ITranslationContext translationContext, DbDataType dbDataType, TranslationFlags translationFlags)
		{
			// Most RDBMS don't have a mapping for DateTimeOffset
			return TranslateNow(translationContext, translationFlags);
		}

		protected virtual ISqlExpression? TranslateZonedUtcNow(ITranslationContext translationContext, DbDataType dbDataType, TranslationFlags translationFlags)
		{
			// Most RDBMS don't have a mapping for DateTimeOffset
			return TranslateUtcNow(translationContext, translationFlags);
		}

		protected virtual ISqlExpression? TranslateMakeDateTime(
			ITranslationContext translationContext,
			DbDataType          resulType,
			ISqlExpression      year,
			ISqlExpression      month,
			ISqlExpression      day,
			ISqlExpression?     hour,
			ISqlExpression?     minute,
			ISqlExpression?     second,
			ISqlExpression?     millisecond)
		{
			return null;
		}

		protected virtual ISqlExpression? TranslateMakeDateOnly(
			ITranslationContext translationContext,
			DbDataType          resulType,
			ISqlExpression      year,
			ISqlExpression      month,
			ISqlExpression      day)
		{
			return TranslateMakeDateTime(translationContext, resulType, year, month, day, null, null, null, null);
		}

		#endregion

	}
}
