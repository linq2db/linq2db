using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB;
using LinqToDB.Expressions;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class TestTemplate : TestBase
	{
		[Table]
		class SampleClass
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test]
		public void SampleSelectTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<SampleClass>())
			{
				var query1 = table.Where(t => t.Id == 1);
				var query2 = table.Where(t2 => t2.Id == 2);

				var zz = query1.JoinByProperties(query2, SqlJoinType.Inner, x => x.Id).ToArray();
			}
		}
	}

	public static class QueryableExtensions
	{
		private static MethodInfo _joinMethodInfo =
			MemberHelper.MethodOfGeneric<IQueryable<object>>(o =>
				o.Join(o, SqlJoinType.Inner, (xo, xi) => true, (xo, xi) => xi));

		public class JoinResult<TOuter, TInner>
		{
			public TOuter Outer { get; set; } = default!;
			public TInner Inner { get; set; } = default!;
		}

		public static Expression? Unwrap(this Expression? ex)
		{
			if (ex == null)
				return null;

			switch (ex.NodeType)
			{
				case ExpressionType.Quote:
				case ExpressionType.ConvertChecked:
				case ExpressionType.Convert:
					return ((UnaryExpression)ex).Operand.Unwrap();
			}

			return ex;
		}
		
		public static IQueryable<JoinResult<T, T>> JoinByProperties<T>(this IQueryable<T> source, IQueryable<T> target, SqlJoinType joinType, params Expression<Func<T, object>>[] properties) 
			where T : class
		{
			Expression<Func<T, T, JoinResult<T, T>>> selectorLambda = (outer, inner) => new JoinResult<T, T> {Outer = outer, Inner = inner};
			
			var outerParam = selectorLambda.Parameters[0];
			var innerParam = selectorLambda.Parameters[1];
			Expression predicate = properties.Length == 0
				? Expression.Constant(true)
				: properties.Select(property => Expression.Equal(
					property.GetBody(outerParam).Unwrap()!,
					property.GetBody(innerParam).Unwrap()!)).Aggregate(Expression.AndAlso);

			var predicateLambda = Expression.Lambda(predicate, outerParam, innerParam);

			var method = _joinMethodInfo.MakeGenericMethod(typeof(T), typeof(T), typeof(JoinResult<T, T>));

			var queryExpression = Expression.Call(method, source.Expression, target.Expression, Expression.Constant(joinType),
				Expression.Quote(predicateLambda), Expression.Quote(selectorLambda));

			return source.Provider.CreateQuery<JoinResult<T, T>>(queryExpression);
		}
	}
}
