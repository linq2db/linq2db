#nullable enable
using System;
using System.Linq.Expressions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.Linq;
using LinqToDB.Internal.Async;

namespace LinqToDB
{
	public static partial class AsyncExtensions
	{
		#region FirstAsync<TSource>

		[ElementAsync]
		public static Task<TSource> FirstAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>,TSource>(Queryable.First), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.FirstAsync<TSource>(source, token);

			return Task.Run<TSource>(source.First, token);
		}

		#endregion

		#region FirstAsync<TSource, predicate>

		[ElementAsync]
		public static Task<TSource> FirstAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>,TSource>(Queryable.First), source, predicate),
						source.Expression, Expression.Quote(predicate)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.FirstAsync<TSource>(source, predicate, token);

			return Task.Run<TSource>(() => source.First(predicate), token);
		}

		#endregion

		#region FirstOrDefaultAsync<TSource>

		[ElementAsync]
		public static Task<TSource?> FirstOrDefaultAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>,TSource?>(Queryable.FirstOrDefault), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.FirstOrDefaultAsync<TSource>(source, token);

			return Task.Run<TSource?>(source.FirstOrDefault, token);
		}

		#endregion

		#region FirstOrDefaultAsync<TSource, predicate>

		[ElementAsync]
		public static Task<TSource?> FirstOrDefaultAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>,TSource?>(Queryable.FirstOrDefault), source, predicate),
						source.Expression, Expression.Quote(predicate)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.FirstOrDefaultAsync<TSource>(source, predicate, token);

			return Task.Run<TSource?>(() => (TSource?)source.FirstOrDefault(predicate), token);
		}

		#endregion

		#region SingleAsync<TSource>

		[ElementAsync]
		public static Task<TSource> SingleAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>,TSource>(Queryable.Single), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SingleAsync<TSource>(source, token);

			return Task.Run<TSource>(source.Single, token);
		}

		#endregion

		#region SingleAsync<TSource, predicate>

		[ElementAsync]
		public static Task<TSource> SingleAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>,TSource>(Queryable.Single), source, predicate),
						source.Expression, Expression.Quote(predicate)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SingleAsync<TSource>(source, predicate, token);

			return Task.Run<TSource>(() => source.Single(predicate), token);
		}

		#endregion

		#region SingleOrDefaultAsync<TSource>

		[ElementAsync]
		public static Task<TSource?> SingleOrDefaultAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>,TSource?>(Queryable.SingleOrDefault), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SingleOrDefaultAsync<TSource>(source, token);

			return Task.Run<TSource?>(source.SingleOrDefault, token);
		}

		#endregion

		#region SingleOrDefaultAsync<TSource, predicate>

		[ElementAsync]
		public static Task<TSource?> SingleOrDefaultAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>,TSource?>(Queryable.SingleOrDefault), source, predicate),
						source.Expression, Expression.Quote(predicate)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SingleOrDefaultAsync<TSource>(source, predicate, token);

			return Task.Run<TSource?>(() => (TSource?)source.SingleOrDefault(predicate), token);
		}

		#endregion

		#region ContainsAsync<TSource, item>

		[ElementAsync]
		public static Task<bool> ContainsAsync<TSource>(
			this IQueryable<TSource> source, TSource item,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<bool>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, TSource,bool>(Queryable.Contains), source, item),
						source.Expression, (Expression)Expression.Constant((object?)item, typeof (TSource))) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.ContainsAsync<TSource>(source, item, token);

			return Task.Run<bool>(() => source.Contains(item), token);
		}

		#endregion

		#region AnyAsync<TSource>

		[ElementAsync]
		public static Task<bool> AnyAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<bool>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>,bool>(Queryable.Any), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AnyAsync<TSource>(source, token);

			return Task.Run<bool>(source.Any, token);
		}

		#endregion

		#region AnyAsync<TSource, predicate>

		[ElementAsync]
		public static Task<bool> AnyAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<bool>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>,bool>(Queryable.Any), source, predicate),
						source.Expression, Expression.Quote(predicate)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AnyAsync<TSource>(source, predicate, token);

			return Task.Run<bool>(() => source.Any(predicate), token);
		}

		#endregion

		#region AllAsync<TSource, predicate>

		[ElementAsync]
		public static Task<bool> AllAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<bool>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>,bool>(Queryable.All), source, predicate),
						source.Expression, Expression.Quote(predicate)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AllAsync<TSource>(source, predicate, token);

			return Task.Run<bool>(() => source.All(predicate), token);
		}

		#endregion

		#region CountAsync<TSource>

		[ElementAsync]
		public static Task<int> CountAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<int>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>,int>(Queryable.Count), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.CountAsync<TSource>(source, token);

			return Task.Run<int>(source.Count, token);
		}

		#endregion

		#region CountAsync<TSource, predicate>

		[ElementAsync]
		public static Task<int> CountAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<int>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>,int>(Queryable.Count), source, predicate),
						source.Expression, Expression.Quote(predicate)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.CountAsync<TSource>(source, predicate, token);

			return Task.Run<int>(() => source.Count(predicate), token);
		}

		#endregion

		#region LongCountAsync<TSource>

		[ElementAsync]
		public static Task<long> LongCountAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<long>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>,long>(Queryable.LongCount), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.LongCountAsync<TSource>(source, token);

			return Task.Run<long>(source.LongCount, token);
		}

		#endregion

		#region LongCountAsync<TSource, predicate>

		[ElementAsync]
		public static Task<long> LongCountAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<long>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>,long>(Queryable.LongCount), source, predicate),
						source.Expression, Expression.Quote(predicate)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.LongCountAsync<TSource>(source, predicate, token);

			return Task.Run<long>(() => source.LongCount(predicate), token);
		}

		#endregion

		#region MinAsync<TSource>

		[ElementAsync]
		public static Task<TSource?> MinAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>,TSource?>(Queryable.Min), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.MinAsync<TSource>(source, token);

			return Task.Run<TSource?>(source.Min, token);
		}

		#endregion

		#region MinAsync<TSource, selector>

		[ElementAsync]
		public static Task<TResult?> MinAsync<TSource,TResult>(
			this IQueryable<TSource> source, Expression<Func<TSource,TResult>> selector,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TResult?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,TResult>>,TResult?>(Queryable.Min), source, selector),
						source.Expression, Expression.Quote(selector)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.MinAsync<TSource,TResult>(source, selector, token);

			return Task.Run<TResult?>(() => source.Min(selector), token);
		}

		#endregion

		#region MaxAsync<TSource>

		[ElementAsync]
		public static Task<TSource?> MaxAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>,TSource?>(Queryable.Max), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.MaxAsync<TSource>(source, token);

			return Task.Run<TSource?>(source.Max, token);
		}

		#endregion

		#region MaxAsync<TSource, selector>

		[ElementAsync]
		public static Task<TResult?> MaxAsync<TSource,TResult>(
			this IQueryable<TSource> source, Expression<Func<TSource,TResult>> selector,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TResult?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,TResult>>,TResult?>(Queryable.Max), source, selector),
						source.Expression, Expression.Quote(selector)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.MaxAsync<TSource,TResult>(source, selector, token);

			return Task.Run<TResult?>(() => source.Max(selector), token);
		}

		#endregion

		#region SumAsync<int>

		[ElementAsync]
		public static Task<int> SumAsync(
			this IQueryable<int> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<int>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<int>,int>(Queryable.Sum), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync(source, token);

			return Task.Run<int>(source.Sum, token);
		}

		#endregion

		#region SumAsync<int?>

		[ElementAsync]
		public static Task<int?> SumAsync(
			this IQueryable<int?> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<int?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<int?>,int?>(Queryable.Sum), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync(source, token);

			return Task.Run<int?>(source.Sum, token);
		}

		#endregion

		#region SumAsync<long>

		[ElementAsync]
		public static Task<long> SumAsync(
			this IQueryable<long> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<long>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<long>,long>(Queryable.Sum), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync(source, token);

			return Task.Run<long>(source.Sum, token);
		}

		#endregion

		#region SumAsync<long?>

		[ElementAsync]
		public static Task<long?> SumAsync(
			this IQueryable<long?> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<long?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<long?>,long?>(Queryable.Sum), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync(source, token);

			return Task.Run<long?>(source.Sum, token);
		}

		#endregion

		#region SumAsync<float>

		[ElementAsync]
		public static Task<float> SumAsync(
			this IQueryable<float> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<float>,float>(Queryable.Sum), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync(source, token);

			return Task.Run<float>(source.Sum, token);
		}

		#endregion

		#region SumAsync<float?>

		[ElementAsync]
		public static Task<float?> SumAsync(
			this IQueryable<float?> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<float?>,float?>(Queryable.Sum), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync(source, token);

			return Task.Run<float?>(source.Sum, token);
		}

		#endregion

		#region SumAsync<double>

		[ElementAsync]
		public static Task<double> SumAsync(
			this IQueryable<double> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<double>,double>(Queryable.Sum), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync(source, token);

			return Task.Run<double>(source.Sum, token);
		}

		#endregion

		#region SumAsync<double?>

		[ElementAsync]
		public static Task<double?> SumAsync(
			this IQueryable<double?> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<double?>,double?>(Queryable.Sum), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync(source, token);

			return Task.Run<double?>(source.Sum, token);
		}

		#endregion

		#region SumAsync<decimal>

		[ElementAsync]
		public static Task<decimal> SumAsync(
			this IQueryable<decimal> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<decimal>,decimal>(Queryable.Sum), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync(source, token);

			return Task.Run<decimal>(source.Sum, token);
		}

		#endregion

		#region SumAsync<decimal?>

		[ElementAsync]
		public static Task<decimal?> SumAsync(
			this IQueryable<decimal?> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<decimal?>,decimal?>(Queryable.Sum), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync(source, token);

			return Task.Run<decimal?>(source.Sum, token);
		}

		#endregion

		#region SumAsync<int, selector>

		[ElementAsync]
		public static Task<int> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int>> selector,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<int>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,int>>,int>(Queryable.Sum), source, selector),
						source.Expression, Expression.Quote(selector)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync<TSource>(source, selector, token);

			return Task.Run<int>(() => source.Sum(selector), token);
		}

		#endregion

		#region SumAsync<int?, selector>

		[ElementAsync]
		public static Task<int?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int?>> selector,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<int?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,int?>>,int?>(Queryable.Sum), source, selector),
						source.Expression, Expression.Quote(selector)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync<TSource>(source, selector, token);

			return Task.Run<int?>(() => source.Sum(selector), token);
		}

		#endregion

		#region SumAsync<long, selector>

		[ElementAsync]
		public static Task<long> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long>> selector,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<long>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,long>>,long>(Queryable.Sum), source, selector),
						source.Expression, Expression.Quote(selector)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync<TSource>(source, selector, token);

			return Task.Run<long>(() => source.Sum(selector), token);
		}

		#endregion

		#region SumAsync<long?, selector>

		[ElementAsync]
		public static Task<long?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long?>> selector,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<long?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,long?>>,long?>(Queryable.Sum), source, selector),
						source.Expression, Expression.Quote(selector)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync<TSource>(source, selector, token);

			return Task.Run<long?>(() => source.Sum(selector), token);
		}

		#endregion

		#region SumAsync<float, selector>

		[ElementAsync]
		public static Task<float> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float>> selector,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,float>>,float>(Queryable.Sum), source, selector),
						source.Expression, Expression.Quote(selector)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync<TSource>(source, selector, token);

			return Task.Run<float>(() => source.Sum(selector), token);
		}

		#endregion

		#region SumAsync<float?, selector>

		[ElementAsync]
		public static Task<float?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float?>> selector,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,float?>>,float?>(Queryable.Sum), source, selector),
						source.Expression, Expression.Quote(selector)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync<TSource>(source, selector, token);

			return Task.Run<float?>(() => source.Sum(selector), token);
		}

		#endregion

		#region SumAsync<double, selector>

		[ElementAsync]
		public static Task<double> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double>> selector,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,double>>,double>(Queryable.Sum), source, selector),
						source.Expression, Expression.Quote(selector)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync<TSource>(source, selector, token);

			return Task.Run<double>(() => source.Sum(selector), token);
		}

		#endregion

		#region SumAsync<double?, selector>

		[ElementAsync]
		public static Task<double?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double?>> selector,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,double?>>,double?>(Queryable.Sum), source, selector),
						source.Expression, Expression.Quote(selector)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync<TSource>(source, selector, token);

			return Task.Run<double?>(() => source.Sum(selector), token);
		}

		#endregion

		#region SumAsync<decimal, selector>

		[ElementAsync]
		public static Task<decimal> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal>> selector,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,decimal>>,decimal>(Queryable.Sum), source, selector),
						source.Expression, Expression.Quote(selector)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync<TSource>(source, selector, token);

			return Task.Run<decimal>(() => source.Sum(selector), token);
		}

		#endregion

		#region SumAsync<decimal?, selector>

		[ElementAsync]
		public static Task<decimal?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal?>> selector,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,decimal?>>,decimal?>(Queryable.Sum), source, selector),
						source.Expression, Expression.Quote(selector)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.SumAsync<TSource>(source, selector, token);

			return Task.Run<decimal?>(() => source.Sum(selector), token);
		}

		#endregion

		#region AverageAsync<int>

		[ElementAsync]
		public static Task<double> AverageAsync(
			this IQueryable<int> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<int>,double>(Queryable.Average), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync(source, token);

			return Task.Run<double>(source.Average, token);
		}

		#endregion

		#region AverageAsync<int?>

		[ElementAsync]
		public static Task<double?> AverageAsync(
			this IQueryable<int?> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<int?>,double?>(Queryable.Average), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync(source, token);

			return Task.Run<double?>(source.Average, token);
		}

		#endregion

		#region AverageAsync<long>

		[ElementAsync]
		public static Task<double> AverageAsync(
			this IQueryable<long> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<long>,double>(Queryable.Average), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync(source, token);

			return Task.Run<double>(source.Average, token);
		}

		#endregion

		#region AverageAsync<long?>

		[ElementAsync]
		public static Task<double?> AverageAsync(
			this IQueryable<long?> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<long?>,double?>(Queryable.Average), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync(source, token);

			return Task.Run<double?>(source.Average, token);
		}

		#endregion

		#region AverageAsync<float>

		[ElementAsync]
		public static Task<float> AverageAsync(
			this IQueryable<float> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<float>,float>(Queryable.Average), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync(source, token);

			return Task.Run<float>(source.Average, token);
		}

		#endregion

		#region AverageAsync<float?>

		[ElementAsync]
		public static Task<float?> AverageAsync(
			this IQueryable<float?> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<float?>,float?>(Queryable.Average), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync(source, token);

			return Task.Run<float?>(source.Average, token);
		}

		#endregion

		#region AverageAsync<double>

		[ElementAsync]
		public static Task<double> AverageAsync(
			this IQueryable<double> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<double>,double>(Queryable.Average), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync(source, token);

			return Task.Run<double>(source.Average, token);
		}

		#endregion

		#region AverageAsync<double?>

		[ElementAsync]
		public static Task<double?> AverageAsync(
			this IQueryable<double?> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<double?>,double?>(Queryable.Average), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync(source, token);

			return Task.Run<double?>(source.Average, token);
		}

		#endregion

		#region AverageAsync<decimal>

		[ElementAsync]
		public static Task<decimal> AverageAsync(
			this IQueryable<decimal> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<decimal>,decimal>(Queryable.Average), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync(source, token);

			return Task.Run<decimal>(source.Average, token);
		}

		#endregion

		#region AverageAsync<decimal?>

		[ElementAsync]
		public static Task<decimal?> AverageAsync(
			this IQueryable<decimal?> source,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<decimal?>,decimal?>(Queryable.Average), source),
						source.Expression) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync(source, token);

			return Task.Run<decimal?>(source.Average, token);
		}

		#endregion

		#region AverageAsync<int, selector>

		[ElementAsync]
		public static Task<double> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int>> selector,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,int>>,double>(Queryable.Average), source, selector),
						source.Expression, Expression.Quote(selector)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync<TSource>(source, selector, token);

			return Task.Run<double>(() => source.Average(selector), token);
		}

		#endregion

		#region AverageAsync<int?, selector>

		[ElementAsync]
		public static Task<double?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int?>> selector,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,int?>>,double?>(Queryable.Average), source, selector),
						source.Expression, Expression.Quote(selector)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync<TSource>(source, selector, token);

			return Task.Run<double?>(() => source.Average(selector), token);
		}

		#endregion

		#region AverageAsync<long, selector>

		[ElementAsync]
		public static Task<double> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long>> selector,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,long>>,double>(Queryable.Average), source, selector),
						source.Expression, Expression.Quote(selector)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync<TSource>(source, selector, token);

			return Task.Run<double>(() => source.Average(selector), token);
		}

		#endregion

		#region AverageAsync<long?, selector>

		[ElementAsync]
		public static Task<double?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long?>> selector,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,long?>>,double?>(Queryable.Average), source, selector),
						source.Expression, Expression.Quote(selector)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync<TSource>(source, selector, token);

			return Task.Run<double?>(() => source.Average(selector), token);
		}

		#endregion

		#region AverageAsync<float, selector>

		[ElementAsync]
		public static Task<float> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float>> selector,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,float>>,float>(Queryable.Average), source, selector),
						source.Expression, Expression.Quote(selector)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync<TSource>(source, selector, token);

			return Task.Run<float>(() => source.Average(selector), token);
		}

		#endregion

		#region AverageAsync<float?, selector>

		[ElementAsync]
		public static Task<float?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float?>> selector,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,float?>>,float?>(Queryable.Average), source, selector),
						source.Expression, Expression.Quote(selector)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync<TSource>(source, selector, token);

			return Task.Run<float?>(() => source.Average(selector), token);
		}

		#endregion

		#region AverageAsync<double, selector>

		[ElementAsync]
		public static Task<double> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double>> selector,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,double>>,double>(Queryable.Average), source, selector),
						source.Expression, Expression.Quote(selector)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync<TSource>(source, selector, token);

			return Task.Run<double>(() => source.Average(selector), token);
		}

		#endregion

		#region AverageAsync<double?, selector>

		[ElementAsync]
		public static Task<double?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double?>> selector,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,double?>>,double?>(Queryable.Average), source, selector),
						source.Expression, Expression.Quote(selector)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync<TSource>(source, selector, token);

			return Task.Run<double?>(() => source.Average(selector), token);
		}

		#endregion

		#region AverageAsync<decimal, selector>

		[ElementAsync]
		public static Task<decimal> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal>> selector,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,decimal>>,decimal>(Queryable.Average), source, selector),
						source.Expression, Expression.Quote(selector)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync<TSource>(source, selector, token);

			return Task.Run<decimal>(() => source.Average(selector), token);
		}

		#endregion

		#region AverageAsync<decimal?, selector>

		[ElementAsync]
		public static Task<decimal?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal?>> selector,
			CancellationToken token = default)
		{
			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,decimal?>>,decimal?>(Queryable.Average), source, selector),
						source.Expression, Expression.Quote(selector)) as Expression,
					token);
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AverageAsync<TSource>(source, selector, token);

			return Task.Run<decimal?>(() => source.Average(selector), token);
		}

		#endregion

	}
}
