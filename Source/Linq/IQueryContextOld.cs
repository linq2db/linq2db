using System;

namespace LinqToDB.Linq
{
	using SqlQuery;

	public interface IQueryContextOld
	{
		SelectQuery    SelectQuery { get; }
		object         Context     { get; set; }
		SqlParameter[] GetParameters();
	}
}
