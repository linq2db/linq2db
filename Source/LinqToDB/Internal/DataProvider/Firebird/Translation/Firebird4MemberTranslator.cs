using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Firebird.Translation
{
	public class Firebird4MemberTranslator : FirebirdMemberTranslator
	{
		protected class Firebird4WindowFunctionsMemberTranslator : WindowFunctionsMemberTranslator
		{
			protected override bool IsPercentRankSupported    => false;
			protected override bool IsCumeDistSupported       => false;
			protected override bool IsNTileSupported          => false;
			protected override bool IsNthValueSupported       => false;
			protected override bool IsFrameGroupsSupported    => false;
			protected override bool IsFrameExclusionSupported => false;
			protected override bool IsPercentileContSupported => false;
			protected override bool IsPercentileDiscSupported => false;
		}

		protected override IMemberTranslator? CreateWindowFunctionsMemberTranslator()
		{
			return new Firebird4WindowFunctionsMemberTranslator();
		}
	}
}
