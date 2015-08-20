using System;
using System.Collections.Generic;

namespace LinqToDB.Linq
{
	using SqlQuery;

	public interface IQueryContextOld
	{
		SelectQuery    SelectQuery { get; }
		object         Context     { get; set; }
		List<string>   QueryHints  { get; }
		SqlParameter[] GetParameters();
	}
}
