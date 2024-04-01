using System;
using System.Linq.Expressions;

using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Informix.Translation
{
	public class InformixMemberTranslator : ProviderMemberTranslatorDefault
	{
		class SqlTypesTranslation : SqlTypesTranslationDefault
		{
			protected override Expression? ConvertBit(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Boolean));

			protected override Expression? ConvertTinyInt(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Int16));

			protected override Expression? ConvertMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(19, 4));

			protected override Expression? ConvertSmallMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(10, 4));

		}

		public class DateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				// EXTEND(your_datetime_column, YEAR TO DAY)

				var extend = new SqlFunction(dateExpression.SystemType!, "EXTEND", dateExpression, new SqlExpression("YEAR TO DAY"));

				return extend;
			}

			protected override ISqlExpression? TranslateSqlGetDate(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var factory          = translationContext.ExpressionFactory;
				var currentTimeStamp = factory.Fragment(factory.GetDbDataType(typeof(DateTime)), "CURRENT");
				return currentTimeStamp;
			}
		}

		protected override IMemberTranslator CreateSqlTypesTranslator()
		{
			return new SqlTypesTranslation();
		}

		protected override IMemberTranslator CreateDateFunctionsTranslator()
		{
			return new DateFunctionsTranslator();
		}

	}
}
