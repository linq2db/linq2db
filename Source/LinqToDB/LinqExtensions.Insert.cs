using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB
{
	using Async;
	using Expressions;
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
		public static TTarget InsertWithOutput<TTarget>(
			[NotNull]                this ITable<TTarget>      target,
			[NotNull, InstantHandle] Expression<Func<TTarget>> setter)
		{
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			IQueryable<TTarget> query = target;

			return query.Provider.Execute<TTarget>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutput, target, setter),
					new[] { query.Expression, Expression.Quote(setter) }));
		}

		/// <summary>
		/// Inserts single record into target table and returns inserted record.
		/// </summary>
		/// <typeparam name="TTarget">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="obj">Object with data to insert.</param>
		/// <returns>Inserted record.</returns>
		public static TTarget InsertWithOutput<TTarget>(
			[NotNull]                this ITable<TTarget> target,
			[NotNull, InstantHandle] TTarget              obj)
		{
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (obj    == null) throw new ArgumentNullException(nameof(obj));

			IQueryable<TTarget> query = target;

			return query.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutput, target, obj),
					new[] { query.Expression, Expression.Constant(obj) })).AsEnumerable().First();
		}

		/// <summary>
		/// Inserts single record into target table asynchronously and returns inserted record.
		/// </summary>
		/// <typeparam name="TTarget">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record.</returns>
		public static Task<TTarget> InsertWithOutputAsync<TTarget>(
			[NotNull]                this ITable<TTarget>      target,
			[NotNull, InstantHandle] Expression<Func<TTarget>> setter,
			CancellationToken                                  token = default)
		{
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			IQueryable<TTarget> query = target;

			var expr =
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutput, target, setter),
					new[] { query.Expression, Expression.Quote(setter) });

			if (query is IQueryProviderAsync queryAsync)
				return queryAsync.ExecuteAsync<TTarget>(expr, token);

			return TaskEx.Run(() => query.Provider.Execute<TTarget>(expr), token);

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
		public static TOutput InsertWithOutput<TTarget,TOutput>(
			[NotNull]                this ITable<TTarget>              target,
			[NotNull, InstantHandle] Expression<Func<TTarget>>         setter,
			[NotNull]                Expression<Func<TTarget,TOutput>> outputExpression)
		{
			if (target           == null) throw new ArgumentNullException(nameof(target));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			IQueryable<TTarget> query = target;

			return query.Provider.Execute<TOutput>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutput, target, setter, outputExpression),
					new[] { query.Expression, Expression.Quote(setter) }));

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
		public static Task<TOutput> InsertWithOutputAsync<TTarget,TOutput>(
			[NotNull]                this ITable<TTarget>              target,
			[NotNull, InstantHandle] Expression<Func<TTarget>>         setter,
			[NotNull]                Expression<Func<TTarget,TOutput>> outputExpression,
			                         CancellationToken                 token = default)

		{
			if (target           == null) throw new ArgumentNullException(nameof(target));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			IQueryable<TTarget> query = target;

			var expr =
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutput, target, setter, outputExpression),
					new[] { query.Expression, Expression.Quote(setter) });

			if (query is IQueryProviderAsync queryAsync)
				return queryAsync.ExecuteAsync<TOutput>(expr, token);

			return TaskEx.Run(() => query.Provider.Execute<TOutput>(expr), token);
		}

		/// <summary>
		/// Inserts single record into target table and outputs that record into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TTarget">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="outputTable">Output table.</param>
		/// <returns>Number of affected records.</returns>
		public static int InsertWithOutputInto<TTarget>(
			[NotNull]                this ITable<TTarget>      target,
			[NotNull, InstantHandle] Expression<Func<TTarget>> setter,
			[NotNull]                ITable<TTarget>           outputTable)
		{
			if (target      == null) throw new ArgumentNullException(nameof(target));
			if (setter      == null) throw new ArgumentNullException(nameof(setter));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			IQueryable<TTarget> query = target;

			return query.Provider.Execute<int>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutputInto, target, setter, outputTable),
					new[] { query.Expression, Expression.Quote(setter), ((IQueryable<TTarget>)outputTable).Expression }));
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
		public static Task<int> InsertWithOutputIntoAsync<TTarget>(
			[NotNull]                this ITable<TTarget>      target,
			[NotNull, InstantHandle] Expression<Func<TTarget>> setter,
			[NotNull]                ITable<TTarget>           outputTable,
			                         CancellationToken         token = default)
		{
			if (target      == null) throw new ArgumentNullException(nameof(target));
			if (setter      == null) throw new ArgumentNullException(nameof(setter));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			IQueryable<TTarget> query = target;

			var expr =
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutputInto, target, setter, outputTable),
					new[] { query.Expression, Expression.Quote(setter), ((IQueryable<TTarget>)outputTable).Expression });

			if (query is IQueryProviderAsync queryAsync)
				return queryAsync.ExecuteAsync<int>(expr, token);

			return TaskEx.Run(() => query.Provider.Execute<int>(expr), token);
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
		public static int InsertWithOutputInto<TTarget,TOutput>(
			[NotNull]                this ITable<TTarget>              target,
			[NotNull, InstantHandle] Expression<Func<TTarget>>         setter,
			[NotNull]                ITable<TOutput>                   outputTable,
			[NotNull]                Expression<Func<TTarget,TOutput>> outputExpression)
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
					query.Expression, Expression.Quote(setter), ((IQueryable<TTarget>)outputTable).Expression,
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
		public static Task<int> InsertWithOutputIntoAsync<TTarget,TOutput>(
			[NotNull]                this ITable<TTarget>              target,
			[NotNull, InstantHandle] Expression<Func<TTarget>>         setter,
			[NotNull]                ITable<TOutput>                   outputTable,
			[NotNull]                Expression<Func<TTarget,TOutput>> outputExpression,
			                         CancellationToken                 token = default)
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
					query.Expression, Expression.Quote(setter), ((IQueryable<TTarget>)outputTable).Expression,
					Expression.Quote(outputExpression));

			if (query is IQueryProviderAsync queryAsync)
				return queryAsync.ExecuteAsync<int>(expr, token);

			return TaskEx.Run(() => query.Provider.Execute<int>(expr), token);
		}


		#region SelectInsertable

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
		public static IQueryable<TTarget> InsertWithOutput<TSource,TTarget>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull]                ITable<TTarget>                   target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter)
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (target           == null) throw new ArgumentNullException(nameof(target));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));

			return source.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutput, source, target, setter),
					source.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter)));
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
		public static IQueryable<TOutput> InsertWithOutput<TSource,TTarget,TOutput>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull]                ITable<TTarget>                   target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter,
			[NotNull]                Expression<Func<TTarget,TOutput>> outputExpression)
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (target           == null) throw new ArgumentNullException(nameof(target));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			return source.Provider.CreateQuery<TOutput>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutput, source, target, setter, outputExpression),
					source.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter), Expression.Quote(outputExpression)));
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
		public static int InsertWithOutputInto<TSource,TTarget>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull]                ITable<TTarget>                   target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter,
			[NotNull]                ITable<TTarget>                   outputTable
			)
		{
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (target      == null) throw new ArgumentNullException(nameof(target));
			if (setter      == null) throw new ArgumentNullException(nameof(setter));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			return source.Provider.Execute<int>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutputInto, source, target, setter, outputTable),
					source.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter), ((IQueryable<TTarget>)outputTable).Expression));
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
		public static Task<int> InsertWithOutputIntoAsync<TSource,TTarget>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull]                ITable<TTarget>                   target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter,
			[NotNull]                ITable<TTarget>                   outputTable,
			                         CancellationToken                 token = default)
		{
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (target      == null) throw new ArgumentNullException(nameof(target));
			if (setter      == null) throw new ArgumentNullException(nameof(setter));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var expr =
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutputInto, source, target, setter, outputTable),
					source.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter), ((IQueryable<TTarget>)outputTable).Expression);

			if (source is IQueryProviderAsync queryAsync)
				return queryAsync.ExecuteAsync<int>(expr, token);

			return TaskEx.Run(() => source.Provider.Execute<int>(expr), token);
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
		public static int InsertWithOutputInto<TSource,TTarget,TOutput>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull]                ITable<TTarget>                   target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter,
			[NotNull]                ITable<TOutput>                   outputTable,
			[NotNull]                Expression<Func<TTarget,TOutput>> outputExpression)
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (target           == null) throw new ArgumentNullException(nameof(target));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputTable      == null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			return source.Provider.Execute<int>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutputInto, source, target, setter, outputTable, outputExpression),
					source.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter),
					((IQueryable<TTarget>)outputTable).Expression, Expression.Quote(outputExpression)));
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
		public static Task<int> InsertWithOutputIntoAsync<TSource,TTarget,TOutput>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull]                ITable<TTarget>                   target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter,
			[NotNull]                ITable<TOutput>                   outputTable,
			[NotNull]                Expression<Func<TTarget,TOutput>> outputExpression,
			                         CancellationToken                 token = default)
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (target           == null) throw new ArgumentNullException(nameof(target));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputTable      == null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var expr =
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutputInto, source, target, setter, outputTable, outputExpression),
					source.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter),
					((IQueryable<TTarget>)outputTable).Expression, Expression.Quote(outputExpression));

			if (source is IQueryProviderAsync queryAsync)
				return queryAsync.ExecuteAsync<int>(expr, token);

			return TaskEx.Run(() => source.Provider.Execute<int>(expr), token);
		}

		/// <summary>
		/// Executes configured insert query and returns inserted record.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Inserted record.</returns>
		public static TTarget InsertWithOutput<TSource,TTarget>([NotNull] this ISelectInsertable<TSource,TTarget> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;

			return query.Provider.Execute<TTarget>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutput, source),
					query.Expression));
		}

		/// <summary>
		/// Executes configured insert query asynchronously and returns inserted record.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record.</returns>
		public static Task<TTarget> InsertWithOutputAsync<TSource,TTarget>(
			[NotNull] this ISelectInsertable<TSource,TTarget> source,
			               CancellationToken                  token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;

			var expr =
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(InsertWithOutput, source),
					query.Expression);

			if (query is IQueryProviderAsync queryAsync)
				return queryAsync.ExecuteAsync<TTarget>(expr, token);

			return TaskEx.Run(() => query.Provider.Execute<TTarget>(expr), token);
		}

		/// <summary>
		/// Executes configured insert query and returns inserted record.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="outputTable">Output table.</param>
		/// <returns>Number of affected records.</returns>
		public static int InsertWithOutputInto<TSource,TTarget>(
			[NotNull] this ISelectInsertable<TSource,TTarget> source,
			[NotNull]      ITable<TTarget>                    outputTable)
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
		public static Task<int> InsertWithOutputIntoAsync<TSource,TTarget>(
			[NotNull] this ISelectInsertable<TSource,TTarget> source,
			[NotNull]      ITable<TTarget>                    outputTable,
			               CancellationToken                  token = default)

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

			return TaskEx.Run(() => query.Provider.Execute<int>(expr), token);
		}

		#endregion
	}
}
