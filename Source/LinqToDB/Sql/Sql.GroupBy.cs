using System;
using System.Linq.Expressions;

namespace LinqToDB
{
	using Linq;

	partial class Sql
	{
		public interface IGroupBy
		{
			public bool None { get; }
			public T Rollup<T>(Expression<Func<T>> rollupKey);
			public T Cube<T>(Expression<Func<T>> cubeKey);
			public T GroupingSets<T>(Expression<Func<T>> setsExpression);
		}

		class GroupByImpl : IGroupBy
		{
			public bool None => true;

			public T Rollup<T>(Expression<Func<T>> rollupKey)
			{
				throw new LinqException($"'{nameof(Rollup)}' should not be called directly.");
			}

			public T Cube<T>(Expression<Func<T>> cubeKey)
			{
				throw new LinqException($"'{nameof(Cube)}' should not be called directly.");
			}

			public T GroupingSets<T>(Expression<Func<T>> setsExpression)
			{
				throw new LinqException($"'{nameof(GroupingSets)}' should not be called directly.");
			}
		}

		public static IGroupBy GroupBy = new GroupByImpl();

		[Sql.Extension("GROUPING({field})", ServerSideOnly = true, CanBeNull = false, IsAggregate = true)]
		public static bool Grouping<T>([ExprParameter] T field) 
			=> throw new LinqException($"'{nameof(GroupBy)}' should not be called directly.");

	}
}
