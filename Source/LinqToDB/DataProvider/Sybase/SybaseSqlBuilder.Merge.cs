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
	}
}
