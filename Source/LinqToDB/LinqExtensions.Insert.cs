using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB
{
	using Async;
	using Linq;

	public static partial class LinqExtensions
	{

		/// <summary>
		/// Inserts single record into target table and returns inserted record.
		/// </summary>
		/// <typeparam name="TTarget">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Inserted record.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.5+</item>
		/// </list>
		/// </remarks>
		public static TTarget InsertWithOutput<TTarget>(
			                this ITable<TTarget>      target,
			[InstantHandle] Expression<Func<TTarget>> setter)
			where TTarget : notnull
		{
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var query = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutput, target, setter),
				query.Expression,
				Expression.Quote(setter));

			return query.CreateQuery<TTarget>(expr).AsEnumerable().First();
		}

		/// <summary>
		/// Inserts single record into target table asynchronously and returns inserted record.
		/// </summary>
		/// <typeparam name="TTarget">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.5+</item>
		/// </list>
		/// </remarks>
		public static Task<TTarget> InsertWithOutputAsync<TTarget>(
			                this ITable<TTarget>      target,
			[InstantHandle] Expression<Func<TTarget>> setter,
							CancellationToken         token = default)
			where TTarget : notnull
		{
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var query = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutput, target, setter),
				query.Expression,
				Expression.Quote(setter));

			return query.CreateQuery<TTarget>(expr)
				.AsAsyncEnumerable()
				.FirstAsync(token);
		}

		/// <summary>
		/// Inserts single record into target table and returns inserted record.
		/// </summary>
		/// <typeparam name="TTarget">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <returns>Inserted record.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.5+</item>
		/// </list>
		/// </remarks>
		public static TTarget InsertWithOutput<TTarget>(
			                this ITable<TTarget> target,
			[InstantHandle] TTarget              obj)
			where TTarget : notnull
		{
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (obj    == null) throw new ArgumentNullException(nameof(obj));

			var query = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutput, target, obj),
				query.Expression,
				Expression.Constant(obj));

			return query.CreateQuery<TTarget>(expr).AsEnumerable().First();
		}

		/// <summary>
		/// Inserts single record into target table asynchronously and returns inserted record.
		/// </summary>
		/// <typeparam name="TTarget">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.5+</item>
		/// </list>
		/// </remarks>
		public static Task<TTarget> InsertWithOutputAsync<TTarget>(
			           this ITable<TTarget>   target,
			[InstantHandle] TTarget           obj,
			                CancellationToken token = default)
			where TTarget : notnull
		{
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (obj    == null) throw new ArgumentNullException(nameof(obj));

			var query = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutput, target, obj),
				query.Expression,
				Expression.Constant(obj));

			return query.CreateQuery<TTarget>(expr)
				.AsAsyncEnumerable()
				.FirstAsync(token);
		}

		/// <summary>
		/// Inserts single record into target table and returns inserted record.
		/// </summary>
		/// <typeparam name="TTarget">Inserted record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Expression supports only record new expression with field initializers.</param>
		/// <returns>Inserted record.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.5+</item>
		/// </list>
		/// </remarks>
		public static TOutput InsertWithOutput<TTarget,TOutput>(
			                this ITable<TTarget>              target,
			[InstantHandle] Expression<Func<TTarget>>         setter,
			                Expression<Func<TTarget,TOutput>> outputExpression)
			where TTarget : notnull
		{
			if (target           == null) throw new ArgumentNullException(nameof(target));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var query = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutput, target, setter, outputExpression),
				query.Expression,
				Expression.Quote(setter),
				Expression.Quote(outputExpression));

			return query.CreateQuery<TOutput>(expr).AsEnumerable().First();
		}

		/// <summary>
		/// Inserts single record into target table asynchronously and returns inserted record.
		/// </summary>
		/// <typeparam name="TTarget">Inserted record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Expression supports only record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.5+</item>
		/// </list>
		/// </remarks>
		public static Task<TOutput> InsertWithOutputAsync<TTarget,TOutput>(
			                this ITable<TTarget>              target,
			[InstantHandle] Expression<Func<TTarget>>         setter,
			                Expression<Func<TTarget,TOutput>> outputExpression,
							CancellationToken                 token = default)
			where TTarget : notnull

		{
			if (target           == null) throw new ArgumentNullException(nameof(target));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var query = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutput, target, setter, outputExpression),
				query.Expression,
				Expression.Quote(setter),
				Expression.Quote(outputExpression));

			return query.CreateQuery<TOutput>(expr)
				.AsAsyncEnumerable()
				.FirstAsync(token);
		}

		/// <summary>
		/// Inserts single record into target table and outputs that record into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TTarget">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <returns>Number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static int InsertWithOutputInto<TTarget>(
			                this ITable<TTarget>      target,
			[InstantHandle] Expression<Func<TTarget>> setter,
			                ITable<TTarget>           outputTable)
			where TTarget : notnull
		{
			if (target      == null) throw new ArgumentNullException(nameof(target));
			if (setter      == null) throw new ArgumentNullException(nameof(setter));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var query = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutputInto, target, setter, outputTable),
				query.Expression,
				Expression.Quote(setter),
				((IQueryable<TTarget>)outputTable).Expression);

			return query.Execute<int>(expr);
		}

		/// <summary>
		/// Inserts single record into target table asynchronously and outputs that record into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TTarget">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static Task<int> InsertWithOutputIntoAsync<TTarget>(
			                this ITable<TTarget>      target,
			[InstantHandle] Expression<Func<TTarget>> setter,
			                ITable<TTarget>           outputTable,
							CancellationToken         token = default)
			where TTarget : notnull
		{
			if (target      == null) throw new ArgumentNullException(nameof(target));
			if (setter      == null) throw new ArgumentNullException(nameof(setter));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var query = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutputInto, target, setter, outputTable),
				query.Expression,
				Expression.Quote(setter),
				((IQueryable<TTarget>)outputTable).Expression);

			return query.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Inserts single record into target table and outputs that record into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TTarget">Inserted record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Expression supports only record new expression with field initializers.</param>
		/// <returns>Number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static int InsertWithOutputInto<TTarget,TOutput>(
			                this ITable<TTarget>              target,
			[InstantHandle] Expression<Func<TTarget>>         setter,
			                ITable<TOutput>                   outputTable,
			                Expression<Func<TTarget,TOutput>> outputExpression)
			where TOutput : notnull
			where TTarget : notnull
		{
			if (target           == null) throw new ArgumentNullException(nameof(target));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputTable      == null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var query = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutputInto, target, setter, outputTable, outputExpression),
				query.Expression,
				Expression.Quote(setter),
				((IQueryable<TOutput>)outputTable).Expression,
				Expression.Quote(outputExpression));

			return query.Execute<int>(expr);
		}

		/// <summary>
		/// Inserts single record into target table asynchronously and outputs that record into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TTarget">Inserted record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Expression supports only record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static Task<int> InsertWithOutputIntoAsync<TTarget,TOutput>(
			                this ITable<TTarget>              target,
			[InstantHandle] Expression<Func<TTarget>>         setter,
			                ITable<TOutput>                   outputTable,
			                Expression<Func<TTarget,TOutput>> outputExpression,
							CancellationToken                 token = default)
			where TOutput : notnull
			where TTarget : notnull
		{
			if (target           == null) throw new ArgumentNullException(nameof(target));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputTable      == null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var query = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutputInto, target, setter, outputTable, outputExpression),
				query.Expression,
				Expression.Quote(setter),
				((IQueryable<TOutput>)outputTable).Expression,
				Expression.Quote(outputExpression));

			return query.ExecuteAsync<int>(expr, token);
		}

		#region Many records

		/// <summary>
		/// Inserts records from source query into target table and returns newly created records.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Enumeration of records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.5+</item>
		/// </list>
		/// </remarks>
		public static IEnumerable<TTarget> InsertWithOutput<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter)
			where TTarget : notnull
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (target           == null) throw new ArgumentNullException(nameof(target));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutput, source, target, setter),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter));

			return currentSource.CreateQuery<TTarget>(expr).AsEnumerable();
		}

		/// <summary>
		/// Inserts records from source query into target table asynchronously and returns newly created records.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Async sequence of records returned by output.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.5+</item>
		/// </list>
		/// </remarks>
		public static IAsyncEnumerable<TTarget> InsertWithOutputAsync<TSource, TTarget>(
			                this IQueryable<TSource>           source,
			                ITable<TTarget>                    target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter)
			where TTarget : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutput, source, target, setter),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter));

			return currentSource.CreateQuery<TTarget>(expr).AsAsyncEnumerable();
		}

		/// <summary>
		/// Inserts records from source query into target table asynchronously and returns newly created records.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Array of records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (doesn't support more than one record; database limitation)</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.5+</item>
		/// </list>
		/// </remarks>
		// TODO: Remove in v7
		[Obsolete("Use overload with IAsyncEnumerable return type. API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		public static Task<TTarget[]> InsertWithOutputAsync<TSource, TTarget>(
							IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
							CancellationToken token)
			where TTarget : notnull
		{
			return InsertWithOutputAsync(source, target, setter)
				.ToArrayAsync(token);
		}

		/// <summary>
		/// Inserts records from source query into target table and returns newly created records.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Expression supports only record new expression with field initializers.</param>
		/// <returns>Enumeration of records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.5+</item>
		/// </list>
		/// </remarks>
		[Pure]
		public static IEnumerable<TOutput> InsertWithOutput<TSource,TTarget,TOutput>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter,
			                Expression<Func<TTarget,TOutput>> outputExpression)
			where TTarget : notnull
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (target           == null) throw new ArgumentNullException(nameof(target));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutput, source, target, setter, outputExpression),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter),
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr).AsEnumerable();
		}

		/// <summary>
		/// Inserts records from source query into target table asynchronously and returns newly created records.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Expression supports only record new expression with field initializers.</param>
		/// <returns>Async sequence of records returned by output.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.5+</item>
		/// </list>
		/// </remarks>
		public static IAsyncEnumerable<TOutput> InsertWithOutputAsync<TSource, TTarget, TOutput>(
							this IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
							Expression<Func<TTarget, TOutput>> outputExpression)
			where TTarget : notnull
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (target           == null) throw new ArgumentNullException(nameof(target));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutput, source, target, setter, outputExpression),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter),
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr).AsAsyncEnumerable();
		}

		/// <summary>
		/// Inserts records from source query into target table asynchronously and returns newly created records.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Expression supports only record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Array of records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (doesn't support more than one record; database limitation)</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.5+</item>
		/// </list>
		/// </remarks>
		// TODO: Remove in v7
		[Obsolete("Use overload with IAsyncEnumerable return type. API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		public static Task<TOutput[]> InsertWithOutputAsync<TSource, TTarget, TOutput>(
							IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
							Expression<Func<TTarget, TOutput>> outputExpression,
							CancellationToken token)
			where TTarget : notnull
		{
			return InsertWithOutputAsync(source, target, setter, outputExpression)
				.ToArrayAsync(token);
		}

		/// <summary>
		/// Inserts records from source query into target table and outputs newly created records into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <returns>Number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static int InsertWithOutputInto<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter,
			                ITable<TTarget>                   outputTable)
			where TTarget : notnull
		{
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (target      == null) throw new ArgumentNullException(nameof(target));
			if (setter      == null) throw new ArgumentNullException(nameof(setter));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutputInto, source, target, setter, outputTable),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter),
				((IQueryable<TTarget>)outputTable).Expression);

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Inserts records from source query into target table asynchronously and outputs inserted records into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static Task<int> InsertWithOutputIntoAsync<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter,
			                ITable<TTarget>                   outputTable,
							CancellationToken                 token = default)
			where TTarget : notnull
		{
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (target      == null) throw new ArgumentNullException(nameof(target));
			if (setter      == null) throw new ArgumentNullException(nameof(setter));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutputInto, source, target, setter, outputTable),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter),
				((IQueryable<TTarget>)outputTable).Expression);

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Inserts records from source query into target table and outputs inserted records into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Expression supports only record new expression with field initializers.</param>
		/// <returns>Number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static int InsertWithOutputInto<TSource,TTarget,TOutput>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter,
			                ITable<TOutput>                   outputTable,
			                Expression<Func<TTarget,TOutput>> outputExpression)
			where TOutput : notnull
			where TTarget : notnull
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (target           == null) throw new ArgumentNullException(nameof(target));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputTable      == null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutputInto, source, target, setter, outputTable, outputExpression),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter),
				((IQueryable<TOutput>)outputTable).Expression,
				Expression.Quote(outputExpression));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Inserts records from source query into target table asynchronously and outputs inserted records into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Expression supports only record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static Task<int> InsertWithOutputIntoAsync<TSource,TTarget,TOutput>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter,
			                ITable<TOutput>                   outputTable,
			                Expression<Func<TTarget,TOutput>> outputExpression,
							CancellationToken                 token = default)
			where TOutput : notnull
			where TTarget : notnull
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (target           == null) throw new ArgumentNullException(nameof(target));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputTable      == null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutputInto, source, target, setter, outputTable, outputExpression),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter),
				((IQueryable<TOutput>)outputTable).Expression,
				Expression.Quote(outputExpression));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Executes configured insert query and returns inserted record.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Inserted record.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.5+</item>
		/// </list>
		/// </remarks>
		public static TTarget InsertWithOutput<TSource,TTarget>(this ISelectInsertable<TSource,TTarget> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query = ((SelectInsertable<TSource,TTarget>)source).Query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutput, source),
				query.Expression);

			return query.CreateQuery<TTarget>(expr).First();
		}

		/// <summary>
		/// Executes configured insert query asynchronously and returns inserted record.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.5+</item>
		/// </list>
		/// </remarks>
		public static Task<TTarget> InsertWithOutputAsync<TSource,TTarget>(
			this ISelectInsertable<TSource,TTarget> source,
			     CancellationToken                  token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query = ((SelectInsertable<TSource,TTarget>)source).Query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutput, source),
				query.Expression);

			return query.CreateQuery<TTarget>(expr)
				.AsAsyncEnumerable()
				.FirstAsync(token);
		}

		/// <summary>
		/// Executes configured insert query and returns inserted record.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="outputTable">Output table.</param>
		/// <returns>Number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static int InsertWithOutputInto<TSource,TTarget>(
			this ISelectInsertable<TSource,TTarget> source,
			     ITable<TTarget>                    outputTable)
			where TTarget : notnull
		{
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var query = ((SelectInsertable<TSource,TTarget>)source).Query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutputInto, source, outputTable),
				query.Expression,
				((IQueryable<TTarget>)outputTable).Expression);

			return query.Execute<int>(expr);
		}

		/// <summary>
		/// Executes configured insert query asynchronously and returns inserted record.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static Task<int> InsertWithOutputIntoAsync<TSource,TTarget>(
			this ISelectInsertable<TSource,TTarget> source,
			     ITable<TTarget>                    outputTable,
			     CancellationToken                  token = default)
			where TTarget : notnull

		{
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var query = ((SelectInsertable<TSource,TTarget>)source).Query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutputInto, source, outputTable),
				query.Expression,
				((IQueryable<TTarget>)outputTable).Expression);

			return query.ExecuteAsync<int>(expr, token);
		}

		#endregion

		#region IValueInsertable
		/// <summary>
		/// Inserts single record into target table and returns inserted record.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Inserted record.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.5+</item>
		/// </list>
		/// </remarks>
		public static T InsertWithOutput<T>(this IValueInsertable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query         = ((ValueInsertable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutput, source),
				currentSource.Expression);

			return currentSource.CreateQuery<T>(expr).AsEnumerable().First();
		}

		/// <summary>
		/// Inserts single record into target table asynchronously and returns inserted record.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.5+</item>
		/// </list>
		/// </remarks>
		public static Task<T> InsertWithOutputAsync<T>(this IValueInsertable<T> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query         = ((ValueInsertable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutput, source),
				currentSource.Expression);

			return currentSource.CreateQuery<T>(expr)
				.AsAsyncEnumerable()
				.FirstAsync(token);
		}

		/// <summary>
		/// Inserts single record into target table and returns inserted record.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Expression supports only record new expression with field initializers.</param>
		/// <returns>Inserted record.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.5+</item>
		/// </list>
		/// </remarks>
		public static TOutput InsertWithOutput<T, TOutput>(this IValueInsertable<T> source, Expression<Func<T, TOutput>> outputExpression)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var query         = ((ValueInsertable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutput, source, outputExpression),
				currentSource.Expression,
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr).AsEnumerable().First();
		}

		/// <summary>
		/// Inserts single record into target table asynchronously and returns inserted record.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Expression supports only record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.5+</item>
		/// </list>
		/// </remarks>
		public static Task<TOutput> InsertWithOutputAsync<T, TOutput>(this IValueInsertable<T> source, Expression<Func<T, TOutput>> outputExpression, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var query         = ((ValueInsertable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutput, source, outputExpression),
				currentSource.Expression,
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr)
				.AsAsyncEnumerable()
				.FirstAsync(token);
		}
		#endregion
	}
}
