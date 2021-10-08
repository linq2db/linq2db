using System;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using LinqToDB.Expressions;
using LinqToDB.Linq;

namespace LinqToDB
{
	public static partial class LinqExtensions
	{
		/// <summary>
		/// Defines inner or outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TOuter">Type of record for left join operand.</typeparam>
		/// <typeparam name="TInner">Type of record for right join operand.</typeparam>
		/// <typeparam name="TResult">The type of the result elements.</typeparam>
		/// <param name="outer">Left join operand.</param>
		/// <param name="inner">Right join operand.</param>
		/// <param name="joinType">Type of join.</param>
		/// <param name="joinHint">Type of join hint.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <param name="resultSelector">A function to create a result element from two matching elements.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TResult> Join<TOuter, TInner, TResult>(
				this			IQueryable<TOuter>						  outer,
								IQueryable<TInner>                        inner,
			[SqlQueryDependent] SqlJoinType                               joinType,
			[SqlQueryDependent] SqlJoinHint                               joinHint,
			[InstantHandle]     Expression<Func<TOuter, TInner, bool>>    predicate,
			[InstantHandle]     Expression<Func<TOuter, TInner, TResult>> resultSelector)
		{
			if (outer          == null) throw new ArgumentNullException(nameof(outer));
			if (inner          == null) throw new ArgumentNullException(nameof(inner));
			if (predicate      == null) throw new ArgumentNullException(nameof(predicate));
			if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

			var currentOuter = ProcessSourceQueryable?.Invoke(outer) ?? outer;

			return currentOuter.Provider.CreateQuery<TResult>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(Join, outer, inner, joinType, joinHint, predicate, resultSelector),
					currentOuter.Expression,
					inner.Expression,
					Expression.Constant(joinType),
					Expression.Constant(joinHint),
					Expression.Quote(predicate),
					Expression.Quote(resultSelector)));
		}

		/// <summary>
		/// Defines inner or outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TSource">Type of record for right join operand.</typeparam>
		/// <param name="source">Right join operand.</param>
		/// <param name="joinType">Type of join.</param>
		/// <param name="joinHint">Type of join hint.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> Join<TSource>(
			this     IQueryable<TSource>             source,
			[SqlQueryDependent] SqlJoinType          joinType,
			[SqlQueryDependent] SqlJoinHint          joinHint,
			[InstantHandle]     Expression<Func<TSource, bool>> predicate)
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(Join, source, joinType, joinHint, predicate),
					currentSource.Expression,
					Expression.Constant(joinType),
					Expression.Constant(joinHint),
					Expression.Quote(predicate)));
		}
	}

	/// <summary>
	/// Defines join type hint. Used with join LINQ helpers.
	/// </summary>
	public enum SqlJoinHint
	{
		/// <summary>
		/// Loop join hint.
		/// </summary>
		Loop,
		/// <summary>
		/// Hash join hint.
		/// </summary>
		Hash,
		/// <summary>
		/// Merge join hint.
		/// </summary>
		Merge,
		/// <summary>
		/// Remote join hint.
		/// </summary>
		Remote,
	}
}
