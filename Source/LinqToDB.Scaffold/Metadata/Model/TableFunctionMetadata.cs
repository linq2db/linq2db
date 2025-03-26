using LinqToDB.SqlQuery;

namespace LinqToDB.Metadata
{
	/// <summary>
	/// Table function mapping attributes, used with <see cref="Sql.TableFunctionAttribute"/> mapping attribute.
	/// </summary>
	public sealed class TableFunctionMetadata
	{
		/// <summary>
		/// Function name.
		/// </summary>
		public SqlObjectName? Name          { get; set; }
		/// <summary>
		/// Mapping configuration name.
		/// </summary>
		public string?        Configuration { get; set; }
		/// <summary>
		/// Contains indexes of mapped method parameters, that should be mapped to table function parameter with position
		/// matching position of index in array.
		/// </summary>
		public int[]?         ArgIndices    { get; set; }
	}
}
