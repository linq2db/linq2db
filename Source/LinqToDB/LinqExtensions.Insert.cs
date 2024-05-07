using System;
using System.Collections.Generic;
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

			IQueryable<TTarget> query = target;

			var items = query.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutput, target, setter),
					query.Expression, Expression.Quote(setter)));

			return items.AsEnumerable().First();
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

			IQueryable<TTarget> query = target;

			var items = query.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutput, target, setter),
					query.Expression, Expression.Quote(setter)));

			return items.AsAsyncEnumerable().FirstAsync(token);

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

			IQueryable<TTarget> query = target;

			var items = query.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutput, target, obj),
					query.Expression, Expression.Constant(obj)));

			return items.AsEnumerable().First();
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

			IQueryable<TTarget> query = target;

			var items = query.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutput, target, obj),
					query.Expression, Expression.Constant(obj)));

			return items.AsAsyncEnumerable().SingleAsync(token);
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

			IQueryable<TTarget> query = target;

			var items = query.Provider.CreateQuery<TOutput>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutput, target, setter, outputExpression),
					query.Expression, Expression.Quote(setter), Expression.Quote(outputExpression)));

			return items.AsEnumerable().First();
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

			IQueryable<TTarget> query = target;

			var items = query.Provider.CreateQuery<TOutput>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutput, target, setter, outputExpression),
					query.Expression, Expression.Quote(setter), Expression.Quote(outputExpression)));

			return items.AsAsyncEnumerable().FirstAsync(token);
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

			IQueryable<TTarget> query = target;

			return query.Provider.Execute<int>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutputInto, target, setter, outputTable),
					query.Expression, Expression.Quote(setter), ((IQueryable<TTarget>)outputTable).Expression));
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

			IQueryable<TTarget> query = target;

			var expr =
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutputInto, target, setter, outputTable),
					query.Expression, Expression.Quote(setter), ((IQueryable<TTarget>)outputTable).Expression);

			return ((IQueryProviderAsync)query).ExecuteAsync<int>(expr, token);
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

			IQueryable<TTarget> query = target;

			return query.Provider.Execute<int>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutputInto, target, setter, outputTable, outputExpression),
					query.Expression, Expression.Quote(setter), ((IQueryable<TOutput>)outputTable).Expression,
					Expression.Quote(outputExpression)));
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

			IQueryable<TTarget> query = target;

			var expr =
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutputInto, target, setter, outputTable, outputExpression),
					query.Expression, Expression.Quote(setter), ((IQueryable<TOutput>)outputTable).Expression,
					Expression.Quote(outputExpression));

			return ((IQueryProviderAsync)query).ExecuteAsync<int>(expr, token);
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

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.CreateQuery<TTarget>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(InsertWithOutput, source, target, setter),
						currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter)))
				.AsEnumerable();
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
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.5+</item>
		/// </list>
		/// </remarks>
		public static Task<TTarget[]> InsertWithOutputAsync<TSource, TTarget>(
			                this IQueryable<TSource>           source,
			                ITable<TTarget>                    target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
							CancellationToken                  token = default)
			where TTarget : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.CreateQuery<TTarget>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(InsertWithOutput, source, target, setter),
						currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter)))
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

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.CreateQuery<TOutput>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(InsertWithOutput, source, target, setter, outputExpression),
						currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter),
						Expression.Quote(outputExpression)))
				.AsEnumerable();
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
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.5+</item>
		/// </list>
		/// </remarks>
		public static Task<TOutput[]> InsertWithOutputAsync<TSource,TTarget,TOutput>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter,
			                Expression<Func<TTarget,TOutput>> outputExpression,
							CancellationToken                 token = default)
			where TTarget : notnull
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (target           == null) throw new ArgumentNullException(nameof(target));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.CreateQuery<TOutput>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(InsertWithOutput, source, target, setter, outputExpression),
						currentSource.Expression, ((IQueryable<TTarget>) target).Expression, Expression.Quote(setter),
						Expression.Quote(outputExpression)))
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

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<int>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutputInto, source, target, setter, outputTable),
					currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter), ((IQueryable<TTarget>)outputTable).Expression));
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

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			var expr =
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutputInto, source, target, setter, outputTable),
					currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter), ((IQueryable<TTarget>)outputTable).Expression);

			if (source is IQueryProviderAsync queryAsync)
				return queryAsync.ExecuteAsync<int>(expr, token);

			return Task.Run(() => source.Provider.Execute<int>(expr), token);
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

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return source.Provider.Execute<int>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutputInto, source, target, setter, outputTable, outputExpression),
					currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter),
					((IQueryable<TOutput>)outputTable).Expression, Expression.Quote(outputExpression)));
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

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			var expr =
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutputInto, source, target, setter, outputTable, outputExpression),
					currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter),
					((IQueryable<TOutput>)outputTable).Expression, Expression.Quote(outputExpression));

			if (currentSource is IQueryProviderAsync queryAsync)
				return queryAsync.ExecuteAsync<int>(expr, token);

			return Task.Run(() => currentSource.Provider.Execute<int>(expr), token);
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

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;

			var items = query.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutput, source),
					query.Expression));

			return items.AsEnumerable().First();
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

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;

			var items = query.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutput, source),
					query.Expression));

			return items.AsAsyncEnumerable().FirstAsync(token);
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

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;

			return query.Provider.Execute<int>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutputInto, source, outputTable),
					query.Expression, ((IQueryable<TTarget>)outputTable).Expression));
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

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;

			var expr =
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutputInto, source, outputTable),
					query.Expression, ((IQueryable<TTarget>)outputTable).Expression);

			if (query is IQueryProviderAsync queryAsync)
				return queryAsync.ExecuteAsync<int>(expr, token);

			return Task.Run(() => query.Provider.Execute<int>(expr), token);
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
			var currentSource = ProcessSourceQueryable?.Invoke(query) ?? query;

			return currentSource.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutput, source),
					currentSource.Expression)).AsEnumerable().First();
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
			var currentSource = ProcessSourceQueryable?.Invoke(query) ?? query;

			return currentSource.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutput, source),
					currentSource.Expression)).AsAsyncEnumerable().FirstAsync(token);
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
			var currentSource = ProcessSourceQueryable?.Invoke(query) ?? query;

			return currentSource.Provider.CreateQuery<TOutput>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutput, source, outputExpression),
					currentSource.Expression,
					Expression.Quote(outputExpression))).AsEnumerable().First();
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
			var currentSource = ProcessSourceQueryable?.Invoke(query) ?? query;

			return currentSource.Provider.CreateQuery<TOutput>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutput, source, outputExpression),
					currentSource.Expression,
					Expression.Quote(outputExpression))).AsAsyncEnumerable().FirstAsync(token);
		}
		#endregion
	}
}
