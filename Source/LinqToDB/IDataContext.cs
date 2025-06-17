using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;

using JetBrains.Annotations;

using LinqToDB.Common.Internal;
using LinqToDB.Interceptors;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

namespace LinqToDB
{
	/// <summary>
	/// Database connection abstraction interface.
	/// </summary>
	[PublicAPI]
	public interface IDataContext : IConfigurationID, IDisposable, IAsyncDisposable
	{
		/// <summary>
		/// Provider identifier.
		/// </summary>
		string              ContextName           { get; }
		/// <summary>
		/// Gets SQL builder service factory method for current context data provider.
		/// </summary>
		Func<ISqlBuilder>   CreateSqlBuilder     { get; }
		/// <summary>
		/// Gets SQL optimizer service factory method for current context data provider.
		/// </summary>
		Func<DataOptions,ISqlOptimizer> GetSqlOptimizer { get; }
		/// <summary>
		/// Gets SQL support flags for current context data provider.
		/// </summary>
		SqlProviderFlags    SqlProviderFlags      { get; }
		/// <summary>
		/// Gets supported table options for current context data provider.
		/// </summary>
		TableOptions        SupportedTableOptions { get; }
		/// <summary>
		/// Gets data reader implementation type for current context data provider.
		/// </summary>
		Type                DataReaderType        { get; }
		/// <summary>
		/// Gets mapping schema, used for current context.
		/// </summary>
		MappingSchema       MappingSchema         { get; }
		/// <summary>
		/// Gets or sets option to force inline parameter values as literals into command text. If parameter inlining not supported
		/// for specific value type, it will be used as parameter.
		/// </summary>
		bool                InlineParameters      { get; set; }
		/// <summary>
		/// Gets list of query hints (writable collection), that will be used for all queries, executed using current context.
		/// </summary>
		List<string>        QueryHints            { get; }
		/// <summary>
		/// Gets list of query hints (writable collection), that will be used only for next query, executed using current context.
		/// </summary>
		List<string>        NextQueryHints        { get; }
		/// <summary>
		/// Gets or sets flag to close context after query execution or leave it open.
		/// </summary>
		bool                CloseAfterUse         { get; set; }

		/// <summary>
		/// Current DataContext LINQ options
		/// </summary>
		DataOptions         Options               { get; }

		/// <summary>
		/// Returns column value reader expression.
		/// </summary>
		/// <param name="reader">Data reader instance.</param>
		/// <param name="idx">Column index.</param>
		/// <param name="readerExpression">Data reader accessor expression.</param>
		/// <param name="toType">Expected value type.</param>
		/// <returns>Column read expression.</returns>
		Expression          GetReaderExpression(DbDataReader reader, int idx, Expression readerExpression, Type toType);
		/// <summary>
		/// Returns true, of data reader column could contain <see cref="DBNull"/> value.
		/// </summary>
		/// <param name="reader">Data reader instance.</param>
		/// <param name="idx">Column index.</param>
		/// <returns><c>true</c> or <c>null</c> if column could contain <see cref="DBNull"/>.</returns>
		bool?               IsDBNullAllowed    (DbDataReader reader, int idx);

		/// <summary>
		/// Closes context connection and disposes underlying resources.
		/// </summary>
		void                Close              ();

		/// <summary>
		/// Closes context connection and disposes underlying resources.
		/// </summary>
		Task                CloseAsync         ();

		/// <summary>
		/// Returns query runner service for current context.
		/// </summary>
		/// <param name="query">Query batch object.</param>
		/// <param name="parametersContext">Context instance which will be used for parameters evaluation.</param>
		/// <param name="queryNumber">Index of query in query batch.</param>
		/// <param name="expressions">Query results mapping expressions.</param>
		/// <param name="parameters">Query parameters.</param>
		/// <param name="preambles">Query preambles.</param>
		/// <returns>Query runner service.</returns>
		IQueryRunner GetQueryRunner(Query query, IDataContext parametersContext, int queryNumber, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles);

		/// <summary>
		/// Adds interceptor instance to context.
		/// </summary>
		/// <param name="interceptor">Interceptor.</param>
		void AddInterceptor(IInterceptor interceptor);

		/// <summary>
		/// Removes interceptor instance from context.
		/// </summary>
		/// <param name="interceptor">Interceptor.</param>
		void RemoveInterceptor(IInterceptor interceptor);

		/// <summary>
		/// Gets initial value for database connection configuration name.
		/// </summary>
		string?                       ConfigurationString         { get; }

		/// <summary>
		/// Sets new options for current data context.
		/// <para>
		/// <b>Implements the Disposable pattern, which must be used to properly restore previous options.</b>
		/// </para>
		/// </summary>
		/// <remarks>
		/// For ConnectionOptions we reapply only mapping schema and connection interceptor. Connection string, configuration, data provider, etc. are not reapplyable.
		/// </remarks>
		/// <param name="optionsSetter">
		/// Options setter function.
		/// </param>
		/// <returns>
		/// Returns disposable object, which could be used to restore previous options.
		/// </returns>
		/// <exception cref="ArgumentNullException"></exception>
		public IDisposable? UseOptions(Func<DataOptions, DataOptions> optionsSetter);

		/// <summary>
		/// Sets new mapping schema for current data context.
		/// <para>
		/// <b>Implements the Disposable pattern, which must be used to properly restore previous settings.</b>
		/// </para>
		/// </summary>
		/// <param name="mappingSchema">Mapping schema to set.</param>
		/// <returns></returns>
		public IDisposable? UseMappingSchema(MappingSchema mappingSchema);

		/// <summary>
		/// Adds mapping schema to current context.
		/// </summary>
		/// <param name="mappingSchema">Mapping schema to add.</param>
		void AddMappingSchema(MappingSchema mappingSchema);

		/// <summary>
		/// Sets the current <see cref="MappingSchema"/> instance for the context.
		/// <para>
		/// <b>Note:</b> This method ultimately replaces the current mapping schema and should only be used
		/// if you need to create a new schema based on the existing one, or you are absolutely sure you know what you are doing.
		/// </para>
		/// </summary>
		/// <param name="mappingSchema">Mapping schema to set.</param>
		void SetMappingSchema(MappingSchema mappingSchema);
	}
}
