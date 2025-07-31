using System;
using System.Linq.Expressions;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Ydb.Translation
{
	public class YdbMemberTranslator : ProviderMemberTranslatorDefault
	{
		#region --- SQL-type helpers -----------------------------------------

		sealed class SqlTypesTranslation : SqlTypesTranslationDefault
		{
			// YDB stores DateTime with microsecond precision in the Timestamp type
			protected override Expression? ConvertDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));

			protected override Expression? ConvertDateTime2(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));

			protected override Expression? ConvertSmallDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));

			protected override Expression? ConvertDateTimeOffset(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));
		}

		#endregion

		#region --- Date/Time Functions --------------------------------------

		public class DateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			// -----------------------------------------------------------------
			// DATEPART translation
			// -----------------------------------------------------------------
			protected override ISqlExpression? TranslateDateTimeDatePart(
					ITranslationContext translationContext, TranslationFlags translationFlag,
					ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				var f       = translationContext.ExpressionFactory;
				var intType = f.GetDbDataType(typeof(int));

				// Use direct DateTime::Get* functions
				string? fn = datepart switch
				{
					Sql.DateParts.Year        => "DateTime::GetYear",
					Sql.DateParts.Month       => "DateTime::GetMonth",
					Sql.DateParts.Day         => "DateTime::GetDayOfMonth",
					Sql.DateParts.DayOfYear   => "DateTime::GetDayOfYear",
					Sql.DateParts.Week        => "DateTime::GetWeekOfYearIso8601",
					Sql.DateParts.Hour        => "DateTime::GetHour",
					Sql.DateParts.Minute      => "DateTime::GetMinute",
					Sql.DateParts.Second      => "DateTime::GetSecond",
					Sql.DateParts.Millisecond => "DateTime::GetMillisecondOfSecond",
					Sql.DateParts.WeekDay     => "DateTime::GetDayOfWeek",
					_                         => null
				};

				// QUARTER = (Month + 2) / 3
				if (datepart == Sql.DateParts.Quarter)
				{
					var month = f.Function(intType, "DateTime::GetMonth", dateTimeExpression);
					var two  = f.Value(intType, 2);
					var three = f.Value(intType, 3);
					return f.Div(intType, f.Add(intType, month, two), three);
				}

				if (fn == null)
					return null;

				var baseExpr = f.Function(intType, fn, dateTimeExpression);

				// Adjust DayOfWeek to match T-SQL format (Sunday=1 ... Saturday=7)
				var seven  = f.Value(intType, 7);
				var one    = f.Value(intType, 1);
				if (datepart == Sql.DateParts.WeekDay)
					return f.Add(intType, f.Mod(baseExpr, seven), one);

				return baseExpr;
			}

			protected override ISqlExpression? TranslateDateTimeOffsetDatePart(
					ITranslationContext translationContext, TranslationFlags translationFlag,
					ISqlExpression dateTimeExpression, Sql.DateParts datepart)
				=> TranslateDateTimeDatePart(translationContext, translationFlag, dateTimeExpression, datepart);

			// -----------------------------------------------------------------
			// DATEADD translation
			// -----------------------------------------------------------------
			protected override ISqlExpression? TranslateDateTimeDateAdd(
					ITranslationContext translationContext, TranslationFlags translationFlag,
					ISqlExpression dateTimeExpression, ISqlExpression increment, Sql.DateParts datepart)
			{
				var f        = translationContext.ExpressionFactory;
				var dateType = f.GetDbDataType(dateTimeExpression);
				var intType  = f.GetDbDataType(typeof(int));

				// Shift by year/month/quarter using Shift* functions
				if (datepart is Sql.DateParts.Year or Sql.DateParts.Month or Sql.DateParts.Quarter)
				{
					string shiftFn = datepart switch
					{
						Sql.DateParts.Year    => "DateTime::ShiftYears",
						Sql.DateParts.Month   => "DateTime::ShiftMonths",
						Sql.DateParts.Quarter => "DateTime::ShiftMonths",
						_                     => throw new InvalidOperationException()
					};

					ISqlExpression shiftArg = increment;

					// Quarter = 3 months
					if (datepart == Sql.DateParts.Quarter)
						shiftArg = f.Multiply(intType, increment, 3);

					var split        = f.Function(f.GetDbDataType(dateTimeExpression), "DateTime::Split", dateTimeExpression);
					var shifted      = f.Function(f.GetDbDataType(split), shiftFn, split, shiftArg);
					var makeDateTime = f.Function(dateType, "DateTime::MakeDatetime", shifted);

					return makeDateTime;
				}

				// Week = 7 days → convert to days
				if (datepart == Sql.DateParts.Week)
				{
					var days = f.Multiply(intType, increment, 7);
					return TranslateDateTimeDateAdd(translationContext, translationFlag, dateTimeExpression, days, Sql.DateParts.Day);
				}

				// Day/Hour/Minute/Second/Millisecond → use interval functions
				string? intervalFn = datepart switch
				{
					Sql.DateParts.Day         => "DateTime::IntervalFromDays",
					Sql.DateParts.Hour        => "DateTime::IntervalFromHours",
					Sql.DateParts.Minute      => "DateTime::IntervalFromMinutes",
					Sql.DateParts.Second      => "DateTime::IntervalFromSeconds",
					Sql.DateParts.Millisecond => "DateTime::IntervalFromMilliseconds",
					_                         => null
				};

				if (intervalFn == null)
					return null;

				var interval = f.Function(
					f.GetDbDataType(typeof(TimeSpan)).WithDataType(DataType.Interval),
					intervalFn, increment);

				return f.Add(dateType, dateTimeExpression, interval);
			}

			protected override ISqlExpression? TranslateDateTimeOffsetDateAdd(
					ITranslationContext translationContext, TranslationFlags translationFlag,
					ISqlExpression dateTimeExpression, ISqlExpression increment, Sql.DateParts datepart)
				=> TranslateDateTimeDateAdd(translationContext, translationFlag, dateTimeExpression, increment, datepart);

			// -----------------------------------------------------------------
			// TRUNC(date) → DATE
			// -----------------------------------------------------------------
			protected override ISqlExpression? TranslateDateTimeTruncationToDate(
					ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var f        = translationContext.ExpressionFactory;
				var resType  = f.GetDbDataType(typeof(DateTime)).WithDataType(DataType.Date);
				var startDay = f.Function(f.GetDbDataType(dateExpression), "DateTime::StartOfDay", dateExpression);
				return f.Function(resType, "DateTime::MakeDate", startDay);
			}

			protected override ISqlExpression? TranslateDateTimeOffsetTruncationToDate(
					ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
				=> TranslateDateTimeTruncationToDate(translationContext, dateExpression, translationFlags);

			// -----------------------------------------------------------------
			// GETDATE()
			// -----------------------------------------------------------------
			protected override ISqlExpression? TranslateSqlGetDate(
					ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var f = translationContext.ExpressionFactory;
				return f.Function(f.GetDbDataType(typeof(DateTime)), "CurrentUtcTimestamp",
								  ParametersNullabilityType.NotNullable);
			}
		}

		#endregion

		sealed class MathMemberTranslator : MathMemberTranslatorBase
		{

			/// <summary>
			/// Banker's rounding: In YQL, ROUND(value, precision?) already performs to-even rounding for Decimal types.
			/// </summary>
			protected override ISqlExpression? TranslateRoundToEven(
				ITranslationContext translationContext,
				MethodCallExpression methodCall,
				ISqlExpression value,
				ISqlExpression? precision)
			{
				var factory   = translationContext.ExpressionFactory;
				var valueType = factory.GetDbDataType(value);

				return precision != null
					? factory.Function(valueType, "ROUND", value, precision)
					: factory.Function(valueType, "ROUND", value);
			}

			/// <summary>
			/// Away-from-zero rounding: In YQL, ROUND(value, precision?) for Numeric types already uses away-from-zero rounding.
			/// </summary>
			protected override ISqlExpression? TranslateRoundAwayFromZero(
				ITranslationContext translationContext,
				MethodCallExpression methodCall,
				ISqlExpression value,
				ISqlExpression? precision)
			{
				var factory   = translationContext.ExpressionFactory;
				var valueType = factory.GetDbDataType(value);

				return precision != null
					? factory.Function(valueType, "ROUND", value, precision)
					: factory.Function(valueType, "ROUND", value);
			}

			/// <summary>
			/// Exponentiation using the built-in POWER(a, b) function.
			/// </summary>
			protected override ISqlExpression? TranslatePow(
				ITranslationContext translationContext,
				MethodCallExpression methodCall,
				ISqlExpression xValue,
				ISqlExpression yValue)
			{
				var factory = translationContext.ExpressionFactory;
				var xType   = factory.GetDbDataType(xValue);
				var yType   = factory.GetDbDataType(yValue);

				if (!xType.EqualsDbOnly(yType))
					yValue = factory.Cast(yValue, xType);

				return factory.Function(xType, "POWER", xValue, yValue);
			}
		}

		#region --- Registration --------------------------------------------

		protected override IMemberTranslator CreateSqlTypesTranslator() => new SqlTypesTranslation();

		protected override IMemberTranslator CreateDateMemberTranslator() => new DateFunctionsTranslator();

		protected override IMemberTranslator CreateMathMemberTranslator() => new MathMemberTranslator();

		#endregion
	}
}
