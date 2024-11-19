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

	public class UpdateOutput<T>
	{
		public T Deleted { get; set; } = default!;
		public T Inserted { get; set; } = default!;
	}

	public static partial class LinqExtensions
	{
		#region Update against ITable<T> target

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Deleted and inserted values for every record updated.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// </list>
		/// </remarks>
		public static IEnumerable<UpdateOutput<TTarget>> UpdateWithOutput<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter)
			where TTarget: class
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, target, setter),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter));

			return currentSource.CreateQuery<UpdateOutput<TTarget>>(expr);
		}

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Deleted and inserted values for every record updated.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// </list>
		/// </remarks>
		public static IAsyncEnumerable<UpdateOutput<TTarget>> UpdateWithOutputAsync<TSource, TTarget>(
							this IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter)
			where TTarget : class
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, target, setter),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter));

			return currentSource.CreateQuery<UpdateOutput<TTarget>>(expr).AsAsyncEnumerable();
		}

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Deleted and inserted values for every record updated.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// </list>
		/// </remarks>
		[Obsolete("Will be removed in Linq To DB 7")]
		public static Task<UpdateOutput<TTarget>[]> UpdateWithOutputAsync<TSource, TTarget>(
							IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
							CancellationToken token)
			where TTarget : class
		{
			return source
				.UpdateWithOutputAsync(target, setter)
				.ToArrayAsync(token);
		}

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Parameters passed are as follows: (<typeparamref name="TSource"/> source, <typeparamref name="TTarget"/> deleted, <typeparamref name="TTarget"/> inserted).
		/// Expression supports only record new expression with field initializers.</param>
		/// <returns>Output values from the update statement.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL (doesn't support old data; database limitation)</item>
		/// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
		/// </list>
		/// </remarks>
		public static IEnumerable<TOutput> UpdateWithOutput<TSource,TTarget,TOutput>(
			                this IQueryable<TSource>                          source,
			                ITable<TTarget>                                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>>                 setter,
			                Expression<Func<TSource,TTarget,TTarget,TOutput>> outputExpression)
			where TTarget : class
		{
			if (source ==           null) throw new ArgumentNullException(nameof(source));
			if (target ==           null) throw new ArgumentNullException(nameof(target));
			if (setter ==           null) throw new ArgumentNullException(nameof(setter));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, target, setter, outputExpression),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter),
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr);
		}

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Parameters passed are as follows: (<typeparamref name="TSource"/> source, <typeparamref name="TTarget"/> deleted, <typeparamref name="TTarget"/> inserted).
		/// Expression supports only record new expression with field initializers.</param>
		/// <returns>Async sequence of records returned by output.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL (doesn't support old data; database limitation)</item>
		/// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
		/// </list>
		/// </remarks>
		public static IAsyncEnumerable<TOutput> UpdateWithOutputAsync<TSource, TTarget, TOutput>(
							this IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
							Expression<Func<TSource, TTarget, TTarget, TOutput>> outputExpression)
			where TTarget : class
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (target           == null) throw new ArgumentNullException(nameof(target));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, target, setter, outputExpression),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter),
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr).AsAsyncEnumerable();
		}

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Parameters passed are as follows: (<typeparamref name="TSource"/> source, <typeparamref name="TTarget"/> deleted, <typeparamref name="TTarget"/> inserted).
		/// Expression supports only record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Sequence of records returned by output.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (doesn't support more than one record; database limitation)</item>
		/// <item>PostgreSQL (doesn't support old data; database limitation)</item>
		/// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
		/// </list>
		/// </remarks>
		[Obsolete("Will be removed in Linq To DB 7")]
		public static Task<TOutput[]> UpdateWithOutputAsync<TSource, TTarget, TOutput>(
							IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
							Expression<Func<TSource, TTarget, TTarget, TOutput>> outputExpression,
							CancellationToken token)
			where TTarget : class
		{
			return source
				.UpdateWithOutputAsync(target, setter, outputExpression)
				.ToArrayAsync(token);
		}

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <returns>Number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static int UpdateWithOutputInto<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter,
			                ITable<TTarget>                   outputTable)
			where TTarget : class
		{
			if (source ==      null) throw new ArgumentNullException(nameof(source));
			if (target ==      null) throw new ArgumentNullException(nameof(target));
			if (setter ==      null) throw new ArgumentNullException(nameof(setter));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, target, setter, outputTable),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter),
				((IQueryable<TTarget>)outputTable).Expression);

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static Task<int> UpdateWithOutputIntoAsync<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter,
			                ITable<TTarget>                   outputTable,
			                CancellationToken                 token = default)
			where TTarget : class
		{
			if (source ==      null) throw new ArgumentNullException(nameof(source));
			if (target ==      null) throw new ArgumentNullException(nameof(target));
			if (setter ==      null) throw new ArgumentNullException(nameof(setter));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, target, setter, outputTable),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter),
				((IQueryable<TTarget>)outputTable).Expression);

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Parameters passed are as follows: (<typeparamref name="TSource"/> source, <typeparamref name="TTarget"/> deleted, <typeparamref name="TTarget"/> inserted).
		/// Expression supports only record new expression with field initializers.</param>
		/// <returns>Output values from the update statement.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static int UpdateWithOutputInto<TSource,TTarget,TOutput>(
			                this IQueryable<TSource>                          source,
			                ITable<TTarget>                                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>>                 setter,
			                ITable<TOutput>                                   outputTable,
			                Expression<Func<TSource,TTarget,TTarget,TOutput>> outputExpression)
			where TTarget : class
			where TOutput : class
		{
			if (source ==           null) throw new ArgumentNullException(nameof(source));
			if (target ==           null) throw new ArgumentNullException(nameof(target));
			if (setter ==           null) throw new ArgumentNullException(nameof(setter));
			if (outputTable ==      null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, target, setter, outputTable, outputExpression),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter),
				((IQueryable<TOutput>)outputTable).Expression,
				Expression.Quote(outputExpression));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Parameters passed are as follows: (<typeparamref name="TSource"/> source, <typeparamref name="TTarget"/> deleted, <typeparamref name="TTarget"/> inserted).
		/// Expression supports only record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Output values from the update statement.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static Task<int> UpdateWithOutputIntoAsync<TSource,TTarget,TOutput>(
			                this IQueryable<TSource>                          source,
			                ITable<TTarget>                                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>>                 setter,
			                ITable<TOutput>                                   outputTable,
			                Expression<Func<TSource,TTarget,TTarget,TOutput>> outputExpression,
			                CancellationToken                                 token = default)
			where TTarget : class
			where TOutput : class
		{
			if (source ==           null) throw new ArgumentNullException(nameof(source));
			if (target ==           null) throw new ArgumentNullException(nameof(target));
			if (setter ==           null) throw new ArgumentNullException(nameof(setter));
			if (outputTable ==      null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, target, setter, outputTable, outputExpression),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter),
				((IQueryable<TOutput>)outputTable).Expression,
				Expression.Quote(outputExpression));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		#endregion

		#region Update against Expression target

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Deleted and inserted values for every record updated.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// </list>
		/// </remarks>
		public static IEnumerable<UpdateOutput<TTarget>> UpdateWithOutput<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                Expression<Func<TSource,TTarget>> target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, target, setter),
				currentSource.Expression,
				Expression.Quote(target),
				Expression.Quote(setter));

			return currentSource.CreateQuery<UpdateOutput<TTarget>>(expr).AsEnumerable();
		}

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Deleted and inserted values for every record updated.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// </list>
		/// </remarks>
		public static IAsyncEnumerable<UpdateOutput<TTarget>> UpdateWithOutputAsync<TSource, TTarget>(
							this IQueryable<TSource> source,
							Expression<Func<TSource, TTarget>> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, target, setter),
				currentSource.Expression,
				Expression.Quote(target),
				Expression.Quote(setter));

			return currentSource.CreateQuery<UpdateOutput<TTarget>>(expr).AsAsyncEnumerable();
		}

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Deleted and inserted values for every record updated.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// </list>
		/// </remarks>
		[Obsolete("Will be removed in Linq To DB 7")]
		public static Task<UpdateOutput<TTarget>[]> UpdateWithOutputAsync<TSource, TTarget>(
							IQueryable<TSource> source,
							Expression<Func<TSource, TTarget>> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
							CancellationToken token)
		{
			return source
				.UpdateWithOutputAsync(target, setter)
				.ToArrayAsync(token);
		}

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Parameters passed are as follows: (<typeparamref name="TSource"/> source, <typeparamref name="TTarget"/> deleted, <typeparamref name="TTarget"/> inserted).
		/// Expression supports only record new expression with field initializers.</param>
		/// <returns>Output values from the update statement.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL (doesn't support old data; database limitation)</item>
		/// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
		/// </list>
		/// </remarks>
		public static IEnumerable<TOutput> UpdateWithOutput<TSource,TTarget,TOutput>(
			                this IQueryable<TSource>                          source,
			                Expression<Func<TSource,TTarget>>                 target,
			[InstantHandle] Expression<Func<TSource,TTarget>>                 setter,
							Expression<Func<TSource,TTarget,TTarget,TOutput>> outputExpression)
		{
			if (source ==           null) throw new ArgumentNullException(nameof(source));
			if (target ==           null) throw new ArgumentNullException(nameof(target));
			if (setter ==           null) throw new ArgumentNullException(nameof(setter));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, target, setter, outputExpression),
				currentSource.Expression,
				Expression.Quote(target),
				Expression.Quote(setter),
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr).AsEnumerable();
		}

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Parameters passed are as follows: (<typeparamref name="TSource"/> source, <typeparamref name="TTarget"/> deleted, <typeparamref name="TTarget"/> inserted).
		/// Expression supports only record new expression with field initializers.</param>
		/// <returns>Async sequence of records returned by output.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL (doesn't support old data; database limitation)</item>
		/// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
		/// </list>
		/// </remarks>
		public static IAsyncEnumerable<TOutput> UpdateWithOutputAsync<TSource, TTarget, TOutput>(
							this IQueryable<TSource> source,
							Expression<Func<TSource, TTarget>> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
							Expression<Func<TSource, TTarget, TTarget, TOutput>> outputExpression)
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (target           == null) throw new ArgumentNullException(nameof(target));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, target, setter, outputExpression),
				currentSource.Expression,
				Expression.Quote(target),
				Expression.Quote(setter),
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr).AsAsyncEnumerable();
		}

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Parameters passed are as follows: (<typeparamref name="TSource"/> source, <typeparamref name="TTarget"/> deleted, <typeparamref name="TTarget"/> inserted).
		/// Expression supports only record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Sequence of records returned by output.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (doesn't support more than one record; database limitation)</item>
		/// <item>PostgreSQL (doesn't support old data; database limitation)</item>
		/// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
		/// </list>
		/// </remarks>
		[Obsolete("Will be removed in Linq To DB 7")]
		public static Task<TOutput[]> UpdateWithOutputAsync<TSource, TTarget, TOutput>(
							IQueryable<TSource> source,
							Expression<Func<TSource, TTarget>> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
							Expression<Func<TSource, TTarget, TTarget, TOutput>> outputExpression,
							CancellationToken token)
		{
			return source
				.UpdateWithOutputAsync(target, setter, outputExpression)
				.ToArrayAsync(token);
		}

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <returns>Number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static int UpdateWithOutputInto<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                Expression<Func<TSource,TTarget>> target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter,
							ITable<TTarget>					  outputTable)
			where TTarget : class
		{
			if (source ==      null) throw new ArgumentNullException(nameof(source));
			if (target ==      null) throw new ArgumentNullException(nameof(target));
			if (setter ==      null) throw new ArgumentNullException(nameof(setter));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, target, setter, outputTable),
				currentSource.Expression,
				Expression.Quote(target),
				Expression.Quote(setter),
				((IQueryable<TTarget>)outputTable).Expression);

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static Task<int> UpdateWithOutputIntoAsync<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                Expression<Func<TSource,TTarget>> target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter,
							ITable<TTarget>					  outputTable,
							CancellationToken                 token = default)
			where TTarget : class
		{
			if (source ==      null) throw new ArgumentNullException(nameof(source));
			if (target ==      null) throw new ArgumentNullException(nameof(target));
			if (setter ==      null) throw new ArgumentNullException(nameof(setter));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, target, setter, outputTable),
				currentSource.Expression,
				Expression.Quote(target),
				Expression.Quote(setter),
				((IQueryable<TTarget>)outputTable).Expression);

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Parameters passed are as follows: (<typeparamref name="TSource"/> source, <typeparamref name="TTarget"/> deleted, <typeparamref name="TTarget"/> inserted).
		/// Expression supports only record new expression with field initializers.</param>
		/// <returns>Output values from the update statement.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static int UpdateWithOutputInto<TSource,TTarget,TOutput>(
			                this IQueryable<TSource>                          source,
			                Expression<Func<TSource,TTarget>>                 target,
			[InstantHandle] Expression<Func<TSource,TTarget>>                 setter,
							ITable<TOutput>					                  outputTable,
							Expression<Func<TSource,TTarget,TTarget,TOutput>> outputExpression)
			where TOutput : class
		{
			if (source ==           null) throw new ArgumentNullException(nameof(source));
			if (target ==           null) throw new ArgumentNullException(nameof(target));
			if (setter ==           null) throw new ArgumentNullException(nameof(setter));
			if (outputTable ==      null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, target, setter, outputTable, outputExpression),
				currentSource.Expression,
				Expression.Quote(target),
				Expression.Quote(setter),
				((IQueryable<TOutput>)outputTable).Expression,
				Expression.Quote(outputExpression));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Parameters passed are as follows: (<typeparamref name="TSource"/> source, <typeparamref name="TTarget"/> deleted, <typeparamref name="TTarget"/> inserted).
		/// Expression supports only record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Output values from the update statement.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static Task<int> UpdateWithOutputIntoAsync<TSource,TTarget,TOutput>(
			                this IQueryable<TSource>                          source,
			                Expression<Func<TSource,TTarget>>                 target,
			[InstantHandle] Expression<Func<TSource,TTarget>>                 setter,
							ITable<TOutput>					                  outputTable,
							Expression<Func<TSource,TTarget,TTarget,TOutput>> outputExpression,
							CancellationToken                                 token = default)
			where TOutput : class
		{
			if (source ==           null) throw new ArgumentNullException(nameof(source));
			if (target ==           null) throw new ArgumentNullException(nameof(target));
			if (setter ==           null) throw new ArgumentNullException(nameof(setter));
			if (outputTable ==      null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, target, setter, outputTable, outputExpression),
				currentSource.Expression,
				Expression.Quote(target),
				Expression.Quote(setter),
				((IQueryable<TOutput>)outputTable).Expression,
				Expression.Quote(outputExpression));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		#endregion

		#region Update from source

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Deleted and inserted values for every record updated.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// </list>
		/// </remarks>
		public static IEnumerable<UpdateOutput<T>> UpdateWithOutput<T>(
			           this IQueryable<T>         source,
			[InstantHandle] Expression<Func<T,T>> setter)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, setter),
				currentSource.Expression, Expression.Quote(setter));

			return currentSource.CreateQuery<UpdateOutput<T>>(expr).AsEnumerable();
		}

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Deleted and inserted values for every record updated.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// </list>
		/// </remarks>
		public static IAsyncEnumerable<UpdateOutput<T>> UpdateWithOutputAsync<T>(
					   this IQueryable<T> source,
			[InstantHandle] Expression<Func<T, T>> setter)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, setter),
				currentSource.Expression,
				Expression.Quote(setter));

			return currentSource.CreateQuery<UpdateOutput<T>>(expr).AsAsyncEnumerable();
		}

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Deleted and inserted values for every record updated.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// </list>
		/// </remarks>
		[Obsolete("Will be removed in Linq To DB 7")]
		public static Task<UpdateOutput<T>[]> UpdateWithOutputAsync<T>(
					        IQueryable<T> source,
			[InstantHandle] Expression<Func<T, T>> setter,
							CancellationToken token)
		{
			return source
				.UpdateWithOutputAsync(setter)
				.ToArrayAsync(token);
		}

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Parameters passed are as follows: (<typeparamref name="T"/> deleted, <typeparamref name="T"/> inserted).
		/// Expression supports only record new expression with field initializers.</param>
		/// <returns>Output values from the update statement.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL (doesn't support old data; database limitation)</item>
		/// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
		/// </list>
		/// </remarks>
		public static IEnumerable<TOutput> UpdateWithOutput<T,TOutput>(
			           this IQueryable<T>                 source,
			[InstantHandle] Expression<Func<T,T>>         setter,
			                Expression<Func<T,T,TOutput>> outputExpression)
		{
			if (source ==           null) throw new ArgumentNullException(nameof(source));
			if (setter ==           null) throw new ArgumentNullException(nameof(setter));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, setter, outputExpression),
				currentSource.Expression,
				Expression.Quote(setter),
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr).AsEnumerable();
		}

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Parameters passed are as follows: (<typeparamref name="T"/> deleted, <typeparamref name="T"/> inserted).
		/// Expression supports only record new expression with field initializers.</param>
		/// <returns>Async sequence of records returned by output.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL (doesn't support old data; database limitation)</item>
		/// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
		/// </list>
		/// </remarks>
		public static IAsyncEnumerable<TOutput> UpdateWithOutputAsync<T, TOutput>(
					   this IQueryable<T> source,
			[InstantHandle] Expression<Func<T, T>> setter,
							Expression<Func<T, T, TOutput>> outputExpression)
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, setter, outputExpression),
				currentSource.Expression,
				Expression.Quote(setter),
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr).AsAsyncEnumerable();
		}

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Parameters passed are as follows: (<typeparamref name="T"/> deleted, <typeparamref name="T"/> inserted).
		/// Expression supports only record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Sequence of records returned by output.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (doesn't support more than one record; database limitation)</item>
		/// <item>PostgreSQL (doesn't support old data; database limitation)</item>
		/// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
		/// </list>
		/// </remarks>
		[Obsolete("Will be removed in Linq To DB 7")]
		public static Task<TOutput[]> UpdateWithOutputAsync<T, TOutput>(
					        IQueryable<T> source,
			[InstantHandle] Expression<Func<T, T>> setter,
							Expression<Func<T, T, TOutput>> outputExpression,
							CancellationToken token)
		{
			return source
				.UpdateWithOutputAsync(setter, outputExpression)
				.ToArrayAsync(token);
		}

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <returns>Number of updated records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static int UpdateWithOutputInto<T>(
			           this IQueryable<T>         source,
			[InstantHandle] Expression<Func<T,T>> setter,
			                ITable<T>             outputTable)
			where T : class
		{
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (setter      == null) throw new ArgumentNullException(nameof(setter));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, setter, outputTable),
				currentSource.Expression,
				Expression.Quote(setter),
				((IQueryable<T>)outputTable).Expression);

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of updated records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static Task<int> UpdateWithOutputIntoAsync<T>(
			           this IQueryable<T>         source,
			[InstantHandle] Expression<Func<T,T>> setter,
			                ITable<T>             outputTable,
			                CancellationToken     token = default)
			where T : class
		{
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (setter      == null) throw new ArgumentNullException(nameof(setter));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, setter, outputTable),
				currentSource.Expression,
				Expression.Quote(setter),
				((IQueryable<T>)outputTable).Expression);

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Parameters passed are as follows: (<typeparamref name="T"/> deleted, <typeparamref name="T"/> inserted).
		/// Expression supports only record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <returns>Number of updated records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static int UpdateWithOutputInto<T,TOutput>(
			           this IQueryable<T>                 source,
			[InstantHandle] Expression<Func<T,T>>         setter,
			                ITable<TOutput>               outputTable,
			                Expression<Func<T,T,TOutput>> outputExpression)
			where TOutput : class
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputTable      == null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, setter, outputTable, outputExpression),
				currentSource.Expression,
				Expression.Quote(setter),
				((IQueryable<TOutput>)outputTable).Expression,
				Expression.Quote(outputExpression));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Parameters passed are as follows: (<typeparamref name="T"/> deleted, <typeparamref name="T"/> inserted).
		/// Expression supports only record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of updated records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static Task<int> UpdateWithOutputIntoAsync<T,TOutput>(
			           this IQueryable<T>                 source,
			[InstantHandle] Expression<Func<T,T>>         setter,
			                ITable<TOutput>               outputTable,
			                Expression<Func<T,T,TOutput>> outputExpression,
			                CancellationToken             token = default)
			where TOutput : class
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputTable      == null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, setter, outputTable, outputExpression),
				currentSource.Expression,
				Expression.Quote(setter),
				((IQueryable<TOutput>)outputTable).Expression,
				Expression.Quote(outputExpression));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		#endregion

		#region IUpdatable

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <returns>Deleted and inserted values for every record updated.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// </list>
		/// </remarks>
		[LinqTunnel, Pure]
		public static IEnumerable<UpdateOutput<T>> UpdateWithOutput<T>(this IUpdatable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query = ((Updatable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source),
				currentSource.Expression);

			return currentSource.CreateQuery<UpdateOutput<T>>(expr).AsEnumerable();
		}

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <returns>Deleted and inserted values for every record updated.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// </list>
		/// </remarks>
		public static IAsyncEnumerable<UpdateOutput<T>> UpdateWithOutputAsync<T>(
					   this IUpdatable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query = ((Updatable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source),
				currentSource.Expression);

			return currentSource.CreateQuery<UpdateOutput<T>>(expr).AsAsyncEnumerable();
		}

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Deleted and inserted values for every record updated.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// </list>
		/// </remarks>
		[Obsolete("Will be removed in Linq To DB 7")]
		public static Task<UpdateOutput<T>[]> UpdateWithOutputAsync<T>(
					        IUpdatable<T> source,
							CancellationToken token)
		{
			return source.UpdateWithOutputAsync().ToArrayAsync(token);
		}

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Parameters passed are as follows: (<typeparamref name="T"/> deleted, <typeparamref name="T"/> inserted).
		/// Expression supports only record new expression with field initializer.</param>
		/// <returns>Output values from the update statement.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL (doesn't support old data; database limitation)</item>
		/// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
		/// </list>
		/// </remarks>
		public static IEnumerable<TOutput> UpdateWithOutput<T,TOutput>(
			this IUpdatable<T>            source,
			Expression<Func<T,T,TOutput>> outputExpression)
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var query         = ((Updatable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, outputExpression),
				currentSource.Expression,
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr).AsEnumerable();
		}

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Parameters passed are as follows: (<typeparamref name="T"/> deleted, <typeparamref name="T"/> inserted).
		/// Expression supports only record new expression with field initializers.</param>
		/// <returns>Async sequence of records returned by output.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL (doesn't support old data; database limitation)</item>
		/// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
		/// </list>
		/// </remarks>
		public static IAsyncEnumerable<TOutput> UpdateWithOutputAsync<T, TOutput>(
					   this IUpdatable<T> source,
							Expression<Func<T, T, TOutput>> outputExpression)
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var query = ((Updatable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, outputExpression),
				currentSource.Expression,
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr).AsAsyncEnumerable();
		}

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Parameters passed are as follows: (<typeparamref name="T"/> deleted, <typeparamref name="T"/> inserted).
		/// Expression supports only record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Output values from the update statement.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (doesn't support more than one record; database limitation)</item>
		/// <item>PostgreSQL (doesn't support old data; database limitation)</item>
		/// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
		/// </list>
		/// </remarks>
		[Obsolete("Will be removed in Linq To DB 7")]
		public static Task<TOutput[]> UpdateWithOutputAsync<T, TOutput>(
					        IUpdatable<T> source,
							Expression<Func<T, T, TOutput>> outputExpression,
							CancellationToken token)
		{
			return source
				.UpdateWithOutputAsync(outputExpression)
				.ToArrayAsync(token);
		}

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="outputTable">Output table.</param>
		/// <returns>Number of updated records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static int UpdateWithOutputInto<T>(
			           this IUpdatable<T>         source,
			                ITable<T>             outputTable)
			where T : class
		{
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var query = ((Updatable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, outputTable),
				currentSource.Expression,
				((IQueryable<T>)outputTable).Expression);

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of updated records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static Task<int> UpdateWithOutputIntoAsync<T>(
			           this IUpdatable<T>         source,
			                ITable<T>             outputTable,
			                CancellationToken     token = default)
			where T : class
		{
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var query = ((Updatable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, outputTable),
				currentSource.Expression,
				((IQueryable<T>)outputTable).Expression);

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Parameters passed are as follows: (<typeparamref name="T"/> deleted, <typeparamref name="T"/> inserted).
		/// Expression supports only record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <returns>Number of updated records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static int UpdateWithOutputInto<T,TOutput>(
			           this IUpdatable<T>                 source,
			                ITable<TOutput>               outputTable,
			                Expression<Func<T,T,TOutput>> outputExpression)
			where TOutput : class
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (outputTable      == null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var query = ((Updatable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, outputTable, outputExpression),
				currentSource.Expression,
				((IQueryable<TOutput>)outputTable).Expression,
				Expression.Quote(outputExpression));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Parameters passed are as follows: (<typeparamref name="T"/> deleted, <typeparamref name="T"/> inserted).
		/// Expression supports only record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of updated records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static Task<int> UpdateWithOutputIntoAsync<T,TOutput>(
			           this IUpdatable<T>                 source,
			                ITable<TOutput>               outputTable,
			                Expression<Func<T,T,TOutput>> outputExpression,
			                CancellationToken             token = default)
			where TOutput : class
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (outputTable      == null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var query = ((Updatable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, outputTable, outputExpression),
				currentSource.Expression,
				((IQueryable<TOutput>)outputTable).Expression,
				Expression.Quote(outputExpression));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		#endregion
	}
}
