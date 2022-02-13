using System;
using System.Collections.Generic;
using LinqToDB.CodeModel;
using LinqToDB.Schema;

namespace LinqToDB.Scaffold
{
	// list of extension points based on:
	// - user requests and PRs to old T4 templates (rsdn forum, bltoolkit and linq2db repositories). Both implemented and not
	// - existing extensibility points in T4 templates that require custom code

	/// <summary>
	/// Base class for scaffold customizations.
	/// Constains virtual methods with no-op implementation for each customization point.
	/// </summary>
	public abstract class ScaffoldInterceptors
	{
		#region Schema
		/// <summary>
		/// Using this method you can add, remove or modify table schema information.
		/// </summary>
		/// <param name="tables">Table schema, provided by schema provider.</param>
		/// <returns>Table schema with interceptor logic applied.</returns>
		public virtual IEnumerable<Table> GetTables(IEnumerable<Table> tables) => tables;

		/// <summary>
		/// Using this method you can add, remove or modify view schema information.
		/// </summary>
		/// <param name="views">View schema, provided by schema provider.</param>
		/// <returns>View schema with interceptor logic applied.</returns>
		public virtual IEnumerable<View> GetViews(IEnumerable<View> views) => views;

		/// <summary>
		/// Using this method you can add, remove or modify foreign key constrains schema information.
		/// </summary>
		/// <param name="keys">Foreign key schema, provided by schema provider.</param>
		/// <returns>Foreign key schema with interceptor logic applied.</returns>
		public virtual IEnumerable<ForeignKey> GetForeignKeys(IEnumerable<ForeignKey> keys) => keys;

		/// <summary>
		/// Using this method you can add, remove or modify stored procedure schema information.
		/// </summary>
		/// <param name="procedures">Stored procedure schema, provided by schema provider.</param>
		/// <returns>Stored procedure schema with interceptor logic applied.</returns>
		public virtual IEnumerable<StoredProcedure> GetProcedures(IEnumerable<StoredProcedure> procedures) => procedures;

		/// <summary>
		/// Using this method you can add, remove or modify table function schema information.
		/// </summary>
		/// <param name="functions">Table function schema, provided by schema provider.</param>
		/// <returns>Table function schema with interceptor logic applied.</returns>
		public virtual IEnumerable<TableFunction> GetTableFunctions(IEnumerable<TableFunction> functions) => functions;

		/// <summary>
		/// Using this method you can add, remove or modify scalar function schema information.
		/// </summary>
		/// <param name="functions">Scalar function schema, provided by schema provider.</param>
		/// <returns>Scalar function schema with interceptor logic applied.</returns>
		public virtual IEnumerable<ScalarFunction> GetScalarFunctions(IEnumerable<ScalarFunction> functions) => functions;

		/// <summary>
		/// Using this method you can add, remove or modify aggregate function schema information.
		/// </summary>
		/// <param name="functions">Aggregate function schema, provided by schema provider.</param>
		/// <returns>Aggregate function schema with interceptor logic applied.</returns>
		public virtual IEnumerable<AggregateFunction> GetAggregateFunctions(IEnumerable<AggregateFunction> functions) => functions;
		#endregion

		#region Type mapping
		/// <summary>
		/// Using this method you can specify which .NET type and <see cref="DataType"/> enum value to use with specific database type.
		/// Method called only once per database type.
		/// <see cref="TypeMapping.CLRType"/> shouldn't be a nullable type, as nullability applied to it later automatically based on owning object (e.g. column or procedure parameter) nullability.
		/// </summary>
		/// <param name="databaseType">Database type specification.</param>
		/// <param name="typeParser">Type parser to create value for <see cref="TypeMapping.CLRType"/> property from <see cref="Type"/> instance or type name string.</param>
		/// <param name="defaultMapping">Default type mapping for specified <paramref name="databaseType"/>.</param>
		/// <returns>Type mapping information for specified <paramref name="databaseType"/>.</returns>
		public virtual TypeMapping GetTypeMapping(DatabaseType databaseType, ITypeParser typeParser, TypeMapping defaultMapping) => defaultMapping;
		#endregion
	}
}
