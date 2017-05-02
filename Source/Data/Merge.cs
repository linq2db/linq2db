using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Data
{
	[PublicAPI]
	public static class MergeExtensions
	{
		#region From
		public static IMergeSource<TSource> From<TTarget, TSource>(
			this ITable<TTarget> target,
			IEnumerable<TSource> source,
			Expression<Func<TTarget, TSource, bool>> matchPredicate)
				where TTarget : class
				where TSource : class
		{
			throw new NotImplementedException();
		}

		public static IMergeSource<TSource> From<TTarget, TSource>(
			this ITable<TTarget> target,
			IQueryable<TSource> source,
			Expression<Func<TTarget, TSource, bool>> matchPredicate)
				where TTarget : class
				where TSource : class
		{
			throw new NotImplementedException();
		}

		public static IMergeSource<TEntity> From<TEntity>(
			this ITable<TEntity> target,
			IEnumerable<TEntity> source)
				where TEntity : class
		{
			throw new NotImplementedException();
		}

		public static IMergeSource<TEntity> From<TEntity>(this ITable<TEntity> target, IQueryable<TEntity> source)
				where TEntity : class
		{
			throw new NotImplementedException();
		}

		public static IMergeSource<TEntity> From<TEntity>(
			this ITable<TEntity> target,
			IEnumerable<TEntity> source,
			Expression<Func<TEntity, TEntity, bool>> matchPredicatee)
				where TEntity : class
		{
			throw new NotImplementedException();
		}

		public static IMergeSource<TEntity> From<TEntity>(
			this ITable<TEntity> target,
			IQueryable<TEntity> source,
			Expression<Func<TEntity, TEntity, bool>> matchPredicate)
				where TEntity : class
		{
			throw new NotImplementedException();
		}
		#endregion

		#region Insert
		public static IMerge<TEntity> Insert<TEntity>(this IMergeSource<TEntity> merge)
				where TEntity : class
		{
			throw new NotImplementedException();
		}

		public static IMerge<TEntity> Insert<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, bool>> predicate)
				where TEntity : class
		{
			throw new NotImplementedException();
		}

		public static IMerge<TEntity> Insert<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, TEntity>> create)
				where TEntity : class
		{
			throw new NotImplementedException();
		}

		public static IMerge<TEntity> Insert<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, bool>> predicate,
			Expression<Func<TEntity, TEntity>> create)
				where TEntity : class
		{
			throw new NotImplementedException();
		}

		public static IMerge<TTarget, TSource> Insert<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TSource, TTarget>> create)
				where TTarget : class
				where TSource : class
		{
			throw new NotImplementedException();
		}

		public static IMerge<TTarget, TSource> Insert<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TSource, bool>> predicate,
			Expression<Func<TSource, TTarget>> create)
				where TTarget : class
				where TSource : class
		{
			throw new NotImplementedException();
		}
		#endregion

		#region Update
		public static IMerge<TEntity> Update<TEntity>(this IMergeSource<TEntity> merge)
				where TEntity : class
		{
			throw new NotImplementedException();
		}

		public static IMerge<TEntity> Update<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, TEntity, bool>> predicate)
				where TEntity : class
		{
			throw new NotImplementedException();
		}

		public static IMerge<TEntity> Update<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, TEntity, TEntity>> update)
				where TEntity : class
		{
			throw new NotImplementedException();
		}

		public static IMerge<TEntity> Update<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, TEntity, bool>> predicate,
			Expression<Func<TEntity, TEntity, TEntity>> update)
				where TEntity : class
		{
			throw new NotImplementedException();
		}

		public static IMerge<TTarget, TSource> Update<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TTarget, TSource, TTarget>> update)
				where TTarget : class
				where TSource : class
		{
			throw new NotImplementedException();
		}

		public static IMerge<TTarget, TSource> Update<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TTarget, TSource, bool>> predicate,
			Expression<Func<TTarget, TSource, TTarget>> update)
				where TTarget : class
				where TSource : class
		{
			throw new NotImplementedException();
		}
		#endregion

		#region Delete
		public static IMerge<TEntity> Delete<TEntity>(this IMergeSource<TEntity> merge)
				where TEntity : class
		{
			throw new NotImplementedException();
		}

		public static IMerge<TEntity> Delete<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, TEntity, bool>> predicate)
				where TEntity : class
		{
			throw new NotImplementedException();
		}

		public static IMerge<TTarget, TSource> Delete<TTarget, TSource>(this IMergeSource<TTarget, TSource> merge)
				where TTarget : class
				where TSource : class
		{
			throw new NotImplementedException();
		}

		public static IMerge<TTarget, TSource> Delete<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TTarget, TSource, bool>> predicate)
				where TTarget : class
				where TSource : class
		{
			throw new NotImplementedException();
		}
		#endregion

		#region UpdateBySource
		public static IMerge<TEntity> UpdateBySource<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, TEntity>> update)
				where TEntity : class
		{
			throw new NotImplementedException();
		}

		public static IMerge<TEntity> UpdateBySource<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, bool>> predicate,
			Expression<Func<TEntity, TEntity>> update)
				where TEntity : class
		{
			throw new NotImplementedException();
		}

		public static IMerge<TTarget, TSource> UpdateBySource<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TTarget, TTarget>> update)
				where TTarget : class
				where TSource : class
		{
			throw new NotImplementedException();
		}

		public static IMerge<TTarget, TSource> UpdateBySource<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TTarget, bool>> predicate,
			Expression<Func<TTarget, TTarget>> update)
				where TTarget : class
				where TSource : class
		{
			throw new NotImplementedException();
		}
		#endregion

		#region DeleteBySource
		public static IMerge<TEntity> DeleteBySource<TEntity>(this IMergeSource<TEntity> merge)
				where TEntity : class
		{
			throw new NotImplementedException();
		}

		public static IMerge<TEntity> DeleteBySource<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, bool>> predicate)
				where TEntity : class
		{
			throw new NotImplementedException();
		}

		public static IMerge<TTarget, TSource> DeleteBySource<TTarget, TSource>(this IMergeSource<TTarget, TSource> merge)
				where TTarget : class
				where TSource : class
		{
			throw new NotImplementedException();
		}

		public static IMerge<TTarget, TSource> DeleteBySource<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TTarget, bool>> predicate)
				where TTarget : class
				where TSource : class
		{
			throw new NotImplementedException();
		}
		#endregion

		#region Merge
		public static int Merge<TTarget, TSource>(this IMerge<TTarget, TSource> merge)
				where TTarget : class
				where TSource : class
		{
			throw new NotImplementedException();
		}

		public static int Merge<TEntity>(this IMerge<TEntity> merge)
			where TEntity : class
		{
			throw new NotImplementedException();
		}
		#endregion
	}

	public interface IMergeSource<TTarget, TSource>
	{
	}

	public interface IMergeSource<TEntity>
	{
	}

	public interface IMerge<TTarget, TSource> : IMergeSource<TTarget, TSource>
	{
	}

	public interface IMerge<TEntity> : IMergeSource<TEntity>
	{
	}

}
