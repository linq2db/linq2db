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
			this IDataContext dataContext,
			object instance,
			[NotNull] MethodInfo methodInfo,
			[NotNull] params object[] parameters)
			where T : class
		{
			if (methodInfo == null) throw new ArgumentNullException(nameof(methodInfo));
			if (parameters == null) throw new ArgumentNullException(nameof(parameters));

			if (!typeof(ITable<>).IsSameOrParentOf(methodInfo.ReturnType))
				throw new LinqException(
					"Method '{0}.{1}' must return type 'ITable<{2}>'",
					methodInfo.Name, methodInfo.DeclaringType.FullName, typeof(T).FullName);

			Expression expr;

			if (parameters.Length > 0)
			{
				var pis  = methodInfo.GetParameters();
				var args = new List<Expression>(parameters.Length);

				for (var i = 0; i < parameters.Length; i++)
				{
					var type = pis[i].ParameterType;
					args.Add(Expression.Constant(parameters[i], type.IsByRef ? type.GetElementType() : type));
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
			[NotNull] this IDataContext dataContext,
			[NotNull] Expression<Func<TDc,TResult>> query)
			where TDc : IDataContext
		{
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
			[NotNull] this IDataContext dataContext,
			[NotNull] Expression<Func<TDc,TArg1,TResult>> query)
			where TDc : IDataContext
		{
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
			[NotNull] this IDataContext dataContext,
			[NotNull] Expression<Func<TDc,TArg1,TArg2,TResult>> query)
			where TDc : IDataContext
		{
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
			[NotNull] this IDataContext dataContext,
			[NotNull] Expression<Func<TDc,TArg1,TArg2,TArg3,TResult>> query)
			where TDc : IDataContext
		{
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
		/// <returns>Number of affected records.</returns>
		public static int Insert<T>([NotNull] this IDataContext dataContext, T obj,
			string tableName = null, string databaseName = null, string schemaName = null)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.Insert<T>.Query(dataContext, obj, tableName, databaseName, schemaName);
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
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> InsertAsync<T>(
			[NotNull] this IDataContext dataContext, T obj,
			string tableName = null, string databaseName = null, string schemaName = null,
			CancellationToken token = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.Insert<T>.QueryAsync(dataContext, obj, tableName, databaseName, schemaName, token);
		}

		#endregion

		#region InsertOrReplace

		/// <summary>
		/// Inserts new record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter
		/// or update exising record, identified by match on primary key value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert or update.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>		
		/// <returns>Number of affected records.</returns>
		public static int InsertOrReplace<T>([NotNull] this IDataContext dataContext, T obj, string tableName = null, string databaseName = null, string schemaName = null)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.InsertOrReplace<T>.Query(dataContext, obj, tableName, databaseName, schemaName);
		}

		/// <summary>
		/// Asynchronously inserts new record into table, identified by <typeparamref name="T"/> mapping class, using values from <paramref name="obj"/> parameter
		/// or update exising record, identified by match on primary key value.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="obj">Object with data to insert or update.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>		
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> InsertOrReplaceAsync<T>([NotNull] this IDataContext dataContext, T obj,
			string tableName = null, string databaseName = null, string schemaName = null, CancellationToken token = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.InsertOrReplace<T>.QueryAsync(dataContext, obj, tableName, databaseName, schemaName, token);
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
		/// <returns>Inserted record's identity value.</returns>
		public static object InsertWithIdentity<T>([NotNull] this IDataContext dataContext, T obj, string tableName = null, string databaseName = null, string schemaName = null)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.InsertWithIdentity<T>.Query(dataContext, obj, tableName, databaseName, schemaName);
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
		/// <returns>Inserted record's identity value.</returns>
		public static int InsertWithInt32Identity<T>([NotNull] this IDataContext dataContext, T obj, string tableName = null, string databaseName = null, string schemaName = null)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return dataContext.MappingSchema.ChangeTypeTo<int>(QueryRunner.InsertWithIdentity<T>.Query(dataContext, obj, tableName, databaseName, schemaName));
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
		/// <returns>Inserted record's identity value.</returns>
		public static long InsertWithInt64Identity<T>([NotNull] this IDataContext dataContext, T obj, string tableName = null, string databaseName = null, string schemaName = null)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return dataContext.MappingSchema.ChangeTypeTo<long>(QueryRunner.InsertWithIdentity<T>.Query(dataContext, obj, tableName, databaseName, schemaName));
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
		/// <returns>Inserted record's identity value.</returns>
		public static decimal InsertWithDecimalIdentity<T>([NotNull] this IDataContext dataContext, T obj, string tableName = null, string databaseName = null, string schemaName = null)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return dataContext.MappingSchema.ChangeTypeTo<decimal>(QueryRunner.InsertWithIdentity<T>.Query(dataContext, obj, tableName, databaseName, schemaName));
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
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static Task<object> InsertWithIdentityAsync<T>(
			[NotNull] this IDataContext dataContext, T obj, string tableName = null, string databaseName = null, string schemaName = null, CancellationToken token = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.InsertWithIdentity<T>.QueryAsync(dataContext, obj, tableName, databaseName, schemaName, token);
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
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<int> InsertWithInt32IdentityAsync<T>(
			[NotNull] this IDataContext dataContext, T obj, string tableName = null, string databaseName = null, string schemaName = null, CancellationToken token = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

			var ret = await QueryRunner.InsertWithIdentity<T>.QueryAsync(dataContext, obj, tableName, databaseName, schemaName, token);
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
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<long> InsertWithInt64IdentityAsync<T>(
			[NotNull] this IDataContext dataContext, T obj, string tableName = null, string databaseName = null, string schemaName = null, CancellationToken token = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

			var ret = await QueryRunner.InsertWithIdentity<T>.QueryAsync(dataContext, obj, tableName, databaseName, schemaName, token);
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
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<decimal> InsertWithDecimalIdentityAsync<T>(
			[NotNull] this IDataContext dataContext, T obj, string tableName = null, string databaseName = null, string schemaName = null, CancellationToken token = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

			var ret = await QueryRunner.InsertWithIdentity<T>.QueryAsync(dataContext, obj, tableName, databaseName, schemaName, token);
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
		/// <returns>Number of affected records.</returns>
		public static int Update<T>([NotNull] this IDataContext dataContext, T obj, string tableName = null, string databaseName = null, string schemaName = null)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.Update<T>.Query(dataContext, obj, tableName, databaseName, schemaName);
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
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> UpdateAsync<T>(
			[NotNull] this IDataContext dataContext, T obj, string tableName = null, string databaseName = null, string schemaName = null, CancellationToken token = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.Update<T>.QueryAsync(dataContext, obj, tableName, databaseName, schemaName, token);
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
		/// <returns>Number of affected records.</returns>
		public static int Delete<T>([NotNull] this IDataContext dataContext, T obj, string tableName = null, string databaseName = null, string schemaName = null)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.Delete<T>.Query(dataContext, obj, tableName, databaseName, schemaName);
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
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> DeleteAsync<T>(
			[NotNull] this IDataContext dataContext, T obj, string tableName = null, string databaseName = null, string schemaName = null, CancellationToken token = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.Delete<T>.QueryAsync(dataContext, obj, tableName, databaseName, schemaName, token);
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
		/// <returns>Created table as queryable source.</returns>
		public static ITable<T> CreateTable<T>([NotNull] this IDataContext dataContext,
			string          tableName       = null,
			string          databaseName    = null,
			string          schemaName      = null,
			string          statementHeader = null,
			string          statementFooter = null,
			DefaultNullable defaultNullable  = DefaultNullable.None)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.CreateTable<T>.Query(dataContext,
				tableName, databaseName, schemaName, statementHeader, statementFooter, defaultNullable);
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
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Created table as queryable source.</returns>
		public static Task<ITable<T>> CreateTableAsync<T>([NotNull] this IDataContext dataContext,
			string            tableName       = null,
			string            databaseName    = null,
			string            schemaName      = null,
			string            statementHeader = null,
			string            statementFooter = null,
			DefaultNullable   defaultNullable = DefaultNullable.None,
			CancellationToken token           = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.CreateTable<T>.QueryAsync(dataContext,
				tableName, databaseName, schemaName, statementHeader, statementFooter, defaultNullable, token);
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
		/// <param name="throwExceptionIfNotExists">If <c>false</c>, any exception during drop operation will be silently catched and <c>0</c> returned.
		/// This behavior is not correct and will be fixed in future to mask only missing table exceptions.
		/// Tracked by <a href="https://github.com/linq2db/linq2db/issues/798">issue</a>.
		/// Default value: <c>true</c>.</param>
		public static void DropTable<T>(
			[NotNull] this IDataContext dataContext,
			string tableName                 = null,
			string databaseName              = null,
			string schemaName                = null,
			bool   throwExceptionIfNotExists = true)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

			if (throwExceptionIfNotExists)
			{
				QueryRunner.DropTable<T>.Query(dataContext, tableName, databaseName, schemaName);
			}
			else try
			{
				QueryRunner.DropTable<T>.Query(dataContext, tableName, databaseName, schemaName);
			}
			catch
			{
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
		/// <param name="throwExceptionIfNotExists">If <c>false</c>, any exception during drop operation will be silently catched and <c>0</c> returned.
		/// This behavior is not correct and will be fixed in future to mask only missing table exceptions.
		/// Tracked by <a href="https://github.com/linq2db/linq2db/issues/798">issue</a>.
		/// Default value: <c>true</c>.</param>
		public static void DropTable<T>(
			[NotNull] this ITable<T> table,
			string tableName                 = null,
			string databaseName              = null,
			string schemaName                = null,
			bool   throwExceptionIfNotExists = true)
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			if (throwExceptionIfNotExists)
			{
				QueryRunner.DropTable<T>.Query(
					table.DataContext,
					tableName    ?? table.TableName,
					databaseName ?? table.DatabaseName,
					schemaName   ?? table.SchemaName);
			}
			else try
			{
				QueryRunner.DropTable<T>.Query(
					table.DataContext,
					tableName    ?? table.TableName,
					databaseName ?? table.DatabaseName,
					schemaName   ?? table.SchemaName);
			}
			catch
			{
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
		/// <param name="throwExceptionIfNotExists">If <c>false</c>, any exception during drop operation will be silently catched and <c>0</c> returned.
		/// This behavior is not correct and will be fixed in future to mask only missing table exceptions.
		/// Tracked by <a href="https://github.com/linq2db/linq2db/issues/798">issue</a>.
		/// Default value: <c>true</c>.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		public static async Task DropTableAsync<T>(
			[NotNull] this IDataContext dataContext,
			string tableName                 = null,
			string databaseName              = null,
			string schemaName                = null,
			bool   throwExceptionIfNotExists = true,
			CancellationToken token          = default)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

			if (throwExceptionIfNotExists)
			{
				await QueryRunner.DropTable<T>.QueryAsync(dataContext, tableName, databaseName, schemaName, token);
			}
			else try
			{
				await QueryRunner.DropTable<T>.QueryAsync(dataContext, tableName, databaseName, schemaName, token);
			}
			catch
			{
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
		/// <param name="throwExceptionIfNotExists">If <c>false</c>, any exception during drop operation will be silently catched and <c>0</c> returned.
		/// This behavior is not correct and will be fixed in future to mask only missing table exceptions.
		/// Tracked by <a href="https://github.com/linq2db/linq2db/issues/798">issue</a>.
		/// Default value: <c>true</c>.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		public static async Task DropTableAsync<T>(
			[NotNull] this ITable<T> table,
			string tableName                 = null,
			string databaseName              = null,
			string schemaName                = null,
			bool   throwExceptionIfNotExists = true,
			CancellationToken token          = default)
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			if (throwExceptionIfNotExists)
			{
				await QueryRunner.DropTable<T>.QueryAsync(
					table.DataContext,
					tableName    ?? table.TableName,
					databaseName ?? table.DatabaseName,
					schemaName   ?? table.SchemaName, token);
			}
			else try
			{
				await QueryRunner.DropTable<T>.QueryAsync(
					table.DataContext,
					tableName    ?? table.TableName,
					databaseName ?? table.DatabaseName,
					schemaName   ?? table.SchemaName,
					token);
			}
			catch
			{
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
			[NotNull]   this IDataContext                 dataContext,
			[NotNull, InstantHandle] Func<IQueryable<T>,IQueryable<T>> cteBody,
			[CanBeNull] string                            cteTableName = null)
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
					new[] {cteTable.Expression, cteQuery.Expression, Expression.Constant(cteTableName ?? param.Name)}));
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
			[NotNull]                this IDataContext                 dataContext,
			[CanBeNull]              string                            cteTableName,
			[NotNull, InstantHandle] Func<IQueryable<T>,IQueryable<T>> cteBody)
		{
			return GetCte(dataContext, cteBody, cteTableName);
		}

		#endregion

		#region FromSql

#if !NET45
		/// <summary>
		///     <para>
		///         Creates a LINQ query based on an interpolated string representing a SQL query.
		///     </para>
		///     <para>
		///         If the database provider supports composing on the supplied SQL, you can compose on top of the raw SQL query using
		///         LINQ operators - <code>context.FromSql&lt;Blogs&gt;("SELECT * FROM dbo.Blogs").OrderBy(b =&gt; b.Name)</code>.
		///     </para>
		///     <para>
		///         As with any API that accepts SQL it is important to parameterize any user input to protect against a SQL injection
		///         attack. You can include interpolated parameter place holders in the SQL query string. Any interpolated parameter values
		///         you supply will automatically be converted to a DbParameter -
		///         <code>context.FromSql&lt;Blogs&gt;($"SELECT * FROM [dbo].[SearchBlogs]({userSuppliedSearchTerm})")</code>.
		///     </para>
		/// </summary>
		/// <typeparam name="TEntity">Source query record type.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="sql"> The interpolated string representing a SQL query. </param>
		/// <returns> An <see cref="IQueryable{T}" /> representing the raw SQL query. </returns>
        [StringFormatMethod("sql")]
		public static IQueryable<TEntity> FromSql<TEntity>(
			[NotNull]  this               IDataContext      dataContext,
			[NotNull, SqlQueryDependent]  FormattableString sql)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			if (sql         == null) throw new ArgumentNullException(nameof(sql));

			var table = new Table<TEntity>(dataContext);

			return ((IQueryable<TEntity>)table).Provider.CreateQuery<TEntity>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(FromSql<TEntity>, dataContext, sql),
					new Expression[] {Expression.Constant(dataContext), Expression.Constant(sql)}));
		}
#endif

		/// <summary>
		///     <para>
		///         Creates a LINQ query based on a raw SQL query.
		///     </para>
		///     <para>
		///         If the database provider supports composing on the supplied SQL, you can compose on top of the raw SQL query using
		///         LINQ operators - <code>context.FromSql&lt;Blogs&gt;("SELECT * FROM dbo.Blogs").OrderBy(b => b.Name)</code>.
		///     </para>
		///     <para>
		///         As with any API that accepts SQL it is important to parameterize any user input to protect against a SQL injection
		///         attack. You can include parameter place holders in the SQL query string and then supply parameter values as additional
		///         arguments. Any parameter values you supply will automatically be converted to a DbParameter -
		///         <code>context.FromSql&lt;Blogs&gt;("SELECT * FROM [dbo].[SearchBlogs]({0})", userSuppliedSearchTerm)</code>.
		///     </para>
		///     <para>
		///         This overload also accepts DbParameter instances as parameter values. 
		///         <code>context.FromSql&lt;Blogs&gt;("SELECT * FROM [dbo].[SearchBlogs]({0})", new DataParameter("@searchTerm", userSuppliedSearchTerm, DataType.Int64))</code>
		///     </para>
		/// </summary>
		/// <typeparam name="TEntity">Source query record type.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="sql">The raw SQL query</param>
		/// <param name="parameters"> The values to be assigned to parameters. </param>
		/// <returns> An <see cref="IQueryable{T}" /> representing the raw SQL query. </returns>
		[StringFormatMethod("sql")]
		public static IQueryable<TEntity> FromSql<TEntity>(
			[NotNull] this                IDataContext dataContext,
			[NotNull, SqlQueryDependent]  RawSqlString   sql,
			[NotNull] params              object[]     parameters)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));

			var table = new Table<TEntity>(dataContext);

			return ((IQueryable<TEntity>)table).Provider.CreateQuery<TEntity>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(FromSql<TEntity>, dataContext, sql, parameters),
					new Expression[] {Expression.Constant(dataContext), Expression.Constant(sql), Expression.Constant(parameters)}));
		}

		#endregion

	}
}
