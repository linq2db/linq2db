using LinqToDB.Schema;
using LinqToDB.SqlQuery;

namespace LinqToDB.Metadata
{
	/// <summary>
	/// Entity mapping attributes for table or view.
	/// </summary>
	public sealed class EntityMetadata
	{
		/// <summary>
		/// Table name.
		/// </summary>
		public SqlObjectName?  Name                      { get; set; }
		/// <summary>
		/// View or table mapping.
		/// </summary>
		public bool            IsView                    { get; set; }
		/// <summary>
		/// Mapping configuration name.
		/// </summary>
		public string?         Configuration             { get; set; }
		/// <summary>
		/// If <c>true</c>, only properties/fields with <see cref="Mapping.ColumnAttribute"/> will be mapped.
		/// Default value: <c>true</c>.
		/// </summary>
		public bool            IsColumnAttributeRequired { get; set; } = true;
		/// <summary>
		/// When <c>true</c>, mapped table is temporary table.
		/// </summary>
		public bool            IsTemporary               { get; set; }
		/// <summary>
		/// Specify table flags for temporary tables and create/drop table API behavior.
		/// </summary>
		public TableOptions    TableOptions              { get; set; }
	}
}
