using System;

namespace LinqToDB.Data.Linq
{
	using Data.Sql;

	public interface IQueryContext
	{
		SqlQuery       SqlQuery { get; }
		object         Context  { get; set; }
		SqlParameter[] GetParameters();
	}
}
