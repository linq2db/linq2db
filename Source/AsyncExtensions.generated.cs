using System;
using System.Linq.Expressions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB
{
	using Linq;

	public static partial class AsyncExtensions
	{
		#region FirstAsync<TSource>

		public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source)
		{
			return FirstAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, CancellationToken token)
		{
			return FirstAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, TaskCreationOptions options)
		{
			return FirstAsync(source, CancellationToken.None, options);
		}

		public static Task<TSource> FirstAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, TSource>(Queryable.First), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.First, token, options);
		}

		#endregion

		#region FirstAsync<TSource, predicate>

		public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate)
		{
			return FirstAsync(source, predicate, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate, CancellationToken token)
		{
			return FirstAsync(source, predicate, token, TaskCreationOptions.None);
		}

		public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate, TaskCreationOptions options)
		{
			return FirstAsync(source, predicate, CancellationToken.None, options);
		}

		public static Task<TSource> FirstAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>, TSource>(Queryable.First), source, predicate),
						arguments: new Expression[2] { source.Expression, Expression.Quote(predicate) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.First(predicate), token, options);
		}

		#endregion

		#region FirstOrDefaultAsync<TSource>

		public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source)
		{
			return FirstOrDefaultAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken token)
		{
			return FirstOrDefaultAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, TaskCreationOptions options)
		{
			return FirstOrDefaultAsync(source, CancellationToken.None, options);
		}

		public static Task<TSource> FirstOrDefaultAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, TSource>(Queryable.FirstOrDefault), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.FirstOrDefault, token, options);
		}

		#endregion

		#region FirstOrDefaultAsync<TSource, predicate>

		public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate)
		{
			return FirstOrDefaultAsync(source, predicate, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate, CancellationToken token)
		{
			return FirstOrDefaultAsync(source, predicate, token, TaskCreationOptions.None);
		}

		public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate, TaskCreationOptions options)
		{
			return FirstOrDefaultAsync(source, predicate, CancellationToken.None, options);
		}

		public static Task<TSource> FirstOrDefaultAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>, TSource>(Queryable.FirstOrDefault), source, predicate),
						arguments: new Expression[2] { source.Expression, Expression.Quote(predicate) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.FirstOrDefault(predicate), token, options);
		}

		#endregion

		#region SingleAsync<TSource>

		public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source)
		{
			return SingleAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, CancellationToken token)
		{
			return SingleAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, TaskCreationOptions options)
		{
			return SingleAsync(source, CancellationToken.None, options);
		}

		public static Task<TSource> SingleAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, TSource>(Queryable.Single), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Single, token, options);
		}

		#endregion

		#region SingleAsync<TSource, predicate>

		public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate)
		{
			return SingleAsync(source, predicate, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate, CancellationToken token)
		{
			return SingleAsync(source, predicate, token, TaskCreationOptions.None);
		}

		public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate, TaskCreationOptions options)
		{
			return SingleAsync(source, predicate, CancellationToken.None, options);
		}

		public static Task<TSource> SingleAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>, TSource>(Queryable.Single), source, predicate),
						arguments: new Expression[2] { source.Expression, Expression.Quote(predicate) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Single(predicate), token, options);
		}

		#endregion

		#region SingleOrDefaultAsync<TSource>

		public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source)
		{
			return SingleOrDefaultAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken token)
		{
			return SingleOrDefaultAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, TaskCreationOptions options)
		{
			return SingleOrDefaultAsync(source, CancellationToken.None, options);
		}

		public static Task<TSource> SingleOrDefaultAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, TSource>(Queryable.SingleOrDefault), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.SingleOrDefault, token, options);
		}

		#endregion

		#region SingleOrDefaultAsync<TSource, predicate>

		public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate)
		{
			return SingleOrDefaultAsync(source, predicate, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate, CancellationToken token)
		{
			return SingleOrDefaultAsync(source, predicate, token, TaskCreationOptions.None);
		}

		public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate, TaskCreationOptions options)
		{
			return SingleOrDefaultAsync(source, predicate, CancellationToken.None, options);
		}

		public static Task<TSource> SingleOrDefaultAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>, TSource>(Queryable.SingleOrDefault), source, predicate),
						arguments: new Expression[2] { source.Expression, Expression.Quote(predicate) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.SingleOrDefault(predicate), token, options);
		}

		#endregion

		#region ContainsAsync<TSource, item>

		public static Task<bool> ContainsAsync<TSource>(this IQueryable<TSource> source, TSource item)
		{
			return ContainsAsync(source, item, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<bool> ContainsAsync<TSource>(this IQueryable<TSource> source, TSource item, CancellationToken token)
		{
			return ContainsAsync(source, item, token, TaskCreationOptions.None);
		}

		public static Task<bool> ContainsAsync<TSource>(this IQueryable<TSource> source, TSource item, TaskCreationOptions options)
		{
			return ContainsAsync(source, item, CancellationToken.None, options);
		}

		public static Task<bool> ContainsAsync<TSource>(
			this IQueryable<TSource> source, TSource item,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<bool>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, TSource, bool>(Queryable.Contains), source, item),
						arguments: new Expression[2] { source.Expression, (Expression)Expression.Constant((object)item, typeof (TSource)) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Contains(item), token, options);
		}

		#endregion

		#region AnyAsync<TSource>

		public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source)
		{
			return AnyAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, CancellationToken token)
		{
			return AnyAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, TaskCreationOptions options)
		{
			return AnyAsync(source, CancellationToken.None, options);
		}

		public static Task<bool> AnyAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<bool>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, bool>(Queryable.Any), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Any, token, options);
		}

		#endregion

		#region AnyAsync<TSource, predicate>

		public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate)
		{
			return AnyAsync(source, predicate, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate, CancellationToken token)
		{
			return AnyAsync(source, predicate, token, TaskCreationOptions.None);
		}

		public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate, TaskCreationOptions options)
		{
			return AnyAsync(source, predicate, CancellationToken.None, options);
		}

		public static Task<bool> AnyAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<bool>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>, bool>(Queryable.Any), source, predicate),
						arguments: new Expression[2] { source.Expression, Expression.Quote(predicate) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Any(predicate), token, options);
		}

		#endregion

		#region AllAsync<TSource, predicate>

		public static Task<bool> AllAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate)
		{
			return AllAsync(source, predicate, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<bool> AllAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate, CancellationToken token)
		{
			return AllAsync(source, predicate, token, TaskCreationOptions.None);
		}

		public static Task<bool> AllAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate, TaskCreationOptions options)
		{
			return AllAsync(source, predicate, CancellationToken.None, options);
		}

		public static Task<bool> AllAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<bool>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>, bool>(Queryable.All), source, predicate),
						arguments: new Expression[2] { source.Expression, Expression.Quote(predicate) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.All(predicate), token, options);
		}

		#endregion

		#region CountAsync<TSource>

		public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source)
		{
			return CountAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, CancellationToken token)
		{
			return CountAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, TaskCreationOptions options)
		{
			return CountAsync(source, CancellationToken.None, options);
		}

		public static Task<int> CountAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<int>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, int>(Queryable.Count), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Count, token, options);
		}

		#endregion

		#region CountAsync<TSource, predicate>

		public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate)
		{
			return CountAsync(source, predicate, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate, CancellationToken token)
		{
			return CountAsync(source, predicate, token, TaskCreationOptions.None);
		}

		public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate, TaskCreationOptions options)
		{
			return CountAsync(source, predicate, CancellationToken.None, options);
		}

		public static Task<int> CountAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<int>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>, int>(Queryable.Count), source, predicate),
						arguments: new Expression[2] { source.Expression, Expression.Quote(predicate) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Count(predicate), token, options);
		}

		#endregion

		#region LongCountAsync<TSource>

		public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source)
		{
			return LongCountAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, CancellationToken token)
		{
			return LongCountAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, TaskCreationOptions options)
		{
			return LongCountAsync(source, CancellationToken.None, options);
		}

		public static Task<long> LongCountAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<long>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, long>(Queryable.LongCount), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.LongCount, token, options);
		}

		#endregion

		#region LongCountAsync<TSource, predicate>

		public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate)
		{
			return LongCountAsync(source, predicate, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate, CancellationToken token)
		{
			return LongCountAsync(source, predicate, token, TaskCreationOptions.None);
		}

		public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate, TaskCreationOptions options)
		{
			return LongCountAsync(source, predicate, CancellationToken.None, options);
		}

		public static Task<long> LongCountAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,bool>> predicate,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<long>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,bool>>, long>(Queryable.LongCount), source, predicate),
						arguments: new Expression[2] { source.Expression, Expression.Quote(predicate) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.LongCount(predicate), token, options);
		}

		#endregion

		#region MinAsync<TSource>

		public static Task<TSource> MinAsync<TSource>(this IQueryable<TSource> source)
		{
			return MinAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<TSource> MinAsync<TSource>(this IQueryable<TSource> source, CancellationToken token)
		{
			return MinAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<TSource> MinAsync<TSource>(this IQueryable<TSource> source, TaskCreationOptions options)
		{
			return MinAsync(source, CancellationToken.None, options);
		}

		public static Task<TSource> MinAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, TSource>(Queryable.Min), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Min, token, options);
		}

		#endregion

		#region MinAsync<TSource, selector>

		public static Task<TResult> MinAsync<TSource,TResult>(this IQueryable<TSource> source, Expression<Func<TSource,TResult>> selector)
		{
			return MinAsync(source, selector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<TResult> MinAsync<TSource,TResult>(this IQueryable<TSource> source, Expression<Func<TSource,TResult>> selector, CancellationToken token)
		{
			return MinAsync(source, selector, token, TaskCreationOptions.None);
		}

		public static Task<TResult> MinAsync<TSource,TResult>(this IQueryable<TSource> source, Expression<Func<TSource,TResult>> selector, TaskCreationOptions options)
		{
			return MinAsync(source, selector, CancellationToken.None, options);
		}

		public static Task<TResult> MinAsync<TSource,TResult>(
			this IQueryable<TSource> source, Expression<Func<TSource,TResult>> selector,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TResult>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,TResult>>, TResult>(Queryable.Min), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Min(selector), token, options);
		}

		#endregion

		#region MaxAsync<TSource>

		public static Task<TSource> MaxAsync<TSource>(this IQueryable<TSource> source)
		{
			return MaxAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<TSource> MaxAsync<TSource>(this IQueryable<TSource> source, CancellationToken token)
		{
			return MaxAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<TSource> MaxAsync<TSource>(this IQueryable<TSource> source, TaskCreationOptions options)
		{
			return MaxAsync(source, CancellationToken.None, options);
		}

		public static Task<TSource> MaxAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, TSource>(Queryable.Max), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Max, token, options);
		}

		#endregion

		#region MaxAsync<TSource, selector>

		public static Task<TResult> MaxAsync<TSource,TResult>(this IQueryable<TSource> source, Expression<Func<TSource,TResult>> selector)
		{
			return MaxAsync(source, selector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<TResult> MaxAsync<TSource,TResult>(this IQueryable<TSource> source, Expression<Func<TSource,TResult>> selector, CancellationToken token)
		{
			return MaxAsync(source, selector, token, TaskCreationOptions.None);
		}

		public static Task<TResult> MaxAsync<TSource,TResult>(this IQueryable<TSource> source, Expression<Func<TSource,TResult>> selector, TaskCreationOptions options)
		{
			return MaxAsync(source, selector, CancellationToken.None, options);
		}

		public static Task<TResult> MaxAsync<TSource,TResult>(
			this IQueryable<TSource> source, Expression<Func<TSource,TResult>> selector,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<TResult>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,TResult>>, TResult>(Queryable.Max), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Max(selector), token, options);
		}

		#endregion

		#region SumAsync<int>

		public static Task<int> SumAsync(this IQueryable<int> source)
		{
			return SumAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<int> SumAsync(this IQueryable<int> source, CancellationToken token)
		{
			return SumAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<int> SumAsync(this IQueryable<int> source, TaskCreationOptions options)
		{
			return SumAsync(source, CancellationToken.None, options);
		}

		public static Task<int> SumAsync(
			this IQueryable<int> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<int>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<int>, int>(Queryable.Sum), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Sum, token, options);
		}

		#endregion

		#region SumAsync<int?>

		public static Task<int?> SumAsync(this IQueryable<int?> source)
		{
			return SumAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<int?> SumAsync(this IQueryable<int?> source, CancellationToken token)
		{
			return SumAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<int?> SumAsync(this IQueryable<int?> source, TaskCreationOptions options)
		{
			return SumAsync(source, CancellationToken.None, options);
		}

		public static Task<int?> SumAsync(
			this IQueryable<int?> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<int?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<int?>, int?>(Queryable.Sum), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Sum, token, options);
		}

		#endregion

		#region SumAsync<long>

		public static Task<long> SumAsync(this IQueryable<long> source)
		{
			return SumAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<long> SumAsync(this IQueryable<long> source, CancellationToken token)
		{
			return SumAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<long> SumAsync(this IQueryable<long> source, TaskCreationOptions options)
		{
			return SumAsync(source, CancellationToken.None, options);
		}

		public static Task<long> SumAsync(
			this IQueryable<long> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<long>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<long>, long>(Queryable.Sum), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Sum, token, options);
		}

		#endregion

		#region SumAsync<long?>

		public static Task<long?> SumAsync(this IQueryable<long?> source)
		{
			return SumAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<long?> SumAsync(this IQueryable<long?> source, CancellationToken token)
		{
			return SumAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<long?> SumAsync(this IQueryable<long?> source, TaskCreationOptions options)
		{
			return SumAsync(source, CancellationToken.None, options);
		}

		public static Task<long?> SumAsync(
			this IQueryable<long?> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<long?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<long?>, long?>(Queryable.Sum), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Sum, token, options);
		}

		#endregion

		#region SumAsync<float>

		public static Task<float> SumAsync(this IQueryable<float> source)
		{
			return SumAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<float> SumAsync(this IQueryable<float> source, CancellationToken token)
		{
			return SumAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<float> SumAsync(this IQueryable<float> source, TaskCreationOptions options)
		{
			return SumAsync(source, CancellationToken.None, options);
		}

		public static Task<float> SumAsync(
			this IQueryable<float> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<float>, float>(Queryable.Sum), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Sum, token, options);
		}

		#endregion

		#region SumAsync<float?>

		public static Task<float?> SumAsync(this IQueryable<float?> source)
		{
			return SumAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<float?> SumAsync(this IQueryable<float?> source, CancellationToken token)
		{
			return SumAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<float?> SumAsync(this IQueryable<float?> source, TaskCreationOptions options)
		{
			return SumAsync(source, CancellationToken.None, options);
		}

		public static Task<float?> SumAsync(
			this IQueryable<float?> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<float?>, float?>(Queryable.Sum), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Sum, token, options);
		}

		#endregion

		#region SumAsync<double>

		public static Task<double> SumAsync(this IQueryable<double> source)
		{
			return SumAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<double> SumAsync(this IQueryable<double> source, CancellationToken token)
		{
			return SumAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<double> SumAsync(this IQueryable<double> source, TaskCreationOptions options)
		{
			return SumAsync(source, CancellationToken.None, options);
		}

		public static Task<double> SumAsync(
			this IQueryable<double> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<double>, double>(Queryable.Sum), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Sum, token, options);
		}

		#endregion

		#region SumAsync<double?>

		public static Task<double?> SumAsync(this IQueryable<double?> source)
		{
			return SumAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<double?> SumAsync(this IQueryable<double?> source, CancellationToken token)
		{
			return SumAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<double?> SumAsync(this IQueryable<double?> source, TaskCreationOptions options)
		{
			return SumAsync(source, CancellationToken.None, options);
		}

		public static Task<double?> SumAsync(
			this IQueryable<double?> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<double?>, double?>(Queryable.Sum), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Sum, token, options);
		}

		#endregion

		#region SumAsync<decimal>

		public static Task<decimal> SumAsync(this IQueryable<decimal> source)
		{
			return SumAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<decimal> SumAsync(this IQueryable<decimal> source, CancellationToken token)
		{
			return SumAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<decimal> SumAsync(this IQueryable<decimal> source, TaskCreationOptions options)
		{
			return SumAsync(source, CancellationToken.None, options);
		}

		public static Task<decimal> SumAsync(
			this IQueryable<decimal> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<decimal>, decimal>(Queryable.Sum), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Sum, token, options);
		}

		#endregion

		#region SumAsync<decimal?>

		public static Task<decimal?> SumAsync(this IQueryable<decimal?> source)
		{
			return SumAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<decimal?> SumAsync(this IQueryable<decimal?> source, CancellationToken token)
		{
			return SumAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<decimal?> SumAsync(this IQueryable<decimal?> source, TaskCreationOptions options)
		{
			return SumAsync(source, CancellationToken.None, options);
		}

		public static Task<decimal?> SumAsync(
			this IQueryable<decimal?> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<decimal?>, decimal?>(Queryable.Sum), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Sum, token, options);
		}

		#endregion

		#region SumAsync<int, selector>

		public static Task<int> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,int>> selector)
		{
			return SumAsync(source, selector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<int> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,int>> selector, CancellationToken token)
		{
			return SumAsync(source, selector, token, TaskCreationOptions.None);
		}

		public static Task<int> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,int>> selector, TaskCreationOptions options)
		{
			return SumAsync(source, selector, CancellationToken.None, options);
		}

		public static Task<int> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int>> selector,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<int>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,int>>, int>(Queryable.Sum), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Sum(selector), token, options);
		}

		#endregion

		#region SumAsync<int?, selector>

		public static Task<int?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,int?>> selector)
		{
			return SumAsync(source, selector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<int?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,int?>> selector, CancellationToken token)
		{
			return SumAsync(source, selector, token, TaskCreationOptions.None);
		}

		public static Task<int?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,int?>> selector, TaskCreationOptions options)
		{
			return SumAsync(source, selector, CancellationToken.None, options);
		}

		public static Task<int?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int?>> selector,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<int?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,int?>>, int?>(Queryable.Sum), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Sum(selector), token, options);
		}

		#endregion

		#region SumAsync<long, selector>

		public static Task<long> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,long>> selector)
		{
			return SumAsync(source, selector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<long> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,long>> selector, CancellationToken token)
		{
			return SumAsync(source, selector, token, TaskCreationOptions.None);
		}

		public static Task<long> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,long>> selector, TaskCreationOptions options)
		{
			return SumAsync(source, selector, CancellationToken.None, options);
		}

		public static Task<long> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long>> selector,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<long>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,long>>, long>(Queryable.Sum), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Sum(selector), token, options);
		}

		#endregion

		#region SumAsync<long?, selector>

		public static Task<long?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,long?>> selector)
		{
			return SumAsync(source, selector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<long?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,long?>> selector, CancellationToken token)
		{
			return SumAsync(source, selector, token, TaskCreationOptions.None);
		}

		public static Task<long?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,long?>> selector, TaskCreationOptions options)
		{
			return SumAsync(source, selector, CancellationToken.None, options);
		}

		public static Task<long?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long?>> selector,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<long?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,long?>>, long?>(Queryable.Sum), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Sum(selector), token, options);
		}

		#endregion

		#region SumAsync<float, selector>

		public static Task<float> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,float>> selector)
		{
			return SumAsync(source, selector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<float> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,float>> selector, CancellationToken token)
		{
			return SumAsync(source, selector, token, TaskCreationOptions.None);
		}

		public static Task<float> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,float>> selector, TaskCreationOptions options)
		{
			return SumAsync(source, selector, CancellationToken.None, options);
		}

		public static Task<float> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float>> selector,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,float>>, float>(Queryable.Sum), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Sum(selector), token, options);
		}

		#endregion

		#region SumAsync<float?, selector>

		public static Task<float?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,float?>> selector)
		{
			return SumAsync(source, selector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<float?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,float?>> selector, CancellationToken token)
		{
			return SumAsync(source, selector, token, TaskCreationOptions.None);
		}

		public static Task<float?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,float?>> selector, TaskCreationOptions options)
		{
			return SumAsync(source, selector, CancellationToken.None, options);
		}

		public static Task<float?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float?>> selector,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,float?>>, float?>(Queryable.Sum), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Sum(selector), token, options);
		}

		#endregion

		#region SumAsync<double, selector>

		public static Task<double> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,double>> selector)
		{
			return SumAsync(source, selector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<double> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,double>> selector, CancellationToken token)
		{
			return SumAsync(source, selector, token, TaskCreationOptions.None);
		}

		public static Task<double> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,double>> selector, TaskCreationOptions options)
		{
			return SumAsync(source, selector, CancellationToken.None, options);
		}

		public static Task<double> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double>> selector,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,double>>, double>(Queryable.Sum), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Sum(selector), token, options);
		}

		#endregion

		#region SumAsync<double?, selector>

		public static Task<double?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,double?>> selector)
		{
			return SumAsync(source, selector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<double?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,double?>> selector, CancellationToken token)
		{
			return SumAsync(source, selector, token, TaskCreationOptions.None);
		}

		public static Task<double?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,double?>> selector, TaskCreationOptions options)
		{
			return SumAsync(source, selector, CancellationToken.None, options);
		}

		public static Task<double?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double?>> selector,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,double?>>, double?>(Queryable.Sum), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Sum(selector), token, options);
		}

		#endregion

		#region SumAsync<decimal, selector>

		public static Task<decimal> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,decimal>> selector)
		{
			return SumAsync(source, selector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<decimal> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,decimal>> selector, CancellationToken token)
		{
			return SumAsync(source, selector, token, TaskCreationOptions.None);
		}

		public static Task<decimal> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,decimal>> selector, TaskCreationOptions options)
		{
			return SumAsync(source, selector, CancellationToken.None, options);
		}

		public static Task<decimal> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal>> selector,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,decimal>>, decimal>(Queryable.Sum), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Sum(selector), token, options);
		}

		#endregion

		#region SumAsync<decimal?, selector>

		public static Task<decimal?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,decimal?>> selector)
		{
			return SumAsync(source, selector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<decimal?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,decimal?>> selector, CancellationToken token)
		{
			return SumAsync(source, selector, token, TaskCreationOptions.None);
		}

		public static Task<decimal?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,decimal?>> selector, TaskCreationOptions options)
		{
			return SumAsync(source, selector, CancellationToken.None, options);
		}

		public static Task<decimal?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal?>> selector,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,decimal?>>, decimal?>(Queryable.Sum), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Sum(selector), token, options);
		}

		#endregion

		#region AverageAsync<int>

		public static Task<double> AverageAsync(this IQueryable<int> source)
		{
			return AverageAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<double> AverageAsync(this IQueryable<int> source, CancellationToken token)
		{
			return AverageAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<double> AverageAsync(this IQueryable<int> source, TaskCreationOptions options)
		{
			return AverageAsync(source, CancellationToken.None, options);
		}

		public static Task<double> AverageAsync(
			this IQueryable<int> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<int>, double>(Queryable.Average), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Average, token, options);
		}

		#endregion

		#region AverageAsync<int?>

		public static Task<double?> AverageAsync(this IQueryable<int?> source)
		{
			return AverageAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<double?> AverageAsync(this IQueryable<int?> source, CancellationToken token)
		{
			return AverageAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<double?> AverageAsync(this IQueryable<int?> source, TaskCreationOptions options)
		{
			return AverageAsync(source, CancellationToken.None, options);
		}

		public static Task<double?> AverageAsync(
			this IQueryable<int?> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<int?>, double?>(Queryable.Average), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Average, token, options);
		}

		#endregion

		#region AverageAsync<long>

		public static Task<double> AverageAsync(this IQueryable<long> source)
		{
			return AverageAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<double> AverageAsync(this IQueryable<long> source, CancellationToken token)
		{
			return AverageAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<double> AverageAsync(this IQueryable<long> source, TaskCreationOptions options)
		{
			return AverageAsync(source, CancellationToken.None, options);
		}

		public static Task<double> AverageAsync(
			this IQueryable<long> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<long>, double>(Queryable.Average), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Average, token, options);
		}

		#endregion

		#region AverageAsync<long?>

		public static Task<double?> AverageAsync(this IQueryable<long?> source)
		{
			return AverageAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<double?> AverageAsync(this IQueryable<long?> source, CancellationToken token)
		{
			return AverageAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<double?> AverageAsync(this IQueryable<long?> source, TaskCreationOptions options)
		{
			return AverageAsync(source, CancellationToken.None, options);
		}

		public static Task<double?> AverageAsync(
			this IQueryable<long?> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<long?>, double?>(Queryable.Average), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Average, token, options);
		}

		#endregion

		#region AverageAsync<float>

		public static Task<float> AverageAsync(this IQueryable<float> source)
		{
			return AverageAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<float> AverageAsync(this IQueryable<float> source, CancellationToken token)
		{
			return AverageAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<float> AverageAsync(this IQueryable<float> source, TaskCreationOptions options)
		{
			return AverageAsync(source, CancellationToken.None, options);
		}

		public static Task<float> AverageAsync(
			this IQueryable<float> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<float>, float>(Queryable.Average), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Average, token, options);
		}

		#endregion

		#region AverageAsync<float?>

		public static Task<float?> AverageAsync(this IQueryable<float?> source)
		{
			return AverageAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<float?> AverageAsync(this IQueryable<float?> source, CancellationToken token)
		{
			return AverageAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<float?> AverageAsync(this IQueryable<float?> source, TaskCreationOptions options)
		{
			return AverageAsync(source, CancellationToken.None, options);
		}

		public static Task<float?> AverageAsync(
			this IQueryable<float?> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<float?>, float?>(Queryable.Average), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Average, token, options);
		}

		#endregion

		#region AverageAsync<double>

		public static Task<double> AverageAsync(this IQueryable<double> source)
		{
			return AverageAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<double> AverageAsync(this IQueryable<double> source, CancellationToken token)
		{
			return AverageAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<double> AverageAsync(this IQueryable<double> source, TaskCreationOptions options)
		{
			return AverageAsync(source, CancellationToken.None, options);
		}

		public static Task<double> AverageAsync(
			this IQueryable<double> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<double>, double>(Queryable.Average), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Average, token, options);
		}

		#endregion

		#region AverageAsync<double?>

		public static Task<double?> AverageAsync(this IQueryable<double?> source)
		{
			return AverageAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<double?> AverageAsync(this IQueryable<double?> source, CancellationToken token)
		{
			return AverageAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<double?> AverageAsync(this IQueryable<double?> source, TaskCreationOptions options)
		{
			return AverageAsync(source, CancellationToken.None, options);
		}

		public static Task<double?> AverageAsync(
			this IQueryable<double?> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<double?>, double?>(Queryable.Average), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Average, token, options);
		}

		#endregion

		#region AverageAsync<decimal>

		public static Task<decimal> AverageAsync(this IQueryable<decimal> source)
		{
			return AverageAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<decimal> AverageAsync(this IQueryable<decimal> source, CancellationToken token)
		{
			return AverageAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<decimal> AverageAsync(this IQueryable<decimal> source, TaskCreationOptions options)
		{
			return AverageAsync(source, CancellationToken.None, options);
		}

		public static Task<decimal> AverageAsync(
			this IQueryable<decimal> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<decimal>, decimal>(Queryable.Average), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Average, token, options);
		}

		#endregion

		#region AverageAsync<decimal?>

		public static Task<decimal?> AverageAsync(this IQueryable<decimal?> source)
		{
			return AverageAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<decimal?> AverageAsync(this IQueryable<decimal?> source, CancellationToken token)
		{
			return AverageAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<decimal?> AverageAsync(this IQueryable<decimal?> source, TaskCreationOptions options)
		{
			return AverageAsync(source, CancellationToken.None, options);
		}

		public static Task<decimal?> AverageAsync(
			this IQueryable<decimal?> source,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<decimal?>, decimal?>(Queryable.Average), source),
						arguments: new Expression[1] { source.Expression }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(source.Average, token, options);
		}

		#endregion

		#region AverageAsync<int, selector>

		public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,int>> selector)
		{
			return AverageAsync(source, selector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,int>> selector, CancellationToken token)
		{
			return AverageAsync(source, selector, token, TaskCreationOptions.None);
		}

		public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,int>> selector, TaskCreationOptions options)
		{
			return AverageAsync(source, selector, CancellationToken.None, options);
		}

		public static Task<double> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int>> selector,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,int>>, double>(Queryable.Average), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Average(selector), token, options);
		}

		#endregion

		#region AverageAsync<int?, selector>

		public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,int?>> selector)
		{
			return AverageAsync(source, selector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,int?>> selector, CancellationToken token)
		{
			return AverageAsync(source, selector, token, TaskCreationOptions.None);
		}

		public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,int?>> selector, TaskCreationOptions options)
		{
			return AverageAsync(source, selector, CancellationToken.None, options);
		}

		public static Task<double?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int?>> selector,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,int?>>, double?>(Queryable.Average), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Average(selector), token, options);
		}

		#endregion

		#region AverageAsync<long, selector>

		public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,long>> selector)
		{
			return AverageAsync(source, selector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,long>> selector, CancellationToken token)
		{
			return AverageAsync(source, selector, token, TaskCreationOptions.None);
		}

		public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,long>> selector, TaskCreationOptions options)
		{
			return AverageAsync(source, selector, CancellationToken.None, options);
		}

		public static Task<double> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long>> selector,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,long>>, double>(Queryable.Average), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Average(selector), token, options);
		}

		#endregion

		#region AverageAsync<long?, selector>

		public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,long?>> selector)
		{
			return AverageAsync(source, selector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,long?>> selector, CancellationToken token)
		{
			return AverageAsync(source, selector, token, TaskCreationOptions.None);
		}

		public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,long?>> selector, TaskCreationOptions options)
		{
			return AverageAsync(source, selector, CancellationToken.None, options);
		}

		public static Task<double?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long?>> selector,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,long?>>, double?>(Queryable.Average), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Average(selector), token, options);
		}

		#endregion

		#region AverageAsync<float, selector>

		public static Task<float> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,float>> selector)
		{
			return AverageAsync(source, selector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<float> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,float>> selector, CancellationToken token)
		{
			return AverageAsync(source, selector, token, TaskCreationOptions.None);
		}

		public static Task<float> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,float>> selector, TaskCreationOptions options)
		{
			return AverageAsync(source, selector, CancellationToken.None, options);
		}

		public static Task<float> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float>> selector,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,float>>, float>(Queryable.Average), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Average(selector), token, options);
		}

		#endregion

		#region AverageAsync<float?, selector>

		public static Task<float?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,float?>> selector)
		{
			return AverageAsync(source, selector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<float?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,float?>> selector, CancellationToken token)
		{
			return AverageAsync(source, selector, token, TaskCreationOptions.None);
		}

		public static Task<float?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,float?>> selector, TaskCreationOptions options)
		{
			return AverageAsync(source, selector, CancellationToken.None, options);
		}

		public static Task<float?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float?>> selector,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<float?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,float?>>, float?>(Queryable.Average), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Average(selector), token, options);
		}

		#endregion

		#region AverageAsync<double, selector>

		public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,double>> selector)
		{
			return AverageAsync(source, selector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,double>> selector, CancellationToken token)
		{
			return AverageAsync(source, selector, token, TaskCreationOptions.None);
		}

		public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,double>> selector, TaskCreationOptions options)
		{
			return AverageAsync(source, selector, CancellationToken.None, options);
		}

		public static Task<double> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double>> selector,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,double>>, double>(Queryable.Average), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Average(selector), token, options);
		}

		#endregion

		#region AverageAsync<double?, selector>

		public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,double?>> selector)
		{
			return AverageAsync(source, selector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,double?>> selector, CancellationToken token)
		{
			return AverageAsync(source, selector, token, TaskCreationOptions.None);
		}

		public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,double?>> selector, TaskCreationOptions options)
		{
			return AverageAsync(source, selector, CancellationToken.None, options);
		}

		public static Task<double?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double?>> selector,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<double?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,double?>>, double?>(Queryable.Average), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Average(selector), token, options);
		}

		#endregion

		#region AverageAsync<decimal, selector>

		public static Task<decimal> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,decimal>> selector)
		{
			return AverageAsync(source, selector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<decimal> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,decimal>> selector, CancellationToken token)
		{
			return AverageAsync(source, selector, token, TaskCreationOptions.None);
		}

		public static Task<decimal> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,decimal>> selector, TaskCreationOptions options)
		{
			return AverageAsync(source, selector, CancellationToken.None, options);
		}

		public static Task<decimal> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal>> selector,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,decimal>>, decimal>(Queryable.Average), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Average(selector), token, options);
		}

		#endregion

		#region AverageAsync<decimal?, selector>

		public static Task<decimal?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,decimal?>> selector)
		{
			return AverageAsync(source, selector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<decimal?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,decimal?>> selector, CancellationToken token)
		{
			return AverageAsync(source, selector, token, TaskCreationOptions.None);
		}

		public static Task<decimal?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource,decimal?>> selector, TaskCreationOptions options)
		{
			return AverageAsync(source, selector, CancellationToken.None, options);
		}

		public static Task<decimal?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal?>> selector,
			CancellationToken   token,
			TaskCreationOptions options)
		{
#if !NOASYNC

			var provider = source.Provider as IQueryProviderAsync;

			if (provider != null)
			{
				return provider.ExecuteAsync<decimal?>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(new Func<IQueryable<TSource>, Expression<Func<TSource,decimal?>>, decimal?>(Queryable.Average), source, selector),
						arguments: new Expression[2] { source.Expression, Expression.Quote(selector) }) as Expression,
					token,
					options);
			}

#endif

			return GetTask(() => source.Average(selector), token, options);
		}

		#endregion

	}
}
