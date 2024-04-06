using System;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.SqlServer.Translation
{
	using LinqToDB.Linq.Translation;

	public class SqlServer2005MemberTranslator : SqlServerMemberTranslator
	{
		class SqlTypes2005Translation : SqlTypesTranslation
		{
			protected override Expression? ConvertDate(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime));
		}

		class DateFunctionsTranslator2005 : SqlServerDateFunctionsTranslator
		{
		}

		protected override IMemberTranslator CreateSqlTypesTranslator()
		{
			return new SqlTypes2005Translation();
		}

		protected override IMemberTranslator CreateDateFunctionsTranslator()
		{
			return new DateFunctionsTranslator2005();
		}
	}
}
