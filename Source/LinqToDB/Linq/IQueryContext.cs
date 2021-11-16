﻿namespace LinqToDB.Linq
{
	using SqlQuery;

	public interface IQueryContext
	{
		SqlStatement                 Statement   { get; }
		object?                      Context     { get; set; }

		SqlParameter[]? Parameters  { get; set; }
		AliasesContext? Aliases     { get; set; }
	}
}
