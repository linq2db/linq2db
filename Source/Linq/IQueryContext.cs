using System;

namespace LinqToDB.Linq
{
	using SqlQuery;

	public interface IQueryContext
	{
		SelectQuery    SelectQuery { get; }
		object         Context     { get; set; }
		SqlParameter[] GetParameters();
	}
}
