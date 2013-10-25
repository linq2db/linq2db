using System;

namespace LinqToDB.ServiceModel
{
	using SqlQuery;

	public class LinqServiceQuery
	{
		public SelectQuery    Query      { get; set; }
		public SqlParameter[] Parameters { get; set; }
	}
}
