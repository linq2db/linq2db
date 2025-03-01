using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Extensions;
using LinqToDB.Internal.Async;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;

namespace LinqToDB.Internal.Async
{
	/// <summary>
	/// Provides factory methods to create async wrappers for <see cref="DbConnection"/> and <see cref="DbTransaction"/> instances.
	/// </summary>
	public static class AsyncFactory
	{
		private static readonly Type?[] _noTokenParams         = [null];

		private static readonly Type[] _tokenParams            = [typeof(CancellationToken)];
		private static readonly Type[] _beginTransactionParams = [typeof(IsolationLevel)   , typeof(CancellationToken)];

		private static readonly ConcurrentDictionary<Type, Func<DbConnection, IAsyncDbConnection>> _connectionFactories = new ();

		private static readonly ConcurrentDictionary<Type, Func<DbTransaction, IAsyncDbTransaction>> _transactionFactories = new ();

#pragma warning disable CA2012 // ValueTask instances returned from method calls should be directly awaited...
		private static readonly MethodInfo _transactionValueWrap = MemberHelper.MethodOf(() => WrapValue<DbTransaction>(default!)).GetGenericMethodDefinition();
#pragma warning restore CA2012 // ValueTask instances returned from method calls should be directly awaited...

		/// <summary>
		/// Register or replace custom <see cref="IAsyncDbConnection"/> for <typeparamref name="TConnection"/> type.
		/// </summary>
		/// <typeparam name="TConnection">Connection type, which should use provided factory.</typeparam>
		/// <param name="factory"><see cref="IAsyncDbConnection"/> factory.</param>
		public static void RegisterConnectionFactory<TConnection>(Func<DbConnection, IAsyncDbConnection> factory)
			where TConnection : DbConnection
		{
			_connectionFactories.AddOrUpdate(typeof(TConnection), factory, (t, old) => factory);
		}

		/// <summary>
		/// Register or replace custom <see cref="IAsyncDbTransaction"/> for <typeparamref name="TTransaction"/> type.
		/// </summary>
		/// <typeparam name="TTransaction">Transaction type, which should use provided factory.</typeparam>
		/// <param name="factory"><see cref="IAsyncDbTransaction"/> factory.</param>
		public static void RegisterTransactionFactory<TTransaction>(Func<DbTransaction, IAsyncDbTransaction> factory)
			where TTransaction : DbTransaction
		{
			_transactionFactories.AddOrUpdate(typeof(TTransaction), factory, (t, old) => factory);
		}

		/// <summary>
		/// Wraps <see cref="DbConnection"/> instance into type, implementing <see cref="IAsyncDbConnection"/>.
		/// </summary>
		/// <param name="connection">Connection to wrap.</param>
		/// <returns><see cref="IAsyncDbConnection"/> implementation for provided connection instance.</returns>
		public static IAsyncDbConnection Create(DbConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			// no wrap required
			if (connection is IAsyncDbConnection asyncConnection)
				return asyncConnection;

			return _connectionFactories.GetOrAdd(connection.GetType(), ConnectionFactory)(connection);
		}

		internal static IAsyncDbConnection CreateAndSetDataContext(DataConnection dataConnection, DbConnection connection)
		{
			var c = Create(connection);
				
			if (c is AsyncDbConnection asyncDbConnection)
				asyncDbConnection.DataConnection = dataConnection;

			return c;
		}

		/// <summary>
		/// Wraps <see cref="DbTransaction"/> instance into type, implementing <see cref="IAsyncDbTransaction"/>.
		/// </summary>
		/// <param name="transaction">Transaction to wrap.</param>
		/// <returns><see cref="IAsyncDbTransaction"/> implementation for provided transaction instance.</returns>
		public static IAsyncDbTransaction Create(DbTransaction transaction)
		{
			if (transaction == null)
				throw new ArgumentNullException(nameof(transaction));

			// no wrap required
			if (transaction is IAsyncDbTransaction asyncTransaction)
				return asyncTransaction;

			return _transactionFactories.GetOrAdd(transaction.GetType(), TransactionFactory)(transaction);
		}

		internal static IAsyncDbConnection SetDataContext(this IAsyncDbConnection connection, DataConnection dataConnection)
		{
			if (connection is AsyncDbConnection asyncDbConnection)
				asyncDbConnection.DataConnection = dataConnection;
			return connection;
		}

		internal static IAsyncDbTransaction CreateAndSetDataContext(DataConnection? dataConnection, DbTransaction transaction)
		{
			var t = Create(transaction);

			if (t is AsyncDbTransaction asyncDbTransaction)
				asyncDbTransaction.DataConnection = dataConnection;

			return t;
		}

		private static async ValueTask<IAsyncDbTransaction> WrapValue<TTransaction>(ValueTask<TTransaction> transaction)
			where TTransaction : DbTransaction
		{
			return Create(await transaction.ConfigureAwait(false));
		}

		private static Func<DbTransaction, IAsyncDbTransaction> TransactionFactory(Type type)
		{
			// Task CommitAsync(CancellationToken)
			// Availability:
			// - DbTransaction (netstandard2.1, netcoreapp3.0)
			// - MySqlConnector
			// - npgsql
			// - FirebirdSql.Data.FirebirdClient 8+
			var commitAsync   = CreateDelegate<Func<DbTransaction, CancellationToken, Task>, DbTransaction>(type, "CommitAsync"  , _tokenParams     , _tokenParams     , _tokenParams     , false, false);

			// Task RollbackAsync(CancellationToken)
			// Availability:
			// - DbTransaction (netstandard2.1, netcoreapp3.0)
			// - MySqlConnector
			// - npgsql
			// - FirebirdSql.Data.FirebirdClient 8+
			var rollbackAsync = CreateDelegate<Func<DbTransaction, CancellationToken, Task>, DbTransaction>(type, "RollbackAsync", _tokenParams     , _tokenParams     , _tokenParams     , false, false);

			// ValueTask DisposeAsync()
			// Availability:
			// - DbTransaction (netstandard2.1, netcoreapp3.0)
			// - Npgsql 4.1.2+
			var disposeAsync  = CreateDelegate<Func<DbTransaction               , ValueTask>, DbTransaction>(type, "DisposeAsync" , [], [], [], true , true )
			// Task DisposeAsync()
			// Availability:
			// - MySqlConnector 0.57+
							 ?? CreateDelegate<Func<DbTransaction               , ValueTask>, DbTransaction>(type, "DisposeAsync" , [], [], [], false, true );

			if (commitAsync      != null
				|| rollbackAsync != null
				|| disposeAsync  != null)
				// if at least one async method found on current type - use found methods for async calls
				return tr => new ReflectedAsyncDbTransaction(tr, commitAsync, rollbackAsync, disposeAsync);

			// no async methods detected, use default fallback-to-sync implementation
			return tr => new AsyncDbTransaction(tr);
		}

		private static Func<DbConnection, IAsyncDbConnection> ConnectionFactory(Type type)
		{
			// ValueTask<DbTransaction> BeginTransactionAsync(CancellationToken)
			// Availability:
			// - (stub) DbConnection (netstandard2.1, netcoreapp3.0)
			// - MySqlConnector 0.57+
			// - Npgsql 4.1.2+
			var beginTransactionAsync   = CreateTaskTDelegate<Func<DbConnection, CancellationToken           , ValueTask<IAsyncDbTransaction>>, DbConnection, DbTransaction>(type, "BeginTransactionAsync", _tokenParams           , _transactionValueWrap, true, true)
			// Task<DbTransaction> BeginTransactionAsync(CancellationToken)
			// Availability:
			// - MySql.Data
			// - MySqlConnector < 0.57
			// - FirebirdSql.Data.FirebirdClient 8+
									   ?? CreateTaskTDelegate<Func<DbConnection, CancellationToken           , ValueTask<IAsyncDbTransaction>>, DbConnection, DbTransaction>(type, "BeginTransactionAsync", _tokenParams           , _transactionValueWrap, false, true);

			// ValueTask<DbTransaction> BeginTransactionAsync(IsolationLevel, CancellationToken)
			// Availability:
			// - (stub) DbConnection (netstandard2.1, netcoreapp3.0)
			// - MySqlConnector 0.57+
			// - Npgsql 4.1.2+
			var beginTransactionIlAsync = CreateTaskTDelegate<Func<DbConnection, IsolationLevel, CancellationToken, ValueTask<IAsyncDbTransaction>>, DbConnection, DbTransaction>(type, "BeginTransactionAsync", _beginTransactionParams, _transactionValueWrap, true, true)
			// Task<DbTransaction> BeginTransactionAsync(IsolationLevel, CancellationToken)
			// Availability:
			// - MySql.Data
			// - MySqlConnector < 0.57
			// - FirebirdSql.Data.FirebirdClient 8+
									   ?? CreateTaskTDelegate<Func<DbConnection, IsolationLevel, CancellationToken, ValueTask<IAsyncDbTransaction>>, DbConnection, DbTransaction>(type, "BeginTransactionAsync", _beginTransactionParams, _transactionValueWrap, false, true);

			// Task OpenAsync(CancellationToken)
			// Availability:
			// - (stub) DbConnection
			var openAsync               = CreateDelegate<Func<DbConnection, CancellationToken, Task>, DbConnection>(type, "OpenAsync", _tokenParams, _tokenParams, _tokenParams, false, false);

			// Task CloseAsync(CancellationToken)
			// Availability:
			var closeAsync              = CreateDelegate<Func<DbConnection, Task>, DbConnection>(type, "CloseAsync", [], _tokenParams, _noTokenParams, false, false)
			// Task CloseAsync()
			// Availability:
			// - (stub) DbConnection (netstandard2.1, netcoreapp3.0)
			// - MySql.Data
			// - MySqlConnector 0.57+
			// - npgsql 4.1.0+
			// - FirebirdSql.Data.FirebirdClient 8+
									   ?? CreateDelegate<Func<DbConnection, Task>, DbConnection>(type, "CloseAsync", [], [], [], false, false);

			// ValueTask DisposeAsync()
			// Availability:
			// - (stub) DbConnection (netstandard2.1, netcoreapp3.0)
			// - Npgsql 4.1.2+
			var disposeAsync            = CreateDelegate<Func<DbConnection, ValueTask>, DbConnection>(type, "DisposeAsync", [], [], [], true , true)
			// Task DisposeAsync()
			// Availability:
			// - MySqlConnector 0.57+
									   ?? CreateDelegate<Func<DbConnection, ValueTask>, DbConnection>(type, "DisposeAsync", [], [], [], false, true);

			if (beginTransactionAsync      != null
				|| beginTransactionIlAsync != null
				|| openAsync               != null
				|| closeAsync              != null
				|| disposeAsync            != null)
				// if at least one async method found on current type - use found methods for async calls
				return cn => new ReflectedAsyncDbConnection(
					cn,
					beginTransactionAsync,
					beginTransactionIlAsync,
					openAsync,
					closeAsync,
					disposeAsync);

			// default sync implementation
			return connection => new AsyncDbConnection(connection);
		}

		private static TDelegate? CreateDelegate<TDelegate, TInstance>(
			Type    instanceType,
			string  methodName,
			Type[]  delegateParameterTypes,
			Type[]  methodParameterTypes,
			Type?[] mappedParameterTypes,
			bool    returnsValueTask,
			bool    returnValueTask)
			where TDelegate : Delegate
		{
			var mi = instanceType.GetPublicInstanceMethodEx(methodName, methodParameterTypes);

			if (mi == null
				|| (!returnsValueTask && mi.ReturnType          != typeof(Task))
				|| (returnsValueTask  && mi.ReturnType.FullName != "System.Threading.Tasks.ValueTask"))
				return default;

			var pInstance      = Expression.Parameter(typeof(TInstance));
			var parameters     = delegateParameterTypes.Select(Expression.Parameter).ToArray();

			var callParameters = new List<Expression>();
			for (var i = 0; i < methodParameterTypes.Length; i++)
				if (mappedParameterTypes[i] != null)
					callParameters.Add(parameters[i]);
				else
					callParameters.Add(Expression.Default(methodParameterTypes[i]));

			Expression body = Expression.Call(Expression.Convert(pInstance, instanceType), mi, callParameters);
			if (returnsValueTask && !returnValueTask)
			{
				//convert a ValueTask result to a Task
				body = ToTask(body);
			}
			else if (!returnsValueTask && returnValueTask)
			{
				//convert a Task result to a ValueTask
				body = ToValueTask(body);
			}

			return Expression
				.Lambda<TDelegate>(
					body,
					new[] { pInstance }.Concat(parameters))
				.CompileExpression();
		}

		/// <summary>
		/// Returns an expression which returns a <see cref="Task{TResult}"/> from a ValueTask.
		/// </summary>
		private static MethodCallExpression ToTask(Expression body)
		{
			return Expression.Call(body, "AsTask", []);
		}

		/// <summary>
		/// Returns an expression which returns a <see cref="ValueTask"/> from a <see cref="Task"/>.
		/// </summary>
		private static NewExpression ToValueTask(Expression body)
		{
			var taskType = typeof(Task);

			var valueTaskType = typeof(ValueTask);

			// constructor = <<< new ValueTask(Task task) >>>
			var constructor = valueTaskType.GetConstructor(new Type[] { taskType })!;

			// return new ValueTask(body);
			return Expression.New(constructor, body);
		}

		/// <summary>
		/// Returns an expression which returns a <see cref="ValueTask{TResult}"/> from a <see cref="Task{TResult}"/>.
		/// </summary>
		private static NewExpression ToValueTTask(Expression body)
		{
			// taskType = typeof(Task<TResult>);
			var taskType = body.Type;

			// dataType = typeof(TResult);
			var dataType = taskType.GenericTypeArguments[0];

			// valueTaskType = typeof(ValueTask<TResult>);
			var valueTaskType = typeof(ValueTask<>).MakeGenericType(dataType);

			// constructor = <<< new ValueTask<TResult>(Task<TResult> task) >>>
			var constructor = valueTaskType.GetConstructor(new Type[] { taskType })!;

			// return new ValueTask<TResult>(body);
			return Expression.New(constructor, body);
		}

		private static TDelegate? CreateTaskTDelegate<TDelegate, TInstance, TTask>(
			Type       instanceType,
			string     methodName,
			Type[]     parametersTypes,
			MethodInfo taskConverter,
			bool       returnsValueTask,
			bool       returnValueTask)
			where TDelegate : Delegate
		{
			var mi = instanceType.GetPublicInstanceMethodEx(methodName, parametersTypes);

			if (mi == null
				|| !mi.ReturnType.IsGenericType
				|| !typeof(TTask).IsAssignableFrom(mi.ReturnType.GetGenericArguments()[0])
				|| (!returnsValueTask && mi.ReturnType.GetGenericTypeDefinition()          != typeof(Task<>))
				|| ( returnsValueTask && mi.ReturnType.GetGenericTypeDefinition().FullName != "System.Threading.Tasks.ValueTask`1"))
				return default;

			var pInstance  = Expression.Parameter(typeof(TInstance));
			var parameters = parametersTypes.Select(Expression.Parameter).ToArray();

			Expression body = Expression.Call(Expression.Convert(pInstance, instanceType), mi, parameters);
			if (returnsValueTask && !returnValueTask)
			{
				//convert a ValueTask result to a Task
				body = ToTask(body);
			}
			else if (!returnsValueTask && returnValueTask)
			{
				//convert a Task result to a ValueTask
				body = ToValueTTask(body);
			}

			return Expression
				.Lambda<TDelegate>(
					Expression.Call(
						taskConverter.MakeGenericMethod(mi.ReturnType.GetGenericArguments()[0]),
						body),
					new[] { pInstance }.Concat(parameters))
				.CompileExpression();
		}
	}
}
