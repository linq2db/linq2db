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
			public T Rollup<T>(T rollupKey);
			public T Cube<T>(T cubeKey);
			public T GroupingSets<T>(T setsExpression);
		}

		sealed class GroupByImpl : IGroupBy
		{
			public bool None => true;

			public T Rollup<T>(T rollupKey)
			{
				throw new LinqException($"'{nameof(Rollup)}' should not be called directly.");
			}

			public T Cube<T>(T cubeKey)
			{
				throw new LinqException($"'{nameof(Cube)}' should not be called directly.");
			}

			public T GroupingSets<T>(T setsExpression)
			{
				throw new LinqException($"'{nameof(GroupingSets)}' should not be called directly.");
			}
		}

		public static IGroupBy GroupBy = new GroupByImpl();

		[Extension("GROUPING({fields, ', '})", ServerSideOnly = true, CanBeNull = false, IsAggregate = true)]
		public static int Grouping([ExprParameter(ParameterKind = ExprParameterKind.Values)] params object[] fields)
			=> throw new LinqException($"'{nameof(Grouping)}' should not be called directly.");
	}
}
