using System;
using System.Linq;

using NUnit.Framework;

namespace Tests.Exceptions
{
	[TestFixture]
	public class AggregationTests : TestBase
	{
		[Test]
		public void NonNullableMax1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.Throws<InvalidOperationException>(() => db.Parent.Where(_ => _.ParentID < 0).Max(_ => _.ParentID));
		}

		[RequiresCorrelatedSubquery]
		[Test]
		public void NonNullableMax2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent
					select new
					{
						max = p.Children.Where(_ => _.ParentID < 0).Max(_ => _.ParentID)
					};

				Assert.That(() => q.ToList(), Throws.InvalidOperationException);
			}
		}

		[Test]
		public void NonNullableAverage([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				Assert.Throws<InvalidOperationException>(() => db.Parent.Where(_ => _.ParentID < 0).Average(_ => _.ParentID));
		}
	}
}
