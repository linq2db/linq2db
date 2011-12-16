using System;

namespace LinqToDB.Data.Linq
{
	using SqlBuilder;

	public interface IQueryContext
	{
		SqlQuery       SqlQuery { get; }
		object         Context  { get; set; }
		SqlParameter[] GetParameters();
	}
}
