using LinqToDB.Internal.SqlQuery;

namespace LinqToDB
{
	/// <summary>
	/// SQL generation options for <see cref="LinqExtensions.ToSqlQuery{T}(System.Linq.IQueryable{T}, SqlGenerationOptions?)"/> group of APIs.
	/// </summary>
	public sealed class SqlGenerationOptions
	{
		/// <summary>
		/// Enforce parameters inlining into SQL as literals.
		/// When not set, current context settings used.
		/// </summary>
		public bool? InlineParameters { get; set; }

		/// <summary>
		/// INSERT ALL mode for Oracle-specific multi-insert API.
		/// </summary>
		public MultiInsertType? MultiInsertMode { get; set; }
	}
}
