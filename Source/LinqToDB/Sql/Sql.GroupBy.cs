using System.Linq.Expressions;

namespace LinqToDB
{
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
				return ThrowHelper.ThrowLinqException<T>($"'{nameof(Rollup)}' should not be called directly.");
			}

			public T Cube<T>(Expression<Func<T>> cubeKey)
			{
				return ThrowHelper.ThrowLinqException<T>($"'{nameof(Cube)}' should not be called directly.");
			}

			public T GroupingSets<T>(Expression<Func<T>> setsExpression)
			{
				return ThrowHelper.ThrowLinqException<T>($"'{nameof(GroupingSets)}' should not be called directly.");
			}
		}

		public static IGroupBy GroupBy = new GroupByImpl();

		[Extension("GROUPING({fields, ', '})", ServerSideOnly = true, CanBeNull = false, IsAggregate = true)]
		public static int Grouping([ExprParameter] params object[] fields) 
			=> ThrowHelper.ThrowLinqException<int>($"'{nameof(Grouping)}' should not be called directly.");

	}
}
