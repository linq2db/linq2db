using LinqToDB;

namespace Tests
{
	public sealed class ThrowsCannotBeConvertedAttribute : ThrowsForProviderAttribute
	{
		public ThrowsCannotBeConvertedAttribute(params string[] providers)
			: base(typeof(LinqToDBException), providers)
		{
			ErrorMessage = "The LINQ expression could not be converted to SQL.";
		}
	}
}
