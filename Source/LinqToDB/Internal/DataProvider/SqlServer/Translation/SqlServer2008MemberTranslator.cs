using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.SqlServer.Translation
{
	public class SqlServer2008MemberTranslator : SqlServer2005MemberTranslator
	{
		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new SqlServer2008DateFunctionsTranslator();
		}

		protected override IMemberTranslator CreateSqlTypesTranslator()
		{
			return new SqlTypes2008Translation();
		}

		protected class SqlTypes2008Translation : SqlTypes2005Translation
		{
			protected override Expression? ConvertDate(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Date));
		}

		protected class SqlServer2008DateFunctionsTranslator : SqlServer2005DateFunctionsTranslator
		{
			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				var cast    = factory.Cast(dateExpression, factory.GetDbDataType(dateExpression).WithDataType(DataType.Date), true);

				return cast;
			}

			protected override ISqlExpression TranslateUtcNow(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				var dbDataType = factory.GetDbDataType(typeof(DateTime));
				return factory.Function(dbDataType, "SYSUTCDATETIME");
			}

			protected override ISqlExpression? TranslateZonedUtcNow(ITranslationContext translationContext, DbDataType dbDataType, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				// Cast to datetimeoffset uses 00:00 timezone by default
				// Better syntax AT TIME ZONE 'UTC' only available in 2016+
				return factory.Cast(TranslateUtcNow(translationContext, translationFlags), dbDataType);
			}
		}
	}
}
