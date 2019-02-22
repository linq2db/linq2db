using System;
using System.Collections.Generic;

namespace LinqToDB.ServiceModel
{
	using SqlQuery;

	public class LinqServiceQuery
	{
		public SqlStatement   Statement  { get; set; }
		public SqlParameter[] Parameters { get; set; }
		public List<string>   QueryHints { get; set; }
	}
}
