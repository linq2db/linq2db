using System;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.Linq.Translation
{
	public class DateFunctionsTranslatorBase : MemberTranslatorBase
	{
		public DateFunctionsTranslatorBase()
		{
			RegisterDateTime();
			RegisterDateTimeOffset();
			RegisterDateOnly();

			Registration.RegisterMethod((int? year, int? month, int? day) => Sql.MakeDateTime(year, month, day), TranslateMakeDateTime);
			Registration.RegisterMethod((int year, int month, int day, int hour, int minute, int second) => Sql.MakeDateTime(year, month, day, hour, minute, second), TranslateMakeDateTime);

			Registration.RegisterReplacement(() => Sql.CurrentTimestamp2, () => Sql.GetDate());

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

			Registration.RegisterMember(() => DateTime.Now,              TranslateDateTimeNow);

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
			Registration.RegisterMember((DateTime dt) => Sql.CurrentTimestamp, TranslateSqlCurrentTimestamp);

			Registration.RegisterReplacement(() => DateTime.Now, () => Sql.GetDate());
			Registration.RegisterMethod((DateTime dt) => Sql.GetDate(), TranslateSqlGetDate);
		}

		void RegisterDateTimeOffset()
		{
			Registration.RegisterMember(() => DateTimeOffset.Now, TranslateSqlCurrentTimestampUtc);
			Registration.RegisterMember(() => Sql.CurrentTimestampUtc, TranslateSqlCurrentTimestampUtc);

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

		void RegisterDateOnly()
		{
#if NET8_0_OR_GREATER
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
#endif
		}

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

		Expression? TranslateDateTimeOffsetConstructor(ITranslationContext translationContext, Expression expression, TranslationFlags translationFlags)
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

			var makeExpression = TranslateMakeDateTime(translationContext, translationContext.ExpressionFactory.GetDbDataType(methodCall.Type.ToNullableUnderlying()), year, month, day, hour, minute, second, null);

			if (makeExpression == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, makeExpression, methodCall);
		}

		Expression? TranslateSqlGetDate(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var translated = TranslateSqlGetDate(translationContext, translationFlags);
			if (translated == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translated, methodCall);
		}

		Expression? TranslateDateTimeNow(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var converted = TranslateSqlGetDate(translationContext, translationFlags);
			if (converted == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, converted, memberExpression);
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

#if NET8_0_OR_GREATER
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

		protected virtual Expression? TranslateSqlCurrentTimestamp(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var translated = TranslateSqlGetDate(translationContext, translationFlags);
			if (translated == null)
				return null;
			return translationContext.CreatePlaceholder(translated, memberExpression);
		}

		protected virtual Expression? TranslateSqlCurrentTimestampUtc(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var translated = TranslateSqlCurrentTimestampUtc(translationContext, translationFlags);
			if (translated == null)
				return null;
			return translationContext.CreatePlaceholder(translated, memberExpression);
		}

		protected virtual ISqlExpression? TranslateSqlGetDate(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			var factory       = translationContext.ExpressionFactory;
			var currentTimeStamp = factory.NotNullFragment(factory.GetDbDataType(typeof(DateTime)), "CURRENT_TIMESTAMP");
			return currentTimeStamp;
		}

		protected virtual ISqlExpression? TranslateSqlCurrentTimestampUtc(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			return null;
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
