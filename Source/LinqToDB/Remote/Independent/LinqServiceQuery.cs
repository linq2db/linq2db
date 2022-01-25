using System.Collections.Generic;

namespace LinqToDB.Remote.Independent
{
	using SqlQuery;

	public class LinqServiceQuery
	{
		public SqlStatement                 Statement  { get; set; } = null!;
		public IReadOnlyCollection<string>? QueryHints { get; set; }
	}
}
