using LinqToDB;
using LinqToDB.Internal.Common;

namespace Tests
{
	public sealed class ThrowsRequiredOuterJoinsAttribute : ThrowsForProviderAttribute
	{
		public ThrowsRequiredOuterJoinsAttribute(params string[] providers)
			: base(typeof(LinqToDBException), providers)
		{
			ErrorMessage = ErrorHelper.Error_OUTER_Joins;
		}
	}
}
