using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.SqlServer.Translation
{
	public class SqlServer2005MemberTranslator : SqlServerMemberTranslator
	{
		protected override IMemberTranslator CreateSqlTypesTranslator()
		{
			return new SqlTypes2005Translation();
		}

		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new SqlServer2005DateFunctionsTranslator();
		}

		protected class SqlTypes2005Translation : SqlTypesTranslation
		{
			protected override Expression? ConvertDate(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime));
		}

		protected class SqlServer2005DateFunctionsTranslator : SqlServerDateFunctionsTranslator
		{
			private protected virtual bool SupportsSubMillisecondDateParts => false;

			private protected override bool SupportsDateTimeOffsetIntervalArithmetic => false;
			private protected override DateTimeIntervalCapabilities GetDefaultDateTimeIntervalCapabilities(bool isDateTimeOffset)
			{
				return new DateTimeIntervalCapabilities(System.TimeSpan.TicksPerMillisecond, DateTimeIntervalUnits.Day | DateTimeIntervalUnits.Hour | DateTimeIntervalUnits.Minute | DateTimeIntervalUnits.Second | DateTimeIntervalUnits.Millisecond, long.MaxValue, !isDateTimeOffset);
			}

			private protected override ISqlExpression? TranslateDateTimeIntervalAdd(ITranslationContext translationContext, TranslationFlags translationFlags, ISqlExpression dateTimeExpression, ISqlExpression intervalExpression, bool isSubtract, bool isDateTimeOffset)
			{
				var intervalType = translationContext.ExpressionFactory.GetDbDataType(intervalExpression);

				if (intervalType.DataType != DataType.Int64)
					return null;

				return TranslateInt64DateTimeIntervalAdd(translationContext, translationFlags, dateTimeExpression, intervalExpression, isSubtract, false, Sql.DateParts.Millisecond);
			}

			protected override ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				if (SupportsSubMillisecondDateParts)
					return base.TranslateDateTimeDatePart(translationContext, translationFlag, dateTimeExpression, datepart);

				var multiplier = datepart switch
				{
					Sql.DateParts.Microsecond => 1_000,
					Sql.DateParts.Nanosecond  => 1_000_000,
					Sql.DateParts.Tick        => 10_000,
					_                         => 0,
				};

				if (multiplier == 0)
					return base.TranslateDateTimeDatePart(translationContext, translationFlag, dateTimeExpression, datepart);

				var milliseconds = base.TranslateDateTimeDatePart(translationContext, translationFlag, dateTimeExpression, Sql.DateParts.Millisecond);
				if (milliseconds == null)
					return null;

				var factory = translationContext.ExpressionFactory;
				var intType = factory.GetDbDataType(typeof(int));
				return factory.Multiply(intType, milliseconds, multiplier);
			}

			protected override ISqlExpression? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment, Sql.DateParts datepart)
			{
				if (SupportsSubMillisecondDateParts)
					return base.TranslateDateTimeDateAdd(translationContext, translationFlag, dateTimeExpression, increment, datepart);

				var divisor = datepart switch
				{
					Sql.DateParts.Microsecond => 1_000L,
					Sql.DateParts.Nanosecond  => 1_000_000L,
					Sql.DateParts.Tick        => 10_000L,
					_                         => 0L,
				};

				if (divisor == 0)
					return base.TranslateDateTimeDateAdd(translationContext, translationFlag, dateTimeExpression, increment, datepart);

				var factory     = translationContext.ExpressionFactory;
				var longType    = factory.GetDbDataType(typeof(long));
				var decimalType = factory.GetDbDataType(typeof(decimal)).WithPrecisionScale(29, 10);
				var value       = factory.Cast(increment, longType, true);
				var quotient    = factory.Div(decimalType, factory.Cast(value, decimalType), factory.Value(decimalType, (decimal)divisor));
				var truncated   = factory.Condition(
					factory.GreaterOrEqual(value, factory.Value(longType, 0L)),
					factory.Function(decimalType, "FLOOR", quotient),
					factory.Function(decimalType, "CEILING", quotient));
				var milliseconds = factory.Cast(truncated, longType, true);

				return base.TranslateDateTimeDateAdd(translationContext, translationFlag, dateTimeExpression, milliseconds, Sql.DateParts.Millisecond);
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				// DATEADD(dd, DATEDIFF(dd, 0, YourDateTimeColumn), 0)

				var factory = translationContext.ExpressionFactory;

				var intDataType = factory.GetDbDataType(typeof(int));
				var dateType = factory.GetDbDataType(dateExpression);

				var datePart = factory.Fragment("dd");
				var dateDiff = factory.Function(intDataType, "DateDiff", ParametersNullabilityType.SameAsLastParameter, datePart, factory.Value(intDataType, 0), dateExpression);
				var dateAdd  = factory.Function(dateType, "DateAdd", ParametersNullabilityType.SameAsSecondParameter, datePart, dateDiff, factory.Value(intDataType, 0));

				return dateAdd;
			}
		}
	}
}
