using System;
using System.Globalization;
using System.Linq.Expressions;

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

		/// <summary>
		/// An operand of a duration comparison, expressed in ticks.
		/// </summary>
		/// <remarks>
		/// A declared column or a computed difference becomes an interval node and its total in ticks. A plain
		/// <see cref="TimeSpan"/> value has no declared unit - by design, since an undeclared one keeps whatever
		/// the provider maps it to - but in a comparison against a duration its unit is not in doubt, so its tick
		/// count is used directly rather than letting the provider's own type meet a number.
		/// </remarks>
		ISqlExpression? TicksOf(ITranslationContext translationContext, Expression operand, TranslationFlags translationFlags, DbDataType tickType)
		{
			var interval = TranslateIntervalOperand(translationContext, operand, translationFlags);

			if (interval != null)
				return new SqlIntervalPartExpression(interval, SqlIntervalUnit.Tick, SqlIntervalPartKind.Total, tickType);

			// Everything that is not an interval reaches here, not only a value: a member of a set operation whose
			// branches disagree arrives too, and it has no tick count to give. So the question is asked before
			// anything else, and asked without answering it - whether the operand could be worked out, not what it
			// works out to. What could not leaves the comparison untranslated, which is how it is refused rather
			// than quietly answered.
			if (!translationContext.CanBeEvaluated(operand))
				return null;

			var unwrapped = operand.UnwrapConvert();

			if (unwrapped.Type != typeof(TimeSpan))
				return null;

			// The tick count is asked for as an expression rather than worked out here, so how the value travels
			// stays the ordinary decision - and it becomes a parameter, as an integer or a date in the same position
			// already does. Deciding it here would settle that by accident: the number would be written into the
			// statement, and a query differing only by its duration would ask the previous one's question.
			//
			// A caller who said how the value should travel wrapped the duration, and the duration is not what
			// reaches the statement - the tick count is - so the request is moved onto it.
			var ticks = ExpressionHelpers.MoveValueMarkerOutside(
				Expression.Property(unwrapped, nameof(TimeSpan.Ticks)));

			return translationContext.Translate(ticks, translationFlags) is SqlPlaceholderExpression placeholder
				? placeholder.Sql
				: null;
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

			var leftTicks  = TicksOf(translationContext, binaryExpression.Left,  translationFlags, tickType);
			var rightTicks = TicksOf(translationContext, binaryExpression.Right, translationFlags, tickType);

			if (leftTicks == null || rightTicks == null)
				return null;

			ISqlPredicate? predicate = binaryExpression.NodeType switch
			{
				ExpressionType.Equal              => factory.Equal(leftTicks, rightTicks),
				ExpressionType.NotEqual           => factory.NotEqual(leftTicks, rightTicks),
				ExpressionType.GreaterThan        => factory.Greater(leftTicks, rightTicks),
				ExpressionType.GreaterThanOrEqual => factory.GreaterOrEqual(leftTicks, rightTicks),
				ExpressionType.LessThan           => factory.Less(leftTicks, rightTicks),
				ExpressionType.LessThanOrEqual    => factory.LessOrEqual(leftTicks, rightTicks),
				_                                 => null,
			};

			if (predicate == null)
				return null;

			return translationContext.CreatePlaceholder(
				translationContext.CurrentSelectQuery,
				factory.SearchCondition().Add(predicate),
				binaryExpression);
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
			var resolution = translationContext.ProviderFlags.IntervalResolution;

			if (kind == SqlIntervalPartKind.Component && within is { } enclosing && !SqlIntervalUnits.IsFinerThan(resolution, enclosing))
			{
				return translationContext.CreateErrorExpression(
					memberExpression,
					string.Format(
						CultureInfo.InvariantCulture,
						ErrorHelper.Error_Interval_ComponentBelowResolution,
						unit.ToString().ToLowerInvariant(),
						resolution.ToString().ToLowerInvariant()));
			}

			var interval = TranslateIntervalOperand(translationContext, memberExpression.Expression, translationFlags);
			if (interval == null)
				return null;

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
