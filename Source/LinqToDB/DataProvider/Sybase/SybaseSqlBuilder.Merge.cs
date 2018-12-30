namespace LinqToDB.DataProvider.Sybase
{
	partial class SybaseSqlBuilder
	{
		protected override void BuildMergeTerminator()
		{
			// TODO: move to extra query
			//if (_hasIdentityInsert)
			//	StringBuilder.AppendFormat("SET IDENTITY_INSERT {0} OFF", TargetTableName).AppendLine();
		}

		// It doesn't make sense to fix empty source generation as it will take too much effort for nothing
		protected override bool MergeEmptySourceSupported => false;
	}
}
