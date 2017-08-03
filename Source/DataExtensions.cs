using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

#if !NOASYNC
using System.Threading;
using System.Threading.Tasks;
#endif

using JetBrains.Annotations;

namespace LinqToDB
{
	using Extensions;
	using Linq;

	using SqlQuery;

	public static class DataExtensions
	{
		#region Table Helpers

		[LinqTunnel]
		public static ITable<T> GetTable<T>(this IDataContext dataContext)
			where T : class
		{
			return new Table<T>(dataContext);
		}

		[LinqTunnel]
		public static ITable<T> GetTable<T>(
			this IDataContext dataContext,
			object instance,
			[NotNull] MethodInfo methodInfo,
			[NotNull] params object[] parameters)
			where T : class
		{
			if (methodInfo == null) throw new ArgumentNullException("methodInfo");
			if (parameters == null) throw new ArgumentNullException("parameters");

			if (!typeof(ITable<>).IsSameOrParentOf(methodInfo.ReturnType))
				throw new LinqException(
					"Method '{0}.{1}' must return type 'Table<{2}>'",
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
		/// <param name="dataContext"></param>
		/// <param name="query">
		/// The query expression to be compiled.
		/// </param>
		/// <typeparam name="TDc">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TResult">
		/// Returned type of the delegate returned by the method.
		/// </typeparam>
		public static Func<TDc,TResult> Compile<TDc,TResult>(
			[NotNull] this IDataContext dataContext,
			[NotNull] Expression<Func<TDc,TResult>> query)
			where TDc : IDataContext
		{
			return CompiledQuery.Compile(query);
		}

		/// <summary>
		/// Compiles the query.
		/// </summary>
		/// <returns>
		/// A generic delegate that represents the compiled query.
		/// </returns>
		/// <param name="dataContext"></param>
		/// <param name="query">
		/// The query expression to be compiled.
		/// </param>
		/// <typeparam name="TDc">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TArg1">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TResult">
		/// Returned type of the delegate returned by the method.
		/// </typeparam>
		public static Func<TDc,TArg1,TResult> Compile<TDc,TArg1, TResult>(
			[NotNull] this IDataContext dataContext,
			[NotNull] Expression<Func<TDc,TArg1,TResult>> query)
			where TDc : IDataContext
		{
			return CompiledQuery.Compile(query);
		}

		/// <summary>
		/// Compiles the query.
		/// </summary>
		/// <returns>
		/// A generic delegate that represents the compiled query.
		/// </returns>
		/// <param name="dataContext"></param>
		/// <param name="query">
		/// The query expression to be compiled.
		/// </param>
		/// <typeparam name="TDc">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TArg1">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TArg2">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TResult">
		/// Returned type of the delegate returned by the method.
		/// </typeparam>
		public static Func<TDc,TArg1,TArg2,TResult> Compile<TDc,TArg1,TArg2,TResult>(
			[NotNull] this IDataContext dataContext,
			[NotNull] Expression<Func<TDc,TArg1,TArg2,TResult>> query)
			where TDc : IDataContext
		{
			return CompiledQuery.Compile(query);
		}

		/// <summary>
		/// Compiles the query.
		/// </summary>
		/// <returns>
		/// A generic delegate that represents the compiled query.
		/// </returns>
		/// <param name="dataContext"></param>
		/// <param name="query">
		/// The query expression to be compiled.
		/// </param>
		/// <typeparam name="TDc">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TArg1">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TArg2">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TArg3">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TResult">
		/// Returned type of the delegate returned by the method.
		/// </typeparam>
		public static Func<TDc,TArg1,TArg2,TArg3,TResult> Compile<TDc,TArg1,TArg2,TArg3,TResult>(
			[NotNull] this IDataContext dataContext,
			[NotNull] Expression<Func<TDc,TArg1,TArg2,TArg3,TResult>> query)
			where TDc : IDataContext
		{
			return CompiledQuery.Compile(query);
		}

		#endregion

		#region Object Operations

		#region Insert

		public static int Insert<T>([NotNull] this IDataContext dataContext, T obj,
			string tableName = null, string databaseName = null, string schemaName = null)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return QueryRunner.Insert<T>.Query(dataContext, obj, tableName, databaseName, schemaName);
		}

#if !NOASYNC

		public static Task<int> InsertAsync<T>([NotNull] this IDataContext dataContext, T obj,
			string tableName = null, string databaseName = null, string schemaName = null)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return QueryRunner.Insert<T>.QueryAsync(
				dataContext, obj, tableName, databaseName, schemaName, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<int> InsertAsync<T>([NotNull] this IDataContext dataContext, T obj, CancellationToken token)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return QueryRunner.Insert<T>.QueryAsync(dataContext, obj, null, null, null, token, TaskCreationOptions.None);
		}

		public static Task<int> InsertAsync<T>([NotNull] this IDataContext dataContext, T obj,
			CancellationToken token, TaskCreationOptions options)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return QueryRunner.Insert<T>.QueryAsync(dataContext, obj, null, null, null, token, options);
		}

		public static Task<int> InsertAsync<T>(
			[NotNull] this IDataContext dataContext, T obj,
			string tableName, string databaseName, string schemaName, CancellationToken token)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return QueryRunner.Insert<T>.QueryAsync(
				dataContext, obj, tableName, databaseName, schemaName, token, TaskCreationOptions.None);
		}

		public static Task<int> InsertAsync<T>(
			[NotNull] this IDataContext dataContext, T obj,
			string tableName, string databaseName, string schemaName,
			CancellationToken token, TaskCreationOptions options)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return QueryRunner.Insert<T>.QueryAsync(dataContext, obj, tableName, databaseName, schemaName, token, options);
		}

#endif

		#endregion

		#region InsertOrReplace

		public static int InsertOrReplace<T>([NotNull] this IDataContext dataContext, T obj)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return Query<T>.InsertOrReplace(dataContext, obj);
		}

		#endregion

		#region InsertWithIdentity

		public static object InsertWithIdentity<T>([NotNull] this IDataContext dataContext, T obj)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return QueryRunner.InsertWithIdentity<T>.Query(dataContext, obj);
		}

#if !NOASYNC

		public static Task<object> InsertWithIdentityAsync<T>([NotNull] this IDataContext dataContext, T obj)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return QueryRunner.InsertWithIdentity<T>.QueryAsync(dataContext, obj, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<object> InsertWithIdentityAsync<T>([NotNull] this IDataContext dataContext, T obj,
			CancellationToken token)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return QueryRunner.InsertWithIdentity<T>.QueryAsync(dataContext, obj, token, TaskCreationOptions.None);
		}

		public static Task<object> InsertWithIdentityAsync<T>([NotNull] this IDataContext dataContext, T obj,
			CancellationToken token, TaskCreationOptions options)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return QueryRunner.InsertWithIdentity<T>.QueryAsync(dataContext, obj, token, options);
		}

#endif

#endregion

		#region Update

		public static int Update<T>([NotNull] this IDataContext dataContext, T obj)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return QueryRunner.Update<T>.Query(dataContext, obj);
		}

#if !NOASYNC

		public static Task<int> UpdateAsync<T>([NotNull] this IDataContext dataContext, T obj)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return QueryRunner.Update<T>.QueryAsync(dataContext, obj, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<int> UpdateAsync<T>([NotNull] this IDataContext dataContext, T obj, CancellationToken token)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return QueryRunner.Update<T>.QueryAsync(dataContext, obj, token, TaskCreationOptions.None);
		}

		public static Task<int> UpdateAsync<T>([NotNull] this IDataContext dataContext, T obj,
			CancellationToken token, TaskCreationOptions options)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return QueryRunner.Update<T>.QueryAsync(dataContext, obj, token, options);
		}

#endif

		#endregion

		#region Delete

		public static int Delete<T>([NotNull] this IDataContext dataContext, T obj)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return QueryRunner.Delete<T>.Query(dataContext, obj);
		}

#if !NOASYNC

		public static Task<int> DeleteAsync<T>([NotNull] this IDataContext dataContext, T obj)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return QueryRunner.Delete<T>.QueryAsync(dataContext, obj, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<int> DeleteAsync<T>([NotNull] this IDataContext dataContext, T obj, CancellationToken token)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return QueryRunner.Delete<T>.QueryAsync(dataContext, obj, token, TaskCreationOptions.None);
		}

		public static Task<int> DeleteAsync<T>([NotNull] this IDataContext dataContext, T obj,
			CancellationToken token, TaskCreationOptions options)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return QueryRunner.Delete<T>.QueryAsync(dataContext, obj, token, options);
		}

#endif

		#endregion

		#endregion

		#region DDL Operations

		public static ITable<T> CreateTable<T>([NotNull] this IDataContext dataContext,
			string         tableName       = null,
			string         databaseName    = null,
			string         schemaName      = null,
			string         statementHeader = null,
			string         statementFooter = null,
			DefaulNullable defaulNullable  = DefaulNullable.None)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return Query<T>.CreateTable(dataContext,
				tableName, databaseName, schemaName, statementHeader, statementFooter, defaulNullable);
		}

		public static void DropTable<T>([NotNull] this IDataContext dataContext,
			string tableName    = null,
			string databaseName = null,
			string schemaName   = null)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			Query<T>.DropTable(dataContext, tableName, databaseName, schemaName);
		}

		public static void DropTable<T>([NotNull] this ITable<T> table, string tableName = null, string databaseName = null, string schemaName = null)
		{
			if (table == null) throw new ArgumentNullException("table");

			var tbl = (Table<T>)table;

			Query<T>.DropTable(tbl.DataContext, tableName ?? tbl.TableName, databaseName ?? tbl.DatabaseName, schemaName ?? tbl.SchemaName);
		}

		#endregion
	}
}
