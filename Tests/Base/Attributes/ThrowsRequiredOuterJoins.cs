using LinqToDB;
using LinqToDB.Internal.Common;

namespace Tests
{
	public sealed class ThrowsRequiredOuterJoins : ThrowsForProviderAttribute
	{
		public ThrowsRequiredOuterJoins(params string[] providers)
			: base(typeof(LinqToDBException), providers)
		{
			ErrorMessage = ErrorHelper.Error_OUTER_Joins;
		}
	}
}
