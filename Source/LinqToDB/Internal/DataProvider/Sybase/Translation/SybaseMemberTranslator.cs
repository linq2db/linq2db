using System;
using System.Globalization;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Sybase.Translation
{
	public class SybaseMemberTranslator : ProviderMemberTranslatorDefault
	{
		protected override IMemberTranslator CreateSqlTypesTranslator()
		{
			return new SqlTypesTranslation();
		}

		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new DateFunctionsTranslator();
		}

		protected override IMemberTranslator CreateStringMemberTranslator()
		{
			return new SybaseStingMemberTranslator();
		}

		protected override IMemberTranslator CreateMathMemberTranslator()
		{
			return new SybaseMathMemberTranslator();
		}

		protected override IMemberTranslator CreateGuidMemberTranslator()
		{
			return new GuidMemberTranslator();
		}

		protected class SqlTypesTranslation : SqlTypesTranslationDefault
		{
			protected override Expression? ConvertDateTime2(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime));

			protected override Expression? ConvertDateTimeOffset(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime));
		}

		protected class DateFunctionsTranslator : DateFunctionsTranslatorBase
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

				var resultExpression = factory.Function(intDbType, "DatePart",
					ParametersNullabilityType.SameAsSecondParameter,
					factory.NotNullExpression(intDbType, partStr),
					dateTimeExpression);

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

				var resultExpression = factory.Function(dateType, "DateAdd",
					factory.NotNullExpression(factory.GetDbDataType(typeof(string)), partStr),
					increment,
					dateTimeExpression);
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
						ParametersNullabilityType.NotNullable,
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
				// CONVERT(date, your_datetime_column)

				var convertFunc = factory.Function(factory.GetDbDataType(dateExpression), "CONVERT", factory.SqlDataType(DataType.Date), dateExpression);
				return convertFunc;
			}

			protected override ISqlExpression? TranslateSqlGetDate(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				return factory.Function(factory.GetDbDataType(typeof(DateTime)), "GetDate", ParametersNullabilityType.NotNullable);
			}

			protected override ISqlExpression? TranslateSqlCurrentTimestampUtc(ITranslationContext translationContext, DbDataType dbDataType, TranslationFlags translationFlags)
			{
				return translationContext.ExpressionFactory.Function(dbDataType, "GETUTCDATE");
			}
		}

		protected class SybaseMathMemberTranslator : MathMemberTranslatorBase
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

		protected class SybaseStingMemberTranslator : StringMemberTranslatorBase
		{
			protected override Expression? TranslateStringJoin(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, bool ignoreNulls)
			{
				var builder = new AggregateFunctionBuilder();

				ConfigureConcatWsEmulation(builder, (factory, valueType, separator, valuesExpr) =>
				{
					var intDbType = factory.GetDbDataType(typeof(int));
					var substring = factory.Function(valueType, "SUBSTRING",
						valuesExpr,
						factory.Add(intDbType, factory.Length(separator), factory.Value(intDbType, 1)),
						factory.Value(intDbType, int.MaxValue));

					return substring;
				});

				return builder.Build(translationContext, methodCall);
			}
		}

		protected override ISqlExpression? TranslateNewGuidMethod(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			var factory  = translationContext.ExpressionFactory;
			var timePart = factory.NonPureFunction(factory.GetDbDataType(typeof(Guid)), "NewID", factory.Value(1));

			return timePart;
		}

		protected class GuidMemberTranslator : GuidMemberTranslatorBase
		{
			protected override ISqlExpression? TranslateGuildToString(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression guidExpr, TranslationFlags translationFlags)
			{
				// Lower(Convert(NVarChar(36), {0}))

				var factory        = translationContext.ExpressionFactory;
				var stringDataType = factory.GetDbDataType(typeof(string)).WithDataType(DataType.NVarChar).WithLength(36);

				var cast    = factory.Cast(guidExpr, stringDataType);
				var toLower = factory.ToLower(cast);

				return toLower;
			}
		}
	}
}
