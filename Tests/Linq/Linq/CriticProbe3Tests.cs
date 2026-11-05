using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class CriticProbe3Tests : TestBase
	{
		sealed class ArrayFlagBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				builder.ResultExpression = new SqlValue(builder.GetValue<int[]>(0)[0] == 1);
			}
		}

		[Sql.Extension("", BuilderType = typeof(ArrayFlagBuilder), ServerSideOnly = true)]
		static bool ArrayFlag(int[] values) => values[0] == 1;

		static readonly MethodInfo _arrayFlag = typeof(CriticProbe3Tests).GetMethod(nameof(ArrayFlag), BindingFlags.NonPublic | BindingFlags.Static)!;

		// Codex F2': one Constant(n) node shared by Where and Take.
		[Test, QueryCacheTest]
		public void SharedConstantInWhereAndTake([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Q(int n)
			{
				var c = Expression.Constant(n);
				var p = Expression.Parameter(typeof(Model.Person), "p");
				var where = Expression.Lambda<Func<Model.Person, bool>>(Expression.Equal(Expression.PropertyOrField(p, nameof(Model.Person.ID)), c), p);
				var src = db.Person.Where(where).Select(x => x.ID);
				var take = Expression.Call(typeof(Queryable), nameof(Queryable.Take), [typeof(int)], src.Expression, c);
				return src.Provider.CreateQuery<int>(take);
			}

			var r1 = Q(1).ToArray();
			var r2 = Q(2).ToArray();
			TestContext.Out.WriteLine($"PROBE shared-take rows n=1 [{string.Join(",", r1)}] n=2 [{string.Join(",", r2)}] sql2={Q(2).ToSqlQuery().Sql.Replace("\n", " ")}");
		}

		// Codex F12': the same array mutated in place.
		[Test, QueryCacheTest]
		public void ArrayReadByExtensionMutatedInPlace([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Q(int[] values)
			{
				var p = Expression.Parameter(typeof(Model.Person), "p");
				var c = Expression.Constant(values);
				var body = Expression.AndAlso(
					Expression.Call(_arrayFlag, c),
					Expression.Equal(Expression.PropertyOrField(p, nameof(Model.Person.ID)), Expression.ArrayIndex(c, Expression.Constant(1))));
				return db.Person.Where(Expression.Lambda<Func<Model.Person, bool>>(body, p)).Select(x => x.ID);
			}

			var arr = new[] { 1, 2 };
			var r1 = Q(arr).ToArray();
			arr[0] = 0;
			var r2 = Q(arr).ToArray();
			TestContext.Out.WriteLine($"PROBE inplace rows before [{string.Join(",", r1)}] after-mutation [{string.Join(",", r2)}] (expected after: empty)");
		}
	}
}
