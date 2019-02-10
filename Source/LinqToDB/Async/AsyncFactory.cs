using JetBrains.Annotations;
using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Async
{
	/// <summary>
	/// Provides factory methods to create async wrappers for <see cref="IDbConnection"/> and <see cref="IDbTransaction"/> instances.
	/// </summary>
	[PublicAPI]
	public static class AsyncFactory
	{
		private static readonly Type[] _tokenParams            = new[] { typeof(CancellationToken) };
		private static readonly Type[] _beginTransactionParams = new[] { typeof(IsolationLevel)   , typeof(CancellationToken) };

		private static readonly ConcurrentDictionary<Type, Func<IDbConnection, IAsyncDbConnection>> _connectionFactories
			= new ConcurrentDictionary<Type, Func<IDbConnection, IAsyncDbConnection>>();

		private static readonly ConcurrentDictionary<Type, Func<IDbTransaction, IAsyncDbTransaction>> _transactionFactories
			= new ConcurrentDictionary<Type, Func<IDbTransaction, IAsyncDbTransaction>>();

		private static readonly MethodInfo _transactionWrap = MemberHelper.MethodOf(() => Wrap<IDbTransaction>(default)).GetGenericMethodDefinition();

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
			return Create(await transaction);
		}

		private static Func<IDbTransaction, IAsyncDbTransaction> TransactionFactory(Type type)
		{
			var commitAsync   = CreateDelegate<Func<IDbTransaction, CancellationToken, Task>, IDbTransaction>(type, "CommitAsync"  , _tokenParams     , typeof(Task));
			var rollbackAsync = CreateDelegate<Func<IDbTransaction, CancellationToken, Task>, IDbTransaction>(type, "RollbackAsync", _tokenParams     , typeof(Task));

			if (commitAsync      != null
				|| rollbackAsync != null)
				// if at least one async method found on current type - use found methods for async calls
				return tr => new ReflectedAsyncDbTransaction(tr, commitAsync, rollbackAsync);

			// no async methods detected, use default fallback-to-sync implementation
			return tr => new AsyncDbTransaction(tr);
		}

		private static Func<IDbConnection, IAsyncDbConnection> ConnectionFactory(Type type)
		{
			var beginTransactionAsync   = CreateTaskTDelegate<Func<IDbConnection, CancellationToken                , Task<IAsyncDbTransaction>>, IDbConnection, IDbTransaction, IAsyncDbTransaction>(type, "BeginTransactionAsync", _tokenParams           , _transactionWrap);
			var beginTransactionIlAsync = CreateTaskTDelegate<Func<IDbConnection, IsolationLevel, CancellationToken, Task<IAsyncDbTransaction>>, IDbConnection, IDbTransaction, IAsyncDbTransaction>(type, "BeginTransactionAsync", _beginTransactionParams, _transactionWrap);
			var closeAsync              = CreateDelegate     <Func<IDbConnection, CancellationToken                , Task>                     , IDbConnection>(type, "CloseAsync"         , _tokenParams         , typeof(Task));

			if (beginTransactionAsync      != null
				|| beginTransactionIlAsync != null
				|| closeAsync              != null)
				// if at least one async method found on current type - use found methods for async calls
				return cn => new ReflectedAsyncDbConnection(cn, beginTransactionAsync, beginTransactionIlAsync, closeAsync);

			// default sync implementation
			return connection => new AsyncDbConnection(connection);
		}

		private static TDelegate CreateDelegate<TDelegate, TInstance>(
			Type   instanceType,
			string methodName,
			Type[] parametersTypes,
			Type   returnType)
			where TDelegate : Delegate
			//where TDelegate : class
		{
			var mi = instanceType.GetPublicInstanceMethodEx(methodName, parametersTypes);

			if (mi == null || mi.ReturnType != returnType)
				return default;

			var pInstance  = Expression.Parameter(typeof(TInstance));
			var parameters = parametersTypes.Select(t => Expression.Parameter(t)).ToArray();

			return Expression
				.Lambda<TDelegate>(
					Expression.Call(Expression.Convert(pInstance, instanceType), mi, parameters),
					new[] { pInstance }.Concat(parameters))
				.Compile();
		}

		private static TDelegate CreateTaskTDelegate<TDelegate, TInstance, TTask, TResult>(
			Type       instanceType,
			string     methodName,
			Type[]     parametersTypes,
			MethodInfo taskConverter)
			where TDelegate : Delegate
			//where TDelegate : class
		{
			var mi = instanceType.GetPublicInstanceMethodEx(methodName, parametersTypes);

			if (mi == null
				|| !mi.ReturnType.IsGenericTypeEx()
				|| mi.ReturnType.GetGenericTypeDefinition() != typeof(Task<>)
				|| mi.ReturnType.GetGenericArgumentsEx()[0].IsSubclassOfEx(typeof(TTask)))
				return default;

			var pInstance  = Expression.Parameter(typeof(TInstance));
			var parameters = parametersTypes.Select(t => Expression.Parameter(t)).ToArray();

			return Expression
				.Lambda<TDelegate>(
					Expression.Call(
						taskConverter.MakeGenericMethod(mi.ReturnType.GetGenericArgumentsEx()[0]),
						Expression.Call(Expression.Convert(pInstance, instanceType), mi, parameters)),
					new[] { pInstance }.Concat(parameters))
				.Compile();
		}
	}
}
