using LinqToDB;

namespace Tests
{
	public sealed class ThrowsCannotBeConvertedAttribute : ThrowsForProviderAttribute
	{
		public ThrowsCannotBeConvertedAttribute(params string[] providers)
			: base(typeof(LinqToDBException), providers)
		{
			// Matches both message formats produced by SqlErrorExpression.CreateException:
			//   multi-line:  "The LINQ expression could not be converted to SQL.\nExpression:\n..."
			//   single-line: "The LINQ expression '<expr>' could not be converted to SQL."
			ErrorMessage = "could not be converted to SQL.";
		}
	}
}
