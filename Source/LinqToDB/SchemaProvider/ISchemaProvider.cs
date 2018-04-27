using System;

namespace LinqToDB.SchemaProvider
{
	using Data;

	/// <summary>
	/// Database schema provider.
	/// </summary>
	public interface ISchemaProvider
	{
		/// <summary>
		/// Returns database schema.
		/// Note that it is recommended to call this method outside of transaction as some providers do not support it
		/// or behave incorrectly.
		/// At least following providers shouldn't be called in transaction:
		/// - MySQL;
		/// - Microsoft SQL Server;
		/// - Sybase;
		/// - DB2.
		/// </summary>
		/// <param name="dataConnection">Data connection to use to read schema from.</param>
		/// <param name="options">Schema read configuration options.</param>
		/// <returns>Returns database schema information.</returns>
		DatabaseSchema GetSchema(DataConnection dataConnection, GetSchemaOptions options = null);
	}
}
