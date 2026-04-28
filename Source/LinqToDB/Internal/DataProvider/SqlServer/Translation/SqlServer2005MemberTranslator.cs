using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

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
			return new DateFunctionsTranslator2005();
		}

		protected class SqlTypes2005Translation : SqlTypesTranslation
		{
			protected override Expression? ConvertDate(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime));
		}

		protected class DateFunctionsTranslator2005 : SqlServerDateFunctionsTranslator
		{
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

			protected override ISqlExpression? TranslateUtcNow(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				var dbDataType = factory.GetDbDataType(typeof(DateTime));
				return factory.NotNullExpression(dbDataType, "GETUTCDATE()");	// Higher-precision [datetime2] SYSUTCDATETIME() only available in 2008+
			}
		}
	}
}
