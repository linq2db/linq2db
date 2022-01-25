using System.Collections.Generic;

namespace LinqToDB.ServiceModel
{
	using SqlQuery;

	public class LinqServiceQuery
	{
		public SqlStatement                 Statement  { get; set; } = null!;
		public IReadOnlyCollection<string>? QueryHints { get; set; }
	}
}
