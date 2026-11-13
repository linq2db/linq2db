using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.Async;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.Linq.Builder;

namespace LinqToDB
{
	/// <summary>
	/// Collects reads from any number of call sites, runs them in one round trip as one
	/// <see cref="LinqExtensions.QueryBatch{T}(IDataContext, Expression{Func{T}})"/> call, and hands each value to its setter.
	/// Created by <see cref="LinqExtensions.CreateQueryBatch(IDataContext)"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Every part is read as a part of that call, so its rules apply: where the context and its provider can combine
	/// statements the batch is sent as one command, otherwise its statements run one after another with the same results;
	/// <c>First()</c> and <c>Single()</c> inside a part throw when they find no row; see
	/// <see cref="LinqExtensions.QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.
	/// </para>
	/// <para>
	/// Variables a part captures are read when the batch is executed, not when the part is added. A part of type
	/// <see cref="IQueryable{T}"/> is read inside the batch and returned as rows in memory.
	/// </para>
	/// <para>
	/// The parts — their number, order and types — are part of the query the batch builds and caches: a batch whose parts follow
	/// the data builds a query for each distinct set of parts.
	/// </para>
	/// <para>
	/// Every part is read before any setter runs, so a part that reads what a setter assigns sees the value it had before the
	/// batch was executed. The setters then run in the order their parts were added, synchronously, where the execution
	/// completes; an asynchronous lambda passed as a setter is not awaited.
	/// </para>
	/// <para>
	/// A batch is executed once. The parts are read as one result, so a part that fails fails the whole batch; a batch that
	/// failed before its setters ran has assigned nothing and can be executed again, in the transaction of its context as that
	/// transaction stands. Once the setters run the batch is executed: a setter that throws ends the run, and the setters after
	/// it do not run. Executing an empty batch sends nothing.
	/// </para>
	/// <para>
	/// A batch is not thread-safe.
	/// </para>
	/// </remarks>
	public sealed class QueryBatchBuilder
	{
		enum State
		{
			Pending,
			Running,
			Executed,
		}

		readonly IDataContext           _dataContext;
		readonly List<LambdaExpression> _parts   = new();
		readonly List<Action<object?>>  _setters = new();

		State _state;

		internal QueryBatchBuilder(IDataContext dataContext)
		{
			_dataContext = dataContext;
		}

		/// <summary>
		/// Adds a read to the batch and the setter that receives its value.
		/// </summary>
		/// <typeparam name="T">Type of the value.</typeparam>
		/// <param name="query">The read, written as a selector of
		/// <see cref="LinqExtensions.QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.</param>
		/// <param name="setter">Receives the value once the batch has read every part.</param>
		/// <exception cref="ArgumentNullException"><paramref name="query"/> or <paramref name="setter"/> is
		/// <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">An expression in <paramref name="query"/> may create a task, which could run a query
		/// outside the batch. Reading a task held by a captured variable is allowed.</exception>
		/// <exception cref="InvalidOperationException">The batch has been executed or is being executed.</exception>
		public void Set<T>(Expression<Func<T>> query, Action<T> setter)
		{
			ArgumentNullException.ThrowIfNull(query);
			ArgumentNullException.ThrowIfNull(setter);

			ThrowIfNotPending();

			// The rule the QueryBatch node enforces when the batch is built; checked here so the part is never kept.
			if (ScalarSelectBuilder.CreatesTask(query.Body))
				throw new ArgumentException(ErrorHelper.Error_QueryBatch_TaskPart, nameof(query));

			_parts.Add(query);
			_setters.Add(value => setter((T)value!));
		}

		/// <summary>
		/// Executes the batch and hands every value to its setter.
		/// </summary>
		/// <exception cref="InvalidOperationException">The batch has been executed or is being executed, or a part's
		/// <c>First()</c> or <c>Single()</c> failed as described for
		/// <see cref="LinqExtensions.QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.</exception>
		/// <exception cref="LinqToDBException">The context has pending <see cref="IDataContext.QueryHints"/> or
		/// <see cref="IDataContext.NextQueryHints"/>, the provider cannot translate the parts as one statement, or an expression
		/// in a part may create a task, which could run a query outside the batch.</exception>
		public void Execute()
		{
			ThrowIfNotPending();

			if (_parts.Count == 0)
			{
				_state = State.Executed;
				return;
			}

			_state = State.Running;

			object?[] values;

			try
			{
				var (expression, query) = BuildQuery();

				values = ReadValues(((IQueryProvider)query).Execute<object>(expression));
			}
			catch
			{
				_state = State.Pending;
				throw;
			}

			Deliver(values);
		}

		/// <summary>
		/// Executes the batch asynchronously and hands every value to its setter.
		/// </summary>
		/// <param name="token">Optional asynchronous operation cancellation token. A token cancelled before the call sends
		/// nothing.</param>
		/// <returns>A task that completes when every setter has run.</returns>
		/// <exception cref="InvalidOperationException">The batch has been executed or is being executed, or a part's
		/// <c>First()</c> or <c>Single()</c> failed as described for
		/// <see cref="LinqExtensions.QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.</exception>
		/// <exception cref="LinqToDBException">The context has pending <see cref="IDataContext.QueryHints"/> or
		/// <see cref="IDataContext.NextQueryHints"/>, the provider cannot translate the parts as one statement, or an expression
		/// in a part may create a task, which could run a query outside the batch.</exception>
		/// <exception cref="OperationCanceledException"><paramref name="token"/> was cancelled and the batch has parts.</exception>
		public async Task ExecuteAsync(CancellationToken token = default)
		{
			ThrowIfNotPending();

			if (_parts.Count == 0)
			{
				_state = State.Executed;
				return;
			}

			_state = State.Running;

			object?[] values;

			try
			{
				var (expression, query) = BuildQuery();

				values = ReadValues(await ((IQueryProviderAsync)query).ExecuteAsync<object>(expression, token).ConfigureAwait(false));
			}
			catch
			{
				_state = State.Pending;
				throw;
			}

			Deliver(values);
		}

		// The batch is executed before the first setter runs, so a setter that throws ends a run that is not repeated.
		void Deliver(object?[] values)
		{
			var setters = _setters.ToArray();

			Complete();

			for (var i = 0; i < setters.Length; i++)
				setters[i](values[i]);
		}

		// Single use: once executed, the parts and the setters are never read again, so a batch kept after the run pins neither
		// the parts' captured closures nor the setters' targets.
		void Complete()
		{
			_state = State.Executed;

			_parts.Clear();
			_setters.Clear();
		}

		void ThrowIfNotPending()
		{
			switch (_state)
			{
				case State.Running  : throw new InvalidOperationException(ErrorHelper.Error_QueryBatch_Running);
				case State.Executed : throw new InvalidOperationException(ErrorHelper.Error_QueryBatch_AlreadyExecuted);
			}
		}

		// The parts become one QueryBatch call whose selector builds a tuple of them, nested through Rest beyond seven; the
		// tuple type, which keys the query cache, is known only now.
		(Expression expression, IQueryable query) BuildQuery()
		{
			var bodies     = _parts.Select(static p => p.Body).ToArray();
			var tupleType  = ExpressionBuilder.BuildValueTupleType(bodies.Select(static b => b.Type).ToArray());
			var body       = ExpressionBuilder.BuildValueTupleNew(tupleType, bodies);
			var expression = LinqExtensions.CreateQueryBatchExpression(_dataContext, Expression.Lambda(body));

			return (expression, ExpressionQueryImpl.CreateQuery(tupleType, _dataContext, expression));
		}

		// Seven values per tuple, the rest in the nested tuple of its Rest field.
		object?[] ReadValues(object? tuple)
		{
			var values = new object?[_setters.Count];
			var holder = tuple!;

			for (var i = 0; i < values.Length; i++)
			{
				var index = i % 7;

				if (i > 0 && index == 0)
					holder = holder.GetType().GetField("Rest")!.GetValue(holder)!;

				values[i] = holder.GetType().GetField("Item" + (index + 1).ToString(CultureInfo.InvariantCulture))!.GetValue(holder);
			}

			return values;
		}
	}
}
