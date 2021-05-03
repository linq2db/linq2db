namespace LinqToDB.DataProvider.Access
{
	class AccessBulkCopy : BasicBulkCopy
	{
		protected override int MaxParameters => 767;
		protected override int MaxSqlLength  => 64000;
	}
}
