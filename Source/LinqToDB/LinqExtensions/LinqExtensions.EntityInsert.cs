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
		#region Entity Insert

		// ---------------------------------------------------------------------
		// Entity-shaped Insert (issue #2558 follow-up):
		//   db.Users.Insert(user, b => b.Set(x => x.CreatedAt, () => DateTime.UtcNow).Ignore(x => x.Notes));
		//
		// Chain methods (.Set, .Ignore) live on IEntityInsertBuilder<T>; the builder is
		// captured as Expression<Func<IEntityInsertBuilder<T>, IEntityInsertBuilder<T>>>
		// and walked by EntityInsertBuilder
		// (Source/LinqToDB/Internal/Linq/Builder/EntityInsertBuilder.cs), which synthesises
		// the existing Insert<T>(ITable<T>, Expression<Func<T>>) shape and defers to
		// InsertBuilder for SQL generation.
		//
		// Async overloads embed the SYNC Insert MethodInfo into the captured expression
		// tree (not InsertAsync) — async-ness is handled by ExecuteAsync. This matches the
		// pattern of LinqExtensions.InsertAsync(this ITable<T>, Expression<Func<T>>, …).
		// ---------------------------------------------------------------------

		static readonly MethodInfo _entityInsertMethodInfo = MemberHelper.MethodOfGeneric(
			(ITable<int> t, int item, Expression<Func<IEntityInsertBuilder<int>, IEntityInsertBuilder<int>>> configure) => t.Insert(item, configure));

		/// <summary>
		/// Inserts a single entity into the target table, configured by a fluent builder.
		/// Whole-object defaults are written from <paramref name="item"/>; user-supplied
		/// <c>.Set</c> overrides and <c>.Ignore</c> exclusions overlay on top.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="item">Entity to insert.</param>
		/// <param name="configure">Fluent configuration expression.</param>
		/// <returns>Number of affected records.</returns>
		public static int Insert<T>(
			                this ITable<T>                                                                  target,
			                T                                                                               item,
			[InstantHandle] Expression<Func<IEntityInsertBuilder<T>, IEntityInsertBuilder<T>>>              configure)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(item);
			ArgumentNullException.ThrowIfNull(configure);

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_entityInsertMethodInfo.MakeGenericMethod(typeof(T)),
				currentSource.Expression,
				Expression.Constant(item, typeof(T)),
				Expression.Quote(configure));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Asynchronously inserts a single entity into the target table, configured by a fluent builder.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="item">Entity to insert.</param>
		/// <param name="configure">Fluent configuration expression.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Task yielding the number of affected records.</returns>
		public static Task<int> InsertAsync<T>(
			                this ITable<T>                                                                  target,
			                T                                                                               item,
			[InstantHandle] Expression<Func<IEntityInsertBuilder<T>, IEntityInsertBuilder<T>>>              configure,
			                CancellationToken                                                               token = default)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(item);
			ArgumentNullException.ThrowIfNull(configure);

			var currentSource = target.GetLinqToDBSource();

			// Embed the SYNC Insert MethodInfo into the expression tree — translation pipeline only knows the sync name.
			var expr = Expression.Call(
				null,
				_entityInsertMethodInfo.MakeGenericMethod(typeof(T)),
				currentSource.Expression,
				Expression.Constant(item, typeof(T)),
				Expression.Quote(configure));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		#endregion
	}
}
