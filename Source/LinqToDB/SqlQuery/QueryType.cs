using System;

namespace LinqToDB.SqlQuery
{
	public enum QueryType
	{
		Select,
		Delete,
		Update,
		Insert,
		InsertOrUpdate,
		CreateTable,
		DropTable,
		TruncateTable,
	}
}
