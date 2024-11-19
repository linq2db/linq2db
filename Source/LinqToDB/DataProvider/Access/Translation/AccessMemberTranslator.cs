using System;
using System.Globalization;
using System.Linq.Expressions;

using LinqToDB.Common;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Access.Translation
{
	using Linq.Translation;

	public class AccessMemberTranslator : ProviderMemberTranslatorDefault
	{
		class SqlTypesTranslation : SqlTypesTranslationDefault
		{
		}

		public class DateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			protected override ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				var factory = translationContext.ExpressionFactory;

				var partStr = datepart switch
				{
					Sql.DateParts.Year      => "yyyy",
					Sql.DateParts.Quarter   => "q",
					Sql.DateParts.Month     => "m",
					Sql.DateParts.DayOfYear => "y",
					Sql.DateParts.Day       => "d",
					Sql.DateParts.Week      => "ww",
					Sql.DateParts.WeekDay   => "w",
					Sql.DateParts.Hour      => "h",
					Sql.DateParts.Minute    => "n",
					Sql.DateParts.Second    => "s",
					_ => null,
				};

				if (partStr == null)
					return null;

				var resultExpression = factory.Function(factory.GetDbDataType(typeof(int)), "DatePart", new SqlValue(typeof(string), partStr), dateTimeExpression);

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment,
				Sql.DateParts                                                       datepart)
			{
				var factory = translationContext.ExpressionFactory;

				var partStr = datepart switch
				{
					Sql.DateParts.Year      => "yyyy",
					Sql.DateParts.Quarter   => "q",
					Sql.DateParts.Month     => "m",
					Sql.DateParts.DayOfYear => "y",
					Sql.DateParts.Day       => "d",
					Sql.DateParts.Week      => "ww",
					Sql.DateParts.WeekDay   => "w",
					Sql.DateParts.Hour      => "h",
					Sql.DateParts.Minute    => "n",
					Sql.DateParts.Second    => "s",
					_                       => null,
				};

				if (partStr == null)
					return null;

				var resultExpression = factory.Function(factory.GetDbDataType(dateTimeExpression), "DateAdd", factory.Value(partStr), increment, dateTimeExpression);
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
				var factory = translationContext.ExpressionFactory;

				ISqlExpression resultExpression;

				if (hour == null && minute == null && second == null && millisecond == null)
				{
					resultExpression = factory.Function(resulType, "DateSerial", year, month, day);
				}
				else
				{
					if (millisecond != null)
					{
						if (translationContext.TryEvaluate(millisecond, out var msecValue))
						{
							if (msecValue is not int intMsec || intMsec != 0)
								return null;
						}
					}

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

						return factory.Function(stringDataType, "Format",
							expression,
							factory.Function(stringDataType, "String", factory.Value(stringDataType, "0"), factory.Value(intDataType, padSize))
						);
					}

					var yearString  = CastToLength(year, 4);
					var monthString = PartExpression(month, 2);
					var dayString   = PartExpression(day, 2);

					hour          ??= factory.Value(intDataType, 0);
					minute        ??= factory.Value(intDataType, 0);
					second        ??= factory.Value(intDataType, 0);

					resultExpression = factory.Concat(
						yearString, factory.Value(stringDataType, "-"),
						monthString, factory.Value(stringDataType, "-"), dayString, factory.Value(stringDataType, " "),
						PartExpression(hour, 2), factory.Value(stringDataType, ":"),
						PartExpression(minute, 2), factory.Value(stringDataType, ":"),
						PartExpression(second, 2)
					);

					resultExpression = factory.Cast(resultExpression, resulType);
				}

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				var cast    = factory.Cast(dateExpression, new DbDataType(typeof(DateTime), DataType.Date));

				return cast;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToTime(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var factory  = translationContext.ExpressionFactory;
				var timePart = factory.Function(factory.GetDbDataType(typeof(TimeSpan)), "TimeValue", dateExpression);

				return timePart;
			}

			protected override ISqlExpression? TranslateSqlGetDate(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var factory       = translationContext.ExpressionFactory;
				var nowExpression = factory.Fragment(factory.GetDbDataType(typeof(DateTime)), "Now");
				return nowExpression;
			}
		}

		protected override IMemberTranslator CreateSqlTypesTranslator()
		{
			return new SqlTypesTranslation();
		}

		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new DateFunctionsTranslator();
		}
	}
}
