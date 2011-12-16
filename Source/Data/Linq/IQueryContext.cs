using System;

using LinqToDB.Sql;

namespace LinqToDB.Data.Linq
{
	public interface IQueryContext
	{
		SqlQuery       SqlQuery { get; }
		object         Context  { get; set; }
		SqlParameter[] GetParameters();
	}
}
