using System;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.SqlQuery;

	interface ITableContext : IBuildContext
	{
		public Type     ObjectType { get; }
		public SqlTable SqlTable { get; }

		public LoadWithInfo  LoadWithRoot { get; set; }
		public MemberInfo[]? LoadWithPath { get; set; }
	}
}
