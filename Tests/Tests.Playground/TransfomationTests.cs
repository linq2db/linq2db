using System.Linq;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Expressions;
using LinqToDB.Linq.Builder;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Playground
{
	public class TransfomationTests : TestBase
	{
		[Table("ATable")]
		class A
		{
			public int AId { get; set; }
			public string AValue { get; set; }
		}

		[Table("BTable")]
		class B
		{
			public int BId { get; set; }
			public int? ParentId { get; set; }
			public string BValue { get; set; }
		}

		IQueryable<A> GenerateA()
		{
			return Enumerable
				.Range(1, 10)
				.Select(i => new A { AId = i, AValue = "AValue" + i })
				.ToArray()
				.AsQueryable();
		}

		IQueryable<B> GenerateB()
		{
			return Enumerable
				.Range(1, 10)
				.Select(i => new B { BId = i * 100, BValue = "BValue" + i, ParentId = i})
				.ToArray()
				.AsQueryable();
		}

		[Test]
		public void TestSelectMany()
		{
			var source = GenerateA();
			var details = GenerateB();
			var query = source.SelectMany(a => details.Where(b => b.ParentId == a.AId), (a, b) => new { A = a.AValue, B = details });
			var filtered = query.Where(q => q.B.Any(bb => bb.BValue == "BValue2"));

			var lambdaToReplace = query.Expression;
			var newQuery = EagerLoading.ApplyReMapping(query.Expression, null);
		}


		[Test]
		public void TestSelectMany1()
		{
			var source  = GenerateA();
			var details = GenerateB();
			var subQuery  = source.SelectMany(a => details.Where(b => b.ParentId == a.AId), (a, b) => new { A = a.AValue, B = details });
			// var query = from s in subQuery.SelectMany(q => q.B, (q, c) => c);

			var query = from s in subQuery
				from d in s.B
				from s2 in source.LeftJoin(s2 => s2.AId == d.BId)
				select new { d, s2 };

			var withGrouping = from q in query
				group q by q.d.BId.ToString()
				into g
				select new { g.Key, Count = g.Count() };

			var lambdaToReplace = (MethodCallExpression)subQuery.Expression;

			var resultSelector = (LambdaExpression)lambdaToReplace.Arguments[1].Unwrap();

			var additionalKey = Expression.PropertyOrField(resultSelector.Parameters[0], "AId");

			var replaceInfo = new EagerLoading.ReplaceInfo(MappingSchema.Default);
			replaceInfo.TargetLambda = resultSelector;
			replaceInfo.Keys.Add(additionalKey);

			var newQuery = EagerLoading.ApplyReMapping(query.Expression, replaceInfo);
			var newQuery2 = EagerLoading.ApplyReMapping(withGrouping.Expression, replaceInfo);
		}
	}
}
