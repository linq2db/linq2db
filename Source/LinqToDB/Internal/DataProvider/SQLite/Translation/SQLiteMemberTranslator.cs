using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.SQLite.Translation
{
	public class SQLiteMemberTranslator : ProviderMemberTranslatorDefault
	{
		protected class SqlTypesTranslation : SqlTypesTranslationDefault
		{
		}

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
			return new StringMemberTranslator();
		}

		protected override IMemberTranslator CreateGuidMemberTranslator()
		{
			return new GuidMemberTranslator();
		}

		protected class DateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			const string StrFTimeFuncName = "strftime";
			const string DateFormat = "%Y-%m-%d %H:%M:%f";
			const string TimeFormat = "%H:%M:%f";

			protected override ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				var factory      = translationContext.ExpressionFactory;
				var stringDbType = factory.GetDbDataType(typeof(string));
				var intDbType    = factory.GetDbDataType(typeof(int));
				var doubleDbType = factory.GetDbDataType(typeof(double));

				string partStr;
				switch (datepart)
				{
					case Sql.DateParts.Year: partStr = "%Y"; break;
					case Sql.DateParts.Quarter:
					{
						var result = StrFTimeInt(factory, intDbType, stringDbType, "%m", dateTimeExpression);
						result = factory.Increment(factory.Div(intDbType, factory.Decrement(result), 3));

						return result;
					}
					case Sql.DateParts.Month:     partStr = "%m"; break;
					case Sql.DateParts.DayOfYear: partStr = "%j"; break;
					case Sql.DateParts.Day:       partStr = "%d"; break;
					case Sql.DateParts.Week:      partStr = "%W"; break;
					case Sql.DateParts.WeekDay:   partStr = "%w"; break;
					case Sql.DateParts.Hour:      partStr = "%H"; break;
					case Sql.DateParts.Minute:    partStr = "%M"; break;
					case Sql.DateParts.Second:    partStr = "%S"; break;
					case Sql.DateParts.Millisecond:
					{
						var result = StrFTime(factory, doubleDbType, "%f", dateTimeExpression);

						result = factory.Mod(factory.Cast(factory.Multiply(doubleDbType, result, 1000), intDbType), 1000);

						return result;
					}	
					default:
						return null;
				}

				var resultExpression = StrFTimeInt(factory, intDbType, stringDbType, partStr, dateTimeExpression);

				if (datepart == Sql.DateParts.WeekDay)
					resultExpression = factory.Increment(resultExpression);

				return resultExpression;
			}

			ISqlExpression StrFTime(ISqlExpressionFactory factory, DbDataType resultDbType, string format, ISqlExpression date)
			{
				return factory!.Function(resultDbType, StrFTimeFuncName, ParametersNullabilityType.SameAsSecondParameter, factory.Value(format), date);
			}

			ISqlExpression StrFTimeInt(ISqlExpressionFactory factory, DbDataType intDbType, DbDataType stringDbType, string format, ISqlExpression date)
			{
				return factory.Cast(StrFTime(factory, stringDbType, format, date), intDbType);
			}

			protected override ISqlExpression? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment,
				Sql.DateParts                                                       datepart)
			{
				var factory      = translationContext.ExpressionFactory;
				var stringDbType = factory.GetDbDataType(typeof(string));
				var intDbType    = factory.GetDbDataType(typeof(int));
				var doubleDbType = factory.GetDbDataType(typeof(double));
				var dateType     = factory.GetDbDataType(dateTimeExpression);

				ISqlExpression CastToString(ISqlExpression expression)
				{
					return factory.Cast(expression, stringDbType);
				}

				ISqlExpression dateExpr;
				switch (datepart)
				{
					case Sql.DateParts.Year:    dateExpr = factory.Concat(stringDbType, CastToString(increment), " Year"); break;
					case Sql.DateParts.Quarter: dateExpr = factory.Concat(stringDbType, CastToString(factory.Multiply(increment, 3)), " Month"); break;
					case Sql.DateParts.Month:   dateExpr = factory.Concat(stringDbType, CastToString(increment), " Month"); break;
					case Sql.DateParts.DayOfYear:
					case Sql.DateParts.WeekDay:
					case Sql.DateParts.Day: dateExpr = factory.Concat(stringDbType, CastToString(increment), factory.Value(stringDbType, " Day")); break;
					case Sql.DateParts.Week:        dateExpr = factory.Concat(CastToString(factory.Multiply(increment, 7)), " Day"); break;
					case Sql.DateParts.Hour:        dateExpr = factory.Concat(CastToString(increment), " Hour"); break;
					case Sql.DateParts.Minute:      dateExpr = factory.Concat(CastToString(increment), " Minute"); break;
					case Sql.DateParts.Second:      dateExpr = factory.Concat(CastToString(increment), " Second"); break;
					case Sql.DateParts.Millisecond: dateExpr = factory.Concat(CastToString(factory.Div(doubleDbType, factory.Cast(increment, doubleDbType), 1000)), " Second"); break;
					default:
						return null;
				}

				var resultExpression = factory.Function(dateType, StrFTimeFuncName, ParametersNullabilityType.SameAsSecondParameter, factory.Value(stringDbType, DateFormat), dateTimeExpression, dateExpr);

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

				ISqlExpression PartExpression(ISqlExpression expression, int padSize)
				{
					if (translationContext.TryEvaluate(expression, out var expressionValue) && expressionValue is int intValue)
					{
						var padLeft = intValue.ToString(CultureInfo.InvariantCulture).PadLeft(padSize, '0');
						return factory.Value(stringDataType.WithLength(padLeft.Length), padLeft);
					}

					var formatStr = "%0" + padSize.ToString(CultureInfo.InvariantCulture) + "d";

					return factory.Function(stringDataType, "printf",
						ParametersNullabilityType.NotNullable,
						factory.Value(stringDataType, formatStr),
						expression);
				}

				var yearString  = PartExpression(year, 4);
				var monthString = PartExpression(month, 2);
				var dayString   = PartExpression(day, 2);

				hour ??= factory.Value(intDataType, 0);
				minute ??= factory.Value(intDataType, 0);
				second ??= factory.Value(intDataType, 0);
				millisecond ??= factory.Value(intDataType, 0);

				var resultExpression = factory.Concat(
					yearString, factory.Value(stringDataType, "-"),
					monthString, factory.Value(stringDataType, "-"), dayString, factory.Value(stringDataType, " "),
					PartExpression(hour, 2), factory.Value(stringDataType, ":"),
					PartExpression(minute, 2), factory.Value(stringDataType, ":"),
					PartExpression(second, 2), factory.Value(stringDataType, "."),
					PartExpression(millisecond, 3)
				);

				resultExpression = factory.Function(resulType, StrFTimeFuncName, ParametersNullabilityType.SameAsSecondParameter, factory.Value(stringDataType, DateFormat), resultExpression);

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				// Date(your_datetime_column)

				var factory  = translationContext.ExpressionFactory;
				var dateFunc = factory.Function(factory.GetDbDataType(dateExpression), "Date", dateExpression);

				return dateFunc;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToTime(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;

				var resultExpression = factory.Function(factory.GetDbDataType(typeof(TimeSpan)), StrFTimeFuncName, ParametersNullabilityType.SameAsSecondParameter, factory.Value(TimeFormat), dateExpression);

				return resultExpression;
			}

			protected override ISqlExpression? TranslateSqlCurrentTimestampUtc(ITranslationContext translationContext, DbDataType dbDataType, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				return factory.Function(dbDataType, "DATETIME", factory.Value("now"));
			}
		}

		protected class StringMemberTranslator : StringMemberTranslatorBase
		{
			public override ISqlExpression? TranslateLPad(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression padding, ISqlExpression paddingChar)
			{
				/*
				 * SELECT value || SUBSTR(
				 *				REPLACE(HEX(ZEROBLOB(padding)), '0', paddingSymbol), 
				 *				1,
				 *				padding - LENGTH(value));
				*/

				var factory = translationContext.ExpressionFactory;

				var valueTypeString = factory.GetDbDataType(value);
				var valueTypeInt    = factory.GetDbDataType(typeof(int));

				var valueZeroBlob = factory.Function(valueTypeString, "ZEROBLOB", padding);
				var valueHex      = factory.Function(valueTypeString, "HEX", valueZeroBlob);
				var paddingString = factory.Function(valueTypeString, "REPLACE", valueHex, factory.Value(valueTypeString, "0"), paddingChar);

				var lengthExpr = TranslateLength(translationContext, translationFlags, value);
				if (lengthExpr == null)
					return null;

				var valueSymbolsToAdd = factory.Sub(valueTypeInt, padding, lengthExpr);
				var fillingString     = factory.Function(valueTypeString, "SUBSTR", paddingString, factory.Value(valueTypeInt, 1), valueSymbolsToAdd);
				
				return factory.Concat(fillingString, value);
			}

			protected override Expression? TranslateStringJoin(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, bool ignoreNulls)
			{
				var builder = new AggregateFunctionBuilder()
					.ConfigureAggregate(c => c
						.AllowFilter()
						.AllowNotNullCheck(true)
						.TranslateArguments(0)
						.OnBuildFunction(composer =>
						{
							var info = composer.BuildInfo;
							if (info.Value == null || info.Argument(0) == null)
							{
								return;
							}

							var factory   = info.Factory;
							var separator = info.Argument(0)!;
							var valueType = factory.GetDbDataType(info.Value);

							var value = info.Value;
							if (!info.IsNullFiltered)
								value = factory.Coalesce(value, factory.Value(valueType, string.Empty));

							if (info.FilterCondition != null && !info.FilterCondition.IsTrue())
							{
								value = factory.Condition(info.FilterCondition, value, factory.Null(valueType));
							}

							var fn = factory.Function(valueType, "GROUP_CONCAT",
								[new SqlFunctionArgument(value), new SqlFunctionArgument(separator)],
								[true, true],
								isAggregate : true,
								canBeAffectedByOrderBy: true);

							composer.SetResult(factory.Coalesce(fn, factory.Value(valueType, string.Empty)));
						}));

				return builder.Build(translationContext, methodCall.Arguments[1], methodCall);
			}

		}

		protected class GuidMemberTranslator : GuidMemberTranslatorBase
		{
			protected override ISqlExpression? TranslateGuildToString(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression guidExpr, TranslationFlags translationFlags)
			{
				// 	lower((substr(hex({0}), 7, 2) || substr(hex({0}), 5, 2) || substr(hex({0}), 3, 2) || substr(hex({0}), 1, 2) || '-' || substr(hex({0}), 11, 2) || substr(hex({0}), 9, 2) || '-' || substr(hex({0}), 15, 2) || substr(hex({0}), 13, 2) || '-' || substr(hex({0}), 17, 4) || '-' || substr(hex({0}), 21, 12)))

				var factory      = translationContext.ExpressionFactory;
				var stringDbType = factory.GetDbDataType(typeof(string));
				var hexExpr      = factory.Function(stringDbType, "hex", guidExpr);

				var dividerExpr = factory.Value(stringDbType, "-");

				var resultExpression = factory.ToLower(
					factory.Concat(
						SubString(hexExpr, 7, 2),
						SubString(hexExpr, 5, 2),
						SubString(hexExpr, 3, 2),
						SubString(hexExpr, 1, 2),
						dividerExpr,
						SubString(hexExpr, 11, 2),
						SubString(hexExpr, 9,  2),
						dividerExpr,
						SubString(hexExpr, 15, 2),
						SubString(hexExpr, 13, 2),
						dividerExpr,
						SubString(hexExpr, 17, 4),
						dividerExpr,
						SubString(hexExpr, 21, 12)
					)
				);

				resultExpression = factory.Condition(factory.IsNullPredicate(guidExpr), factory.Value<string?>(stringDbType, null), factory.NotNull(resultExpression));

				return resultExpression; 

				ISqlExpression SubString(ISqlExpression expression, int pos, int length)
				{
					return factory.Function(stringDbType, "substr", expression, factory.Value(pos), factory.Value(length));
				}
			}
		}
	}
}
