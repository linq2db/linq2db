using System.Linq.Expressions;

namespace LinqToDB.DataProvider.SqlServer.Translation
{
	using Common;
	using Linq.Translation;
	using SqlQuery;

	public class SqlServer2005MemberTranslator : SqlServerMemberTranslator
	{
		class SqlTypes2005Translation : SqlTypesTranslation
		{
			protected override Expression? ConvertDate(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime));
		}

		class DateFunctionsTranslator2005 : SqlServerDateFunctionsTranslator
		{
			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				// DATEADD(dd, DATEDIFF(dd, 0, YourDateTimeColumn), 0)

				var factory = translationContext.ExpressionFactory;

				var intDataType = factory.GetDbDataType(typeof(int));
				var dateType = factory.GetDbDataType(dateExpression);

				var datePart = factory.Fragment(DbDataType.Undefined, "dd");
				var dateDiff = factory.Function(intDataType, "DateDiff", datePart, factory.Value(intDataType, 0), dateExpression);
				var dateAdd  = factory.Function(dateType, "DateAdd", datePart, dateDiff, factory.Value(intDataType, 0));

				return dateAdd;
			}
		}

		protected override IMemberTranslator CreateSqlTypesTranslator()
		{
			return new SqlTypes2005Translation();
		}

		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new DateFunctionsTranslator2005();
		}
	}
}
