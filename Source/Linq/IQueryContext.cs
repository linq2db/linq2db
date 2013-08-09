using System;

namespace LinqToDB.Linq
{
	using SqlBuilder;

	public interface IQueryContext
	{
		SelectQuery    SelectQuery { get; }
		object         Context     { get; set; }
		SqlParameter[] GetParameters();
	}
}
