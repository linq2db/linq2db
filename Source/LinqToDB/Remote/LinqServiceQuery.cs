using System.Collections.Generic;

using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Remote
{
	public class LinqServiceQuery
	{
		public SqlStatement                 Statement   { get; set; } = null!;
		public IReadOnlyCollection<string>? QueryHints  { get; set; }
		public DataOptions                  DataOptions { get; set; } = null!;
	}
}
