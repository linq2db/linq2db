using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.MySql.Translation
{
	// MariaDB shares the MySQL 8 dialect translator but adds the MariaDB-only UUID_v7() generator.
	// linq2db does not version-split MariaDB, so UUID_v7() is emitted for every MariaDB dialect
	// (the function requires MariaDB 11.7+; older MariaDB versions predate practical support).
	public class MariaDBMemberTranslator : MySql80MemberTranslator
	{
		protected override ISqlExpression? TranslateNewGuid7Method(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			var factory  = translationContext.ExpressionFactory;
			var timePart = factory.NonPureFunction(factory.GetDbDataType(typeof(Guid)), "UUID_v7");

			return timePart;
		}
	}
}
