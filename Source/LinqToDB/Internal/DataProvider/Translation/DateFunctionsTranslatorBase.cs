using System;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public abstract class DateFunctionsTranslatorBase : MemberTranslatorBase
	{
		private protected enum TimeSpanPart
		{
			Days,
			TotalDays,
			Hours,
			TotalHours,
			Minutes,
			TotalMinutes,
			Seconds,
			TotalSeconds,
			Milliseconds,
			TotalMilliseconds,
#if NET7_0_OR_GREATER
			Microseconds,
			TotalMicroseconds,
			Nanoseconds,
			TotalNanoseconds,
#endif
			Ticks,
		}

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

		void RegisterTimeSpan()
		{
			Registration.RegisterMember((TimeSpan ts) => ts.Days,              (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, TimeSpanPart.Days));
			Registration.RegisterMember((TimeSpan ts) => ts.TotalDays,         (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, TimeSpanPart.TotalDays));
			Registration.RegisterMember((TimeSpan ts) => ts.Hours,             (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, TimeSpanPart.Hours));
			Registration.RegisterMember((TimeSpan ts) => ts.TotalHours,        (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, TimeSpanPart.TotalHours));
			Registration.RegisterMember((TimeSpan ts) => ts.Minutes,           (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, TimeSpanPart.Minutes));
			Registration.RegisterMember((TimeSpan ts) => ts.TotalMinutes,      (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, TimeSpanPart.TotalMinutes));
			Registration.RegisterMember((TimeSpan ts) => ts.Seconds,           (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, TimeSpanPart.Seconds));
			Registration.RegisterMember((TimeSpan ts) => ts.TotalSeconds,      (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, TimeSpanPart.TotalSeconds));
			Registration.RegisterMember((TimeSpan ts) => ts.Milliseconds,      (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, TimeSpanPart.Milliseconds));
			Registration.RegisterMember((TimeSpan ts) => ts.TotalMilliseconds, (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, TimeSpanPart.TotalMilliseconds));
#if NET7_0_OR_GREATER
			Registration.RegisterMember((TimeSpan ts) => ts.Microseconds,       (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, TimeSpanPart.Microseconds));
			Registration.RegisterMember((TimeSpan ts) => ts.TotalMicroseconds,  (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, TimeSpanPart.TotalMicroseconds));
			Registration.RegisterMember((TimeSpan ts) => ts.Nanoseconds,        (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, TimeSpanPart.Nanoseconds));
			Registration.RegisterMember((TimeSpan ts) => ts.TotalNanoseconds,   (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, TimeSpanPart.TotalNanoseconds));
#endif
			Registration.RegisterMember((TimeSpan ts) => ts.Ticks,             (tc, me, tf) => TranslateTimeSpanMember(tc, me, tf, TimeSpanPart.Ticks));

			Registration.RegisterUnary((TimeSpan ts) => -ts, TranslateTimeSpanNegate);
			Registration.RegisterUnary((TimeSpan? ts) => -ts, TranslateTimeSpanNegate);

			Registration.RegisterBinary((DateTime dt, TimeSpan ts) => dt + ts, TranslateDateTimeIntervalAdd);
			Registration.RegisterBinary((DateTime dt, TimeSpan? ts) => dt + ts, TranslateDateTimeIntervalAdd);
			Registration.RegisterBinary((DateTime? dt, TimeSpan ts) => dt + ts, TranslateDateTimeIntervalAdd);
			Registration.RegisterBinary((DateTime? dt, TimeSpan? ts) => dt + ts, TranslateDateTimeIntervalAdd);
			Registration.RegisterBinary((DateTime dt, TimeSpan ts) => dt - ts, TranslateDateTimeIntervalAdd);
			Registration.RegisterBinary((DateTime dt, TimeSpan? ts) => dt - ts, TranslateDateTimeIntervalAdd);
			Registration.RegisterBinary((DateTime? dt, TimeSpan ts) => dt - ts, TranslateDateTimeIntervalAdd);
			Registration.RegisterBinary((DateTime? dt, TimeSpan? ts) => dt - ts, TranslateDateTimeIntervalAdd);
			Registration.RegisterBinary((DateTimeOffset dt, TimeSpan ts) => dt + ts, TranslateDateTimeOffsetIntervalAdd);
			Registration.RegisterBinary((DateTimeOffset dt, TimeSpan? ts) => dt + ts, TranslateDateTimeOffsetIntervalAdd);
			Registration.RegisterBinary((DateTimeOffset? dt, TimeSpan ts) => dt + ts, TranslateDateTimeOffsetIntervalAdd);
			Registration.RegisterBinary((DateTimeOffset? dt, TimeSpan? ts) => dt + ts, TranslateDateTimeOffsetIntervalAdd);
			Registration.RegisterBinary((DateTimeOffset dt, TimeSpan ts) => dt - ts, TranslateDateTimeOffsetIntervalAdd);
			Registration.RegisterBinary((DateTimeOffset dt, TimeSpan? ts) => dt - ts, TranslateDateTimeOffsetIntervalAdd);
			Registration.RegisterBinary((DateTimeOffset? dt, TimeSpan ts) => dt - ts, TranslateDateTimeOffsetIntervalAdd);
			Registration.RegisterBinary((DateTimeOffset? dt, TimeSpan? ts) => dt - ts, TranslateDateTimeOffsetIntervalAdd);
			Registration.RegisterBinary((DateTime left, DateTime right) => left - right, TranslateDateTimeIntervalDifference);
			Registration.RegisterBinary((DateTime left, DateTime? right) => left - right, TranslateDateTimeIntervalDifference);
			Registration.RegisterBinary((DateTime? left, DateTime right) => left - right, TranslateDateTimeIntervalDifference);
			Registration.RegisterBinary((DateTime? left, DateTime? right) => left - right, TranslateDateTimeIntervalDifference);
			Registration.RegisterBinary((DateTimeOffset left, DateTimeOffset right) => left - right, TranslateDateTimeOffsetIntervalDifference);
			Registration.RegisterBinary((DateTimeOffset left, DateTimeOffset? right) => left - right, TranslateDateTimeOffsetIntervalDifference);
			Registration.RegisterBinary((DateTimeOffset? left, DateTimeOffset right) => left - right, TranslateDateTimeOffsetIntervalDifference);
			Registration.RegisterBinary((DateTimeOffset? left, DateTimeOffset? right) => left - right, TranslateDateTimeOffsetIntervalDifference);
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
			Registration.RegisterMethod((DateTime dt) => Sql.DatePartLong(Sql.DateParts.Year, dt), TranslateDateTimeSqlDatepart);

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
			Registration.RegisterMethod((DateTimeOffset dt) => Sql.DatePartLong(Sql.DateParts.Year, dt), TranslateDateTimeOffsetSqlDatepart);
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

			if (methodCall.Type.UnwrapNullableType() == typeof(long))
				converted = translationContext.ExpressionFactory.Cast(converted, translationContext.ExpressionFactory.GetDbDataType(methodCall.Type));

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

			if (methodCall.Type.UnwrapNullableType() == typeof(long))
				converted = translationContext.ExpressionFactory.Cast(converted, translationContext.ExpressionFactory.GetDbDataType(methodCall.Type));

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

		Expression? TranslateTimeSpanMember(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags, TimeSpanPart part)
		{
			var placeholder = TranslateNoRequiredExpression(translationContext, memberExpression.Expression, translationFlags);
			if (placeholder == null)
				return null;

			var converted = TranslateTimeSpanPart(translationContext, translationFlags, placeholder.Sql, part, memberExpression.Type);
			if (converted == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, memberExpression);
		}

		Expression? TranslateTimeSpanNegate(ITranslationContext translationContext, UnaryExpression unaryExpression, TranslationFlags translationFlags)
		{
			var placeholder = TranslateNoRequiredExpression(translationContext, unaryExpression.Operand, translationFlags);
			if (placeholder == null)
				return null;

			var converted = TranslateTimeSpanNegate(translationContext, translationFlags, placeholder.Sql);
			if (converted == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, unaryExpression);
		}

		Expression? TranslateDateTimeIntervalAdd(ITranslationContext translationContext, BinaryExpression binaryExpression, TranslationFlags translationFlags)
		{
			return TranslateDateTimeIntervalAdd(translationContext, binaryExpression, translationFlags, false);
		}

		Expression? TranslateDateTimeOffsetIntervalAdd(ITranslationContext translationContext, BinaryExpression binaryExpression, TranslationFlags translationFlags)
		{
			return TranslateDateTimeIntervalAdd(translationContext, binaryExpression, translationFlags, true);
		}

		Expression? TranslateDateTimeIntervalAdd(ITranslationContext translationContext, BinaryExpression binaryExpression, TranslationFlags translationFlags, bool isDateTimeOffset)
		{
			var datePlaceholder = TranslateNoRequiredExpression(translationContext, binaryExpression.Left, translationFlags, false);
			if (datePlaceholder == null)
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			var intervalPlaceholder = TranslateNoRequiredExpression(translationContext, binaryExpression.Right, translationFlags, false);
			if (intervalPlaceholder == null)
				return null;

			if (datePlaceholder.Sql is SqlParameter && intervalPlaceholder.Sql is SqlParameter)
				return null;

			var converted = TranslateDateTimeIntervalAdd(
				translationContext,
				translationFlags,
				datePlaceholder.Sql,
				intervalPlaceholder.Sql,
				binaryExpression.NodeType == ExpressionType.Subtract,
				isDateTimeOffset);

			if (converted == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, binaryExpression);
		}

		Expression? TranslateDateTimeIntervalDifference(ITranslationContext translationContext, BinaryExpression binaryExpression, TranslationFlags translationFlags)
		{
			return TranslateDateTimeIntervalDifference(translationContext, binaryExpression, translationFlags, false);
		}

		Expression? TranslateDateTimeOffsetIntervalDifference(ITranslationContext translationContext, BinaryExpression binaryExpression, TranslationFlags translationFlags)
		{
			return TranslateDateTimeIntervalDifference(translationContext, binaryExpression, translationFlags, true);
		}

		Expression? TranslateDateTimeIntervalDifference(ITranslationContext translationContext, BinaryExpression binaryExpression, TranslationFlags translationFlags, bool isDateTimeOffset)
		{
			var leftPlaceholder = TranslateNoRequiredExpression(translationContext, binaryExpression.Left, translationFlags, false);
			if (leftPlaceholder == null)
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			var rightPlaceholder = TranslateNoRequiredExpression(translationContext, binaryExpression.Right, translationFlags, false);
			if (rightPlaceholder == null)
				return null;

			if (leftPlaceholder.Sql is SqlParameter && rightPlaceholder.Sql is SqlParameter)
				return null;

			var converted = TranslateDateTimeIntervalDifference(
				translationContext,
				translationFlags,
				leftPlaceholder.Sql,
				rightPlaceholder.Sql,
				isDateTimeOffset);

			if (converted == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, binaryExpression);
		}

		#region Methods to override

		private protected virtual ISqlExpression? TranslateTimeSpanPart(ITranslationContext translationContext, TranslationFlags translationFlags, ISqlExpression timeSpanExpression, TimeSpanPart part, Type resultType)
		{
			var factory      = translationContext.ExpressionFactory;
			var expressionType = factory.GetDbDataType(timeSpanExpression);

			if (expressionType.DataType != DataType.Int64)
				return TranslateNativeTimeSpanPart(translationContext, translationFlags, timeSpanExpression, part, resultType);

			var longType   = factory.GetDbDataType(typeof(long));
			var resultDbType = factory.GetDbDataType(resultType);
			var ticks      = factory.Cast(timeSpanExpression, longType, true);

			ISqlExpression Divide(long value)
			{
				var remainder = factory.Mod(ticks, factory.Value(longType, value));
				return factory.Div(longType, factory.Sub(longType, ticks, remainder), factory.Value(longType, value));
			}

			ISqlExpression Component(long divisor, int modulo)
			{
				var value = Divide(divisor);
				value = factory.Mod(value, factory.Value(longType, (long)modulo));
				return factory.Cast(value, resultDbType);
			}

			ISqlExpression Total(double divisor)
			{
				var doubleType = factory.GetDbDataType(typeof(double));
				var value      = factory.Cast(ticks, doubleType);
				return factory.Div(doubleType, value, factory.Value(doubleType, divisor));
			}

			return part switch
			{
				TimeSpanPart.Days              => factory.Cast(Divide(TimeSpan.TicksPerDay), resultDbType),
				TimeSpanPart.TotalDays         => Total(TimeSpan.TicksPerDay),
				TimeSpanPart.Hours             => Component(TimeSpan.TicksPerHour, 24),
				TimeSpanPart.TotalHours        => Total(TimeSpan.TicksPerHour),
				TimeSpanPart.Minutes           => Component(TimeSpan.TicksPerMinute, 60),
				TimeSpanPart.TotalMinutes      => Total(TimeSpan.TicksPerMinute),
				TimeSpanPart.Seconds           => Component(TimeSpan.TicksPerSecond, 60),
				TimeSpanPart.TotalSeconds      => Total(TimeSpan.TicksPerSecond),
				TimeSpanPart.Milliseconds      => Component(TimeSpan.TicksPerMillisecond, 1000),
				TimeSpanPart.TotalMilliseconds => Total(TimeSpan.TicksPerMillisecond),
#if NET7_0_OR_GREATER
				TimeSpanPart.Microseconds      => Component(TimeSpan.TicksPerMicrosecond, 1000),
				TimeSpanPart.TotalMicroseconds => Total(TimeSpan.TicksPerMicrosecond),
				TimeSpanPart.Nanoseconds       => factory.Cast(factory.Mod(factory.Multiply(longType, ticks, 100L), 1000L), resultDbType),
				TimeSpanPart.TotalNanoseconds  => factory.Multiply(resultDbType, factory.Cast(ticks, resultDbType), 100D),
#endif
				TimeSpanPart.Ticks             => ticks,
				_                              => null,
			};
		}

		private protected virtual ISqlExpression? TranslateNativeTimeSpanPart(ITranslationContext translationContext, TranslationFlags translationFlags, ISqlExpression timeSpanExpression, TimeSpanPart part, Type resultType)
		{
			return null;
		}

		private protected virtual ISqlExpression? TranslateTimeSpanNegate(ITranslationContext translationContext, TranslationFlags translationFlags, ISqlExpression timeSpanExpression)
		{
			var factory        = translationContext.ExpressionFactory;
			var expressionType = factory.GetDbDataType(timeSpanExpression);

			if (expressionType.DataType != DataType.Int64)
				return TranslateNativeTimeSpanNegate(translationContext, translationFlags, timeSpanExpression);

			return factory.Negate(expressionType, timeSpanExpression);
		}

		private protected virtual ISqlExpression? TranslateNativeTimeSpanNegate(ITranslationContext translationContext, TranslationFlags translationFlags, ISqlExpression timeSpanExpression)
		{
			return null;
		}

		private protected virtual ISqlExpression? TranslateDateTimeIntervalAdd(ITranslationContext translationContext, TranslationFlags translationFlags, ISqlExpression dateTimeExpression, ISqlExpression intervalExpression, bool isSubtract, bool isDateTimeOffset)
		{
			var factory      = translationContext.ExpressionFactory;
			var intervalType = factory.GetDbDataType(intervalExpression);

			if (intervalType.DataType != DataType.Int64)
				return TranslateNativeDateTimeIntervalAdd(translationContext, translationFlags, dateTimeExpression, intervalExpression, isSubtract, isDateTimeOffset);

			var longType = factory.GetDbDataType(typeof(long));
			var ticks    = factory.Cast(intervalExpression, longType, true);
			var remainder = factory.Mod(ticks, TimeSpan.TicksPerDay);
			var days      = factory.Div(longType, factory.Sub(longType, ticks, remainder), TimeSpan.TicksPerDay);

			if (isSubtract)
			{
				days      = factory.Negate(longType, days);
				remainder = factory.Negate(longType, remainder);
			}

			var result = isDateTimeOffset
				? TranslateDateTimeOffsetDateAdd(translationContext, translationFlags, dateTimeExpression, days, Sql.DateParts.Day)
				: TranslateDateTimeDateAdd(translationContext, translationFlags, dateTimeExpression, days, Sql.DateParts.Day);

			if (result == null)
				return null;

			var increment = remainder;
			var part      = Sql.DateParts.Tick;
			var withRemainder = isDateTimeOffset
				? TranslateDateTimeOffsetDateAdd(translationContext, translationFlags, result, increment, part)
				: TranslateDateTimeDateAdd(translationContext, translationFlags, result, increment, part);

			if (withRemainder != null)
				return withRemainder;

			var subMicrosecondTicks = factory.Mod(remainder, 10L);
			increment = factory.Div(longType, factory.Sub(longType, remainder, subMicrosecondTicks), 10L);
			part      = Sql.DateParts.Microsecond;
			withRemainder = isDateTimeOffset
				? TranslateDateTimeOffsetDateAdd(translationContext, translationFlags, result, increment, part)
				: TranslateDateTimeDateAdd(translationContext, translationFlags, result, increment, part);

			if (withRemainder != null)
				return withRemainder;

			var subMillisecondTicks = factory.Mod(remainder, TimeSpan.TicksPerMillisecond);
			increment = factory.Div(longType, factory.Sub(longType, remainder, subMillisecondTicks), TimeSpan.TicksPerMillisecond);
			part      = Sql.DateParts.Millisecond;
			withRemainder = isDateTimeOffset
				? TranslateDateTimeOffsetDateAdd(translationContext, translationFlags, result, increment, part)
				: TranslateDateTimeDateAdd(translationContext, translationFlags, result, increment, part);

			if (withRemainder != null)
				return withRemainder;

			increment = factory.Div(longType, remainder, TimeSpan.TicksPerSecond);
			part      = Sql.DateParts.Second;
			return isDateTimeOffset
				? TranslateDateTimeOffsetDateAdd(translationContext, translationFlags, result, increment, part)
				: TranslateDateTimeDateAdd(translationContext, translationFlags, result, increment, part);
		}

		private protected virtual ISqlExpression? TranslateNativeDateTimeIntervalAdd(ITranslationContext translationContext, TranslationFlags translationFlags, ISqlExpression dateTimeExpression, ISqlExpression intervalExpression, bool isSubtract, bool isDateTimeOffset)
		{
			return null;
		}

		private protected virtual ISqlExpression? TranslateDateTimeIntervalDifference(ITranslationContext translationContext, TranslationFlags translationFlags, ISqlExpression leftExpression, ISqlExpression rightExpression, bool isDateTimeOffset)
		{
			return null;
		}

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
