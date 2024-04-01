using System;
using System.Linq.Expressions;

using LinqToDB.Common;
using LinqToDB.DataProvider.ClickHouse.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.DB2.Translation
{
	using LinqToDB.Linq.Translation;

	public class DB2MemberTranslator : ProviderMemberTranslatorDefault
	{
		class SqlTypesTranslation : SqlTypesTranslationDefault
		{
			protected override Expression? ConvertDefaultNChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			{
				var dbDataType = translationContext.MappingSchema.GetDbDataType(typeof(char));

				dbDataType = dbDataType.WithDataType(DataType.Char).WithSystemType(typeof(string)).WithLength(null);

				return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, new SqlValue(dbDataType, ""), memberExpression);
			}

			protected override Expression? ConvertNVarChar(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			{
				if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
					return null;

				return MakeSqlTypeExpression(translationContext, methodCall, typeof(string), t => t.WithLength(length).WithDataType(DataType.Char));
			}

			protected override Expression? ConvertDefaultNVarChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			{
				var dbDataType = translationContext.MappingSchema.GetDbDataType(typeof(string));

				dbDataType = dbDataType.WithDataType(DataType.Char);

				return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, new SqlValue(dbDataType, ""), memberExpression);
			}

		}

		public class DateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var dateFunction = new SqlFunction(dateExpression.SystemType!, "DATE", dateExpression);

				return dateFunction;
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
