using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB
{
	using Extensions;
	using Linq;
	using SqlQuery;
	using Common;
	using Expressions;

	/// <summary>
	/// Data context extension methods.
	/// </summary>
	[PublicAPI]
	public static partial class DataExtensions
	{
		#region Table Helpers

		/// <summary>
		/// Returns queryable source for specified mapping class for current connection, mapped to database table or view.
		/// </summary>
		/// <typeparam name="T">Mapping class type.</typeparam>
		/// <param name="dataContext">Data connection context.</param>
		/// <returns>Queryable source.</returns>
		[LinqTunnel]
		[Pure]
		public static ITable<T> GetTable<T>(this IDataContext dataContext)
			where T : class
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

			return new Table<T>(dataContext);
		}

		/// <summary>
		/// Returns queryable source for specified mapping class for current connection, mapped to table expression or function.
		/// It could be used e.g. for queries to table-valued functions or to decorate queried table with hints.
		/// </summary>
		/// <typeparam name="T">Mapping class type.</typeparam>
		/// <param name="dataContext">Data connection context.</param>
		/// <param name="instance">Instance object for <paramref name="methodInfo"/> method or null for static method.</param>
		/// <param name="methodInfo">Method, decorated with expression attribute, based on <see cref="LinqToDB.Sql.TableFunctionAttribute"/>.</param>
		/// <param name="parameters">Parameters for <paramref name="methodInfo"/> method.</param>
		/// <returns>Queryable source.</returns>
		[LinqTunnel]
		[Pure]
		public static ITable<T> GetTable<T>(
			this   IDataContext dataContext,
			       object?      instance,
			       MethodInfo   methodInfo,
			params object?[]    parameters)
			where T : class
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			if (methodInfo  == null) throw new ArgumentNullException(nameof(methodInfo));
			if (parameters  == null) throw new ArgumentNullException(nameof(parameters));

			if (!typeof(ITable<>).IsSameOrParentOf(methodInfo.ReturnType))
				throw new LinqException(
					"Method '{0}.{1}' must return type 'ITable<{2}>'",
					methodInfo.Name, methodInfo.DeclaringType!.FullName, typeof(T).FullName);

			Expression expr;

			if (parameters.Length > 0)
			{
				var pis  = methodInfo.GetParameters();
				var args = new List<Expression>(parameters.Length);

				for (var i = 0; i < parameters.Length; i++)
				{
					var type = pis[i].ParameterType;
					args.Add(Expression.Constant(parameters[i], (type.IsByRef ? type.GetElementType() : type)!));
				}

				expr = Expression.Call(instance == null ? null : Expression.Constant(instance), methodInfo, args);
			}
			else
				expr = Expression.Call(instance == null ? null : Expression.Constant(instance), methodInfo);

			return new Table<T>(dataContext, expr);
		}

		#endregion

		#region Compile

		/// <summary>
		/// Compiles the query.
		/// </summary>
		/// <returns>
		/// A generic delegate that represents the compiled query.
		/// </returns>
		/// <param name="dataContext">Data connection context.</param>
		/// <param name="query">The query expression to be compiled.</param>
		/// <typeparam name="TDc">Type of data context parameter, passed to compiled query.</typeparam>
		/// <typeparam name="TResult">Query result type.</typeparam>
		public static Func<TDc,TResult> Compile<TDc,TResult>(
			this IDataContext             dataContext,
			Expression<Func<TDc,TResult>> query)
			where TDc : IDataContext
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			if (query       == null) throw new ArgumentNullException(nameof(query));

			return CompiledQuery.Compile(query);
		}

		/// <summary>
		/// Compiles the query with parameter.
		/// </summary>
		/// <returns>
		/// A generic delegate that represents the compiled query.
		/// </returns>
		/// <param name="dataContext">Data connection context.</param>
		/// <param name="query">The query expression to be compiled.</param>
		/// <typeparam name="TDc">Type of data context parameter, passed to compiled query.</typeparam>
		/// <typeparam name="TArg1">Type of parameter for compiled query.</typeparam>
		/// <typeparam name="TResult">Query result type.</typeparam>
		public static Func<TDc,TArg1,TResult> Compile<TDc,TArg1, TResult>(
			this IDataContext dataContext,
			Expression<Func<TDc,TArg1,TResult>> query)
			where TDc : IDataContext
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			if (query       == null) throw new ArgumentNullException(nameof(query));

			return CompiledQuery.Compile(query);
		}

		/// <summary>
		/// Compiles the query with two parameters.
		/// </summary>
		/// <returns>
		/// A generic delegate that represents the compiled query.
		/// </returns>
		/// <param name="dataContext">Data connection context.</param>
		/// <param name="query">The query expression to be compiled.</param>
		/// <typeparam name="TDc">Type of data context parameter, passed to compiled query.</typeparam>
		/// <typeparam name="TArg1">Type of first parameter for compiled query.</typeparam>
		/// <typeparam name="TArg2">Type of second parameter for compiled query.</typeparam>
		/// <typeparam name="TResult">Query result type.</typeparam>
		public static Func<TDc,TArg1,TArg2,TResult> Compile<TDc,TArg1,TArg2,TResult>(
			this IDataContext dataContext,
			Expression<Func<TDc,TArg1,TArg2,TResult>> query)
			where TDc : IDataContext
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			if (query       == null) throw new ArgumentNullException(nameof(query));

			return CompiledQuery.Compile(query);
		}

		/// <summary>
		/// Compiles the query with three parameters.
		/// </summary>
		/// <returns>
		/// A generic delegate that represents the compiled query.
		/// </returns>
		/// <param name="dataContext">Data connection context.</param>
		/// <param name="query">The query expression to be compiled.</param>
		/// <typeparam name="TDc">Type of data context parameter, passed to compiled query.</typeparam>
		/// <typeparam name="TArg1">Type of first parameter for compiled query.</typeparam>
		/// <typeparam name="TArg2">Type of second parameter for compiled query.</typeparam>
		/// <typeparam name="TArg3">Type of third parameter for compiled query.</typeparam>
		/// <typeparam name="TResult">Query result type.</typeparam>
		public static Func<TDc,TArg1,TArg2,TArg3,TResult> Compile<TDc,TArg1,TArg2,TArg3,TResult>(
			this IDataContext dataContext,
			Expression<Func<TDc,TArg1,TArg2,TArg3,TResult>> query)
			where TDc : IDataContext
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			if (query       == null) throw new ArgumentNullException(nameof(query));

			return CompiledQuery.Compile(query);
		}

		#endregion

		#region Insert

		/// <summary>
		/// Inserts record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Number of affected records.</returns>
		public static int Insert<T>(this IDataContext dataContext, T obj,
			string? tableName = default, string? databaseName = default, string? schemaName = default, string? serverName = default, TableOptions tableOptions = default)
		{
			return Insert(dataContext, obj, null, tableName: tableName, databaseName: databaseName, schemaName: schemaName, serverName: serverName, tableOptions: tableOptions);
		}

		/// <summary>
		/// Inserts record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <param name="columnFilter">Filter columns to insert.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Number of affected records.</returns>
		public static int Insert<T>(this IDataContext dataContext, T obj, InsertColumnFilter<T>? columnFilter,
			string? tableName = default, string? databaseName = default, string? schemaName = default, string? serverName = default, TableOptions tableOptions = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

			return QueryRunner.Insert<T>.Query(dataContext, obj, columnFilter, tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, tableOptions: tableOptions);
		}

		/// <summary>
		/// Inserts record asynchronously into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> InsertAsync<T>(
			this IDataContext dataContext,
			T                 obj,
			string?           tableName    = default,
			string?           databaseName = default,
			string?           schemaName   = default,
			string?           serverName   = default,
			TableOptions      tableOptions = default,
			CancellationToken token        = default)
		{
			return InsertAsync(dataContext, obj, null, tableName: tableName, databaseName: databaseName, schemaName: schemaName, serverName: serverName, tableOptions: tableOptions, token);
		}

		/// <summary>
		/// Inserts record asynchronously into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <param name="columnFilter">Filter columns to insert.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> InsertAsync<T>(
			this IDataContext dataContext,
			T obj,
			InsertColumnFilter<T>? columnFilter,
			string?                tableName    = default,
			string?                databaseName = default,
			string?                schemaName   = default,
			string?                serverName   = default,
			TableOptions           tableOptions = default,
			CancellationToken      token        = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.Insert<T>.QueryAsync(dataContext, obj, columnFilter, tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, tableOptions: tableOptions, token);
		}

		#endregion

		#region InsertOrReplace

		/// <summary>
		/// Inserts new record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter
		/// or update existing record, identified by match on primary key value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert or update.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Number of affected records.</returns>
		public static int InsertOrReplace<T>(this IDataContext dataContext, T obj,
			string?      tableName    = default,
			string?      databaseName = default,
			string?      schemaName   = default,
			string?      serverName   = default,
			TableOptions tableOptions = default)
		{
			return InsertOrReplace(dataContext, obj, null, tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, tableOptions: tableOptions);
		}

		/// <summary>
		/// Inserts new record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter
		/// or update existing record, identified by match on primary key value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert or update.</param>
		/// <param name="columnFilter">Filter columns to insert and update. Parameters: entity, column descriptor and operation (<c>true</c> for insert )</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Number of affected records.</returns>
		public static int InsertOrReplace<T>(this IDataContext dataContext, T obj,
			InsertOrUpdateColumnFilter<T>? columnFilter,
			string?      tableName    = default,
			string?      databaseName = default,
			string?      schemaName   = default,
			string?      serverName   = default,
			TableOptions tableOptions = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.InsertOrReplace<T>.Query(dataContext, obj, columnFilter, tableName: tableName, serverName: serverName, databaseName: databaseName, schema: schemaName, tableOptions: tableOptions);
		}

		/// <summary>
		/// Asynchronously inserts new record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter
		/// or update existing record, identified by match on primary key value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert or update.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> InsertOrReplaceAsync<T>(
			this IDataContext dataContext,
			T                 obj,
			string?           tableName    = default,
			string?           databaseName = default,
			string?           schemaName   = default,
			string?           serverName   = default,
			TableOptions      tableOptions = default,
			CancellationToken token        = default)
		{
			return InsertOrReplaceAsync(dataContext, obj, null, tableName: tableName, databaseName: databaseName, schemaName: schemaName, serverName: serverName, tableOptions: tableOptions, token);
		}

		/// <summary>
		/// Asynchronously inserts new record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter
		/// or update existing record, identified by match on primary key value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="columnFilter">Filter columns to insert and update.</param>
		/// <param name="obj">Object with data to insert or update.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> InsertOrReplaceAsync<T>(
			this IDataContext              dataContext,
			T                              obj,
			InsertOrUpdateColumnFilter<T>? columnFilter,
			string?                        tableName    = default,
			string?                        databaseName = default,
			string?                        schemaName   = default,
			string?                        serverName   = default,
			TableOptions                   tableOptions = default,
			CancellationToken              token        = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.InsertOrReplace<T>.QueryAsync(dataContext, obj, columnFilter, tableName: tableName, serverName: serverName, databaseName: databaseName, schema: schemaName, tableOptions: tableOptions, token);
		}

		#endregion

		#region InsertWithIdentity

		/// <summary>
		/// Inserts record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// Returns identity value for inserted record.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static object InsertWithIdentity<T>(
			this IDataContext dataContext,
			T             obj,
			string?       tableName    = default,
			string?       databaseName = default,
			string?       schemaName   = default,
			string?       serverName   = default,
			TableOptions  tableOptions = default)
		{
			return InsertWithIdentity(dataContext, obj, null, tableName: tableName, databaseName: databaseName, schemaName: schemaName, serverName: serverName, tableOptions: tableOptions);
		}

		/// <summary>
		/// Inserts record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// Returns identity value for inserted record.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <param name="columnFilter">Filter columns to insert.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static object InsertWithIdentity<T>(
			this IDataContext      dataContext,
			T                      obj,
			InsertColumnFilter<T>? columnFilter,
			string?                tableName    = default,
			string?                databaseName = default,
			string?                schemaName   = default,
			string?                serverName   = default,
			TableOptions           tableOptions = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.InsertWithIdentity<T>.Query(dataContext, obj, columnFilter, tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, tableOptions: tableOptions);
		}

		/// <summary>
		/// Inserts record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// Returns identity value for inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static int InsertWithInt32Identity<T>(
			this IDataContext dataContext,
			T                 obj,
			string?           tableName    = default,
			string?           databaseName = default,
			string?           schemaName   = default,
			string?           serverName   = default,
			TableOptions      tableOptions = default)
		{
			return InsertWithInt32Identity(dataContext, obj, null, tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, tableOptions: tableOptions);
		}

		/// <summary>
		/// Inserts record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// Returns identity value for inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <param name="columnFilter">Filter columns to insert.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static int InsertWithInt32Identity<T>(
			this IDataContext      dataContext,
			T                      obj,
			InsertColumnFilter<T>? columnFilter,
			string?                tableName    = default,
			string?                databaseName = default,
			string?                schemaName   = default,
			string?                serverName   = default,
			TableOptions           tableOptions = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return dataContext.MappingSchema.ChangeTypeTo<int>(QueryRunner.InsertWithIdentity<T>.Query(dataContext, obj, columnFilter, tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, tableOptions: tableOptions));
		}

		/// <summary>
		/// Inserts record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// Returns identity value for inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static long InsertWithInt64Identity<T>(
			this IDataContext dataContext,
			T                 obj,
			string?           tableName    = default,
			string?           databaseName = default,
			string?           schemaName   = default,
			string?           serverName   = default,
			TableOptions      tableOptions = default)
		{
			return InsertWithInt64Identity(dataContext, obj, null, tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, tableOptions: tableOptions);
		}

		/// <summary>
		/// Inserts record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// Returns identity value for inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <param name="columnFilter">Filter columns to insert.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static long InsertWithInt64Identity<T>(
			this IDataContext      dataContext,
			T                      obj,
			InsertColumnFilter<T>? columnFilter,
			string?                tableName    = default,
			string?                databaseName = default,
			string?                schemaName   = default,
			string?                serverName   = default,
			TableOptions           tableOptions = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return dataContext.MappingSchema.ChangeTypeTo<long>(QueryRunner.InsertWithIdentity<T>.Query(dataContext, obj, columnFilter, tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, tableOptions: tableOptions));
		}

		/// <summary>
		/// Inserts record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// Returns identity value for inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static decimal InsertWithDecimalIdentity<T>(
			this IDataContext dataContext,
			T                 obj,
			string?           tableName    = default,
			string?           databaseName = default,
			string?           schemaName   = default,
			string?           serverName   = default,
			TableOptions      tableOptions = default)
		{
			return InsertWithDecimalIdentity(dataContext, obj, null, tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, tableOptions: tableOptions);
		}

		/// <summary>
		/// Inserts record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// Returns identity value for inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <param name="columnFilter">Filter columns to insert.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static decimal InsertWithDecimalIdentity<T>(
			this IDataContext      dataContext,
			T                      obj,
			InsertColumnFilter<T>? columnFilter,
			string?                tableName    = default,
			string?                databaseName = default,
			string?                schemaName   = default,
			string?                serverName   = default,
			TableOptions           tableOptions = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return dataContext.MappingSchema.ChangeTypeTo<decimal>(QueryRunner.InsertWithIdentity<T>.Query(dataContext, obj, columnFilter, tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, tableOptions: tableOptions));
		}

		/// <summary>
		/// Asynchronously inserts record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// Returns identity value for inserted record.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static Task<object> InsertWithIdentityAsync<T>(
			this IDataContext dataContext,
			T                 obj,
			string?           tableName    = default,
			string?           databaseName = default,
			string?           schemaName   = default,
			string?           serverName   = default,
			TableOptions      tableOptions = default,
			CancellationToken token        = default)
		{
			return InsertWithIdentityAsync(dataContext, obj, null, tableName: tableName, databaseName: databaseName, schemaName: schemaName, serverName: serverName, tableOptions: tableOptions, token);
		}

		/// <summary>
		/// Asynchronously inserts record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// Returns identity value for inserted record.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <param name="columnFilter">Filter columns to insert.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static Task<object> InsertWithIdentityAsync<T>(
			this IDataContext      dataContext,
			T                      obj,
			InsertColumnFilter<T>? columnFilter,
			string?                tableName    = default,
			string?                databaseName = default,
			string?                schemaName   = default,
			string?                serverName   = default,
			TableOptions           tableOptions = default,
			CancellationToken      token        = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.InsertWithIdentity<T>.QueryAsync(dataContext, obj, columnFilter, tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, tableOptions: tableOptions, token);
		}

		/// <summary>
		/// Asynchronously inserts record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// Returns identity value for inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<int> InsertWithInt32IdentityAsync<T>(
			this IDataContext dataContext,
			T                 obj,
			string?           tableName    = default,
			string?           databaseName = default,
			string?           schemaName   = default,
			string?           serverName   = default,
			TableOptions      tableOptions = default,
			CancellationToken token        = default)
		{
			return await InsertWithInt32IdentityAsync(dataContext, obj, null, tableName: tableName, databaseName: databaseName, schemaName: schemaName, serverName: serverName, tableOptions: tableOptions, token);
		}

		/// <summary>
		/// Asynchronously inserts record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// Returns identity value for inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <param name="columnFilter">Filter columns to insert.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<int> InsertWithInt32IdentityAsync<T>(
			this IDataContext      dataContext,
			T                      obj,
			InsertColumnFilter<T>? columnFilter,
			string?                tableName    = default,
			string?                databaseName = default,
			string?                schemaName   = default,
			string?                serverName   = default,
			TableOptions           tableOptions = default,
			CancellationToken      token        = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

			var ret = await QueryRunner.InsertWithIdentity<T>
				.QueryAsync(dataContext, obj, columnFilter, tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, tableOptions: tableOptions, token)
				.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			return dataContext.MappingSchema.ChangeTypeTo<int>(ret);
		}

		/// <summary>
		/// Asynchronously inserts record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// Returns identity value for inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<long> InsertWithInt64IdentityAsync<T>(
			this IDataContext dataContext,
			T                 obj,
			string?           tableName    = default,
			string?           databaseName = default,
			string?           schemaName   = default,
			string?           serverName   = default,
			TableOptions      tableOptions = default,
			CancellationToken token        = default)
		{
			return await InsertWithInt64IdentityAsync(dataContext, obj, null, tableName: tableName, databaseName: databaseName, schemaName: schemaName, serverName: serverName, tableOptions: tableOptions, token);
		}

		/// <summary>
		/// Asynchronously inserts record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// Returns identity value for inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <param name="columnFilter">Filter columns to insert.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<long> InsertWithInt64IdentityAsync<T>(
			this IDataContext      dataContext,
			T                      obj,
			InsertColumnFilter<T>? columnFilter,
			string?                tableName    = default,
			string?                databaseName = default,
			string?                schemaName   = default,
			string?                serverName   = default,
			TableOptions           tableOptions = default,
			CancellationToken      token        = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

			var ret = await QueryRunner.InsertWithIdentity<T>
				.QueryAsync(dataContext, obj, columnFilter, tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, tableOptions: tableOptions, token)
				.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return dataContext.MappingSchema.ChangeTypeTo<long>(ret);
		}

		/// <summary>
		/// Asynchronously inserts record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// Returns identity value for inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<decimal> InsertWithDecimalIdentityAsync<T>(
			this IDataContext dataContext,
			T                 obj,
			string?           tableName    = default,
			string?           databaseName = default,
			string?           schemaName   = default,
			string?           serverName   = default,
			TableOptions      tableOptions = default,
			CancellationToken token        = default)
		{
			return await InsertWithDecimalIdentityAsync(dataContext, obj, null, tableName: tableName, databaseName: databaseName, schemaName: schemaName, serverName: serverName, tableOptions: tableOptions, token);
		}

		/// <summary>
		/// Asynchronously inserts record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// Returns identity value for inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <param name="columnFilter">Filter columns to insert.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<decimal> InsertWithDecimalIdentityAsync<T>(
			this IDataContext      dataContext,
			T                      obj,
			InsertColumnFilter<T>? columnFilter,
			string?                tableName    = default,
			string?                databaseName = default,
			string?                schemaName   = default,
			string?                serverName   = default,
			TableOptions           tableOptions = default,
			CancellationToken      token        = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

			var ret = await QueryRunner.InsertWithIdentity<T>
				.QueryAsync(dataContext, obj, columnFilter, tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, tableOptions: tableOptions, token)
				.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return dataContext.MappingSchema.ChangeTypeTo<decimal>(ret);
		}

		#endregion

		#region Update

		/// <summary>
		/// Updates record in table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// Record to update identified by match on primary key value from <paramref name="obj"/> value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to update.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Number of affected records.</returns>
		public static int Update<T>(
			this IDataContext dataContext,
			T                 obj,
			string?           tableName    = default,
			string?           databaseName = default,
			string?           schemaName   = default,
			string?           serverName   = default,
			TableOptions      tableOptions = default)
		{
			return Update(dataContext, obj, null, tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, tableOptions: tableOptions);
		}

		/// <summary>
		/// Updates record in table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// Record to update identified by match on primary key value from <paramref name="obj"/> value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to update.</param>
		/// <param name="columnFilter">Filter columns to update.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Number of affected records.</returns>
		public static int Update<T>(
			this IDataContext      dataContext,
			T                      obj,
			UpdateColumnFilter<T>? columnFilter,
			string?                tableName    = default,
			string?                databaseName = default,
			string?                schemaName   = default,
			string?                serverName   = default,
			TableOptions           tableOptions = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.Update<T>.Query(dataContext, obj, columnFilter, tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, tableOptions: tableOptions);
		}

		/// <summary>
		/// Asynchronously updates record in table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// Record to update identified by match on primary key value from <paramref name="obj"/> value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to update.</param>
		/// <param name="tableName">Name of the table</param>
		/// <param name="databaseName">Name of the database</param>
		/// <param name="schemaName">Name of the schema</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> UpdateAsync<T>(
			this IDataContext dataContext,
			T                 obj,
			string?           tableName    = default,
			string?           databaseName = default,
			string?           schemaName   = default,
			string?           serverName   = default,
			TableOptions      tableOptions = default,
			CancellationToken token        = default)
		{
			return UpdateAsync(dataContext, obj, null, tableName: tableName, databaseName: databaseName, schemaName: schemaName, serverName: serverName, tableOptions: tableOptions, token);
		}

		/// <summary>
		/// Asynchronously updates record in table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter.
		/// Record to update identified by match on primary key value from <paramref name="obj"/> value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to update.</param>
		/// <param name="columnFilter">Filter columns to update.</param>
		/// <param name="tableName">Name of the table</param>
		/// <param name="databaseName">Name of the database</param>
		/// <param name="schemaName">Name of the schema</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> UpdateAsync<T>(
			this IDataContext      dataContext,
			T                      obj,
			UpdateColumnFilter<T>? columnFilter,
			string?                tableName    = default,
			string?                databaseName = default,
			string?                schemaName   = default,
			string?                serverName   = default,
			TableOptions           tableOptions = default,
			CancellationToken      token        = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.Update<T>.QueryAsync(dataContext, obj, columnFilter, tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, tableOptions: tableOptions, token);
		}

		#endregion

		#region Delete

		/// <summary>
		/// Deletes record in table, identified by <typeparamref name="T"/> mapping class.
		/// Record to delete identified by match on primary key value from <paramref name="obj"/> value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data for delete operation.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Number of affected records.</returns>
		public static int Delete<T>(
			this IDataContext dataContext,
			T                 obj,
			string?           tableName    = default,
			string?           databaseName = default,
			string?           schemaName   = default,
			string?           serverName   = default,
			TableOptions      tableOptions = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.Delete<T>.Query(dataContext, obj, tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, tableOptions: tableOptions);
		}

		/// <summary>
		/// Asynchronously deletes record in table, identified by <typeparamref name="T"/> mapping class.
		/// Record to delete identified by match on primary key value from <paramref name="obj"/> value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data for delete operation.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> DeleteAsync<T>(
			this IDataContext dataContext,
			T                 obj,
			string?           tableName    = default,
			string?           databaseName = default,
			string?           schemaName   = default,
			string?           serverName   = default,
			TableOptions      tableOptions = default,
			CancellationToken token        = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.Delete<T>.QueryAsync(dataContext, obj, tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, tableOptions: tableOptions, token);
		}

		#endregion

		#region CreateTable

		/// <summary>
		/// Creates new table in database for mapping class <typeparamref name="T"/>.
		/// Information about table name, columns names and types is taken from mapping class.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="statementHeader">Optional replacement for <c>"CREATE TABLE table_name"</c> header. Header is a template with <c>{0}</c> parameter for table name.</param>
		/// <param name="statementFooter">Optional SQL, appended to generated create table statement.</param>
		/// <param name="defaultNullable">Defines how columns nullability flag should be generated:
		/// <para> - <see cref="DefaultNullable.Null"/> - generate only <c>NOT NULL</c> for non-nullable fields. Missing nullability information treated as <c>NULL</c> by database.</para>
		/// <para> - <see cref="DefaultNullable.NotNull"/> - generate only <c>NULL</c> for nullable fields. Missing nullability information treated as <c>NOT NULL</c> by database.</para>
		/// <para> - <see cref="DefaultNullable.None"/> - explicitly generate <c>NULL</c> and <c>NOT NULL</c> for all columns.</para>
		/// Default value: <see cref="DefaultNullable.None"/>.
		/// </param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Created table as queryable source.</returns>
		public static ITable<T> CreateTable<T>(
			this IDataContext dataContext,
			string?           tableName       = default,
			string?           databaseName    = default,
			string?           schemaName      = default,
			string?           statementHeader = default,
			string?           statementFooter = default,
			DefaultNullable   defaultNullable = DefaultNullable.None,
			string?           serverName      = default,
			TableOptions      tableOptions    = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.CreateTable<T>.Query(dataContext,
				tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, statementHeader, statementFooter, defaultNullable, tableOptions);
		}

		/// <summary>
		/// Asynchronously creates new table in database for mapping class <typeparamref name="T"/>.
		/// Information about table name, columns names and types is taken from mapping class.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="statementHeader">Optional replacement for <c>"CREATE TABLE table_name"</c> header. Header is a template with <c>{0}</c> parameter for table name.</param>
		/// <param name="statementFooter">Optional SQL, appended to generated create table statement.</param>
		/// <param name="defaultNullable">Defines how columns nullability flag should be generated:
		/// <para> - <see cref="DefaultNullable.Null"/> - generate only <c>NOT NULL</c> for non-nullable fields. Missing nullability information treated as <c>NULL</c> by database.</para>
		/// <para> - <see cref="DefaultNullable.NotNull"/> - generate only <c>NULL</c> for nullable fields. Missing nullability information treated as <c>NOT NULL</c> by database.</para>
		/// <para> - <see cref="DefaultNullable.None"/> - explicitly generate <c>NULL</c> and <c>NOT NULL</c> for all columns.</para>
		/// Default value: <see cref="DefaultNullable.None"/>.
		/// </param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Created table as queryable source.</returns>
		public static Task<ITable<T>> CreateTableAsync<T>(
			this IDataContext dataContext,
			string?           tableName       = default,
			string?           databaseName    = default,
			string?           schemaName      = default,
			string?           statementHeader = default,
			string?           statementFooter = default,
			DefaultNullable   defaultNullable = DefaultNullable.None,
			string?           serverName      = default,
			TableOptions      tableOptions    = default,
			CancellationToken token           = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.CreateTable<T>.QueryAsync(dataContext,
				tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, statementHeader, statementFooter, defaultNullable, tableOptions, token);
		}

		#endregion

		#region DropTable

		/// <summary>
		/// Drops table identified by mapping class <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="throwExceptionIfNotExists">If <c>false</c>, any exception during drop operation will be silently caught and <c>0</c> returned.
		/// This behavior is not correct and will be fixed in future to mask only missing table exceptions.
		/// Tracked by <a href="https://github.com/linq2db/linq2db/issues/798">issue</a>.
		/// Default value: <c>true</c>.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		public static void DropTable<T>(
			this IDataContext dataContext,
			string?           tableName                 = default,
			string?           databaseName              = default,
			string?           schemaName                = default,
			bool?             throwExceptionIfNotExists = default,
			string?           serverName                = default,
			TableOptions      tableOptions              = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

			try
			{
				QueryRunner.DropTable<T>.Query(dataContext, tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, !throwExceptionIfNotExists, tableOptions: tableOptions);
			}
			catch when (!throwExceptionIfNotExists ?? tableOptions.HasDropIfExists())
			{
				// ignore
			}
		}

		/// <summary>
		/// Drops table identified by <paramref name="table"/> parameter.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="table">Dropped table.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="throwExceptionIfNotExists">If <c>false</c>, any exception during drop operation will be silently caught and <c>0</c> returned.
		/// This behavior is not correct and will be fixed in future to mask only missing table exceptions.
		/// Tracked by <a href="https://github.com/linq2db/linq2db/issues/798">issue</a>.
		/// Default value: <c>true</c>.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		public static void DropTable<T>(
			this ITable<T> table,
			string?        tableName                 = default,
			string?        databaseName              = default,
			string?        schemaName                = default,
			bool?          throwExceptionIfNotExists = default,
			string?        serverName                = default,
			TableOptions   tableOptions              = default)
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			try
			{
				QueryRunner.DropTable<T>.Query(
					table.DataContext,
					tableName:    tableName    ?? table.TableName,
					serverName:   serverName   ?? table.ServerName,
					databaseName: databaseName ?? table.DatabaseName,
					schemaName:   schemaName   ?? table.SchemaName,
					!throwExceptionIfNotExists,
					tableOptions.IsSet() ? tableOptions : table.TableOptions);
			}
			catch when (!throwExceptionIfNotExists ?? tableOptions.HasDropIfExists())
			{
				// ignore
			}
		}

		/// <summary>
		/// Asynchronously drops table identified by mapping class <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="throwExceptionIfNotExists">If <c>false</c>, any exception during drop operation will be silently caught and <c>0</c> returned.
		/// This behavior is not correct and will be fixed in future to mask only missing table exceptions.
		/// Tracked by <a href="https://github.com/linq2db/linq2db/issues/798">issue</a>.
		/// Default value: <c>true</c>.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		public static async Task DropTableAsync<T>(
			this IDataContext dataContext,
			string?           tableName                 = default,
			string?           databaseName              = default,
			string?           schemaName                = default,
			bool?             throwExceptionIfNotExists = default,
			string?           serverName                = default,
			TableOptions      tableOptions              = default,
			CancellationToken token                     = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

			try
			{
				await QueryRunner.DropTable<T>
					.QueryAsync(dataContext, tableName: tableName, serverName: serverName, databaseName: databaseName, schemaName: schemaName, !throwExceptionIfNotExists, tableOptions: tableOptions, token)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
			catch when (!throwExceptionIfNotExists ?? tableOptions.HasDropIfExists())
			{
				// ignore
			}
		}

		/// <summary>
		/// Asynchronously drops table identified by <paramref name="table"/> parameter.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="table">Dropped table.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="throwExceptionIfNotExists">If <c>false</c>, any exception during drop operation will be silently caught and <c>0</c> returned.
		/// This behavior is not correct and will be fixed in future to mask only missing table exceptions.
		/// Tracked by <a href="https://github.com/linq2db/linq2db/issues/798">issue</a>.
		/// Default value: <c>true</c>.</param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>

		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		public static async Task DropTableAsync<T>(
			this ITable<T>    table,
			string?           tableName                 = default,
			string?           databaseName              = default,
			string?           schemaName                = default,
			bool?             throwExceptionIfNotExists = default,
			string?           serverName                = default,
			TableOptions      tableOptions              = default,
			CancellationToken token                     = default)
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			try
			{
				await QueryRunner.DropTable<T>
					.QueryAsync(
						table.DataContext,
						tableName:    tableName    ?? table.TableName,
						serverName:   serverName   ?? table.ServerName,
						databaseName: databaseName ?? table.DatabaseName,
						schemaName:   schemaName   ?? table.SchemaName,
						!throwExceptionIfNotExists,
						tableOptions.IsSet() ? tableOptions : table.TableOptions,
						token)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
			catch when (!throwExceptionIfNotExists ?? tableOptions.HasDropIfExists())
			{
				// ignore
			}
		}

		#endregion

		#region CTE

		/// <summary>
		/// Helps to define a recursive CTE.
		/// </summary>
		/// <typeparam name="T">Source query record type.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="cteBody">Recursive query body.</param>
		/// <param name="cteTableName">Common table expression name.</param>
		/// <returns>Common table expression.</returns>
		public static IQueryable<T> GetCte<T>(
			                this IDataContext                 dataContext,
			[InstantHandle] Func<IQueryable<T>,IQueryable<T>> cteBody,
			                string?                           cteTableName = null)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			if (cteBody     == null) throw new ArgumentNullException(nameof(cteBody));

			var cteTable = new CteTable<T>(dataContext);
			var param    = MethodHelper.GetMethodInfo(cteBody, cteTable).GetParameters()[0];

			var cteQuery = cteBody(cteTable);

			return ((IQueryable<T>)cteTable).Provider.CreateQuery<T>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(LinqExtensions.AsCte, cteQuery, cteQuery, cteTableName),
					cteTable.Expression, cteQuery.Expression, Expression.Constant(cteTableName ?? param.Name)));
		}

		/// <summary>
		/// Helps to define a recursive CTE.
		/// </summary>
		/// <typeparam name="T">Source query record type.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="cteBody">Recursive query body.</param>
		/// <param name="cteTableName">Common table expression name.</param>
		/// <returns>Common table expression.</returns>
		public static IQueryable<T> GetCte<T>(
			                this IDataContext                 dataContext,
			                string?                           cteTableName,
			[InstantHandle] Func<IQueryable<T>,IQueryable<T>> cteBody)
		{
			return GetCte(dataContext, cteBody, cteTableName);
		}

		#endregion

		#region FromSql

#if !NET45
		/// <summary>
		/// Compares two FormattableString parameters
		/// </summary>
		public class SqlFormattableComparerAttribute : SqlQueryDependentAttribute
		{
			public override bool ExpressionsEqual(Expression expr1, Expression expr2, Func<Expression, Expression, bool> comparer)
			{
				if (expr1.NodeType != expr2.NodeType)
					return false;

				if (expr1.NodeType == ExpressionType.Call)
				{
					var mc1 = (MethodCallExpression)expr1;
					var mc2 = (MethodCallExpression)expr2;
					if (!ObjectsEqual(mc1.Arguments[0].EvaluateExpression(), mc2.Arguments[0].EvaluateExpression()))
						return false;
					return comparer(mc1.Arguments[1], mc2.Arguments[1]);
				}

				if (expr1.NodeType == ExpressionType.Constant)
				{
					var c1 = (ConstantExpression)expr1;
					var c2 = (ConstantExpression)expr2;

					if (c1.Value is FormattableString str1 && c2.Value is FormattableString str2)
					{
						if (str1.Format != str2.Format || str1.ArgumentCount != str2.ArgumentCount)
							return false;

						for (var i = 0; i < str1.ArgumentCount; i++)
							if (!comparer(Expression.Constant(str1.GetArgument(i)), Expression.Constant(str2.GetArgument(i))))
								return false;

						return true;
					}
				}

				return base.ExpressionsEqual(expr1, expr2, comparer);
			}

			public override Expression PrepareForCache(Expression expression)
			{
				if (expression.NodeType != ExpressionType.Call)
					return base.PrepareForCache(expression);

				var mc = (MethodCallExpression)expression;
				var newArguments = new List<Expression>();
				newArguments.Add(Expression.Constant(mc.Arguments[0].EvaluateExpression()));
				newArguments.AddRange(mc.Arguments.Skip(1));

				mc = mc.Update(mc.Object, newArguments);
				return mc;
			}
		}

		/// <summary>
		///     <para>
		///         Creates a LINQ query based on an interpolated string representing a SQL query.
		///     </para>
		///     <para>
		///         If the database provider supports composing on the supplied SQL, you can compose on top of the raw SQL query using
		///         LINQ operators - <code>context.FromSql&lt;Blogs&gt;("SELECT * FROM dbo.Blogs").OrderBy(b =&gt; b.Name);</code>
		///     </para>
		///     <para>
		///         As with any API that accepts SQL it is important to parameterize any user input to protect against a SQL injection
		///         attack. You can include interpolated parameter place holders in the SQL query string. Any interpolated parameter values
		///         you supply will automatically be converted to a DbParameter -
		///         <code>context.FromSql&lt;Blogs&gt;($"SELECT * FROM [dbo].[SearchBlogs]({userSuppliedSearchTerm})");</code>
		///     </para>
		/// </summary>
		/// <typeparam name="TEntity">Source query record type.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="sql"> The interpolated string representing a SQL query. </param>
		/// <remarks>Additional parentheses will be added to the query if first word in raw query is 'SELECT', otherwise users are responsible to add them themselves.</remarks>
		/// <returns> An <see cref="IQueryable{T}" /> representing the raw SQL query. </returns>
		[StringFormatMethod("sql")]
		public static IQueryable<TEntity> FromSql<TEntity>(
			this                     IDataContext      dataContext,
			[SqlFormattableComparer] FormattableString sql)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			if (sql         == null) throw new ArgumentNullException(nameof(sql));

			var table = new Table<TEntity>(dataContext);

			return ((IQueryable<TEntity>)table).Provider.CreateQuery<TEntity>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(FromSql<TEntity>, dataContext, sql),
					Expression.Constant(dataContext), Expression.Constant(sql)));
		}

		/// <summary>
		///     <para>
		///         Creates a LINQ query based on an interpolated string representing a SQL query.
		///     </para>
		///     <para>
		///         If the database provider supports composing on the supplied SQL, you can compose on top of the raw SQL query using
		///         LINQ operators - <code>context.FromSql&lt;Blogs&gt;("SELECT * FROM dbo.Blogs").OrderBy(b =&gt; b.Name);</code>
		///     </para>
		///     <para>
		///         As with any API that accepts SQL it is important to parameterize any user input to protect against a SQL injection
		///         attack. You can include interpolated parameter place holders in the SQL query string. Any interpolated parameter values
		///         you supply will automatically be converted to a DbParameter -
		///         <code>context.FromSqlScalar&lt;Blogs&gt;($"UNNEST({array})");</code>
		///     </para>
		/// </summary>
		/// <typeparam name="TEntity">Source query record type.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="sql"> The interpolated string representing a SQL query. </param>
		/// <remarks>Additional parentheses will be added to the query if first word in raw query is 'SELECT', otherwise users are responsible to add them themselves.</remarks>
		/// <returns> An <see cref="IQueryable{T}" /> representing the raw SQL query. </returns>
		[StringFormatMethod("sql")]
		public static IQueryable<TEntity> FromSqlScalar<TEntity>(
			this                     IDataContext      dataContext,
			[SqlFormattableComparer] FormattableString sql)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			if (sql         == null) throw new ArgumentNullException(nameof(sql));

			var table = new Table<TEntity>(dataContext);

			return ((IQueryable<TEntity>)table).Provider.CreateQuery<TEntity>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(FromSqlScalar<TEntity>, dataContext, sql),
					Expression.Constant(dataContext), Expression.Constant(sql)));
		}

#endif

		/// <summary>
		///     <para>
		///         Creates a LINQ query based on a raw SQL query.
		///     </para>
		///     <para>
		///         If the database provider supports composing on the supplied SQL, you can compose on top of the raw SQL query using
		///         LINQ operators - <code>context.FromSql&lt;Blogs&gt;("SELECT * FROM dbo.Blogs").OrderBy(b => b.Name);</code>
		///     </para>
		///     <para>
		///         As with any API that accepts SQL it is important to parametrize any user input to protect against a SQL injection
		///         attack. You can include parameter place holders in the SQL query string and then supply parameter values as additional
		///         arguments. Any parameter values you supply will automatically be converted to a DbParameter -
		///         <code>context.FromSql&lt;Blogs&gt;("SELECT * FROM [dbo].[SearchBlogs]({0})", userSuppliedSearchTerm);</code>
		///     </para>
		///     <para>
		///         This overload also accepts DbParameter instances as parameter values.
		///         <code>context.FromSql&lt;Blogs&gt;("SELECT * FROM [dbo].[SearchBlogs]({0})", new DataParameter("@searchTerm", userSuppliedSearchTerm, DataType.Int64));</code>
		///     </para>
		/// </summary>
		/// <typeparam name="TEntity">Source query record type.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="sql">The raw SQL query</param>
		/// <param name="parameters"> The values to be assigned to parameters. </param>
		/// <remarks>Additional parentheses will be added to the query if first word in raw query is 'SELECT', otherwise users are responsible to add them themselves.</remarks>
		/// <returns> An <see cref="IQueryable{T}" /> representing the raw SQL query. </returns>
		[StringFormatMethod("sql")]
		public static IQueryable<TEntity> FromSql<TEntity>(
			this                             IDataContext dataContext,
			[SqlQueryDependent]              RawSqlString sql,
			[SqlQueryDependentParams] params object?[]    parameters)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

			var table = new Table<TEntity>(dataContext);

			return ((IQueryable<TEntity>)table).Provider.CreateQuery<TEntity>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(FromSql<TEntity>, dataContext, sql, parameters),
					Expression.Constant(dataContext), Expression.Constant(sql), Expression.Constant(parameters)));
		}

		#endregion

		#region SelectQuery

		public static MethodInfo SelectQueryMethodInfo =
			MemberHelper.MethodOf(() => SelectQuery<int>(null!, null!)).GetGenericMethodDefinition();

		/// <summary>
		///     <para>
		///         Creates a LINQ query based on expression. Returned <see cref="IQueryable{T}" /> represents single record.<para />
		///         Could be useful for function calls, querying of database variables, properties or sub-queries.
		///     </para>
		/// </summary>
		/// <typeparam name="TEntity">Type of result.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="selector">Value selection expression.</param>
		/// <returns> An <see cref="IQueryable{T}" /> representing single record. </returns>
		/// <remarks>
		///     Method works for most supported database engines, except databases which do not support <code>SELECT Value</code> without FROM statement.<para />
		///     For Oracle it will be translated to <code>SELECT Value FROM SYS.DUAL</code>
		/// </remarks>
		/// <example>
		/// Complex record:
		/// <code>
		/// db.SelectQuery(() => new { Version = 1, CurrentTimeStamp = Sql.CurrentTimeStamp });
		/// </code>
		/// Scalar value:
		/// <code>
		/// db.SelectQuery(() => Sql.CurrentTimeStamp);
		/// </code>
		/// </example>
		[Pure]
		public static IQueryable<TEntity> SelectQuery<TEntity>(
			                this IDataContext         dataContext,
			[InstantHandle] Expression<Func<TEntity>> selector)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			if (selector    == null) throw new ArgumentNullException(nameof(selector));

			var table = new Table<TEntity>(dataContext);

			return ((IQueryable<TEntity>)table).Provider.CreateQuery<TEntity>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(SelectQuery, dataContext, selector),
					Expression.Constant(dataContext), Expression.Quote(selector)));
		}


		#endregion

	}
}
