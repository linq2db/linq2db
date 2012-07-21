using System;

namespace LinqToDB.Linq
{
	using SqlBuilder;

	public interface IQueryContext
	{
		SqlQuery       SqlQuery { get; }
		object         Context  { get; set; }
		SqlParameter[] GetParameters();
	}
}
