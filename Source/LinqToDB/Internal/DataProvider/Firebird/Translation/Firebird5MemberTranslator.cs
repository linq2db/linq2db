using LinqToDB.Internal.Linq.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Firebird.Translation
{
	public class Firebird5MemberTranslator : FirebirdMemberTranslator
	{
		sealed class Firebird5DateFunctionsTranslator : FirebirdDateFunctionsTranslator
		{
			protected override ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				var factory          = translationContext.ExpressionFactory;
				var intDataType      = factory.GetDbDataType(typeof(int));
				var shortIntDataType = factory.GetDbDataType(typeof(short));

				string partStr;

				switch (datepart)
				{
					case Sql.DateParts.Year:        partStr = "year"; break;
					case Sql.DateParts.Quarter:     partStr = "quarter"; break;
					case Sql.DateParts.Month:       partStr = "month"; break;
					case Sql.DateParts.DayOfYear:   partStr = "yearday"; break;
					case Sql.DateParts.Day:         partStr = "day"; break;
					case Sql.DateParts.Week:        partStr = "week"; break;
					case Sql.DateParts.WeekDay:     partStr = "weekday"; break;
					case Sql.DateParts.Hour:        partStr = "hour"; break;
					case Sql.DateParts.Minute:      partStr = "minute"; break;
					case Sql.DateParts.Second:      partStr = "second"; break;
					case Sql.DateParts.Millisecond: partStr = "millisecond"; break;
					default:
						return null;
				}

				// Cast(Floor(Extract({part} from {date})) as int)

				var extractDbType = shortIntDataType;

				switch (datepart)
				{
					case Sql.DateParts.Second:
						extractDbType = factory.GetDbDataType(typeof(decimal)).WithPrecisionScale(9, 4);
						break;
					case Sql.DateParts.Millisecond:
						extractDbType = factory.GetDbDataType(typeof(decimal)).WithPrecisionScale(9, 1);
						break;
				}

				var resultExpression =
					factory.Function(extractDbType, "Extract", factory.Expression(shortIntDataType, partStr + " from {0}", dateTimeExpression));

				switch (datepart)
				{
					case Sql.DateParts.DayOfYear:
					case Sql.DateParts.WeekDay:
					{
						resultExpression = factory.Increment(resultExpression);
						break;
					}
					case Sql.DateParts.Second:
					case Sql.DateParts.Millisecond:
					{
						resultExpression = factory.Cast(factory.Function(factory.GetDbDataType(typeof(long)), "Floor", resultExpression), intDataType);
						break;
					}
				}

				return resultExpression;
			}
		}

		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new Firebird5DateFunctionsTranslator();
		}
	}
}
