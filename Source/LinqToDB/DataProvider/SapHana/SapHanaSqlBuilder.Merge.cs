namespace LinqToDB.DataProvider.SapHana
{
	partial class SapHanaSqlBuilder
	{
		// TABLE_ALIAS(COLUMN_ALIAS, ...) syntax not supported
		protected override bool MergeSupportsColumnAliasesInSource => false;
	}
}
