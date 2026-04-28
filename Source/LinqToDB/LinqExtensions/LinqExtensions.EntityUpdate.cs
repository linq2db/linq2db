using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using LinqToDB.Expressions;
using LinqToDB.Internal.Linq;
using LinqToDB.Linq;

namespace LinqToDB
{
	public static partial class LinqExtensions
	{
		#region Entity Update

		// ---------------------------------------------------------------------
		// Entity-shaped Update (issue #2558 follow-up):
		//   db.Users.Update(user, b => b.Set(x => x.UpdatedAt, () => DateTime.UtcNow));
		//
		// Match is by primary key — the entity's PK column values supply the WHERE.
		// Chain methods (.Set with 3 overloads, .Ignore) live on IEntityUpdateBuilder<T>;
		// captured as Expression<Func<IEntityUpdateBuilder<T>, IEntityUpdateBuilder<T>>>
		// and walked by EntityUpdateBuilder
		// (Source/LinqToDB/Internal/Linq/Builder/EntityUpdateBuilder.cs), which synthesises
		// the existing q.Update(predicate, setter) shape and defers to UpdateBuilder.
		//
		// Async overloads embed the SYNC Update MethodInfo into the captured expression
		// tree (not UpdateAsync) — async-ness is handled by ExecuteAsync.
		// ---------------------------------------------------------------------

		static readonly MethodInfo _entityUpdateMethodInfo = MemberHelper.MethodOfGeneric(
			(ITable<int> t, int item, Expression<Func<IEntityUpdateBuilder<int>, IEntityUpdateBuilder<int>>> configure) => t.Update(item, configure));

		/// <summary>
		/// Updates a single entity in the target table by primary-key match, configured by a fluent builder.
		/// Whole-object defaults (every non-PK, non-skip-on-update column) are written from
		/// <paramref name="item"/>; user-supplied <c>.Set</c> overrides and <c>.Ignore</c> exclusions
		/// overlay on top.
		/// </summary>
		/// <typeparam name="T">Target table record type. Must declare at least one primary-key column.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="item">Entity to update. Its PK columns identify the row to update.</param>
		/// <param name="configure">Fluent configuration expression.</param>
		/// <returns>Number of affected records.</returns>
		public static int Update<T>(
			                this ITable<T>                                                                  target,
			                T                                                                               item,
			[InstantHandle] Expression<Func<IEntityUpdateBuilder<T>, IEntityUpdateBuilder<T>>>              configure)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(item);
			ArgumentNullException.ThrowIfNull(configure);

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_entityUpdateMethodInfo.MakeGenericMethod(typeof(T)),
				currentSource.Expression,
				Expression.Constant(item, typeof(T)),
				Expression.Quote(configure));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Asynchronously updates a single entity in the target table by primary-key match, configured by a fluent builder.
		/// </summary>
		/// <typeparam name="T">Target table record type. Must declare at least one primary-key column.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="item">Entity to update.</param>
		/// <param name="configure">Fluent configuration expression.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Task yielding the number of affected records.</returns>
		public static Task<int> UpdateAsync<T>(
			                this ITable<T>                                                                  target,
			                T                                                                               item,
			[InstantHandle] Expression<Func<IEntityUpdateBuilder<T>, IEntityUpdateBuilder<T>>>              configure,
			                CancellationToken                                                               token = default)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(item);
			ArgumentNullException.ThrowIfNull(configure);

			var currentSource = target.GetLinqToDBSource();

			// Embed the SYNC Update MethodInfo into the expression tree — translation pipeline only knows the sync name.
			var expr = Expression.Call(
				null,
				_entityUpdateMethodInfo.MakeGenericMethod(typeof(T)),
				currentSource.Expression,
				Expression.Constant(item, typeof(T)),
				Expression.Quote(configure));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		#endregion
	}
}
