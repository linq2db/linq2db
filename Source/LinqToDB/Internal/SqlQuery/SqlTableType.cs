namespace LinqToDB.Internal.SqlQuery
{
	public enum SqlTableType
	{
		Table = 0,
		/// <summary>
		/// Special context-specific table with fixed name.
		/// E.g. NEW/OLD, INSERTED/DELETED tables, available in OUTPUT/RETURNING clause contexts.
		/// </summary>
		SystemTable,
		Function,
		Expression,
		Cte,
		RawSql,
		MergeSource,
		Values,
	}
}
