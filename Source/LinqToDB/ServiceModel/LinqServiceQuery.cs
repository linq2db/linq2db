#if NETFRAMEWORK
using System.Collections.Generic;

namespace LinqToDB.ServiceModel
{
	using SqlQuery;

	public class LinqServiceQuery
	{
		public SqlStatement   Statement  { get; set; } = null!;
		public List<string>?  QueryHints { get; set; }
	}
}
#endif
