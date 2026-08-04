namespace LinqToDB
{
	public partial class Sql
	{
		/// <summary>
		/// Specifies which generated SQL identifier should be resolved for a table source registered
		/// with <see cref="LinqExtensions.TableID{T}(ITable{T}, string?)"/>.
		/// </summary>
		/// <remarks>
		/// LinqToDB can generate provider-specific table aliases and table names during SQL translation.
		/// Use <c>Sql.TableAlias</c>, <c>Sql.TableName</c>, or <c>Sql.TableSpec</c> to create a
		/// <c>Sql.SqlID</c> with the corresponding <c>Sql.SqlIDType</c> when a hint or custom SQL extension needs the exact identifier
		/// emitted by the SQL builder.
		/// </remarks>
		public enum SqlIDType
		{
			/// <summary>
			/// Resolve the generated SQL table alias for the table source.
			/// </summary>
			TableAlias,

			/// <summary>
			/// Resolve the generated SQL table name for the table source.
			/// </summary>
			TableName,

			/// <summary>
			/// Resolve the generated SQL table specification/path for the table source.
			/// </summary>
			TableSpec,
		}
	}
}
