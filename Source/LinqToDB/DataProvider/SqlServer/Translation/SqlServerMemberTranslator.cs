using System;
using System.Globalization;
using System.Linq.Expressions;

using LinqToDB.Common;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.SqlServer.Translation
{
	public class SqlServerMemberTranslator : ProviderMemberTranslatorDefault
	{
		protected override IMemberTranslator CreateSqlTypesTranslator()
		{
			return new SqlTypesTranslation();
		}

		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new SqlServerDateFunctionsTranslator();
		}

		protected override IMemberTranslator CreateMathMemberTranslator()
		{
			return new SqlServerMathMemberTranslator();
		}

		protected override IMemberTranslator CreateStringMemberTranslator()
		{
			return new StringMemberTranslator();
		}

		protected override IMemberTranslator CreateGuidMemberTranslator()
		{
			return new GuidMemberTranslator();
		}

		protected class SqlTypesTranslation : SqlTypesTranslationDefault
		{
			protected override Expression? ConvertDateTimeOffset(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime));
		}

		public class SqlServerDateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			public static string? DatePartToStr(Sql.DateParts part)
			{
				return part switch
				{
					Sql.DateParts.Year => "year",
					Sql.DateParts.Quarter => "quarter",
					Sql.DateParts.Month => "month",
					Sql.DateParts.DayOfYear => "dayofyear",
					Sql.DateParts.Day => "day",
					Sql.DateParts.Week => "week",
					Sql.DateParts.WeekDay => "weekday",
					Sql.DateParts.Hour => "hour",
					Sql.DateParts.Minute => "minute",
					Sql.DateParts.Second => "second",
					Sql.DateParts.Millisecond => "millisecond",
					Sql.DateParts.Microsecond => "microsecond",
					Sql.DateParts.Tick => "microsecond",
					Sql.DateParts.Nanosecond => "nanosecond",
					_ => null
				};
			}

			protected override ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				var partStr = DatePartToStr(datepart);

				if (partStr == null)
					return null;

				var factory   = translationContext.ExpressionFactory;
				var intDbType = factory.GetDbDataType(typeof(int));

				var resultExpression = factory.Function(intDbType, "DatePart", ParametersNullabilityType.SameAsSecondParameter, factory.NotNullFragment(intDbType, partStr), dateTimeExpression);

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeOffsetDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				return TranslateDateTimeDatePart(translationContext, translationFlag, dateTimeExpression, datepart);
			}

			protected override ISqlExpression? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment,
				Sql.DateParts                                                       datepart)
			{
				var factory = translationContext.ExpressionFactory;
				var dateType = factory.GetDbDataType(dateTimeExpression);


				var partStr = DatePartToStr(datepart);

				if (partStr == null)
				{
					return null;
				}

				var fragment = factory.NotNullFragment(factory.GetDbDataType(typeof(string)), partStr);

				if (datepart == Sql.DateParts.Tick)
				{
					fragment = factory.Div(fragment, 10);
				}

				var resultExpression = factory.Function(dateType, "DateAdd", fragment, increment, dateTimeExpression);

				return resultExpression;
			}

			protected override ISqlExpression? TranslateMakeDateTime(
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
				var factory        = translationContext.ExpressionFactory;
				var stringDataType = factory.GetDbDataType(typeof(string)).WithDataType(DataType.VarChar);
				var intDataType    = factory.GetDbDataType(typeof(int));

				ISqlExpression CastToLength(ISqlExpression expression, int stringLength)
				{
					return factory.Cast(expression, stringDataType.WithLength(stringLength));
				}

				ISqlExpression PartExpression(ISqlExpression expression, int padSize)
				{
					if (translationContext.TryEvaluate(expression, out var expressionValue) && expressionValue is int intValue)
					{
						var padLeft = intValue.ToString(CultureInfo.InvariantCulture).PadLeft(padSize, '0');
						return factory.Value(stringDataType.WithLength(padLeft.Length), padLeft);
					}

					return factory.Function(stringDataType, "RIGHT",
						factory.Concat(factory.Value(stringDataType, "0"), CastToLength(expression, padSize)),
						factory.Value(intDataType, padSize));
				}

				var yearString  = PartExpression(year, 4);
				var monthString = PartExpression(month, 2);
				var dayString   = PartExpression(day, 2);

				var resultExpression = factory.Concat(
					yearString, factory.Value(stringDataType, "-"),
					monthString, factory.Value(stringDataType, "-"), dayString);

				if (hour != null || minute != null || second != null || millisecond != null)
				{
					hour        ??= factory.Value(intDataType, 0);
					minute      ??= factory.Value(intDataType, 0);
					second      ??= factory.Value(intDataType, 0);
					millisecond ??= factory.Value(intDataType, 0);

					resultExpression = factory.Concat(
						resultExpression,
						factory.Value(stringDataType, " "),
						PartExpression(hour, 2), factory.Value(stringDataType, ":"),
						PartExpression(minute, 2), factory.Value(stringDataType, ":"),
						PartExpression(second, 2), factory.Value(stringDataType, "."),
						PartExpression(millisecond, 3)
					);
				}

				resultExpression = factory.Cast(resultExpression, resulType);

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				var cast    = factory.Cast(dateExpression, factory.GetDbDataType(dateExpression).WithDataType(DataType.Date), true);

				return cast;
			}

			protected override ISqlExpression? TranslateDateTimeOffsetTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				return TranslateDateTimeTruncationToDate(translationContext, dateExpression, translationFlags);
			}

			protected override ISqlExpression? TranslateSqlGetDate(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				return factory.NotNullFragment(factory.GetDbDataType(typeof(DateTime)), "CURRENT_TIMESTAMP");
			}
		}

		public class SqlServerMathMemberTranslator : MathMemberTranslatorBase
		{
			protected override ISqlExpression? TranslateRoundAwayFromZero(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression value, ISqlExpression? precision)
			{
				return base.TranslateRoundAwayFromZero(translationContext, methodCall, value, precision ?? translationContext.ExpressionFactory.Value(0));
			}

			protected override ISqlExpression? TranslateRoundToEven(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression value, ISqlExpression? precision)
			{
				return base.TranslateRoundToEven(translationContext, methodCall, value, precision ?? translationContext.ExpressionFactory.Value(0));
			}
		}

		public class StringMemberTranslator : StringMemberTranslatorBase
		{
			public override ISqlExpression? TranslateLPad(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression padding, ISqlExpression paddingChar)
			{
				/*
				 * REPLICATE(paddingSymbol, padding - LEN(value)) + value
				 */
				var factory         = translationContext.ExpressionFactory;
				var valueTypeString = factory.GetDbDataType(value);
				var valueTypeInt    = factory.GetDbDataType(typeof(int));
				
				var lengthValue = TranslateLength(translationContext, translationFlags, value);
				if (lengthValue == null)
					return null;

				var symbolsToAdd = factory.Sub(valueTypeInt, padding, lengthValue);
				var stringToAdd  = factory.Function(valueTypeString, "REPLICATE", paddingChar, symbolsToAdd);
				
				return factory.Add(valueTypeString, stringToAdd, value);
			}
		}

		protected override ISqlExpression? TranslateNewGuidMethod(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			var factory  = translationContext.ExpressionFactory;
			var timePart = factory.NonPureFunction(factory.GetDbDataType(typeof(Guid)), "NewID");

			return timePart;
		}

		class GuidMemberTranslator : GuidMemberTranslatorBase
		{
			protected override ISqlExpression? TranslateGuildToString(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression guidExpr, TranslationFlags translationFlags)
		{
				//"LOWER(CAST({0} AS char(36)))"

			var factory  = translationContext.ExpressionFactory;
				var stringDataType = factory.GetDbDataType(typeof(string)).WithDataType(DataType.Char).WithLength(36);

				var cast  = factory.Cast(guidExpr, stringDataType);
				var lower = factory.ToLower(cast);

				return lower;
			}
		}
	}
}
