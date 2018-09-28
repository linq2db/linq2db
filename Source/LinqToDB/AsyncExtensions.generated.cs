using System;
using System.Linq.Expressions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB
{
	using Async;
	using Linq;

	public static partial class AsyncExtensions
	{
		#region FirstAsync<TSource>

		public static Task<TSource> FirstAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, TSource>(Queryable.First), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.FirstAsync<TSource>(source, token);

			return GetTask(source.First, token);
		}

		#endregion

		#region FirstAsync<TSource, predicate>

		public static Task<TSource> FirstAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>, TSource>(Queryable.First), source, predicate),
						arguments: new Expression[2] { source.Expression, Expression.Quote(predicate) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.FirstAsync<TSource>(source, predicate, token);

			return GetTask(() => source.First(predicate), token);
		}

		#endregion

		#region FirstOrDefaultAsync<TSource>

		public static Task<TSource> FirstOrDefaultAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, TSource>(Queryable.FirstOrDefault), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.FirstOrDefaultAsync<TSource>(source, token);

			return GetTask(source.FirstOrDefault, token);
		}

		#endregion

		#region FirstOrDefaultAsync<TSource, predicate>

		public static Task<TSource> FirstOrDefaultAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>, TSource>(Queryable.FirstOrDefault), source, predicate),
						arguments: new Expression[2] { source.Expression, Expression.Quote(predicate) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.FirstOrDefaultAsync<TSource>(source, predicate, token);

			return GetTask(() => source.FirstOrDefault(predicate), token);
		}

		#endregion

		#region SingleAsync<TSource>

		public static Task<TSource> SingleAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, TSource>(Queryable.Single), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SingleAsync<TSource>(source, token);

			return GetTask(source.Single, token);
		}

		#endregion

		#region SingleAsync<TSource, predicate>

		public static Task<TSource> SingleAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>, TSource>(Queryable.Single), source, predicate),
						arguments: new Expression[2] { source.Expression, Expression.Quote(predicate) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SingleAsync<TSource>(source, predicate, token);

			return GetTask(() => source.Single(predicate), token);
		}

		#endregion

		#region SingleOrDefaultAsync<TSource>

		public static Task<TSource> SingleOrDefaultAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, TSource>(Queryable.SingleOrDefault), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SingleOrDefaultAsync<TSource>(source, token);

			return GetTask(source.SingleOrDefault, token);
		}

		#endregion

		#region SingleOrDefaultAsync<TSource, predicate>

		public static Task<TSource> SingleOrDefaultAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>, TSource>(Queryable.SingleOrDefault), source, predicate),
						arguments: new Expression[2] { source.Expression, Expression.Quote(predicate) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SingleOrDefaultAsync<TSource>(source, predicate, token);

			return GetTask(() => source.SingleOrDefault(predicate), token);
		}

		#endregion

		#region ContainsAsync<TSource, item>

		public static Task<bool> ContainsAsync<TSource>(
			this IQueryable<TSource> source, TSource item,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<bool>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, TSource, bool>(Queryable.Contains), source, item),
						arguments: new Expression[2] { source.Expression, (Expression)Expression.Constant((object)item, typeof (TSource)) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.ContainsAsync<TSource>(source, item, token);

			return GetTask(() => source.Contains(item), token);
		}

		#endregion

		#region AnyAsync<TSource>

		public static Task<bool> AnyAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<bool>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, bool>(Queryable.Any), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AnyAsync<TSource>(source, token);

			return GetTask(source.Any, token);
		}

		#endregion

		#region AnyAsync<TSource, predicate>

		public static Task<bool> AnyAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<bool>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>, bool>(Queryable.Any), source, predicate),
						arguments: new Expression[2] { source.Expression, Expression.Quote(predicate) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AnyAsync<TSource>(source, predicate, token);

			return GetTask(() => source.Any(predicate), token);
		}

		#endregion

		#region AllAsync<TSource, predicate>

		public static Task<bool> AllAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<bool>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>, bool>(Queryable.All), source, predicate),
						arguments: new Expression[2] { source.Expression, Expression.Quote(predicate) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AllAsync<TSource>(source, predicate, token);

			return GetTask(() => source.All(predicate), token);
		}

		#endregion

		#region CountAsync<TSource>

		public static Task<int> CountAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<int>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, int>(Queryable.Count), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.CountAsync<TSource>(source, token);

			return GetTask(source.Count, token);
		}

		#endregion

		#region CountAsync<TSource, predicate>

		public static Task<int> CountAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<int>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>, int>(Queryable.Count), source, predicate),
						arguments: new Expression[2] { source.Expression, Expression.Quote(predicate) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.CountAsync<TSource>(source, predicate, token);

			return GetTask(() => source.Count(predicate), token);
		}

		#endregion

		#region LongCountAsync<TSource>

		public static Task<long> LongCountAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<long>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, long>(Queryable.LongCount), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.LongCountAsync<TSource>(source, token);

			return GetTask(source.LongCount, token);
		}

		#endregion

		#region LongCountAsync<TSource, predicate>

		public static Task<long> LongCountAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<long>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>, long>(Queryable.LongCount), source, predicate),
						arguments: new Expression[2] { source.Expression, Expression.Quote(predicate) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.LongCountAsync<TSource>(source, predicate, token);

			return GetTask(() => source.LongCount(predicate), token);
		}

		#endregion

		#region MinAsync<TSource>

		public static Task<TSource> MinAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, TSource>(Queryable.Min), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.MinAsync<TSource>(source, token);

			return GetTask(source.Min, token);
		}

		#endregion

		#region MinAsync<TSource, selector>

		public static Task<TResult> MinAsync<TSource,TResult>(
			this IQueryable<TSource> source, Expression<Func<TSource,TResult>> selector,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TResult>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,TResult>>, TResult>(Queryable.Min), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.MinAsync<TSource,TResult>(source, selector, token);

			return GetTask(() => source.Min(selector), token);
		}

		#endregion

		#region MaxAsync<TSource>

		public static Task<TSource> MaxAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, TSource>(Queryable.Max), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.MaxAsync<TSource>(source, token);

			return GetTask(source.Max, token);
		}

		#endregion

		#region MaxAsync<TSource, selector>

		public static Task<TResult> MaxAsync<TSource,TResult>(
			this IQueryable<TSource> source, Expression<Func<TSource,TResult>> selector,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TResult>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,TResult>>, TResult>(Queryable.Max), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.MaxAsync<TSource,TResult>(source, selector, token);

			return GetTask(() => source.Max(selector), token);
		}

		#endregion

		#region SumAsync<int>

		public static Task<int> SumAsync(
			this IQueryable<int> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<int>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<int>, int>(Queryable.Sum), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync(source, token);

			return GetTask(source.Sum, token);
		}

		#endregion

		#region SumAsync<int?>

		public static Task<int?> SumAsync(
			this IQueryable<int?> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<int?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<int?>, int?>(Queryable.Sum), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync(source, token);

			return GetTask(source.Sum, token);
		}

		#endregion

		#region SumAsync<long>

		public static Task<long> SumAsync(
			this IQueryable<long> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<long>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<long>, long>(Queryable.Sum), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync(source, token);

			return GetTask(source.Sum, token);
		}

		#endregion

		#region SumAsync<long?>

		public static Task<long?> SumAsync(
			this IQueryable<long?> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<long?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<long?>, long?>(Queryable.Sum), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync(source, token);

			return GetTask(source.Sum, token);
		}

		#endregion

		#region SumAsync<float>

		public static Task<float> SumAsync(
			this IQueryable<float> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<float>, float>(Queryable.Sum), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync(source, token);

			return GetTask(source.Sum, token);
		}

		#endregion

		#region SumAsync<float?>

		public static Task<float?> SumAsync(
			this IQueryable<float?> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<float?>, float?>(Queryable.Sum), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync(source, token);

			return GetTask(source.Sum, token);
		}

		#endregion

		#region SumAsync<double>

		public static Task<double> SumAsync(
			this IQueryable<double> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<double>, double>(Queryable.Sum), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync(source, token);

			return GetTask(source.Sum, token);
		}

		#endregion

		#region SumAsync<double?>

		public static Task<double?> SumAsync(
			this IQueryable<double?> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<double?>, double?>(Queryable.Sum), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync(source, token);

			return GetTask(source.Sum, token);
		}

		#endregion

		#region SumAsync<decimal>

		public static Task<decimal> SumAsync(
			this IQueryable<decimal> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<decimal>, decimal>(Queryable.Sum), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync(source, token);

			return GetTask(source.Sum, token);
		}

		#endregion

		#region SumAsync<decimal?>

		public static Task<decimal?> SumAsync(
			this IQueryable<decimal?> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<decimal?>, decimal?>(Queryable.Sum), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync(source, token);

			return GetTask(source.Sum, token);
		}

		#endregion

		#region SumAsync<int, selector>

		public static Task<int> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int>> selector,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<int>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,int>>, int>(Queryable.Sum), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync<TSource>(source, selector, token);

			return GetTask(() => source.Sum(selector), token);
		}

		#endregion

		#region SumAsync<int?, selector>

		public static Task<int?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int?>> selector,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<int?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,int?>>, int?>(Queryable.Sum), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync<TSource>(source, selector, token);

			return GetTask(() => source.Sum(selector), token);
		}

		#endregion

		#region SumAsync<long, selector>

		public static Task<long> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long>> selector,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<long>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,long>>, long>(Queryable.Sum), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync<TSource>(source, selector, token);

			return GetTask(() => source.Sum(selector), token);
		}

		#endregion

		#region SumAsync<long?, selector>

		public static Task<long?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long?>> selector,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<long?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,long?>>, long?>(Queryable.Sum), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync<TSource>(source, selector, token);

			return GetTask(() => source.Sum(selector), token);
		}

		#endregion

		#region SumAsync<float, selector>

		public static Task<float> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float>> selector,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,float>>, float>(Queryable.Sum), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync<TSource>(source, selector, token);

			return GetTask(() => source.Sum(selector), token);
		}

		#endregion

		#region SumAsync<float?, selector>

		public static Task<float?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float?>> selector,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,float?>>, float?>(Queryable.Sum), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync<TSource>(source, selector, token);

			return GetTask(() => source.Sum(selector), token);
		}

		#endregion

		#region SumAsync<double, selector>

		public static Task<double> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double>> selector,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,double>>, double>(Queryable.Sum), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync<TSource>(source, selector, token);

			return GetTask(() => source.Sum(selector), token);
		}

		#endregion

		#region SumAsync<double?, selector>

		public static Task<double?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double?>> selector,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,double?>>, double?>(Queryable.Sum), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync<TSource>(source, selector, token);

			return GetTask(() => source.Sum(selector), token);
		}

		#endregion

		#region SumAsync<decimal, selector>

		public static Task<decimal> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal>> selector,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,decimal>>, decimal>(Queryable.Sum), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync<TSource>(source, selector, token);

			return GetTask(() => source.Sum(selector), token);
		}

		#endregion

		#region SumAsync<decimal?, selector>

		public static Task<decimal?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal?>> selector,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,decimal?>>, decimal?>(Queryable.Sum), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync<TSource>(source, selector, token);

			return GetTask(() => source.Sum(selector), token);
		}

		#endregion

		#region AverageAsync<int>

		public static Task<double> AverageAsync(
			this IQueryable<int> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<int>, double>(Queryable.Average), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync(source, token);

			return GetTask(source.Average, token);
		}

		#endregion

		#region AverageAsync<int?>

		public static Task<double?> AverageAsync(
			this IQueryable<int?> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<int?>, double?>(Queryable.Average), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync(source, token);

			return GetTask(source.Average, token);
		}

		#endregion

		#region AverageAsync<long>

		public static Task<double> AverageAsync(
			this IQueryable<long> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<long>, double>(Queryable.Average), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync(source, token);

			return GetTask(source.Average, token);
		}

		#endregion

		#region AverageAsync<long?>

		public static Task<double?> AverageAsync(
			this IQueryable<long?> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<long?>, double?>(Queryable.Average), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync(source, token);

			return GetTask(source.Average, token);
		}

		#endregion

		#region AverageAsync<float>

		public static Task<float> AverageAsync(
			this IQueryable<float> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<float>, float>(Queryable.Average), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync(source, token);

			return GetTask(source.Average, token);
		}

		#endregion

		#region AverageAsync<float?>

		public static Task<float?> AverageAsync(
			this IQueryable<float?> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<float?>, float?>(Queryable.Average), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync(source, token);

			return GetTask(source.Average, token);
		}

		#endregion

		#region AverageAsync<double>

		public static Task<double> AverageAsync(
			this IQueryable<double> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<double>, double>(Queryable.Average), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync(source, token);

			return GetTask(source.Average, token);
		}

		#endregion

		#region AverageAsync<double?>

		public static Task<double?> AverageAsync(
			this IQueryable<double?> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<double?>, double?>(Queryable.Average), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync(source, token);

			return GetTask(source.Average, token);
		}

		#endregion

		#region AverageAsync<decimal>

		public static Task<decimal> AverageAsync(
			this IQueryable<decimal> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<decimal>, decimal>(Queryable.Average), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync(source, token);

			return GetTask(source.Average, token);
		}

		#endregion

		#region AverageAsync<decimal?>

		public static Task<decimal?> AverageAsync(
			this IQueryable<decimal?> source,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<decimal?>, decimal?>(Queryable.Average), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync(source, token);

			return GetTask(source.Average, token);
		}

		#endregion

		#region AverageAsync<int, selector>

		public static Task<double> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int>> selector,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,int>>, double>(Queryable.Average), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync<TSource>(source, selector, token);

			return GetTask(() => source.Average(selector), token);
		}

		#endregion

		#region AverageAsync<int?, selector>

		public static Task<double?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int?>> selector,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,int?>>, double?>(Queryable.Average), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync<TSource>(source, selector, token);

			return GetTask(() => source.Average(selector), token);
		}

		#endregion

		#region AverageAsync<long, selector>

		public static Task<double> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long>> selector,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,long>>, double>(Queryable.Average), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync<TSource>(source, selector, token);

			return GetTask(() => source.Average(selector), token);
		}

		#endregion

		#region AverageAsync<long?, selector>

		public static Task<double?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long?>> selector,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,long?>>, double?>(Queryable.Average), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync<TSource>(source, selector, token);

			return GetTask(() => source.Average(selector), token);
		}

		#endregion

		#region AverageAsync<float, selector>

		public static Task<float> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float>> selector,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,float>>, float>(Queryable.Average), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync<TSource>(source, selector, token);

			return GetTask(() => source.Average(selector), token);
		}

		#endregion

		#region AverageAsync<float?, selector>

		public static Task<float?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float?>> selector,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,float?>>, float?>(Queryable.Average), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync<TSource>(source, selector, token);

			return GetTask(() => source.Average(selector), token);
		}

		#endregion

		#region AverageAsync<double, selector>

		public static Task<double> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double>> selector,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,double>>, double>(Queryable.Average), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync<TSource>(source, selector, token);

			return GetTask(() => source.Average(selector), token);
		}

		#endregion

		#region AverageAsync<double?, selector>

		public static Task<double?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double?>> selector,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,double?>>, double?>(Queryable.Average), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync<TSource>(source, selector, token);

			return GetTask(() => source.Average(selector), token);
		}

		#endregion

		#region AverageAsync<decimal, selector>

		public static Task<decimal> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal>> selector,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,decimal>>, decimal>(Queryable.Average), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync<TSource>(source, selector, token);

			return GetTask(() => source.Average(selector), token);
		}

		#endregion

		#region AverageAsync<decimal?, selector>

		public static Task<decimal?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal?>> selector,
			CancellationToken token = default(CancellationToken))
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,decimal?>>, decimal?>(Queryable.Average), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync<TSource>(source, selector, token);

			return GetTask(() => source.Average(selector), token);
		}

		#endregion

	}
}
