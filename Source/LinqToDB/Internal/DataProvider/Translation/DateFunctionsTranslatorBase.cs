using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public abstract class DateFunctionsTranslatorBase : MemberTranslatorBase
	{
		static readonly IValueConverter _timeSpanTicksConverter = new ValueConverter<TimeSpan, long>(
			value => value.Ticks,
			value => TimeSpan.FromTicks(value),
			handlesNulls: false);

		[Flags]
		private protected enum DateTimeIntervalUnits
		{
			None        = 0,
			Day         = 1,
			Hour        = 2,
			Minute      = 4,
			Second      = 8,
			Millisecond = 16,
			Microsecond = 32,
			Tick        = 64,
		}

		/// <summary>
		/// Describes the arithmetic guarantees for the actual mapped date expression. Provider defaults are
		/// refined by the expression's <see cref="DbDataType"/> whenever it identifies a concrete storage type.
		/// </summary>
		private protected readonly struct DateTimeIntervalCapabilities
		{
			public DateTimeIntervalCapabilities(long storageResolutionTicks, DateTimeIntervalUnits supportedUnits, long maxIntervalTicks, bool preservesInstant)
			{
				StorageResolutionTicks = storageResolutionTicks;
				SupportedUnits          = supportedUnits;
				MaxIntervalTicks        = maxIntervalTicks;
				PreservesInstant        = preservesInstant;
			}

			public long                  StorageResolutionTicks { get; }
			public DateTimeIntervalUnits SupportedUnits          { get; }
			public long                  MaxIntervalTicks        { get; }
			public bool                  PreservesInstant        { get; }
		}

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
			Registration.RegisterMember((TimeSpan? ts) => ts!.Value, TranslateNullableTimeSpanValue);
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

			Registration.RegisterBinary((TimeSpan left, TimeSpan right) => left + right, TranslateTimeSpanArithmetic);
			Registration.RegisterBinary((TimeSpan? left, TimeSpan right) => left + right, TranslateTimeSpanArithmetic);
			Registration.RegisterBinary((TimeSpan left, TimeSpan? right) => left + right, TranslateTimeSpanArithmetic);
			Registration.RegisterBinary((TimeSpan? left, TimeSpan? right) => left + right, TranslateTimeSpanArithmetic);
			Registration.RegisterBinary((TimeSpan left, TimeSpan right) => left - right, TranslateTimeSpanArithmetic);
			Registration.RegisterBinary((TimeSpan? left, TimeSpan right) => left - right, TranslateTimeSpanArithmetic);
			Registration.RegisterBinary((TimeSpan left, TimeSpan? right) => left - right, TranslateTimeSpanArithmetic);
			Registration.RegisterBinary((TimeSpan? left, TimeSpan? right) => left - right, TranslateTimeSpanArithmetic);

			Registration.RegisterBinary((TimeSpan left, TimeSpan right) => left == right, TranslateTimeSpanComparison);
			Registration.RegisterBinary((TimeSpan? left, TimeSpan right) => left == right, TranslateTimeSpanComparison);
			Registration.RegisterBinary((TimeSpan left, TimeSpan? right) => left == right, TranslateTimeSpanComparison);
			Registration.RegisterBinary((TimeSpan? left, TimeSpan? right) => left == right, TranslateTimeSpanComparison);
			Registration.RegisterBinary((TimeSpan left, TimeSpan right) => left != right, TranslateTimeSpanComparison);
			Registration.RegisterBinary((TimeSpan? left, TimeSpan right) => left != right, TranslateTimeSpanComparison);
			Registration.RegisterBinary((TimeSpan left, TimeSpan? right) => left != right, TranslateTimeSpanComparison);
			Registration.RegisterBinary((TimeSpan? left, TimeSpan? right) => left != right, TranslateTimeSpanComparison);

			Registration.RegisterBinary((TimeSpan left, TimeSpan right) => left < right, TranslateTimeSpanComparison);
			Registration.RegisterBinary((TimeSpan? left, TimeSpan right) => left < right, TranslateTimeSpanComparison);
			Registration.RegisterBinary((TimeSpan left, TimeSpan? right) => left < right, TranslateTimeSpanComparison);
			Registration.RegisterBinary((TimeSpan? left, TimeSpan? right) => left < right, TranslateTimeSpanComparison);
			Registration.RegisterBinary((TimeSpan left, TimeSpan right) => left <= right, TranslateTimeSpanComparison);
			Registration.RegisterBinary((TimeSpan? left, TimeSpan right) => left <= right, TranslateTimeSpanComparison);
			Registration.RegisterBinary((TimeSpan left, TimeSpan? right) => left <= right, TranslateTimeSpanComparison);
			Registration.RegisterBinary((TimeSpan? left, TimeSpan? right) => left <= right, TranslateTimeSpanComparison);
			Registration.RegisterBinary((TimeSpan left, TimeSpan right) => left > right, TranslateTimeSpanComparison);
			Registration.RegisterBinary((TimeSpan? left, TimeSpan right) => left > right, TranslateTimeSpanComparison);
			Registration.RegisterBinary((TimeSpan left, TimeSpan? right) => left > right, TranslateTimeSpanComparison);
			Registration.RegisterBinary((TimeSpan? left, TimeSpan? right) => left > right, TranslateTimeSpanComparison);
			Registration.RegisterBinary((TimeSpan left, TimeSpan right) => left >= right, TranslateTimeSpanComparison);
			Registration.RegisterBinary((TimeSpan? left, TimeSpan right) => left >= right, TranslateTimeSpanComparison);
			Registration.RegisterBinary((TimeSpan left, TimeSpan? right) => left >= right, TranslateTimeSpanComparison);
			Registration.RegisterBinary((TimeSpan? left, TimeSpan? right) => left >= right, TranslateTimeSpanComparison);

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
			Registration.RegisterMethod((DateTime dt) => Sql.DatePartLong(Sql.DateParts.Year, dt), TranslateDateTimeSqlDatepartLong);

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
			Registration.RegisterMember((DateTimeOffset dt) => dt.UtcDateTime, TranslateDateTimeOffsetToUtc);

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
			Registration.RegisterMethod((DateTimeOffset dt) => Sql.DatePartLong(Sql.DateParts.Year, dt), TranslateDateTimeOffsetSqlDatepartLong);
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
			return TranslateDateTimeSqlDatepartCore(translationContext, methodCall, translationFlags, false);
		}

		Expression? TranslateDateTimeSqlDatepartLong(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			return TranslateDateTimeSqlDatepartCore(translationContext, methodCall, translationFlags, true);
		}

		Expression? TranslateDateTimeSqlDatepartCore(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, bool longResult)
		{
			if (!translationContext.TryEvaluate<Sql.DateParts>(methodCall.Arguments[0], out var datePart))
				return null;

			var dateExpr = translationContext.Translate(methodCall.Arguments[1]);

			if (dateExpr is not SqlPlaceholderExpression datePlaceholder)
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			var converted = longResult
				? TranslateDateTimeDatePartLong(translationContext, translationFlags, datePlaceholder.Sql, datePart)
				: TranslateDateTimeDatePart(translationContext, translationFlags, datePlaceholder.Sql, datePart);
			converted ??= TranslateSubsecondDatePartFallback(translationContext, translationFlags, datePlaceholder.Sql, datePart, false);
			if (converted == null)
				return null;

			if (longResult)
				converted = translationContext.ExpressionFactory.Cast(converted, translationContext.ExpressionFactory.GetDbDataType(methodCall.Type));

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, methodCall);
		}

		Expression? TranslateDateTimeOffsetSqlDatepart(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			return TranslateDateTimeOffsetSqlDatepartCore(translationContext, methodCall, translationFlags, false);
		}

		Expression? TranslateDateTimeOffsetSqlDatepartLong(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			return TranslateDateTimeOffsetSqlDatepartCore(translationContext, methodCall, translationFlags, true);
		}

		Expression? TranslateDateTimeOffsetSqlDatepartCore(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, bool longResult)
		{
			if (!translationContext.TryEvaluate<Sql.DateParts>(methodCall.Arguments[0], out var datePart))
				return null;

			var dateExpr = translationContext.Translate(methodCall.Arguments[1]);

			if (dateExpr is not SqlPlaceholderExpression datePlaceholder)
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			var converted = longResult
				? TranslateDateTimeOffsetDatePartLong(translationContext, translationFlags, datePlaceholder.Sql, datePart)
				: TranslateDateTimeOffsetDatePart(translationContext, translationFlags, datePlaceholder.Sql, datePart);
			converted ??= TranslateSubsecondDatePartFallback(translationContext, translationFlags, datePlaceholder.Sql, datePart, true);
			if (converted == null)
				return null;

			if (longResult)
				converted = translationContext.ExpressionFactory.Cast(converted, translationContext.ExpressionFactory.GetDbDataType(methodCall.Type));

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, methodCall);
		}

		ISqlExpression? TranslateSubsecondDatePartFallback(ITranslationContext translationContext, TranslationFlags translationFlags, ISqlExpression dateExpression, Sql.DateParts datePart, bool isDateTimeOffset)
		{
			var microsecondMultiplier = datePart switch
			{
				Sql.DateParts.Nanosecond => 1_000L,
				Sql.DateParts.Tick       => 10L,
				_                         => 0L,
			};

			if (microsecondMultiplier != 0)
			{
				var microseconds = isDateTimeOffset
					? TranslateDateTimeOffsetDatePart(translationContext, translationFlags, dateExpression, Sql.DateParts.Microsecond)
					: TranslateDateTimeDatePart(translationContext, translationFlags, dateExpression, Sql.DateParts.Microsecond);

				if (microseconds != null)
				{
					var microsecondFactory  = translationContext.ExpressionFactory;
					var microsecondLongType = microsecondFactory.GetDbDataType(typeof(long));
					return microsecondFactory.Multiply(microsecondLongType, microsecondFactory.Cast(microseconds, microsecondLongType, true), microsecondMultiplier);
				}
			}

			var millisecondMultiplier = datePart switch
			{
				Sql.DateParts.Microsecond => 1_000L,
				Sql.DateParts.Nanosecond  => 1_000_000L,
				Sql.DateParts.Tick        => 10_000L,
				_                         => 0L,
			};

			if (millisecondMultiplier == 0)
				return null;

			var milliseconds = isDateTimeOffset
				? TranslateDateTimeOffsetDatePart(translationContext, translationFlags, dateExpression, Sql.DateParts.Millisecond)
				: TranslateDateTimeDatePart(translationContext, translationFlags, dateExpression, Sql.DateParts.Millisecond);

			if (milliseconds == null)
				return null;

			var factory  = translationContext.ExpressionFactory;
			var longType = factory.GetDbDataType(typeof(long));
			return factory.Multiply(longType, factory.Cast(milliseconds, longType, true), millisecondMultiplier);
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

			var increment = NormalizeSqlDateAddIncrement(translationContext, incrementPlaceholder.Sql, ref datepart);
			var converted = TranslateDateTimeDateAdd(translationContext, translationFlags, datePlaceholder.Sql, increment, datepart);
			converted ??= TranslateSubsecondDateAddFallback(translationContext, translationFlags, datePlaceholder.Sql, increment, datepart, false);
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

			var increment = NormalizeSqlDateAddIncrement(translationContext, incrementPlaceholder.Sql, ref datepart);
			var converted = TranslateDateTimeOffsetDateAdd(translationContext, translationFlags, datePlaceholder.Sql, increment, datepart);
			converted ??= TranslateSubsecondDateAddFallback(translationContext, translationFlags, datePlaceholder.Sql, increment, datepart, true);
			if (converted == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, methodCall);
		}

		ISqlExpression NormalizeSqlDateAddIncrement(ITranslationContext translationContext, ISqlExpression increment, ref Sql.DateParts datepart)
		{
			if (datepart is not (Sql.DateParts.Microsecond or Sql.DateParts.Nanosecond or Sql.DateParts.Tick))
				return increment;

			var factory     = translationContext.ExpressionFactory;
			var decimalType = factory.GetDbDataType(typeof(decimal)).WithPrecisionScale(29, 10);
			var value       = factory.Cast(increment, decimalType, true);

			// The public DateAdd contract converts its floating increment to whole CLR units.
			// Do that explicitly because providers disagree on fractional DATEADD handling;
			// SQL Server, for example, rounds nanoseconds 50..99 to one 100ns quantum.
			// Nanoseconds are first converted to ticks using C# truncation toward zero.
			if (datepart == Sql.DateParts.Nanosecond)
			{
				value    = factory.Div(decimalType, value, factory.Value(decimalType, 100m));
				datepart = Sql.DateParts.Tick;
			}

			return TruncateToInt64(translationContext, value);
		}

		ISqlExpression? TranslateSubsecondDateAddFallback(
			ITranslationContext translationContext,
			TranslationFlags     translationFlags,
			ISqlExpression       dateExpression,
			ISqlExpression       increment,
			Sql.DateParts        datepart,
			bool                 isDateTimeOffset)
		{
			var sourceTicks = datepart switch
			{
				Sql.DateParts.Tick        => 1L,
				Sql.DateParts.Microsecond => 10L,
				_                         => 0L,
			};

			if (sourceTicks == 0)
				return null;

			var capabilities = GetDateTimeIntervalCapabilities(translationContext, dateExpression, isDateTimeOffset);
			(Sql.DateParts Part, long Ticks)[] candidates =
			[
				(Sql.DateParts.Microsecond, 10L),
				(Sql.DateParts.Millisecond, TimeSpan.TicksPerMillisecond),
				(Sql.DateParts.Second,      TimeSpan.TicksPerSecond),
				(Sql.DateParts.Minute,      TimeSpan.TicksPerMinute),
			];

			foreach (var (part, ticks) in candidates)
			{
				if (ticks < sourceTicks
					|| ticks > capabilities.StorageResolutionTicks
					|| !SupportsUnit(capabilities.SupportedUnits, part))
					continue;

				var factory     = translationContext.ExpressionFactory;
				var longType    = factory.GetDbDataType(typeof(long));
				var divisor     = ticks / sourceTicks;
				var value       = factory.Cast(increment, longType, true);
				var convertedIncrement = TruncateDivision(translationContext, value, divisor);

				var converted = isDateTimeOffset
					? TranslateDateTimeOffsetDateAdd(translationContext, translationFlags, dateExpression, convertedIncrement, part)
					: TranslateDateTimeDateAdd(translationContext, translationFlags, dateExpression, convertedIncrement, part);

				if (converted != null)
					return converted;
			}

			return null;
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

		Expression? TranslateDateTimeOffsetToUtc(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var placeholder = TranslateNoRequiredExpression(translationContext, memberExpression.Expression, translationFlags);
			if (placeholder == null)
				return null;

			var converted = TranslateDateTimeOffsetToUtc(translationContext, placeholder.Sql, translationFlags);
			if (converted == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, memberExpression);
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

		Expression? TranslateNullableTimeSpanValue(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var placeholder = TranslateNoRequiredExpression(translationContext, memberExpression.Expression, translationFlags, false);
			return placeholder?.MakeNotNullable().WithPath(memberExpression);
		}

		Expression? TranslateTimeSpanNegate(ITranslationContext translationContext, UnaryExpression unaryExpression, TranslationFlags translationFlags)
		{
			var placeholder = TranslateNoRequiredExpression(translationContext, unaryExpression.Operand, translationFlags);
			if (placeholder == null)
				return null;

			var converted = TranslateTimeSpanNegate(translationContext, translationFlags, placeholder.Sql);
			if (converted == null)
				return null;

			converted = new SqlDurationExpression(
				translationContext.ExpressionFactory.GetDbDataType(converted),
				converted,
				DurationUnit.Tick,
				SqlDurationKind.Arithmetic);

			return CreateTimeSpanNegateResult(translationContext, converted, unaryExpression);
		}

		Expression? TranslateTimeSpanArithmetic(ITranslationContext translationContext, BinaryExpression binaryExpression, TranslationFlags translationFlags)
		{
			var leftPlaceholder = TranslateNoRequiredExpression(translationContext, binaryExpression.Left, translationFlags, false);
			if (leftPlaceholder == null)
				return null;

			var rightPlaceholder = TranslateNoRequiredExpression(translationContext, binaryExpression.Right, translationFlags, false);
			if (rightPlaceholder == null)
				return null;

			if (!TryNormalizeDurationToTicks(translationContext, leftPlaceholder.Sql, out var leftTicks)
				|| !TryNormalizeDurationToTicks(translationContext, rightPlaceholder.Sql, out var rightTicks))
				return SqlErrorExpression.EnsureError(binaryExpression);

			var factory  = translationContext.ExpressionFactory;
			var longType = factory.GetDbDataType(typeof(long));
			var result = binaryExpression.NodeType switch
			{
				ExpressionType.Add      => factory.Add(longType, leftTicks, rightTicks),
				ExpressionType.Subtract => factory.Sub(longType, leftTicks, rightTicks),
				_                       => throw new InvalidOperationException($"Unexpected TimeSpan arithmetic: {binaryExpression.NodeType}"),
			};
			var durationResult = new SqlDurationExpression(longType, result, DurationUnit.Tick, SqlDurationKind.Arithmetic);
			var converted = new SqlExpression(longType, "{0}", durationResult)
				.WithResultConverter(_timeSpanTicksConverter);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, binaryExpression);
		}

		Expression? TranslateTimeSpanComparison(ITranslationContext translationContext, BinaryExpression binaryExpression, TranslationFlags translationFlags)
		{
			var leftPlaceholder = TranslateNoRequiredExpression(translationContext, binaryExpression.Left, translationFlags, false);
			if (leftPlaceholder == null)
				return null;

			var rightPlaceholder = TranslateNoRequiredExpression(translationContext, binaryExpression.Right, translationFlags, false);
			if (rightPlaceholder == null)
				return null;

			if (binaryExpression.NodeType is ExpressionType.Equal or ExpressionType.NotEqual)
			{
				var nonNullExpression = leftPlaceholder.Sql is SqlValue { Value: null }
					? rightPlaceholder.Sql
					: rightPlaceholder.Sql is SqlValue { Value: null }
						? leftPlaceholder.Sql
						: null;
				if (nonNullExpression != null)
				{
					var nullPredicate = translationContext.ExpressionFactory.IsNull(
						nonNullExpression,
						binaryExpression.NodeType == ExpressionType.NotEqual);
					var nullCondition = translationContext.ExpressionFactory.SearchCondition().Add(nullPredicate);
					return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, nullCondition, binaryExpression);
				}
			}

			if (!TryNormalizeDurationToTicks(translationContext, leftPlaceholder.Sql, out var leftTicks)
				|| !TryNormalizeDurationToTicks(translationContext, rightPlaceholder.Sql, out var rightTicks))
				return SqlErrorExpression.EnsureError(binaryExpression);

			var factory = translationContext.ExpressionFactory;
			var predicate = binaryExpression.NodeType switch
			{
				ExpressionType.Equal                => factory.Equal(leftTicks, rightTicks),
				ExpressionType.NotEqual             => factory.NotEqual(leftTicks, rightTicks),
				ExpressionType.LessThan             => factory.Less(leftTicks, rightTicks),
				ExpressionType.LessThanOrEqual      => factory.LessOrEqual(leftTicks, rightTicks),
				ExpressionType.GreaterThan          => factory.Greater(leftTicks, rightTicks),
				ExpressionType.GreaterThanOrEqual   => factory.GreaterOrEqual(leftTicks, rightTicks),
				_                                  => throw new InvalidOperationException($"Unexpected TimeSpan comparison: {binaryExpression.NodeType}"),
			};
			var condition = factory.SearchCondition().Add(predicate);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, condition, binaryExpression);
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

			var dateCapabilities = GetDateTimeIntervalCapabilities(translationContext, datePlaceholder.Sql, isDateTimeOffset);
			if (isDateTimeOffset && (!SupportsDateTimeOffsetIntervalAdd || !dateCapabilities.PreservesInstant))
				return SqlErrorExpression.EnsureError(binaryExpression);

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			var intervalPlaceholder = TranslateNoRequiredExpression(translationContext, binaryExpression.Right, translationFlags, false);
			if (intervalPlaceholder == null)
				return null;

			if (!TryNormalizeDurationToTicks(translationContext, intervalPlaceholder.Sql, out var intervalTicks))
				return SqlErrorExpression.EnsureError(binaryExpression);

			if (!IsIntervalWithinRange(intervalTicks, dateCapabilities.MaxIntervalTicks))
				return SqlErrorExpression.EnsureError(binaryExpression);

			if (datePlaceholder.Sql is SqlParameter
				&& intervalTicks is SqlParameter or SqlDurationExpression { Expression: SqlParameter })
				return null;

			var converted = TranslateDateTimeIntervalAdd(
				translationContext,
				translationFlags,
				datePlaceholder.Sql,
				intervalTicks,
				binaryExpression.NodeType == ExpressionType.Subtract,
				isDateTimeOffset);

			if (converted == null)
				return SqlErrorExpression.EnsureError(binaryExpression);

			return CreateDateTimeIntervalAddResult(translationContext, converted, binaryExpression);
		}

		private static bool IsIntervalWithinRange(ISqlExpression intervalExpression, long maxIntervalTicks)
		{
			if (maxIntervalTicks == long.MaxValue)
				return true;

			while (intervalExpression is SqlDurationExpression duration)
				intervalExpression = duration.Expression;

			var value = intervalExpression switch
			{
				SqlValue     sqlValue     => sqlValue.Value,
				SqlParameter sqlParameter => sqlParameter.Value,
				_                         => null,
			};

			var ticks = value switch
			{
				TimeSpan timeSpan => timeSpan.Ticks,
				long     longValue => longValue,
				_                  => (long?)null,
			};

			return ticks == null || ticks.Value >= -maxIntervalTicks && ticks.Value <= maxIntervalTicks;
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

			var leftCapabilities  = GetDateTimeIntervalCapabilities(translationContext, leftPlaceholder.Sql,  isDateTimeOffset);
			var rightCapabilities = GetDateTimeIntervalCapabilities(translationContext, rightPlaceholder.Sql, isDateTimeOffset);
			if (isDateTimeOffset && (!SupportsDateTimeOffsetIntervalDifference || !leftCapabilities.PreservesInstant || !rightCapabilities.PreservesInstant))
				return SqlErrorExpression.EnsureError(binaryExpression);

			if (leftPlaceholder.Sql is SqlParameter && rightPlaceholder.Sql is SqlParameter)
				return null;

			var converted = TranslateDateTimeIntervalDifference(
				translationContext,
				translationFlags,
				leftPlaceholder.Sql,
				rightPlaceholder.Sql,
				isDateTimeOffset);

			if (converted == null)
				return SqlErrorExpression.EnsureError(binaryExpression);

			var convertedType = translationContext.ExpressionFactory.GetDbDataType(converted);
			converted = new SqlDurationExpression(
				convertedType,
				converted,
				IsNumericDurationType(convertedType.DataType) ? DurationUnit.Tick : DurationUnit.Native,
				SqlDurationKind.Elapsed);

			return CreateDateTimeIntervalDifferenceResult(translationContext, converted, binaryExpression);
		}

		#region Methods to override

		private protected virtual bool SupportsDateTimeOffsetIntervalArithmetic => false;
		private protected virtual bool SupportsDateTimeOffsetIntervalAdd        => SupportsDateTimeOffsetIntervalArithmetic;
		private protected virtual bool SupportsDateTimeOffsetIntervalDifference => SupportsDateTimeOffsetIntervalArithmetic;

		private protected virtual DateTimeIntervalCapabilities GetDefaultDateTimeIntervalCapabilities(bool isDateTimeOffset)
		{
			return new DateTimeIntervalCapabilities(
				TimeSpan.TicksPerMillisecond,
				DateTimeIntervalUnits.Day | DateTimeIntervalUnits.Hour | DateTimeIntervalUnits.Minute | DateTimeIntervalUnits.Second | DateTimeIntervalUnits.Millisecond,
				long.MaxValue,
				!isDateTimeOffset || SupportsDateTimeOffsetIntervalAdd || SupportsDateTimeOffsetIntervalDifference);
		}

		private protected virtual bool PreservesDateTimeOffsetInstant(DbDataType dataType)
		{
			return dataType.DataType is DataType.DateTimeTz or DataType.DateTime2Tz or DataType.DateTimeOffset;
		}

		private protected virtual DateTimeIntervalCapabilities GetDateTimeIntervalCapabilities(ITranslationContext translationContext, ISqlExpression dateTimeExpression, bool isDateTimeOffset)
		{
			var defaults = GetDefaultDateTimeIntervalCapabilities(isDateTimeOffset);
			var dataType = translationContext.ExpressionFactory.GetDbDataType(dateTimeExpression);
			var resolution = dataType.DataType switch
			{
				DataType.Date          => TimeSpan.TicksPerDay,
				DataType.SmallDateTime => TimeSpan.TicksPerMinute,
				DataType.DateTime2 or DataType.DateTime2Tz or DataType.DateTimeOffset or DataType.Timestamp or DataType.Timestamp64 or DataType.DateTime64 when dataType.Precision != null => PrecisionToTicks(dataType.Precision.Value),
				_ => defaults.StorageResolutionTicks,
			};
			var preservesInstant = defaults.PreservesInstant
				&& (!isDateTimeOffset || PreservesDateTimeOffsetInstant(dataType));

			return new DateTimeIntervalCapabilities(resolution, defaults.SupportedUnits, defaults.MaxIntervalTicks, preservesInstant);

			static long PrecisionToTicks(int precision)
			{
				return precision switch
				{
					<= 0 => TimeSpan.TicksPerSecond,
					1    => 1_000_000,
					2    => 100_000,
					3    => 10_000,
					4    => 1_000,
					5    => 100,
					6    => 10,
					_    => 1,
				};
			}
		}

		private protected virtual Expression CreateDateTimeIntervalAddResult(ITranslationContext translationContext, ISqlExpression translatedExpression, BinaryExpression binaryExpression)
		{
			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translatedExpression, binaryExpression);
		}

		private protected virtual Expression CreateDateTimeIntervalDifferenceResult(ITranslationContext translationContext, ISqlExpression translatedExpression, BinaryExpression binaryExpression)
		{
			var factory    = translationContext.ExpressionFactory;
			var resultType = factory.GetDbDataType(translatedExpression);

			if (resultType.DataType == DataType.Int64)
			{
				var longType = factory.GetDbDataType(typeof(long));
				var ticks     = factory.Cast(translatedExpression, longType, true);
				translatedExpression = new SqlExpression(longType, "{0}", ticks)
					.WithResultConverter(_timeSpanTicksConverter);
			}

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translatedExpression, binaryExpression);
		}

		private protected virtual Expression CreateTimeSpanNegateResult(ITranslationContext translationContext, ISqlExpression translatedExpression, UnaryExpression unaryExpression)
		{
			var factory    = translationContext.ExpressionFactory;
			var resultType = factory.GetDbDataType(translatedExpression);

			if (resultType.DataType == DataType.Int64)
			{
				var longType = factory.GetDbDataType(typeof(long));
				var ticks     = factory.Cast(translatedExpression, longType, true);
				translatedExpression = new SqlExpression(longType, "{0}", ticks)
					.WithResultConverter(_timeSpanTicksConverter);
			}

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translatedExpression, unaryExpression);
		}

		private protected virtual ISqlExpression TruncateToInt64(ITranslationContext translationContext, ISqlExpression value)
		{
			var factory      = translationContext.ExpressionFactory;
			var longType     = factory.GetDbDataType(typeof(long));
			var decimalType  = factory.GetDbDataType(typeof(decimal)).WithPrecisionScale(29, 10);
			var decimalValue = factory.Cast(value, decimalType, true);
			var zero        = factory.Value(decimalType, 0m);
			var truncated   = factory.Condition(
				factory.GreaterOrEqual(decimalValue, zero),
				factory.Function(decimalType, "FLOOR", decimalValue),
				factory.Function(decimalType, "CEILING", decimalValue));

			return factory.Cast(truncated, longType, true);
		}

		private protected virtual ISqlExpression TruncateDivision(ITranslationContext translationContext, ISqlExpression value, long divisor)
		{
			var factory     = translationContext.ExpressionFactory;
			var decimalType = factory.GetDbDataType(typeof(decimal)).WithPrecisionScale(29, 10);
			var quotient    = factory.Div(decimalType, factory.Cast(value, decimalType, true), factory.Value(decimalType, (decimal)divisor));

			return TruncateToInt64(translationContext, quotient);
		}

		private static SqlDurationExpression? FindDurationExpression(ISqlExpression expression)
		{
			HashSet<ISqlExpression>? visited = null;

			while (true)
			{
				if (expression is SqlDurationExpression duration)
					return duration;

				visited ??= new HashSet<ISqlExpression>();
				if (!visited.Add(expression))
					return null;

				switch (expression)
				{
					case SqlColumn { Parent.HasSetOperators: false } column:
						expression = column.Expression;
						continue;
					case SqlCteTableField { CteField.Column: { } cteColumn }:
						expression = cteColumn.Expression;
						continue;
					case SqlCteField { Column: { } cteColumn }:
						expression = cteColumn.Expression;
						continue;
					case SqlNullabilityExpression nullability:
						expression = nullability.SqlExpression;
						continue;
					case SqlCastExpression cast:
						expression = cast.Expression;
						continue;
					case SqlExpression { Expr: "{0}", Parameters: [var parameter] }:
						expression = parameter;
						continue;
					default:
						return null;
				}
			}
		}

		private protected bool TryNormalizeDurationToTicks(
			ITranslationContext translationContext,
			ISqlExpression       durationExpression,
			out ISqlExpression   ticks)
		{
			var factory      = translationContext.ExpressionFactory;
			var longType     = factory.GetDbDataType(typeof(long));

			if (durationExpression is SqlColumn { Parent: { HasSetOperators: true } setQuery } setColumn)
			{
				var columnIndex = setQuery.Select.Columns.IndexOf(setColumn);
				if (columnIndex < 0)
				{
					ticks = null!;
					return false;
				}

				var columns = new List<SqlColumn>(setQuery.SetOperators.Count + 1)
				{
					setQuery.Select.Columns[columnIndex],
				};
				foreach (var setOperator in setQuery.SetOperators)
				{
					if (setOperator.SelectQuery.Select.Columns.Count <= columnIndex)
					{
						ticks = null!;
						return false;
					}

					columns.Add(setOperator.SelectQuery.Select.Columns[columnIndex]);
				}

				var normalized = new ISqlExpression[columns.Count];
				for (var index = 0; index < columns.Count; index++)
				{
					if (!TryNormalizeDurationToTicks(translationContext, columns[index].Expression, out normalized[index]))
					{
						ticks = null!;
						return false;
					}
				}

				// UNION/INTERSECT/EXCEPT combine values before an outer expression can inspect them.
				// Canonicalize every arm independently so mixed declared units cannot be interpreted as
				// whichever descriptor QueryHelper happens to encounter first.
				for (var index = 0; index < columns.Count; index++)
					columns[index].Expression = normalized[index];

				ticks = new SqlDurationExpression(longType, setColumn, DurationUnit.Tick, SqlDurationKind.Storage);
				return true;
			}

			var durationMetadata = FindDurationExpression(durationExpression);
			var expressionUnit   = durationMetadata?.Unit;
			var durationKind     = durationMetadata?.Kind ?? SqlDurationKind.Storage;
			DbDataType dataType;
			if (durationExpression is SqlDurationExpression duration)
			{
				expressionUnit      = duration.Unit;
				dataType            = duration.Type;
				durationExpression  = duration.Expression;
			}
			else
			{
				dataType = factory.GetDbDataType(durationExpression);
			}

			if ((!IsNumericDurationType(dataType.DataType) || expressionUnit == null)
				&& TryConvertTimeSpanValueToTicks(durationExpression, longType, out var valueTicks))
			{
				ticks = WrapTicks(valueTicks);
				return true;
			}

			var descriptor = QueryHelper.GetColumnDescriptor(durationExpression);
			DurationUnit unit;
			if (expressionUnit != null)
			{
				unit = expressionUnit.Value;
			}
			else if (descriptor?.MemberType.ToUnderlying() == typeof(TimeSpan))
			{
				if (descriptor.Duration == null)
				{
					ticks = null!;
					return false;
				}

				unit = descriptor.Duration.Unit;
			}
			else
			{
				ticks = null!;
				return false;
			}

			if (unit == DurationUnit.Native)
			{
				// Keep native interval semantics in the SQL tree. Provider conversion lowers this node
				// only after query-shape optimization, so projection/CTE rewrites cannot erase its unit.
				ticks = new SqlDurationExpression(longType, durationExpression, unit, durationKind);
				return true;
			}

			if (!IsNumericDurationType(dataType.DataType))
			{
				ticks = null!;
				return false;
			}

			// Numeric units are also preserved until provider conversion. This is important for set
			// operations and nested projections: every arm is normalized at its own storage boundary.
			ticks = new SqlDurationExpression(longType, durationExpression, unit, durationKind);
			return true;

			ISqlExpression WrapTicks(ISqlExpression expression)
			{
				return new SqlDurationExpression(longType, expression, DurationUnit.Tick, durationKind);
			}

			static bool TryConvertTimeSpanValueToTicks(ISqlExpression expression, DbDataType longType, out ISqlExpression ticks)
			{
				switch (expression)
				{
					case SqlValue { Value: TimeSpan value }:
						ticks = new SqlValue(longType, value.Ticks);
						return true;
					case SqlParameter parameter when parameter.Type.SystemType.ToUnderlying() == typeof(TimeSpan):
					{
						var valueConverter = parameter.ValueConverter;
						var tickParameter = new SqlParameter(longType, parameter.Name, parameter.Value)
						{
							AccessorId       = parameter.AccessorId,
							IsQueryParameter = parameter.IsQueryParameter,
							ValueConverter   = value =>
							{
								if (value is TimeSpan timeSpanValue)
									return timeSpanValue.Ticks;

								var converted = valueConverter == null ? value : valueConverter(value);
								return converted is TimeSpan timeSpan ? timeSpan.Ticks : converted;
							},
						};

						ticks = tickParameter;
						return true;
					}
					default:
						ticks = null!;
						return false;
				}
			}
		}

		private static bool IsNumericDurationType(DataType dataType)
		{
			return dataType is DataType.SByte or DataType.Int16 or DataType.Int32 or DataType.Int64
				or DataType.Byte or DataType.UInt16 or DataType.UInt32 or DataType.UInt64
				or DataType.Half or DataType.Single or DataType.Double or DataType.Decimal
				or DataType.Money or DataType.SmallMoney;
		}

		private protected virtual ISqlExpression? TranslateTimeSpanPart(ITranslationContext translationContext, TranslationFlags translationFlags, ISqlExpression timeSpanExpression, TimeSpanPart part, Type resultType)
		{
			var factory = translationContext.ExpressionFactory;
			if (!TryNormalizeDurationToTicks(translationContext, timeSpanExpression, out var normalizedTicks))
				return null;

			var longType   = factory.GetDbDataType(typeof(long));
			var resultDbType = factory.GetDbDataType(resultType);
			var ticks      = factory.Cast(normalizedTicks, longType, true);

			ISqlExpression DivideTicks(long divisor)
			{
				return TruncateDivision(translationContext, ticks, divisor);
			}

			ISqlExpression Remainder(ISqlExpression value, int modulo)
			{
				var quotient = TruncateDivision(translationContext, value, modulo);
				return factory.Sub(
					longType,
					value,
					factory.Multiply(longType, quotient, factory.Value(longType, (long)modulo)));
			}

			ISqlExpression Component(long divisor, int modulo)
			{
				return factory.Cast(Remainder(DivideTicks(divisor), modulo), resultDbType);
			}

			ISqlExpression Total(double divisor)
			{
				var doubleType = factory.GetDbDataType(typeof(double));
				var value      = factory.Cast(ticks, doubleType);
				return factory.Div(doubleType, value, factory.Value(doubleType, divisor));
			}

			return part switch
			{
				TimeSpanPart.Days              => factory.Cast(DivideTicks(TimeSpan.TicksPerDay), resultDbType),
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
				TimeSpanPart.Nanoseconds       => factory.Cast(factory.Multiply(longType, Remainder(ticks, 10), 100L), resultDbType),
				TimeSpanPart.TotalNanoseconds  => factory.Multiply(resultDbType, factory.Cast(ticks, resultDbType), 100D),
#endif
				TimeSpanPart.Ticks             => ticks,
				_                              => null,
			};
		}

		private protected virtual ISqlExpression? TranslateTimeSpanNegate(ITranslationContext translationContext, TranslationFlags translationFlags, ISqlExpression timeSpanExpression)
		{
			var factory = translationContext.ExpressionFactory;
			if (!TryNormalizeDurationToTicks(translationContext, timeSpanExpression, out var ticks))
				return null;

			return factory.Negate(factory.GetDbDataType(typeof(long)), ticks);
		}

		private protected virtual ISqlExpression? TranslateDateTimeIntervalAdd(ITranslationContext translationContext, TranslationFlags translationFlags, ISqlExpression dateTimeExpression, ISqlExpression intervalExpression, bool isSubtract, bool isDateTimeOffset)
		{
			var factory      = translationContext.ExpressionFactory;
			var longType    = factory.GetDbDataType(typeof(long));
			var ticks       = factory.Cast(intervalExpression, longType, true);

			var days      = TruncateDivision(translationContext, ticks, TimeSpan.TicksPerDay);
			var remainder = factory.Sub(longType, ticks, factory.Multiply(longType, days, TimeSpan.TicksPerDay));

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

			var capabilities = GetDateTimeIntervalCapabilities(translationContext, dateTimeExpression, isDateTimeOffset);
			var precision    = GetDateTimeIntervalAddPrecision(translationContext, dateTimeExpression, isDateTimeOffset);
			if (precision == null)
				return null;

			var ticksPerUnit = precision switch
			{
				Sql.DateParts.Tick        => 1L,
				Sql.DateParts.Microsecond => 10L,
				Sql.DateParts.Millisecond => TimeSpan.TicksPerMillisecond,
				Sql.DateParts.Second      => TimeSpan.TicksPerSecond,
				Sql.DateParts.Minute      => TimeSpan.TicksPerMinute,
				_                         => throw new InvalidOperationException($"Unexpected interval precision: {precision}"),
			};

			if (ticksPerUnit > capabilities.StorageResolutionTicks || !SupportsUnit(capabilities.SupportedUnits, precision.Value))
				return null;

			var increment = TruncateDivision(translationContext, remainder, ticksPerUnit);

			return isDateTimeOffset
				? TranslateDateTimeOffsetDateAdd(translationContext, translationFlags, result, increment, precision.Value)
				: TranslateDateTimeDateAdd(translationContext, translationFlags, result, increment, precision.Value);
		}

		private protected virtual Sql.DateParts? GetDateTimeIntervalAddPrecision(ITranslationContext translationContext, ISqlExpression dateTimeExpression, bool isDateTimeOffset)
		{
			var capabilities = GetDateTimeIntervalCapabilities(translationContext, dateTimeExpression, isDateTimeOffset);

			if (capabilities.StorageResolutionTicks <= 1 && capabilities.SupportedUnits.HasFlag(DateTimeIntervalUnits.Tick))
				return Sql.DateParts.Tick;
			if (capabilities.StorageResolutionTicks <= 10 && capabilities.SupportedUnits.HasFlag(DateTimeIntervalUnits.Microsecond))
				return Sql.DateParts.Microsecond;
			if (capabilities.StorageResolutionTicks <= TimeSpan.TicksPerMillisecond && capabilities.SupportedUnits.HasFlag(DateTimeIntervalUnits.Millisecond))
				return Sql.DateParts.Millisecond;
			if (capabilities.StorageResolutionTicks <= TimeSpan.TicksPerSecond && capabilities.SupportedUnits.HasFlag(DateTimeIntervalUnits.Second))
				return Sql.DateParts.Second;
			if (capabilities.StorageResolutionTicks <= TimeSpan.TicksPerMinute && capabilities.SupportedUnits.HasFlag(DateTimeIntervalUnits.Minute))
				return Sql.DateParts.Minute;

			return null;
		}

		private protected static bool SupportsUnit(DateTimeIntervalUnits units, Sql.DateParts part)
		{
			return (units & (part switch
			{
				Sql.DateParts.Tick        => DateTimeIntervalUnits.Tick,
				Sql.DateParts.Microsecond => DateTimeIntervalUnits.Microsecond,
				Sql.DateParts.Millisecond => DateTimeIntervalUnits.Millisecond,
				Sql.DateParts.Second      => DateTimeIntervalUnits.Second,
				Sql.DateParts.Minute      => DateTimeIntervalUnits.Minute,
				_                         => DateTimeIntervalUnits.None,
			})) != 0;
		}

		private protected virtual ISqlExpression? TranslateDateTimeIntervalDifference(ITranslationContext translationContext, TranslationFlags translationFlags, ISqlExpression leftExpression, ISqlExpression rightExpression, bool isDateTimeOffset)
		{
			return null;
		}

		protected virtual ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
		{
			return null;
		}

		protected virtual ISqlExpression? TranslateDateTimeDatePartLong(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
		{
			return TranslateDateTimeDatePart(translationContext, translationFlag, dateTimeExpression, datepart);
		}

		protected virtual ISqlExpression? TranslateDateTimeOffsetDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
		{
			return null;
		}

		protected virtual ISqlExpression? TranslateDateTimeOffsetDatePartLong(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
		{
			return TranslateDateTimeOffsetDatePart(translationContext, translationFlag, dateTimeExpression, datepart);
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

		protected virtual ISqlExpression? TranslateDateTimeOffsetToUtc(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;
			return factory.Expression(factory.GetDbDataType(typeof(DateTime)), "{0}", dateExpression);
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
