using System.Collections.Generic;

namespace LinqToDB.Remote
{
	using SqlQuery;

	public class LinqServiceQuery
	{
		public SqlStatement                 Statement   { get; set; } = null!;
		public IReadOnlyCollection<string>? QueryHints  { get; set; }
		public DataOptions                  DataOptions { get; set; } = null!;
	}
}
