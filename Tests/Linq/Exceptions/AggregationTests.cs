using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Exceptions
{
	[TestFixture]
	public class AggregationTests : TestBase
	{
		[Test]
		public void NonNullableMin1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(() => db.Parent.Where(_ => _.ParentID < 0).Min(_ => _.ParentID), Throws.TypeOf<InvalidOperationException>());
		}

		[Test]
		public void NonNullableMin2([DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
				select new
				{
					max = p.Children.Where(_ => _.ParentID < 0).Min(_ => _.ParentID)
				};

			Assert.That(() => q.ToList(), Throws.InstanceOf<InvalidOperationException>());
		}

		[Test]
		public void NonNullableMax1([DataSources] string context)
		{
			using var db = GetDataContext(context);

			Assert.That(() => db.Parent.Where(_ => _.ParentID < 0).Max(_ => _.ParentID), Throws.TypeOf<InvalidOperationException>());
		}

		[Test]
		public void NonNullableMax2([DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			using var db = GetDataContext(context);

			var q =
					from p in db.Parent
					select new
					{
						max = p.Children.Where(_ => _.ParentID < 0).Max(_ => _.ParentID)
					};

			Assert.That(() => q.ToList(), Throws.InvalidOperationException);
		}

		[Test]
		public void NonNullableAverage([DataSources] string context)
		{
			using var db = GetDataContext(context);

			Assert.That(() => db.Parent.Where(_ => _.ParentID < 0).Average(_ => _.ParentID), Throws.TypeOf<InvalidOperationException>());
		}
	}
}
