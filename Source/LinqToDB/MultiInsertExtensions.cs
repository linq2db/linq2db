using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using LinqToDB.Internal.Linq;

using Methods = LinqToDB.Reflection.Methods.LinqToDB.MultiInsert;

namespace LinqToDB
{
	public static class MultiInsertExtensions
	{
		#region Public fluent API

		/// <summary>
		/// Inserts records from source query into multiple target tables.
		/// </summary>
		/// <remarks>Only supported by Oracle data provider.</remarks>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		[Pure, LinqTunnel]
		public static IMultiInsertSource<TSource> MultiInsert<TSource>(this IQueryable<TSource> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query = source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					Methods.Begin.MakeGenericMethod(typeof(TSource)),
					source.Expression));

			return new MultiInsertQuery<TSource>(query);
		}

		/// <summary>
		/// Unconditionally insert into target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table to insert into.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		[Pure, LinqTunnel]
		public static IMultiInsertInto<TSource> Into<TSource, TTarget>(
			this            IMultiInsertInto<TSource>          source,
			                ITable<TTarget>                    target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter)
			where TTarget : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var query = ((MultiInsertQuery<TSource>)source).Query;

			query = query.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					Methods.Into.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
					query.Expression,
					target.Expression,
					Expression.Quote(setter)));

			return new MultiInsertQuery<TSource>(query);
		}

		/// <summary>
		/// Conditionally insert into target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="condition">Predicate indicating when to insert into target table.</param>
		/// <param name="target">Target table to insert into.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		[Pure, LinqTunnel]
		public static IMultiInsertWhen<TSource> When<TSource, TTarget>(
			this            IMultiInsertWhen<TSource>          source,
			[InstantHandle] Expression<Func<TSource, bool>>    condition,
							ITable<TTarget>                    target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter)
			where TTarget : notnull
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (condition == null) throw new ArgumentNullException(nameof(condition));
			if (target    == null) throw new ArgumentNullException(nameof(target));
			if (setter    == null) throw new ArgumentNullException(nameof(setter));

			var query = ((MultiInsertQuery<TSource>)source).Query;

			query = query.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					Methods.When.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
					query.Expression,
					Expression.Quote(condition),
					target.Expression,
					Expression.Quote(setter)));

			return new MultiInsertQuery<TSource>(query);
		}

		/// <summary>
		/// Insert into target table when previous conditions don't match.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table to insert into.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		[Pure, LinqTunnel]
		public static IMultiInsertElse<TSource> Else<TSource, TTarget>(
			this            IMultiInsertWhen<TSource>          source,
							ITable<TTarget>                    target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter)
			where TTarget : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var query = ((MultiInsertQuery<TSource>)source).Query;

			query = query.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					Methods.Else.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
					query.Expression,
					target.Expression,
					Expression.Quote(setter)));

			return new MultiInsertQuery<TSource>(query);
		}

		/// <summary>
		/// Inserts source data into every configured table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="insert">Multi-table insert to perform.</param>
		/// <returns>Number of inserted rows.</returns>
		public static int Insert<TSource>(this IMultiInsertInto<TSource> insert)
		{
			if (insert == null) throw new ArgumentNullException(nameof(insert));

			var query = ((MultiInsertQuery<TSource>)insert).Query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.Insert.MakeGenericMethod(typeof(TSource)),
				query.Expression);

			return query.Execute<int>(expr);
		}

		/// <summary>
		/// Asynchronously inserts source data into every configured table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="insert">Multi-table insert to perform.</param>
		/// <param name="token">Cancellation token for async operation.</param>
		/// <returns>Number of inserted rows.</returns>
		public static Task<int> InsertAsync<TSource>(this IMultiInsertInto<TSource> insert, CancellationToken token = default)
		{
			if (insert == null) throw new ArgumentNullException(nameof(insert));

			var query = ((MultiInsertQuery<TSource>)insert).Query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.Insert.MakeGenericMethod(typeof(TSource)),
				query.Expression);

			return query.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Inserts source data into every matching condition.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="insert">Multi-table insert to perform.</param>
		/// <returns>Number of inserted rows.</returns>
		public static int InsertAll<TSource>(this IMultiInsertElse<TSource> insert)
		{
			if (insert == null) throw new ArgumentNullException(nameof(insert));

			var query = ((MultiInsertQuery<TSource>)insert).Query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.InsertAll.MakeGenericMethod(typeof(TSource)),
				query.Expression);

			return query.Execute<int>(expr);
		}

		/// <summary>
		/// Asynchronously inserts source data into every matching condition.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="insert">Multi-table insert to perform.</param>
		/// <param name="token">Cancellation token for async operation.</param>
		/// <returns>Number of inserted rows.</returns>
		public static Task<int> InsertAllAsync<TSource>(this IMultiInsertElse<TSource> insert, CancellationToken token = default)
		{
			if (insert == null) throw new ArgumentNullException(nameof(insert));

			var query = ((MultiInsertQuery<TSource>)insert).Query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.InsertAll.MakeGenericMethod(typeof(TSource)),
				query.Expression);

			return query.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Inserts source data into the first matching condition.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="insert">Multi-table insert to perform.</param>
		/// <returns>Number of inserted rows.</returns>
		public static int InsertFirst<TSource>(this IMultiInsertElse<TSource> insert)
		{
			if (insert == null) throw new ArgumentNullException(nameof(insert));

			var query = ((MultiInsertQuery<TSource>)insert).Query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.InsertFirst.MakeGenericMethod(typeof(TSource)),
				query.Expression);

			return query.Execute<int>(expr);
		}

		/// <summary>
		/// Asynchronously inserts source data into the first matching condition.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="insert">Multi-table insert to perform.</param>
		/// <param name="token">Cancellation token for async operation.</param>
		/// <returns>Number of inserted rows.</returns>
		public static Task<int> InsertFirstAsync<TSource>(this IMultiInsertElse<TSource> insert, CancellationToken token = default)
		{
			if (insert == null) throw new ArgumentNullException(nameof(insert));

			var query = ((MultiInsertQuery<TSource>)insert).Query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.InsertFirst.MakeGenericMethod(typeof(TSource)),
				query.Expression);

			return query.ExecuteAsync<int>(expr, token);
		}

		#endregion

		#region Fluent state

		public interface IMultiInsertSource<TSource>
			: IMultiInsertInto<TSource>
			, IMultiInsertWhen<TSource>
		{ }

		public interface IMultiInsertInto<TSource>
		{ }

		public interface IMultiInsertWhen<TSource> : IMultiInsertElse<TSource>
		{ }

		public interface IMultiInsertElse<TSource>
		{ }

		internal sealed class MultiInsertQuery<TSource> : IMultiInsertSource<TSource>
		{
			public readonly IQueryable<TSource> Query;

			public MultiInsertQuery(IQueryable<TSource> query)
			{
				Query = query;
			}
		}

		#endregion
	}
}
