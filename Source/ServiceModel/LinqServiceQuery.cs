using System;

namespace LinqToDB.ServiceModel
{
	using SqlBuilder;

	public class LinqServiceQuery
	{
		public SqlQuery       Query      { get; set; }
		public SqlParameter[] Parameters { get; set; }
	}
}
