namespace LinqToDB.Internal.DataProvider.SapHana
{
	public partial class SapHanaSqlBuilder
	{
		// TABLE_ALIAS(COLUMN_ALIAS, ...) syntax not supported
		protected override bool SupportsColumnAliasesInSource => false;

		// VALUES(...) syntax not supported
		protected override bool IsValuesSyntaxSupported => false;

		// unfortunatelly, user could change this table
		protected override string FakeTable => "DUMMY";
	}
}
