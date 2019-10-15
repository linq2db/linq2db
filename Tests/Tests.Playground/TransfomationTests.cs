using System.Linq;
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
			var query = source.SelectMany(a => details.Where(b => b.ParentId == a.AId), (a, b) => new { A = a, B = b });

			EagerLoadingProbes.RegisterTransformation(query.Expression);
		}


	}
}
