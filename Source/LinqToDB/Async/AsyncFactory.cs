using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Extensions;

namespace LinqToDB.Async
{
	/// <summary>
	/// Provides factory methods to create async wrappers for <see cref="IDbConnection"/> and <see cref="IDbTransaction"/> instances.
	/// </summary>
	[PublicAPI]
	public static class AsyncFactory
	{
		private static readonly Type?[] _noTokenParams         = new Type?[] { null };

		private static readonly Type[] _tokenParams            = new Type[] { typeof(CancellationToken) };
		private static readonly Type[] _beginTransactionParams = new Type[] { typeof(IsolationLevel)   , typeof(CancellationToken) };

		private static readonly ConcurrentDictionary<Type, Func<IDbConnection, IAsyncDbConnection>> _connectionFactories = new ();

		private static readonly ConcurrentDictionary<Type, Func<IDbTransaction, IAsyncDbTransaction>> _transactionFactories = new ();

#if !NATIVE_ASYNC
		private static readonly MethodInfo _transactionWrap      = MemberHelper.MethodOf(() => Wrap<IDbTransaction>(default!)).GetGenericMethodDefinition();
#else
#pragma warning disable CA2012 // ValueTask instances returned from method calls should be directly awaited...
		private static readonly MethodInfo _transactionValueWrap = MemberHelper.MethodOf(() => WrapValue<IDbTransaction>(default!)).GetGenericMethodDefinition();
#pragma warning restore CA2012 // ValueTask instances returned from method calls should be directly awaited...
#endif

		/// <summary>
		/// Register or replace custom <see cref="IAsyncDbConnection"/> for <typeparamref name="TConnection"/> type.
		/// </summary>
		/// <typeparam name="TConnection">Connection type, which should use provided factory.</typeparam>
		/// <param name="factory"><see cref="IAsyncDbConnection"/> factory.</param>
		public static void RegisterConnectionFactory<TConnection>(Func<IDbConnection, IAsyncDbConnection> factory)
			where TConnection : IDbConnection
		{
			_connectionFactories.AddOrUpdate(typeof(TConnection), factory, (t, old) => factory);
		}

		/// <summary>
		/// Register or replace custom <see cref="IAsyncDbTransaction"/> for <typeparamref name="TTransaction"/> type.
		/// </summary>
		/// <typeparam name="TTransaction">Transaction type, which should use provided factory.</typeparam>
		/// <param name="factory"><see cref="IAsyncDbTransaction"/> factory.</param>
		public static void RegisterTransactionFactory<TTransaction>(Func<IDbTransaction, IAsyncDbTransaction> factory)
			where TTransaction : IDbTransaction
		{
			_transactionFactories.AddOrUpdate(typeof(TTransaction), factory, (t, old) => factory);
		}

		/// <summary>
		/// Wraps <see cref="IDbConnection"/> instance into type, implementing <see cref="IAsyncDbConnection"/>.
		/// </summary>
		/// <param name="connection">Connection to wrap.</param>
		/// <returns><see cref="IAsyncDbConnection"/> implementation for provided connection instance.</returns>
		public static IAsyncDbConnection Create(IDbConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			// no wrap required
			if (connection is IAsyncDbConnection asyncConnection)
				return asyncConnection;

			return _connectionFactories.GetOrAdd(connection.GetType(), ConnectionFactory)(connection);
		}

		/// <summary>
		/// Wraps <see cref="IDbTransaction"/> instance into type, implementing <see cref="IAsyncDbTransaction"/>.
		/// </summary>
		/// <param name="transaction">Transaction to wrap.</param>
		/// <returns><see cref="IAsyncDbTransaction"/> implementation for provided transaction instance.</returns>
		public static IAsyncDbTransaction Create(IDbTransaction transaction)
		{
			if (transaction == null)
				throw new ArgumentNullException(nameof(transaction));

			// no wrap required
			if (transaction is IAsyncDbTransaction asyncTransaction)
				return asyncTransaction;

			return _transactionFactories.GetOrAdd(transaction.GetType(), TransactionFactory)(transaction);
		}

		private static async Task<IAsyncDbTransaction> Wrap<TTransaction>(Task<TTransaction> transaction)
			where TTransaction: IDbTransaction
		{
			return Create(await transaction.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext));
		}

#if NATIVE_ASYNC
		private static async ValueTask<IAsyncDbTransaction> WrapValue<TTransaction>(ValueTask<TTransaction> transaction)
			where TTransaction : IDbTransaction
		{
			return Create(await transaction.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext));
		}
#endif

		private static Func<IDbTransaction, IAsyncDbTransaction> TransactionFactory(Type type)
		{
			// Task CommitAsync(CancellationToken)
			// Availability:
			// - DbTransaction (netstandard2.1, netcoreapp3.0)
			// - MySqlConnector
			// - npgsql
			// - FirebirdSql.Data.FirebirdClient 8+
			var commitAsync   = CreateDelegate<Func<IDbTransaction, CancellationToken, Task>, IDbTransaction>(type, "CommitAsync"  , _tokenParams     , _tokenParams     , _tokenParams     , false, false);

			// Task RollbackAsync(CancellationToken)
			// Availability:
			// - DbTransaction (netstandard2.1, netcoreapp3.0)
			// - MySqlConnector
			// - npgsql
			// - FirebirdSql.Data.FirebirdClient 8+
			var rollbackAsync = CreateDelegate<Func<IDbTransaction, CancellationToken, Task>, IDbTransaction>(type, "RollbackAsync", _tokenParams     , _tokenParams     , _tokenParams     , false, false);

			// ValueTask DisposeAsync()
			// Availability:
			// - DbTransaction (netstandard2.1, netcoreapp3.0)
			// - Npgsql 4.1.2+
#if !NATIVE_ASYNC
			var disposeAsync  = CreateDelegate<Func<IDbTransaction               ,      Task>, IDbTransaction>(type, "DisposeAsync" , Array<Type>.Empty, Array<Type>.Empty, Array<Type>.Empty, true , false)
#else
			var disposeAsync  = CreateDelegate<Func<IDbTransaction               , ValueTask>, IDbTransaction>(type, "DisposeAsync" , Array<Type>.Empty, Array<Type>.Empty, Array<Type>.Empty, true , true )
#endif
			// Task DisposeAsync()
			// Availability:
			// - MySqlConnector 0.57+
#if !NATIVE_ASYNC
							 ?? CreateDelegate<Func<IDbTransaction               ,      Task>, IDbTransaction>(type, "DisposeAsync" , Array<Type>.Empty, Array<Type>.Empty, Array<Type>.Empty, false, false);
#else
							 ?? CreateDelegate<Func<IDbTransaction               , ValueTask>, IDbTransaction>(type, "DisposeAsync" , Array<Type>.Empty, Array<Type>.Empty, Array<Type>.Empty, false, true );
#endif

			if (commitAsync      != null
				|| rollbackAsync != null
				|| disposeAsync  != null)
				// if at least one async method found on current type - use found methods for async calls
				return tr => new ReflectedAsyncDbTransaction(tr, commitAsync, rollbackAsync, disposeAsync);

			// no async methods detected, use default fallback-to-sync implementation
			return tr => new AsyncDbTransaction(tr);
		}

		private static Func<IDbConnection, IAsyncDbConnection> ConnectionFactory(Type type)
		{
			// ValueTask<IDbTransaction> BeginTransactionAsync(CancellationToken)
			// Availability:
			// - (stub) DbConnection (netstandard2.1, netcoreapp3.0)
			// - MySqlConnector 0.57+
			// - Npgsql 4.1.2+
#if !NATIVE_ASYNC
			var beginTransactionAsync   = CreateTaskTDelegate<Func<IDbConnection, CancellationToken           ,      Task<IAsyncDbTransaction>>, IDbConnection, IDbTransaction>(type, "BeginTransactionAsync", _tokenParams           , _transactionWrap,      true, false)
#else
			var beginTransactionAsync   = CreateTaskTDelegate<Func<IDbConnection, CancellationToken           , ValueTask<IAsyncDbTransaction>>, IDbConnection, IDbTransaction>(type, "BeginTransactionAsync", _tokenParams           , _transactionValueWrap, true, true)
#endif
			// Task<IDbTransaction> BeginTransactionAsync(CancellationToken)
			// Availability:
			// - MySql.Data
			// - MySqlConnector < 0.57
			// - FirebirdSql.Data.FirebirdClient 8+
#if !NATIVE_ASYNC
									   ?? CreateTaskTDelegate<Func<IDbConnection, CancellationToken           ,      Task<IAsyncDbTransaction>>, IDbConnection, IDbTransaction>(type, "BeginTransactionAsync", _tokenParams           , _transactionWrap,      false, false);
#else
									   ?? CreateTaskTDelegate<Func<IDbConnection, CancellationToken           , ValueTask<IAsyncDbTransaction>>, IDbConnection, IDbTransaction>(type, "BeginTransactionAsync", _tokenParams           , _transactionValueWrap, false, true);
#endif

			// ValueTask<IDbTransaction> BeginTransactionAsync(IsolationLevel, CancellationToken)
			// Availability:
			// - (stub) DbConnection (netstandard2.1, netcoreapp3.0)
			// - MySqlConnector 0.57+
			// - Npgsql 4.1.2+
#if !NATIVE_ASYNC
			var beginTransactionIlAsync = CreateTaskTDelegate<Func<IDbConnection, IsolationLevel, CancellationToken,      Task<IAsyncDbTransaction>>, IDbConnection, IDbTransaction>(type, "BeginTransactionAsync", _beginTransactionParams, _transactionWrap,      true, false)
#else
			var beginTransactionIlAsync = CreateTaskTDelegate<Func<IDbConnection, IsolationLevel, CancellationToken, ValueTask<IAsyncDbTransaction>>, IDbConnection, IDbTransaction>(type, "BeginTransactionAsync", _beginTransactionParams, _transactionValueWrap, true, true)
#endif
			// Task<IDbTransaction> BeginTransactionAsync(IsolationLevel, CancellationToken)
			// Availability:
			// - MySql.Data
			// - MySqlConnector < 0.57
			// - FirebirdSql.Data.FirebirdClient 8+
#if !NATIVE_ASYNC
									   ?? CreateTaskTDelegate<Func<IDbConnection, IsolationLevel, CancellationToken,      Task<IAsyncDbTransaction>>, IDbConnection, IDbTransaction>(type, "BeginTransactionAsync", _beginTransactionParams, _transactionWrap,      false, false);
#else
									   ?? CreateTaskTDelegate<Func<IDbConnection, IsolationLevel, CancellationToken, ValueTask<IAsyncDbTransaction>>, IDbConnection, IDbTransaction>(type, "BeginTransactionAsync", _beginTransactionParams, _transactionValueWrap, false, true);
#endif

			// Task OpenAsync(CancellationToken)
			// Availability:
			// - (stub) DbConnection
			var openAsync               = CreateDelegate<Func<IDbConnection, CancellationToken, Task>, IDbConnection>(type, "OpenAsync", _tokenParams, _tokenParams, _tokenParams, false, false);

			// Task CloseAsync(CancellationToken)
			// Availability:
			var closeAsync              = CreateDelegate<Func<IDbConnection, Task>, IDbConnection>(type, "CloseAsync", Array<Type>.Empty,   _tokenParams     , _noTokenParams,    false, false)
			// Task CloseAsync()
			// Availability:
			// - (stub) DbConnection (netstandard2.1, netcoreapp3.0)
			// - MySql.Data
			// - MySqlConnector 0.57+
			// - npgsql 4.1.0+
			// - FirebirdSql.Data.FirebirdClient 8+
									   ?? CreateDelegate<Func<IDbConnection, Task>, IDbConnection>(type, "CloseAsync", Array<Type>.Empty,   Array<Type>.Empty, Array<Type>.Empty, false, false);

			// ValueTask DisposeAsync()
			// Availability:
			// - (stub) DbConnection (netstandard2.1, netcoreapp3.0)
			// - Npgsql 4.1.2+
#if !NATIVE_ASYNC
			var disposeAsync            = CreateDelegate<Func<IDbConnection, Task     >, IDbConnection>(type, "DisposeAsync", Array<Type>.Empty, Array<Type>.Empty, Array<Type>.Empty, true , false)
#else
			var disposeAsync            = CreateDelegate<Func<IDbConnection, ValueTask>, IDbConnection>(type, "DisposeAsync", Array<Type>.Empty, Array<Type>.Empty, Array<Type>.Empty, true , true)
#endif
			// Task DisposeAsync()
			// Availability:
			// - MySqlConnector 0.57+
#if !NATIVE_ASYNC
									   ?? CreateDelegate<Func<IDbConnection,      Task>, IDbConnection>(type, "DisposeAsync", Array<Type>.Empty, Array<Type>.Empty, Array<Type>.Empty, false, false);
#else
									   ?? CreateDelegate<Func<IDbConnection, ValueTask>, IDbConnection>(type, "DisposeAsync", Array<Type>.Empty, Array<Type>.Empty, Array<Type>.Empty, false, true);
#endif


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
			var parameters     = delegateParameterTypes.Select(t => Expression.Parameter(t)).ToArray();

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
#if NATIVE_ASYNC
			else if (!returnsValueTask && returnValueTask)
			{
				//convert a Task result to a ValueTask
				body = ToValueTask(body);
			}
#endif

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
			return Expression.Call(body, "AsTask", Array<Type>.Empty);
		}

#if NATIVE_ASYNC
		/// <summary>
		/// Returns an expression which returns a <see cref="ValueTask"/> from a <see cref="Task"/>.
		/// </summary>
		private static NewExpression ToValueTask(Expression body)
		{
			var taskType = typeof(Task);

			var valueTaskType = typeof(ValueTask);

			// constructor = <<< new ValueTask(Task task) >>>
			var constructor = valueTaskType.GetConstructor(new Type[] { taskType });

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
			var constructor = valueTaskType.GetConstructor(new Type[] { taskType });

			// return new ValueTask<TResult>(body);
			return Expression.New(constructor, body);
		}
#endif

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
			var parameters = parametersTypes.Select(t => Expression.Parameter(t)).ToArray();

			Expression body = Expression.Call(Expression.Convert(pInstance, instanceType), mi, parameters);
			if (returnsValueTask && !returnValueTask)
			{
				//convert a ValueTask result to a Task
				body = ToTask(body);
			}
#if NATIVE_ASYNC
			else if (!returnsValueTask && returnValueTask)
			{
				//convert a Task result to a ValueTask
				body = ToValueTTask(body);
			}
#endif

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
