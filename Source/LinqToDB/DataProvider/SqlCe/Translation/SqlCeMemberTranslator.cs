using System;
using System.Globalization;
using System.Linq.Expressions;

using LinqToDB.Common;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.SqlCe.Translation
{
	public class SqlCeMemberTranslator : ProviderMemberTranslatorDefault
	{
		protected override IMemberTranslator CreateSqlTypesTranslator()
		{
			return new SqlTypesTranslation();
		}

		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new DateFunctionsTranslator();
		}

		protected override IMemberTranslator CreateMathMemberTranslator()
		{
			return new SqlCeMathMemberTranslator();
		}

		protected override IMemberTranslator CreateStringMemberTranslator()
		{
			return new StringMemberTranslator();
		}

		protected override IMemberTranslator CreateGuidMemberTranslator()
		{
			return new GuidMemberTranslator();
		}

		class SqlTypesTranslation : SqlTypesTranslationDefault
		{
			protected override Expression? ConvertSmallMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(10, 4));

			protected override Expression? ConvertDateTime2(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime));

			protected override Expression? ConvertSmallDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime));

			protected override Expression? ConvertDate(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime));

			protected override Expression? ConvertDateTimeOffset(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> translationContext.CreateErrorExpression(memberExpression, "SqlCe do not support 'DateTimeOffset'");

			protected override Expression? ConvertVarChar(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			{
				if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
					return null;

				return MakeSqlTypeExpression(translationContext, methodCall, typeof(string), t => t.WithLength(length).WithDataType(DataType.NVarChar));
			}
		}

		public class DateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			protected override ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				var partStr = datepart switch
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
					_ => null
				};

				if (partStr == null)
					return null;

				var factory = translationContext.ExpressionFactory;
				var intDbType = factory.GetDbDataType(typeof(int));

				var resultExpression = factory.Function(intDbType, "DatePart", ParametersNullabilityType.SameAsSecondParameter, factory.NotNullFragment(intDbType, partStr), dateTimeExpression);

				return resultExpression;
			}

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
					_ => null
				};
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

					var castToLength = CastToLength(expression, padSize);

					return
						factory.Concat(
							factory.Function(stringDataType, "REPLICATE",
								ParametersNullabilityType.SameAsSecondParameter,
								factory.Value(stringDataType, "0"),
								factory.Sub(intDataType, factory.Value(intDataType, padSize), factory.Function(intDataType, "LEN", castToLength))
							),
							castToLength
						);
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

			protected override ISqlExpression? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment,
				Sql.DateParts                                                       datepart)
			{
				var factory  = translationContext.ExpressionFactory;
				var dateType = factory.GetDbDataType(dateTimeExpression);

				var partStr = DatePartToStr(datepart);

				if (partStr == null)
				{
					return null;
				}

				var resultExpression = factory.Function(dateType, "DateAdd", factory.NotNullFragment(factory.GetDbDataType(typeof(string)), partStr), increment, dateTimeExpression);
				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				// CAST(CONVERT(nvarchar(10), your_datetime_column, 101) AS datetime)

				var factory    = translationContext.ExpressionFactory;
				var stringType = factory.GetDbDataType(typeof(string)).WithDataType(DataType.NVarChar).WithLength(10);

				var convert = factory.Function(stringType, "CONVERT", ParametersNullabilityType.SameAsSecondParameter, new SqlDataType(stringType), dateExpression, factory.Value(101));
				var cast    = factory.Cast(convert, translationContext.GetDbDataType(dateExpression));

				return cast;
			}

			protected override ISqlExpression? TranslateSqlGetDate(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;

				return factory.Function(factory.GetDbDataType(typeof(DateTime)), "GetDate", ParametersNullabilityType.NotNullable);
			}
		}

		public class SqlCeMathMemberTranslator : MathMemberTranslatorBase
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
			public override ISqlExpression? TranslateLength(ITranslationContext translationContext, TranslationFlags translationFlags, ISqlExpression value)
			{
				var factory         = translationContext.ExpressionFactory;
				var valueTypeString = factory.GetDbDataType(value);
				var valueTypeInt    = factory.GetDbDataType(typeof(int));

				var valueString = factory.Add(valueTypeString, value, factory.Value(valueTypeString, "."));
				var valueLength = factory.Length(valueString);

				return factory.Sub(valueTypeInt, valueLength, factory.Value(valueTypeInt, 1));
			}

			public override ISqlExpression? TranslateLPad(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression padding, ISqlExpression paddingChar)
			{
				/*
				 * SELECT REPLICATE(paddingSymbol, padding - LEN(value)) + value
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
