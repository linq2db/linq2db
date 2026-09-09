using LinqToDB.Mapping;

namespace LinqToDB
{
	partial class Sql
	{
		public interface IGroupBy
		{
			public bool None { get; }

			// Marked on the interface, not on GroupByImpl: Sql.GroupBy is typed IGroupBy, so every call
			// binds the interface method and that is the MemberInfo the runtime reads the attribute from.
			[ServerSideOnly] public T Rollup<T>(T rollupKey);
			[ServerSideOnly] public T Cube<T>(T cubeKey);
			[ServerSideOnly] public T GroupingSets<T>(T setsExpression);
		}

		sealed class GroupByImpl : IGroupBy
		{
			public bool None => true;

			public T Rollup<T>(T rollupKey)
				=> throw new ServerSideOnlyException(nameof(Rollup));

			public T Cube<T>(T cubeKey)
				=> throw new ServerSideOnlyException(nameof(Cube));

			public T GroupingSets<T>(T setsExpression)
				=> throw new ServerSideOnlyException(nameof(GroupingSets));
		}

		public static readonly IGroupBy GroupBy = new GroupByImpl();

		[Extension("GROUPING({fields, ', '})", ServerSideOnly = true, CanBeNull = false, IsAggregate = true)]
		public static int Grouping([ExprParameter(ParameterKind = ExprParameterKind.Values)] params object[] fields)
			=> throw new ServerSideOnlyException(nameof(Grouping));
	}
}
