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

		public static Task<int> InsertAsync<T>(
			[NotNull] this IDataContext dataContext, T obj,
			string tableName = null, string databaseName = null, string schemaName = null,
			CancellationToken token = default(CancellationToken), TaskCreationOptions options = TaskCreationOptions.None)
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
			return QueryRunner.InsertOrReplace<T>.Query(dataContext, obj);
		}

#if !NOASYNC

		public static Task<int> InsertOrReplaceAsync<T>([NotNull] this IDataContext dataContext, T obj,
			CancellationToken token = default(CancellationToken), TaskCreationOptions options = TaskCreationOptions.None)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return QueryRunner.InsertOrReplace<T>.QueryAsync(dataContext, obj, token, options);
		}

#endif

#endregion

		#region InsertWithIdentity

		public static object InsertWithIdentity<T>([NotNull] this IDataContext dataContext, T obj)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return QueryRunner.InsertWithIdentity<T>.Query(dataContext, obj);
		}

		public static int InsertWithInt32Identity<T>([NotNull] this IDataContext dataContext, T obj)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return dataContext.MappingSchema.ChangeTypeTo<int>(QueryRunner.InsertWithIdentity<T>.Query(dataContext, obj));
		}

		public static long InsertWithInt64Identity<T>([NotNull] this IDataContext dataContext, T obj)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return dataContext.MappingSchema.ChangeTypeTo<long>(QueryRunner.InsertWithIdentity<T>.Query(dataContext, obj));
		}

		public static decimal InsertWithDecimalIdentity<T>([NotNull] this IDataContext dataContext, T obj)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return dataContext.MappingSchema.ChangeTypeTo<decimal>(QueryRunner.InsertWithIdentity<T>.Query(dataContext, obj));
		}

#if !NOASYNC

		public static Task<object> InsertWithIdentityAsync<T>([NotNull] this IDataContext dataContext, T obj,
			CancellationToken token = default(CancellationToken), TaskCreationOptions options = TaskCreationOptions.None)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return QueryRunner.InsertWithIdentity<T>.QueryAsync(dataContext, obj, token, options);
		}

		public static async Task<int> InsertWithInt32IdentityAsync<T>([NotNull] this IDataContext dataContext, T obj,
			CancellationToken token = default(CancellationToken), TaskCreationOptions options = TaskCreationOptions.None)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");

			var ret = await QueryRunner.InsertWithIdentity<T>.QueryAsync(dataContext, obj, token, options);
			return dataContext.MappingSchema.ChangeTypeTo<int>(ret);
		}

		public static async Task<long> InsertWithInt64IdentityAsync<T>([NotNull] this IDataContext dataContext, T obj,
			CancellationToken token = default(CancellationToken), TaskCreationOptions options = TaskCreationOptions.None)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");

			var ret = await QueryRunner.InsertWithIdentity<T>.QueryAsync(dataContext, obj, token, options);
			return dataContext.MappingSchema.ChangeTypeTo<long>(ret);
		}

		public static async Task<decimal> InsertWithDecimalIdentityAsync<T>([NotNull] this IDataContext dataContext, T obj,
			CancellationToken token = default(CancellationToken), TaskCreationOptions options = TaskCreationOptions.None)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");

			var ret = await QueryRunner.InsertWithIdentity<T>.QueryAsync(dataContext, obj, token, options);
			return dataContext.MappingSchema.ChangeTypeTo<decimal>(ret);
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

		public static Task<int> UpdateAsync<T>([NotNull] this IDataContext dataContext, T obj,
			CancellationToken token = default(CancellationToken), TaskCreationOptions options = TaskCreationOptions.None)
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

		public static Task<int> DeleteAsync<T>([NotNull] this IDataContext dataContext, T obj,
			CancellationToken token = default(CancellationToken), TaskCreationOptions options = TaskCreationOptions.None)
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
			return QueryRunner.CreateTable<T>.Query(dataContext,
				tableName, databaseName, schemaName, statementHeader, statementFooter, defaulNullable);
		}

#if !NOASYNC

		public static Task<ITable<T>> CreateTableAsync<T>([NotNull] this IDataContext dataContext,
			string              tableName       = null,
			string              databaseName    = null,
			string              schemaName      = null,
			string              statementHeader = null,
			string              statementFooter = null,
			DefaulNullable      defaulNullable  = DefaulNullable.None,
			CancellationToken   token           = default(CancellationToken),
			TaskCreationOptions options         = TaskCreationOptions.None)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			return QueryRunner.CreateTable<T>.QueryAsync(dataContext,
				tableName, databaseName, schemaName, statementHeader, statementFooter, defaulNullable, token, options);
		}

#endif

		public static void DropTable<T>(
			[NotNull] this IDataContext dataContext,
			string tableName                 = null,
			string databaseName              = null,
			string schemaName                = null,
			bool   throwExceptionIfNotExists = true)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");

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

		public static void DropTable<T>(
			[NotNull] this ITable<T> table,
			string tableName                 = null,
			string databaseName              = null,
			string schemaName                = null,
			bool   throwExceptionIfNotExists = true)
		{
			if (table == null) throw new ArgumentNullException("table");

			var tbl = (Table<T>)table;

			if (throwExceptionIfNotExists)
			{
				QueryRunner.DropTable<T>.Query(tbl.DataContext, tableName ?? tbl.TableName, databaseName ?? tbl.DatabaseName, schemaName ?? tbl.SchemaName);
			}
			else try
			{
				QueryRunner.DropTable<T>.Query(tbl.DataContext, tableName ?? tbl.TableName, databaseName ?? tbl.DatabaseName, schemaName ?? tbl.SchemaName);
			}
			catch
			{
			}
		}

#if !NOASYNC

		public static async Task DropTableAsync<T>(
			[NotNull] this IDataContext dataContext,
			string tableName                 = null,
			string databaseName              = null,
			string schemaName                = null,
			bool   throwExceptionIfNotExists = true,
			CancellationToken   token        = default(CancellationToken),
			TaskCreationOptions options      = TaskCreationOptions.None)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");

			if (throwExceptionIfNotExists)
			{
				await QueryRunner.DropTable<T>.QueryAsync(dataContext, tableName, databaseName, schemaName, token, options);
			}
			else try
			{
				await QueryRunner.DropTable<T>.QueryAsync(dataContext, tableName, databaseName, schemaName, token, options);
			}
			catch
			{
			}
		}

		public static async Task DropTableAsync<T>(
			[NotNull] this ITable<T> table,
			string tableName                 = null,
			string databaseName              = null,
			string schemaName                = null,
			bool   throwExceptionIfNotExists = true,
			CancellationToken   token        = default(CancellationToken),
			TaskCreationOptions options      = TaskCreationOptions.None)
		{
			if (table == null) throw new ArgumentNullException("table");

			var tbl = (Table<T>)table;

			if (throwExceptionIfNotExists)
			{
				await QueryRunner.DropTable<T>.QueryAsync(
					tbl.DataContext, tableName ?? tbl.TableName, databaseName ?? tbl.DatabaseName, schemaName ?? tbl.SchemaName, token, options);
			}
			else try
			{
				await QueryRunner.DropTable<T>.QueryAsync(
					tbl.DataContext, tableName ?? tbl.TableName, databaseName ?? tbl.DatabaseName, schemaName ?? tbl.SchemaName, token, options);
			}
			catch
			{
			}
		}


#endif

#endregion
	}
}
