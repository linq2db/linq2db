using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.SqlServer.Translation
{
	public class SqlServer2008MemberTranslator : SqlServer2005MemberTranslator
	{
		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new SqlServer2008DateFunctionsTranslator();
		}

		protected class SqlServer2008DateFunctionsTranslator : SqlServer2005DateFunctionsTranslator
		{
			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				var cast    = factory.Cast(dateExpression, factory.GetDbDataType(dateExpression).WithDataType(DataType.Date), true);

				return cast;
			}

			protected override ISqlExpression? TranslateZonedNow(ITranslationContext translationContext, DbDataType dbDataType, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				return factory.Function(dbDataType, "SYSDATETIMEOFFSET");
			}

			protected override ISqlExpression? TranslateZonedUtcNow(ITranslationContext translationContext, DbDataType dbDataType, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				// Cast to datetimeoffset uses 00:00 timezone by default
				// Better syntax AT TIME ZONE 'UTC' only available in 2016+
				return factory.NotNullExpression(dbDataType, "CAST(SYSUTCDATETIME() AS datetimeoffset)");
			}
		}
	}
}
