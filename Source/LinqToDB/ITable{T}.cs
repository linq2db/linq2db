using JetBrains.Annotations;

using LinqToDB.Internal.Linq;

namespace LinqToDB
{
	/// <summary>
	/// Table-like query root for LINQ query construction (table, view, or table-valued function).
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="ITable{T}"/> represents a mapped table-like source and participates
	/// in LINQ query construction. It does not execute queries by itself.
	/// </para>
	/// <para>
	/// <see cref="ITable{T}"/> is a query root. Query execution occurs only when the composed query
	/// is enumerated or explicitly materialized via an <see cref="IDataContext"/>.
	/// </para>
	/// <para>
	/// Queries composed over <see cref="ITable{T}"/> are translated by the configured
	/// <see cref="IDataContext"/> (Expression Tree → internal SQL AST → provider-specific SQL text).
	/// </para>
	/// <para>
	/// The underlying table source may be a physical table, a view, or a table-valued function.
	/// This distinction is expressed by mapping/provider configuration and affects translation.
	/// </para>
	/// </remarks>
	[PublicAPI]
	public interface ITable<out T> : IExpressionQuery<T>
		// TODO: IT: Review in v6, it should be 'class'.
		where T : notnull
	{
		/// <summary>
		/// Logical server name component of the mapped table source, if specified.
		/// </summary>
		/// <remarks>
		/// May originate from mapping configuration. Interpreted by the active provider during SQL generation.
		/// </remarks>
		string? ServerName { get; }

		/// <summary>
		/// Logical database name component of the mapped table source, if specified.
		/// </summary>
		/// <remarks>
		/// May originate from mapping configuration. Interpreted by the active provider during SQL generation.
		/// </remarks>
		string? DatabaseName { get; }

		/// <summary>
		/// Logical schema name component of the mapped table source, if specified.
		/// </summary>
		/// <remarks>
		/// May originate from mapping configuration. Interpreted by the active provider during SQL generation.
		/// </remarks>
		string? SchemaName { get; }

		/// <summary>
		/// Logical table name used during SQL generation.
		/// </summary>
		/// <remarks>
		/// May originate from mapping configuration and is interpreted by the active provider.
		/// It does not necessarily correspond to a physical database object name without provider-specific context.
		/// </remarks>
		string TableName { get; }

		/// <summary>
		/// Table-level translation flags affecting SQL generation and DDL behavior.
		/// </summary>
		/// <remarks>
		/// Interpreted by the configured provider. Translation-time only.
		/// </remarks>
		TableOptions TableOptions { get; }

		/// <summary>
		/// Optional logical identifier for this table source within a query.
		/// </summary>
		/// <remarks>
		/// Used with <see cref="LinqExtensions.TableID{T}(ITable{T}, string?)"/> to reference a table
		/// in SQL generation via <see cref="Sql.TableAlias(string)"/>, <see cref="Sql.TableName(string)"/>,
		/// or <see cref="Sql.TableSpec(string)"/> (e.g., in provider-specific hints).
		/// This identifier exists only within the translation scope of the query and is not a database object identifier.
		/// </remarks>
		string? TableID { get; }
	}
}
