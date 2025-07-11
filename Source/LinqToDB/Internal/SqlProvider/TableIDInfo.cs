namespace LinqToDB.Internal.SqlProvider
{
	public class TableIDInfo
	{
		public TableIDInfo(string tableAlias, string tableName, string tableSpec)
		{
			TableAlias = tableAlias;
			TableName  = tableName;
			TableSpec  = tableSpec;
		}

		public string TableAlias;
		public string TableName;
		public string TableSpec;
	}
}
