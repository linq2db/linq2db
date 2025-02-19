using LinqToDB.Data;

namespace LinqToDB.Internal.DataProvider.Oracle
{
	/// <summary>
	/// Defines type of multi-row INSERT operation to generate for <see cref="BulkCopyType.RowByRow"/> bulk copy mode.
	/// </summary>
	public enum AlternativeBulkCopy
	{
		/// <summary>
		/// This mode generates INSERT ALL statement.
		/// Note that INSERT ALL doesn't support sequences and will use single generated value for all rows.
		/// <code>
		/// INSERT ALL
		///     INTO target_table VALUES(/*row data*/)
		///     ...
		///     INTO target_table VALUES(/*row data*/)
		/// </code>
		/// </summary>
		InsertAll,
		/// <summary>
		/// This mode performs regular INSERT INTO query with array of values for each column.
		/// <code>
		/// INSERT INTO target_table(/*columns*/)
		///     VALUES(:column1ArrayParameter, ..., :columnXArrayParameter)
		/// </code>
		/// </summary>
		InsertInto,
		/// <summary>
		/// This mode generates INSERT ... SELECT statement.
		/// <code>
		/// INSERT INTO target_table(/*columns*/)
		///     SELECT /*row data*/ FROM DUAL
		///     UNION ALL
		///     ...
		///     UNION ALL
		///     SELECT /*row data*/ FROM DUAL
		/// </code>
		/// </summary>
		InsertDual,
	}
}
