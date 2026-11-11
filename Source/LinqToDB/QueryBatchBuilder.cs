using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Expressions;
using LinqToDB.Internal.Async;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.Linq.Builder;

namespace LinqToDB
{
	/// <summary>
	/// Collects reads from any number of call sites and runs them in one round trip, as one
	/// <see cref="LinqExtensions.QueryBatch{T}(IDataContext, Expression{Func{T}})"/> call. Created by
	/// <see cref="LinqExtensions.CreateQueryBatch(IDataContext)"/>.
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
	/// A batch is executed once. The parts are read as one result, so a part that fails fails the whole batch; a batch that
	/// failed keeps no values and can be executed again, in the transaction of its context as that transaction stands.
	/// Executing an empty batch sends nothing.
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

		internal bool IsExecuted => _state == State.Executed;

		/// <summary>
		/// Adds a read to the batch.
		/// </summary>
		/// <typeparam name="T">Type of the result.</typeparam>
		/// <param name="part">The read, written as a selector of
		/// <see cref="LinqExtensions.QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.</param>
		/// <returns>The result, readable after the batch is executed.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="part"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException"><paramref name="part"/> returns a task, or creates one, which could run a separate
		/// query outside the batch. Reading the result of a task held by a captured variable is allowed.</exception>
		/// <exception cref="InvalidOperationException">The batch has been executed or is being executed.</exception>
		public QueryBatchResult<T> Add<T>(Expression<Func<T>> part)
		{
			ArgumentNullException.ThrowIfNull(part);

			ThrowIfNotPending();

			if (IsTask(StripConvert(part.Body).Type) || part.Body.Find(static e => IsTask(e.Type) && !IsHeldTask(e)) != null)
				throw new ArgumentException(ErrorHelper.Error_QueryBatch_TaskPart, nameof(part));

			var result = new QueryBatchResult<T>(this);

			_parts.Add(part);
			_setters.Add(result.SetValue);

			return result;
		}

		/// <summary>
		/// Executes the batch and sets the value of every result it returned.
		/// </summary>
		/// <exception cref="InvalidOperationException">The batch has been executed or is being executed, or a part's
		/// <c>First()</c> or <c>Single()</c> failed as described for
		/// <see cref="LinqExtensions.QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.</exception>
		/// <exception cref="LinqToDBException">The context has pending <see cref="IDataContext.QueryHints"/> or
		/// <see cref="IDataContext.NextQueryHints"/>, or the provider cannot translate the parts as one statement.</exception>
		public void Execute()
		{
			ThrowIfNotPending();

			if (_parts.Count == 0)
			{
				_state = State.Executed;
				return;
			}

			_state = State.Running;

			try
			{
				var (expression, query) = BuildQuery();

				SetValues(((IQueryProvider)query).Execute<object>(expression));

				Complete();
			}
			catch
			{
				_state = State.Pending;
				throw;
			}
		}

		/// <summary>
		/// Executes the batch asynchronously and sets the value of every result it returned.
		/// </summary>
		/// <param name="token">Optional asynchronous operation cancellation token. A token cancelled before the call sends
		/// nothing.</param>
		/// <returns>A task that completes when every value is set.</returns>
		/// <exception cref="InvalidOperationException">The batch has been executed or is being executed, or a part's
		/// <c>First()</c> or <c>Single()</c> failed as described for
		/// <see cref="LinqExtensions.QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.</exception>
		/// <exception cref="LinqToDBException">The context has pending <see cref="IDataContext.QueryHints"/> or
		/// <see cref="IDataContext.NextQueryHints"/>, or the provider cannot translate the parts as one statement.</exception>
		/// <exception cref="OperationCanceledException"><paramref name="token"/> was cancelled.</exception>
		public async Task ExecuteAsync(CancellationToken token = default)
		{
			ThrowIfNotPending();

			token.ThrowIfCancellationRequested();

			if (_parts.Count == 0)
			{
				_state = State.Executed;
				return;
			}

			_state = State.Running;

			try
			{
				var (expression, query) = BuildQuery();

				SetValues(await ((IQueryProviderAsync)query).ExecuteAsync<object>(expression, token).ConfigureAwait(false));

				Complete();
			}
			catch
			{
				_state = State.Pending;
				throw;
			}
		}

		// Single use: once executed, the parts and the setters are never read again, so a result kept after the run pins
		// neither the other parts' values nor their captured closures.
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

		static bool IsTask(Type type)
		{
			return typeof(Task).IsAssignableFrom(type)
				|| type == typeof(ValueTask)
				|| type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>);
		}

		static Expression StripConvert(Expression expression)
		{
			while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert)
				expression = convert.Operand;

			return expression;
		}

		// A task already held by a captured variable or a static field starts nothing when it is read; a call, a constructor,
		// a property or a field of a value computed in the part may start one.
		static bool IsHeldTask(Expression expression)
		{
			while (true)
			{
				switch (expression)
				{
					case ConstantExpression:
						return true;

					case MemberExpression { Member: FieldInfo } field:
						if (field.Expression == null)
							return true;

						expression = field.Expression;
						break;

					default:
						return false;
				}
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
		void SetValues(object? tuple)
		{
			var holder = tuple!;

			for (var i = 0; i < _setters.Count; i++)
			{
				var index = i % 7;

				if (i > 0 && index == 0)
					holder = holder.GetType().GetField("Rest")!.GetValue(holder)!;

				_setters[i](holder.GetType().GetField("Item" + (index + 1).ToString(CultureInfo.InvariantCulture))!.GetValue(holder));
			}
		}
	}
}
