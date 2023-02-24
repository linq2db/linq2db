using System.Reflection;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	interface ITableContext : IBuildContext
	{
		public Type     ObjectType { get; }
		public SqlTable SqlTable { get; }

		public LoadWithInfo  LoadWithRoot { get; set; }
		public MemberInfo[]? LoadWithPath { get; set; }
	}
}
