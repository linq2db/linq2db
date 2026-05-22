using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Firebird.Translation
{
	public class Firebird4MemberTranslator : FirebirdMemberTranslator
	{
		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new Firebird4DateFunctionsTranslator();
		}

		protected override IMemberTranslator? CreateWindowFunctionsMemberTranslator()
		{
			return new Firebird4WindowFunctionsMemberTranslator();
		}

		protected class Firebird4DateFunctionsTranslator : FirebirdDateFunctionsTranslator
		{
			protected override ISqlExpression? TranslateServerNow(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				var dbDataType = factory.GetDbDataType(typeof(DateTime));
				return factory.NotNullExpression(dbDataType, "CURRENT_TIMESTAMP");
			}

			protected override ISqlExpression? TranslateUtcNow(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				var dbDataType = factory.GetDbDataType(typeof(DateTime));
				return factory.NotNullExpression(dbDataType, "CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");
			}

			protected override ISqlExpression? TranslateZonedUtcNow(ITranslationContext translationContext, DbDataType dbDataType, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;
				return factory.NotNullExpression(dbDataType, "CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");
			}
		}

		protected class Firebird4WindowFunctionsMemberTranslator : WindowFunctionsMemberTranslator
		{
			protected override bool IsNullsOrderSupported     => true;
			protected override bool IsPercentRankSupported    => false;
			protected override bool IsCumeDistSupported       => false;
			protected override bool IsNTileSupported          => false;
			protected override bool IsNthValueSupported       => false;
			protected override bool IsFrameGroupsSupported    => false;
			protected override bool IsFrameExclusionSupported => false;
			protected override bool IsPercentileContSupported => false;
			protected override bool IsPercentileDiscSupported => false;
		}
	}
}
