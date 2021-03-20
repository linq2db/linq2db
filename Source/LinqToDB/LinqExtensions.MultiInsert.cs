using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;

namespace LinqToDB
{
	public static class MultiInsertExtensions
	{
		#region MethodInfo

		internal static readonly MethodInfo MultiInsertMethodInfo   = typeof(MultiInsertExtensions).GetMethod(nameof(MultiInsert))!;
		internal static readonly MethodInfo IntoMethodInfo          = typeof(MultiInsertExtensions).GetMethod(nameof(Into))!;
		internal static readonly MethodInfo WhenMethodInfo          = typeof(MultiInsertExtensions).GetMethod(nameof(When))!;
		internal static readonly MethodInfo ElseMethodInfo          = typeof(MultiInsertExtensions).GetMethod(nameof(Else))!;
		internal static readonly MethodInfo InsertMethodInfo        = typeof(MultiInsertExtensions).GetMethod(nameof(Insert))!;
		internal static readonly MethodInfo InsertAllMethodInfo     = typeof(MultiInsertExtensions).GetMethod(nameof(InsertAll))!;
		internal static readonly MethodInfo InsertFirstMethodInfo   = typeof(MultiInsertExtensions).GetMethod(nameof(InsertFirst))!;

		#endregion

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
					MultiInsertMethodInfo.MakeGenericMethod(typeof(TSource)),
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
					IntoMethodInfo.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
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
					WhenMethodInfo.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
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
					ElseMethodInfo.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
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
			
			IQueryable query = ((MultiInsertQuery<TSource>)insert).Query;
			query = LinqExtensions.ProcessSourceQueryable?.Invoke(query) ?? query;

			return query.Provider.Execute<int>(
				Expression.Call(
					null,
					InsertMethodInfo.MakeGenericMethod(typeof(TSource)),
					query.Expression));
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

			IQueryable query = ((MultiInsertQuery<TSource>)insert).Query;
			query = LinqExtensions.ProcessSourceQueryable?.Invoke(query) ?? query;

			return query.Provider.Execute<int>(
				Expression.Call(
					null,
					InsertAllMethodInfo.MakeGenericMethod(typeof(TSource)),
					query.Expression));
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

			IQueryable query = ((MultiInsertQuery<TSource>)insert).Query;
			query = LinqExtensions.ProcessSourceQueryable?.Invoke(query) ?? query;

			return query.Provider.Execute<int>(
				Expression.Call(
					null,
					InsertFirstMethodInfo.MakeGenericMethod(typeof(TSource)),
					query.Expression));
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

		private class MultiInsertQuery<TSource> : IMultiInsertSource<TSource>
		{
			public readonly IQueryable<TSource> Query;

			public MultiInsertQuery(IQueryable<TSource> query)
			{
				this.Query = query;
			}
		}

		#endregion
	}
}
