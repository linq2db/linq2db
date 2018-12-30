namespace LinqToDB.DataProvider.SapHana
{
	partial class SapHanaSqlBuilder
	{
		// TABLE_ALIAS(COLUMN_ALIAS, ...) syntax not supported
		protected override bool MergeSupportsColumnAliasesInSource => false;


		// It doesn't make sense to fix empty source generation as it will take too much effort for nothing
		protected override bool MergeEmptySourceSupported => false;
	}
}
