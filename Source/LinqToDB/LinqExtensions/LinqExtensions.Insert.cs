using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using LinqToDB.Async;
using LinqToDB.Internal.Async;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.Reflection;
using LinqToDB.Linq;

namespace LinqToDB
{
	public static partial class LinqExtensions
	{
		#region InsertWithOutput

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
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);

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
		public static ValueTask<TTarget> InsertWithOutputAsync<TTarget>(
			                this ITable<TTarget>      target,
			[InstantHandle] Expression<Func<TTarget>> setter,
							CancellationToken         token = default)
			where TTarget : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);

			var query = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutput, target, setter),
				query.Expression,
				Expression.Quote(setter));

			return query.CreateQuery<TTarget>(expr).AsAsyncEnumerable().FirstAsync(token);
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
			ArgumentNullException.ThrowIfNull(target);
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
		public static ValueTask<TTarget> InsertWithOutputAsync<TTarget>(
			           this ITable<TTarget>   target,
			[InstantHandle] TTarget           obj,
			                CancellationToken token = default)
			where TTarget : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			if (obj    == null) throw new ArgumentNullException(nameof(obj));

			var query = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutput, target, obj),
				query.Expression,
				Expression.Constant(obj));

			return query.CreateQuery<TTarget>(expr).AsAsyncEnumerable().FirstAsync(token);
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
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);
			ArgumentNullException.ThrowIfNull(outputExpression);

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
		public static ValueTask<TOutput> InsertWithOutputAsync<TTarget,TOutput>(
			                this ITable<TTarget>              target,
			[InstantHandle] Expression<Func<TTarget>>         setter,
			                Expression<Func<TTarget,TOutput>> outputExpression,
							CancellationToken                 token = default)
			where TTarget : notnull

		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);
			ArgumentNullException.ThrowIfNull(outputExpression);

			var query = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutput, target, setter, outputExpression),
				query.Expression,
				Expression.Quote(setter),
				Expression.Quote(outputExpression));

			return query.CreateQuery<TOutput>(expr).AsAsyncEnumerable().FirstAsync(token);
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
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);
			ArgumentNullException.ThrowIfNull(outputTable);

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
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);
			ArgumentNullException.ThrowIfNull(outputTable);

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
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);
			ArgumentNullException.ThrowIfNull(outputTable);
			ArgumentNullException.ThrowIfNull(outputExpression);

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
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);
			ArgumentNullException.ThrowIfNull(outputTable);
			ArgumentNullException.ThrowIfNull(outputExpression);

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
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);

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
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);

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
		public static ValueTask<TTarget[]> InsertWithOutputAsync<TSource, TTarget>(
							IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
							CancellationToken token)
			where TTarget : notnull
		{
			return InsertWithOutputAsync(source, target, setter).ToArrayAsync(token);
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
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);
			ArgumentNullException.ThrowIfNull(outputExpression);

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
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);
			ArgumentNullException.ThrowIfNull(outputExpression);

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
		public static ValueTask<TOutput[]> InsertWithOutputAsync<TSource, TTarget, TOutput>(
							IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
							Expression<Func<TTarget, TOutput>> outputExpression,
							CancellationToken token)
			where TTarget : notnull
		{
			return InsertWithOutputAsync(source, target, setter, outputExpression).ToArrayAsync(token);
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
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);
			ArgumentNullException.ThrowIfNull(outputTable);

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
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);
			ArgumentNullException.ThrowIfNull(outputTable);

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
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);
			ArgumentNullException.ThrowIfNull(outputTable);
			ArgumentNullException.ThrowIfNull(outputExpression);

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
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);
			ArgumentNullException.ThrowIfNull(outputTable);
			ArgumentNullException.ThrowIfNull(outputExpression);

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
			ArgumentNullException.ThrowIfNull(source);

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
		public static ValueTask<TTarget> InsertWithOutputAsync<TSource,TTarget>(
			this ISelectInsertable<TSource,TTarget> source,
			     CancellationToken                  token = default)
		{
			ArgumentNullException.ThrowIfNull(source);

			var query = ((SelectInsertable<TSource,TTarget>)source).Query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutput, source),
				query.Expression);

			return query.CreateQuery<TTarget>(expr).AsAsyncEnumerable().FirstAsync(token);
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
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(outputTable);

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
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(outputTable);

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
			ArgumentNullException.ThrowIfNull(source);

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
		public static ValueTask<T> InsertWithOutputAsync<T>(this IValueInsertable<T> source, CancellationToken token = default)
		{
			ArgumentNullException.ThrowIfNull(source);

			var query         = ((ValueInsertable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutput, source),
				currentSource.Expression);

			return currentSource.CreateQuery<T>(expr).AsAsyncEnumerable().FirstAsync(token);
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
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(outputExpression);

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
		public static ValueTask<TOutput> InsertWithOutputAsync<T, TOutput>(
			this IValueInsertable<T> source,
			Expression<Func<T, TOutput>> outputExpression,
			CancellationToken token = default)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(outputExpression);

			var query         = ((ValueInsertable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutput, source, outputExpression),
				currentSource.Expression,
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr).AsAsyncEnumerable().FirstAsync(token);
		}
		#endregion

		#endregion

		#region InsertWithOutputQuery

		/// <summary>
		/// Creates a query source over the record inserted into <paramref name="target"/> table.
		/// The insert and the consuming query are executed as a single SQL statement when the returned query
		/// (or a query composed from it) is executed.
		/// </summary>
		/// <typeparam name="TTarget">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Composable query over the inserted record.</returns>
		/// <remarks>
		/// Rendered as a data-modifying common table expression, e.g. <c>WITH t AS (INSERT ... RETURNING ...) SELECT ... FROM t</c>.
		/// Target table changes are not visible to the rest of the statement: read inserted values through the returned query.
		/// Project the output with <c>Select</c> in the consuming query; only the columns it reads are returned.
		/// <para>
		/// Each call inserts once per execution of a query that contains it, however many times that query references the
		/// returned query and whether or not it reads its rows. Separate calls insert separately, even when they are identical.
		/// A condition on a captured value is evaluated before translation and can leave the call out of the query, so that it
		/// does not insert; a condition on a compiled query parameter is translated and keeps the call.
		/// The call cannot depend on the rows of an enclosing query.
		/// Eager loading over the output loads child records by key from the returned rows (<see cref="EagerLoadingStrategy.KeyedQuery"/>,
		/// regardless of the configured strategy), in one transaction with the insert; child queries that cannot be keyed
		/// to the output, or that read the output themselves, throw <see cref="LinqToDBException"/>.
		/// </para>
		/// Database support:
		/// <list type="bullet">
		/// <item>PostgreSQL</item>
		/// </list>
		/// Other databases throw <see cref="LinqToDBException"/> when the query is built.
		/// </remarks>
		[Pure]
		public static IQueryable<TTarget> InsertWithOutputQuery<TTarget>(
			this ITable<TTarget>      target,
			Expression<Func<TTarget>> setter)
			where TTarget : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);

			return InsertWithOutputQuery(target, setter, new DataModificationIdentity());
		}

		/// <summary>
		/// Creates a query source over the records inserted from <paramref name="source"/> query into <paramref name="target"/> table.
		/// The insert and the consuming query are executed as a single SQL statement when the returned query
		/// (or a query composed from it) is executed.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Composable query over the inserted records.</returns>
		/// <remarks>
		/// Rendered as a data-modifying common table expression, e.g. <c>WITH t AS (INSERT ... RETURNING ...) SELECT ... FROM t</c>.
		/// Target table changes are not visible to the rest of the statement: read inserted values through the returned query.
		/// Project the output with <c>Select</c> in the consuming query; only the columns it reads are returned.
		/// <para>
		/// Each call inserts once per execution of a query that contains it, however many times that query references the
		/// returned query and whether or not it reads its rows. Separate calls insert separately, even when they are identical.
		/// A condition on a captured value is evaluated before translation and can leave the call out of the query, so that it
		/// does not insert; a condition on a compiled query parameter is translated and keeps the call.
		/// The call cannot depend on the rows of an enclosing query.
		/// Eager loading over the output loads child records by key from the returned rows (<see cref="EagerLoadingStrategy.KeyedQuery"/>,
		/// regardless of the configured strategy), in one transaction with the insert; child queries that cannot be keyed
		/// to the output, or that read the output themselves, throw <see cref="LinqToDBException"/>.
		/// </para>
		/// Database support:
		/// <list type="bullet">
		/// <item>PostgreSQL</item>
		/// </list>
		/// Other databases throw <see cref="LinqToDBException"/> when the query is built.
		/// </remarks>
		[Pure]
		public static IQueryable<TTarget> InsertWithOutputQuery<TSource,TTarget>(
			this IQueryable<TSource>          source,
			ITable<TTarget>                   target,
			Expression<Func<TSource,TTarget>> setter)
			where TTarget : notnull
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);

			return InsertWithOutputQuery(source, target, setter, new DataModificationIdentity());
		}

		// The identified forms carry the identity of one call as a constant, so references to that call share it and
		// separate calls do not. When expose evaluates one, it returns the same call with the same identity.

		internal static IQueryable<TTarget> InsertWithOutputQuery<TTarget>(
			this ITable<TTarget>      target,
			Expression<Func<TTarget>> setter,
			DataModificationIdentity  identity)
			where TTarget : notnull
		{
			var query = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutputQuery, target, setter, identity),
				query.Expression,
				Expression.Quote(setter),
				Expression.Constant(identity));

			return query.CreateQuery<TTarget>(expr);
		}

		internal static IQueryable<TTarget> InsertWithOutputQuery<TSource,TTarget>(
			this IQueryable<TSource>          source,
			ITable<TTarget>                   target,
			Expression<Func<TSource,TTarget>> setter,
			DataModificationIdentity          identity)
			where TTarget : notnull
		{
			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithOutputQuery, source, target, setter, identity),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter),
				Expression.Constant(identity));

			return currentSource.CreateQuery<TTarget>(expr);
		}

		/// <summary>
		/// Body of the CTE that an <c>InsertWithOutputQuery</c> call is translated to: the rows the insert returns.
		/// Created by the query builder only.
		/// </summary>
		internal static IQueryable<TTarget> DataModificationSource<TTarget>(this IQueryable<TTarget> insertWithOutputQuery)
		{
			var query = insertWithOutputQuery.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(DataModificationSource, insertWithOutputQuery),
				query.Expression);

			return query.CreateQuery<TTarget>(expr);
		}

		static readonly MethodInfo _insertWithOutputQueryTable           = MethodHelper.GetMethodInfo(InsertWithOutputQuery, (ITable<object>)null!, (Expression<Func<object>>)null!).GetGenericMethodDefinition();
		static readonly MethodInfo _insertWithOutputQueryTableIdentified = MethodHelper.GetMethodInfo(InsertWithOutputQuery, (ITable<object>)null!, (Expression<Func<object>>)null!, (DataModificationIdentity)null!).GetGenericMethodDefinition();
		static readonly MethodInfo _insertWithOutputQuerySource           = MethodHelper.GetMethodInfo(InsertWithOutputQuery, (IQueryable<object>)null!, (ITable<object>)null!, (Expression<Func<object,object>>)null!).GetGenericMethodDefinition();
		static readonly MethodInfo _insertWithOutputQuerySourceIdentified = MethodHelper.GetMethodInfo(InsertWithOutputQuery, (IQueryable<object>)null!, (ITable<object>)null!, (Expression<Func<object,object>>)null!, (DataModificationIdentity)null!).GetGenericMethodDefinition();

		internal static DataModificationIdentity? GetDataModificationIdentity(MethodCallExpression call)
		{
			return call.Arguments[^1] is ConstantExpression { Value: DataModificationIdentity identity } ? identity : null;
		}

		/// <summary>
		/// Rewrites a call of a public <c>InsertWithOutputQuery</c> overload, which carries no identity, into the identified form.
		/// </summary>
		internal static MethodCallExpression IdentifyDataModificationCall(MethodCallExpression call, DataModificationIdentity identity)
		{
			if (GetDataModificationIdentity(call) != null)
				return call;

			var definition = call.Method.GetGenericMethodDefinition();
			var identified =
				definition == _insertWithOutputQueryTable  ? _insertWithOutputQueryTableIdentified  :
				definition == _insertWithOutputQuerySource ? _insertWithOutputQuerySourceIdentified :
				throw new InvalidOperationException($"Unexpected method '{call.Method}'.");

			return Expression.Call(
				null,
				identified.MakeGenericMethod(call.Method.GetGenericArguments()),
				[.. call.Arguments, Expression.Constant(identity)]);
		}

		#endregion

		#region Insert

		/// <summary>
		/// Inserts single record into target table.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Number of affected records.</returns>
		public static int Insert<T>(
							this ITable<T> target,
			[InstantHandle] Expression<Func<T>> setter)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Insert, target, setter),
				currentSource.Expression, Expression.Quote(setter));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Inserts single record into target table asynchronously.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> InsertAsync<T>(
							this ITable<T> target,
			[InstantHandle] Expression<Func<T>> setter,
			CancellationToken token = default)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Insert, target, setter),
				currentSource.Expression, Expression.Quote(setter));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Inserts single record into target table and returns identity value of inserted record.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static object InsertWithIdentity<T>(
							this ITable<T> target,
			[InstantHandle] Expression<Func<T>> setter)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);

			var currentQuery = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithIdentity, target, setter),
				currentQuery.Expression, Expression.Quote(setter));

			return currentQuery.Execute<object>(expr);
		}

		/// <summary>
		/// Inserts single record into target table and returns identity value of inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static int InsertWithInt32Identity<T>(
							this ITable<T> target,
			[InstantHandle] Expression<Func<T>> setter)
			where T : notnull
		{
			return target.DataContext.MappingSchema.ChangeTypeTo<int>(InsertWithIdentity(target, setter));
		}

		/// <summary>
		/// Inserts single record into target table and returns identity value of inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static long InsertWithInt64Identity<T>(
							this ITable<T> target,
			[InstantHandle] Expression<Func<T>> setter)
			where T : notnull
		{
			return target.DataContext.MappingSchema.ChangeTypeTo<long>(InsertWithIdentity(target, setter));
		}

		/// <summary>
		/// Inserts single record into target table and returns identity value of inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static decimal InsertWithDecimalIdentity<T>(
							this ITable<T> target,
			[InstantHandle] Expression<Func<T>> setter)
			where T : notnull
		{
			return target.DataContext.MappingSchema.ChangeTypeTo<decimal>(InsertWithIdentity(target, setter));
		}

		/// <summary>
		/// Inserts single record into target table asynchronously and returns identity value of inserted record.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static Task<object> InsertWithIdentityAsync<T>(
							this ITable<T> target,
			[InstantHandle] Expression<Func<T>> setter,
			CancellationToken token = default)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithIdentity, target, setter),
				currentSource.Expression, Expression.Quote(setter));

			return currentSource.ExecuteAsync<object>(expr, token);
		}

		/// <summary>
		/// Inserts single record into target table asynchronously and returns identity value of inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<int> InsertWithInt32IdentityAsync<T>(
							this ITable<T> target,
			[InstantHandle] Expression<Func<T>> setter,
			CancellationToken token = default)
			where T : notnull
		{
			return target.DataContext.MappingSchema.ChangeTypeTo<int>(
				await InsertWithIdentityAsync(target, setter, token).ConfigureAwait(false)
			);
		}

		/// <summary>
		/// Inserts single record into target table asynchronously and returns identity value of inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<long> InsertWithInt64IdentityAsync<T>(
							this ITable<T> target,
			[InstantHandle] Expression<Func<T>> setter,
			CancellationToken token = default)
			where T : notnull
		{
			return target.DataContext.MappingSchema.ChangeTypeTo<long>(
				await InsertWithIdentityAsync(target, setter, token).ConfigureAwait(false)
			);
		}

		/// <summary>
		/// Inserts single record into target table asynchronously and returns identity value of inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<decimal> InsertWithDecimalIdentityAsync<T>(
							this ITable<T> target,
			[InstantHandle] Expression<Func<T>> setter,
			CancellationToken token = default)
			where T : notnull
		{
			return target.DataContext.MappingSchema.ChangeTypeTo<decimal>(
				await InsertWithIdentityAsync(target, setter, token).ConfigureAwait(false)
			);
		}

		#region ValueInsertable

		internal sealed class ValueInsertable<T> : IValueInsertable<T>
		{
			public ValueInsertable(IQueryable<T> query)
			{
				Query = query;
			}

			public IQueryable<T> Query;
		}

		/// <summary>
		/// Starts insert operation LINQ query definition.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="target">Target table.</param>
		/// <returns>Insertable source query.</returns>
		[LinqTunnel]
		[Pure]
		public static IValueInsertable<T> Into<T>(this IDataContext dataContext, ITable<T> target)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(target);

			var currentSource = target.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Into, dataContext, target),
				SqlQueryRootExpression.Create(dataContext), currentSource.Expression);

			var v = currentSource.Provider.CreateQuery<T>(expr);
			return new ValueInsertable<T>(v);
		}

		/// <summary>
		/// Starts insert operation LINQ query definition.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="source">Target table.</param>
		/// <returns>Insertable source query.</returns>
		public static IValueInsertable<T> AsValueInsertable<T>(this ITable<T> source)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(source);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Insert.FromTable.AsValueInsertable.MakeGenericMethod(typeof(T)),
				currentSource.Expression);

			var query = currentSource.Provider.CreateQuery<T>(expr);
			return new ValueInsertable<T>(query);
		}

		/// <summary>
		/// Starts insert operation LINQ query definition from field setter expression.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <typeparam name="TV">Setter field type.</typeparam>
		/// <param name="source">Source table to insert to.</param>
		/// <param name="field">Setter field selector expression.</param>
		/// <param name="value">Setter field value expression.</param>
		/// <returns>Insert query.</returns>
		[LinqTunnel]
		[Pure]
		public static IValueInsertable<T> Value<T, TV>(
							this ITable<T> source,
			[InstantHandle] Expression<Func<T, TV>> field,
			[InstantHandle] Expression<Func<TV>> value)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(field);
			ArgumentNullException.ThrowIfNull(value);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Value, source, field, value),
				currentSource.Expression, Expression.Quote(field), Expression.Quote(value));

			var q = currentSource.Provider.CreateQuery<T>(expr);
			return new ValueInsertable<T>(q);
		}

		/// <summary>
		/// Starts insert operation LINQ query definition from field setter expression.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <typeparam name="TV">Setter field type.</typeparam>
		/// <param name="source">Source table to insert to.</param>
		/// <param name="field">Setter field selector expression.</param>
		/// <param name="value">Setter field value.</param>
		/// <returns>Insert query.</returns>
		[LinqTunnel]
		[Pure]
		public static IValueInsertable<T> Value<T, TV>(
							 this ITable<T> source,
			[InstantHandle] Expression<Func<T, TV>> field,
			[SkipIfConstant] TV value)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(field);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Value, source, field, value),
				currentSource.Expression, Expression.Quote(field), Expression.Constant(value, typeof(TV)));

			var q = currentSource.Provider.CreateQuery<T>(expr);
			return new ValueInsertable<T>(q);
		}

		/// <summary>
		/// Add field setter to insert operation LINQ query.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <typeparam name="TV">Setter field type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="field">Setter field selector expression.</param>
		/// <param name="value">Setter field value expression.</param>
		/// <returns>Insert query.</returns>
		[LinqTunnel]
		[Pure]
		public static IValueInsertable<T> Value<T, TV>(
							this IValueInsertable<T> source,
			[InstantHandle] Expression<Func<T, TV>> field,
			[InstantHandle] Expression<Func<TV>> value)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(field);
			ArgumentNullException.ThrowIfNull(value);

			var query = ((ValueInsertable<T>)source).Query;

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Value, source, field, value),
				query.Expression, Expression.Quote(field), Expression.Quote(value));

			var q = query.Provider.CreateQuery<T>(expr);
			return new ValueInsertable<T>(q);
		}

		/// <summary>
		/// Add field setter to insert operation LINQ query.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <typeparam name="TV">Setter field type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="field">Setter field selector expression.</param>
		/// <param name="value">Setter field value.</param>
		/// <returns>Insert query.</returns>
		[LinqTunnel]
		[Pure]
		public static IValueInsertable<T> Value<T, TV>(
							 this IValueInsertable<T> source,
			[InstantHandle] Expression<Func<T, TV>> field,
			[SkipIfConstant] TV value)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(field);

			var query = ((ValueInsertable<T>)source).Query;

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Value, source, field, value),
				query.Expression, Expression.Quote(field), Expression.Constant(value, typeof(TV)));

			var q = query.Provider.CreateQuery<T>(expr);
			return new ValueInsertable<T>(q);
		}

		/// <summary>
		/// Executes insert query.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Number of affected records.</returns>
		public static int Insert<T>(this IValueInsertable<T> source)
		{
			ArgumentNullException.ThrowIfNull(source);

			var query = ((ValueInsertable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Insert.FromValueInsertable.Insert.MakeGenericMethod(typeof(T)),
				currentSource.Expression);

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes insert query asynchronously.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> InsertAsync<T>(this IValueInsertable<T> source, CancellationToken token = default)
		{
			ArgumentNullException.ThrowIfNull(source);

			var query = ((ValueInsertable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Insert.FromValueInsertable.Insert.MakeGenericMethod(typeof(T)),
				currentSource.Expression);

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Executes insert query and returns identity value of inserted record.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Inserted record's identity value.</returns>
		[Pure]
		public static object InsertWithIdentity<T>(this IValueInsertable<T> source)
		{
			ArgumentNullException.ThrowIfNull(source);

			var query = ((ValueInsertable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithIdentity, source),
				currentSource.Expression);

			return currentSource.Execute<object>(expr);
		}

		/// <summary>
		/// Executes insert query and returns identity value of inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static int? InsertWithInt32Identity<T>(this IValueInsertable<T> source)
		{
			ArgumentNullException.ThrowIfNull(source);

			return ((ExpressionQuery<T>)((ValueInsertable<T>)source).Query).DataContext.MappingSchema.ChangeTypeTo<int?>(InsertWithIdentity(source));
		}

		/// <summary>
		/// Executes insert query and returns identity value of inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static long? InsertWithInt64Identity<T>(this IValueInsertable<T> source)
		{
			ArgumentNullException.ThrowIfNull(source);

			return ((ExpressionQuery<T>)((ValueInsertable<T>)source).Query).DataContext.MappingSchema.ChangeTypeTo<long?>(InsertWithIdentity(source));
		}

		/// <summary>
		/// Executes insert query and returns identity value of inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static decimal? InsertWithDecimalIdentity<T>(this IValueInsertable<T> source)
		{
			ArgumentNullException.ThrowIfNull(source);

			return ((ExpressionQuery<T>)((ValueInsertable<T>)source).Query).DataContext.MappingSchema.ChangeTypeTo<decimal?>(InsertWithIdentity(source));
		}

		/// <summary>
		/// Executes insert query asynchronously and returns identity value of inserted record.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static Task<object> InsertWithIdentityAsync<T>(
			 this IValueInsertable<T> source, CancellationToken token = default)
		{
			ArgumentNullException.ThrowIfNull(source);

			var query = ((ValueInsertable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithIdentity, source),
				currentSource.Expression);

			return currentSource.ExecuteAsync<object>(expr, token);
		}

		/// <summary>
		/// Executes insert query asynchronously and returns identity value of inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<int?> InsertWithInt32IdentityAsync<T>(
			 this IValueInsertable<T> source, CancellationToken token = default)
		{
			ArgumentNullException.ThrowIfNull(source);

			return ((ExpressionQuery<T>)((ValueInsertable<T>)source).Query).DataContext.MappingSchema.ChangeTypeTo<int?>(
				await InsertWithIdentityAsync(source, token).ConfigureAwait(false));
		}

		/// <summary>
		/// Executes insert query asynchronously and returns identity value of inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<long?> InsertWithInt64IdentityAsync<T>(
			 this IValueInsertable<T> source, CancellationToken token = default)
		{
			ArgumentNullException.ThrowIfNull(source);

			return ((ExpressionQuery<T>)((ValueInsertable<T>)source).Query).DataContext.MappingSchema.ChangeTypeTo<long?>(
				await InsertWithIdentityAsync(source, token).ConfigureAwait(false));
		}

		/// <summary>
		/// Executes insert query asynchronously and returns identity value of inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<decimal?> InsertWithDecimalIdentityAsync<T>(
			 this IValueInsertable<T> source, CancellationToken token = default)
		{
			ArgumentNullException.ThrowIfNull(source);

			return ((ExpressionQuery<T>)((ValueInsertable<T>)source).Query).DataContext.MappingSchema.ChangeTypeTo<decimal?>(
				await InsertWithIdentityAsync(source, token).ConfigureAwait(false));
		}

		#endregion

		#region SelectInsertable

		/// <summary>
		/// Inserts records from source query into target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Number of affected records.</returns>
		public static int Insert<TSource, TTarget>(
							this IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter)
			where TTarget : notnull
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Insert, source, target, setter),
				currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Inserts records from source query into target table asynchronously.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> InsertAsync<TSource, TTarget>(
							this IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
			CancellationToken token = default)
			where TTarget : notnull
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Insert, source, target, setter),
				currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Inserts records from source query into target table and returns identity value of last inserted record.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static object InsertWithIdentity<TSource, TTarget>(
							this IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter)
			where TTarget : notnull
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithIdentity, source, target, setter),
				currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter));

			return currentSource.Execute<object>(expr);
		}

		/// <summary>
		/// Inserts records from source query into target table and returns identity value of last inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static int? InsertWithInt32Identity<TSource, TTarget>(
							this IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter)
			where TTarget : notnull
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);

			var currentSource = (IQueryable<TSource>)source.GetLinqToDBSource();

			return ((ExpressionQuery<TSource>)currentSource).DataContext.MappingSchema.ChangeTypeTo<int?>(
				InsertWithIdentity(currentSource, target, setter));
		}

		/// <summary>
		/// Inserts records from source query into target table and returns identity value of last inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static long? InsertWithInt64Identity<TSource, TTarget>(
							this IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter)
			where TTarget : notnull
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);

			var currentSource = (IQueryable<TSource>)source.GetLinqToDBSource();

			return ((ExpressionQuery<TSource>)currentSource).DataContext.MappingSchema.ChangeTypeTo<long?>(
				InsertWithIdentity(currentSource, target, setter));
		}

		/// <summary>
		/// Inserts records from source query into target table and returns identity value of last inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static decimal? InsertWithDecimalIdentity<TSource, TTarget>(
							this IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter)
			where TTarget : notnull
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);

			var currentSource = (IQueryable<TSource>)source.GetLinqToDBSource();

			return ((ExpressionQuery<TSource>)currentSource).DataContext.MappingSchema.ChangeTypeTo<decimal?>(
				InsertWithIdentity(currentSource, target, setter));
		}

		/// <summary>
		/// Inserts records from source query into target table asynchronously and returns identity value of last inserted record.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static Task<object> InsertWithIdentityAsync<TSource, TTarget>(
							this IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
			CancellationToken token = default)
			where TTarget : notnull
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithIdentity, source, target, setter),
				currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter));

			return currentSource.ExecuteAsync<object>(expr, token);
		}

		/// <summary>
		/// Inserts records from source query into target table asynchronously and returns identity value of last inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static async Task<int?> InsertWithInt32IdentityAsync<TSource, TTarget>(
							this IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
			CancellationToken token = default)
			where TTarget : notnull
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);

			var currentSource = (IQueryable<TSource>)source.GetLinqToDBSource();

			return ((ExpressionQuery<TSource>)currentSource).DataContext.MappingSchema.ChangeTypeTo<int?>(
				await InsertWithIdentityAsync(currentSource, target, setter, token).ConfigureAwait(false));
		}

		/// <summary>
		/// Inserts records from source query into target table asynchronously and returns identity value of last inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static async Task<long?> InsertWithInt64IdentityAsync<TSource, TTarget>(
							this IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
			CancellationToken token = default)
			where TTarget : notnull
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);

			var currentSource = (IQueryable<TSource>)source.GetLinqToDBSource();

			return ((ExpressionQuery<TSource>)currentSource).DataContext.MappingSchema.ChangeTypeTo<long?>(
				await InsertWithIdentityAsync(currentSource, target, setter, token).ConfigureAwait(false));
		}

		/// <summary>
		/// Inserts records from source query into target table asynchronously and returns identity value of last inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static async Task<decimal?> InsertWithDecimalIdentityAsync<TSource, TTarget>(
							this IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
			CancellationToken token = default)
			where TTarget : notnull
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(setter);

			var currentSource = (IQueryable<TSource>)source.GetLinqToDBSource();

			return ((ExpressionQuery<TSource>)currentSource).DataContext.MappingSchema.ChangeTypeTo<decimal?>(
				await InsertWithIdentityAsync(currentSource, target, setter, token).ConfigureAwait(false));
		}

		internal sealed class SelectInsertable<T, TT> : ISelectInsertable<T, TT>
		{
			public SelectInsertable(IQueryable<T> query)
			{
				Query = query;
			}

			public IQueryable<T> Query;
		}

		/// <summary>
		/// Converts LINQ query into insert query with source query data as data to insert.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <returns>Insertable source query.</returns>
		[LinqTunnel]
		[Pure]
		public static ISelectInsertable<TSource, TTarget> Into<TSource, TTarget>(
			 this IQueryable<TSource> source,
			 ITable<TTarget> target)
			where TTarget : notnull
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(target);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Into, source, target),
				currentSource.Expression, ((IQueryable<TTarget>)target).Expression);

			var q = currentSource.Provider.CreateQuery<TSource>(expr);
			return new SelectInsertable<TSource, TTarget>(q);
		}

		/// <summary>
		/// Add field setter to insert operation LINQ query.
		/// </summary>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <typeparam name="TTarget">Target record type</typeparam>
		/// <typeparam name="TValue">Field type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="field">Setter field selector expression.</param>
		/// <param name="value">Setter field value expression. Accepts source record as parameter.</param>
		/// <returns>Insert query.</returns>
		[LinqTunnel]
		[Pure]
		public static ISelectInsertable<TSource, TTarget> Value<TSource, TTarget, TValue>(
							this ISelectInsertable<TSource, TTarget> source,
			[InstantHandle] Expression<Func<TTarget, TValue>> field,
			[InstantHandle] Expression<Func<TSource, TValue>> value)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(field);
			ArgumentNullException.ThrowIfNull(value);

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Value, source, field, value),
				query.Expression, Expression.Quote(field), Expression.Quote(value));

			var q = query.Provider.CreateQuery<TSource>(expr);
			return new SelectInsertable<TSource, TTarget>(q);
		}

		/// <summary>
		/// Add field setter to insert operation LINQ query.
		/// </summary>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <typeparam name="TTarget">Target record type</typeparam>
		/// <typeparam name="TValue">Field type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="field">Setter field selector expression.</param>
		/// <param name="value">Setter field value expression.</param>
		/// <returns>Insert query.</returns>
		[LinqTunnel]
		[Pure]
		public static ISelectInsertable<TSource, TTarget> Value<TSource, TTarget, TValue>(
							this ISelectInsertable<TSource, TTarget> source,
			[InstantHandle] Expression<Func<TTarget, TValue>> field,
			[InstantHandle] Expression<Func<TValue>> value)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(field);
			ArgumentNullException.ThrowIfNull(value);

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;
			var expr = Expression.Call(
					null,
					MethodHelper.GetMethodInfo(Value, source, field, value),
					query.Expression, Expression.Quote(field), Expression.Quote(value));

			var q = query.Provider.CreateQuery<TSource>(expr);
			return new SelectInsertable<TSource, TTarget>(q);
		}

		/// <summary>
		/// Add field setter to insert operation LINQ query.
		/// </summary>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <typeparam name="TTarget">Target record type</typeparam>
		/// <typeparam name="TValue">Field type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="field">Setter field selector expression.</param>
		/// <param name="value">Setter field value.</param>
		/// <returns>Insert query.</returns>
		[LinqTunnel]
		[Pure]
		public static ISelectInsertable<TSource, TTarget> Value<TSource, TTarget, TValue>(
							this ISelectInsertable<TSource, TTarget> source,
			[InstantHandle] Expression<Func<TTarget, TValue>> field,
			TValue value)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(field);

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(Value, source, field, value),
				query.Expression, Expression.Quote(field), Expression.Constant(value, typeof(TValue)));

			var q = query.Provider.CreateQuery<TSource>(expr);
			return new SelectInsertable<TSource, TTarget>(q);
		}

		/// <summary>
		/// Executes configured insert query.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Number of affected records.</returns>
		public static int Insert<TSource, TTarget>(this ISelectInsertable<TSource, TTarget> source)
		{
			ArgumentNullException.ThrowIfNull(source);

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Insert.FromSelectInsertable.Insert.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
				currentSource.Expression);

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes configured insert query asynchronously.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<int> InsertAsync<TSource, TTarget>(
			 this ISelectInsertable<TSource, TTarget> source, CancellationToken token = default)
		{
			ArgumentNullException.ThrowIfNull(source);

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Insert.FromSelectInsertable.Insert.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
				currentSource.Expression);

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Executes configured insert query and returns identity value of last inserted record.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Number of affected records.</returns>
		public static object InsertWithIdentity<TSource, TTarget>(this ISelectInsertable<TSource, TTarget> source)
		{
			ArgumentNullException.ThrowIfNull(source);

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithIdentity, source),
				currentSource.Expression);

			return currentSource.Execute<object>(expr);
		}

		/// <summary>
		/// Executes configured insert query and returns identity value of last inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Number of affected records.</returns>
		public static int? InsertWithInt32Identity<TSource, TTarget>(this ISelectInsertable<TSource, TTarget> source)
		{
			ArgumentNullException.ThrowIfNull(source);

			return ((ExpressionQuery<TSource>)((SelectInsertable<TSource, TTarget>)source).Query).DataContext.MappingSchema.ChangeTypeTo<int?>(
				InsertWithIdentity(source));
		}

		/// <summary>
		/// Executes configured insert query and returns identity value of last inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Number of affected records.</returns>
		public static long? InsertWithInt64Identity<TSource, TTarget>(this ISelectInsertable<TSource, TTarget> source)
		{
			ArgumentNullException.ThrowIfNull(source);

			return ((ExpressionQuery<TSource>)((SelectInsertable<TSource, TTarget>)source).Query).DataContext.MappingSchema.ChangeTypeTo<long?>(
				InsertWithIdentity(source));
		}

		/// <summary>
		/// Executes configured insert query and returns identity value of last inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Number of affected records.</returns>
		public static decimal? InsertWithDecimalIdentity<TSource, TTarget>(this ISelectInsertable<TSource, TTarget> source)
		{
			ArgumentNullException.ThrowIfNull(source);

			return ((ExpressionQuery<TSource>)((SelectInsertable<TSource, TTarget>)source).Query).DataContext.MappingSchema.ChangeTypeTo<decimal?>(
				InsertWithIdentity(source));
		}

		/// <summary>
		/// Executes configured insert query asynchronously and returns identity value of last inserted record.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static Task<object> InsertWithIdentityAsync<TSource, TTarget>(
			 this ISelectInsertable<TSource, TTarget> source, CancellationToken token = default)
		{
			ArgumentNullException.ThrowIfNull(source);

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(InsertWithIdentity, source),
				currentSource.Expression);

			return currentSource.ExecuteAsync<object>(expr, token);
		}

		/// <summary>
		/// Executes configured insert query asynchronously and returns identity value of last inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static async Task<int?> InsertWithInt32IdentityAsync<TSource, TTarget>(
			 this ISelectInsertable<TSource, TTarget> source, CancellationToken token = default)
		{
			ArgumentNullException.ThrowIfNull(source);

			return ((ExpressionQuery<TSource>)((SelectInsertable<TSource, TTarget>)source).Query).DataContext.MappingSchema.ChangeTypeTo<int?>(
				await InsertWithIdentityAsync(source, token).ConfigureAwait(false));
		}

		/// <summary>
		/// Executes configured insert query asynchronously and returns identity value of last inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static async Task<long?> InsertWithInt64IdentityAsync<TSource, TTarget>(
			 this ISelectInsertable<TSource, TTarget> source, CancellationToken token = default)
		{
			ArgumentNullException.ThrowIfNull(source);

			return ((ExpressionQuery<TSource>)((SelectInsertable<TSource, TTarget>)source).Query).DataContext.MappingSchema.ChangeTypeTo<long?>(
				await InsertWithIdentityAsync(source, token).ConfigureAwait(false));
		}

		/// <summary>
		/// Executes configured insert query asynchronously and returns identity value of last inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static async Task<decimal?> InsertWithDecimalIdentityAsync<TSource, TTarget>(
			 this ISelectInsertable<TSource, TTarget> source, CancellationToken token = default)
		{
			ArgumentNullException.ThrowIfNull(source);

			return ((ExpressionQuery<TSource>)((SelectInsertable<TSource, TTarget>)source).Query).DataContext.MappingSchema.ChangeTypeTo<decimal?>(
				await InsertWithIdentityAsync(source, token).ConfigureAwait(false));
		}

		#endregion

		#endregion

	}
}
