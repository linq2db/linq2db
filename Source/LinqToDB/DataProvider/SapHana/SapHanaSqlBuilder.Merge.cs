namespace LinqToDB.DataProvider.SapHana
{
	partial class SapHanaSqlBuilder
	{
		// TABLE_ALIAS(COLUMN_ALIAS, ...) syntax not supported
		protected override bool MergeSupportsColumnAliasesInSource => false;

		// VALUES(...) syntax not supported in MERGE source
		protected override bool MergeSupportsSourceDirectValues => false;

		// unfortunatelly, user could change this table
		protected override string FakeTable => "DUMMY";
	}
}
