using System;

using LinqToDB.Sql;

namespace LinqToDB.ServiceModel
{
	public class LinqServiceQuery
	{
		public SqlQuery       Query      { get; set; }
		public SqlParameter[] Parameters { get; set; }
	}
}
