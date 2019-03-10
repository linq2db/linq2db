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
				Assert.Throws(typeof(InvalidOperationException), () => db.Parent.Where(_ => _.ParentID < 0).Max(_ => _.ParentID));
		}

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

				Assert.Catch<InvalidOperationException>(() => q.ToList());
			}
		}

		[Test]
		public void NonNullableAverage([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.Throws(typeof(InvalidOperationException), () => db.Parent.Where(_ => _.ParentID < 0).Average(_ => _.ParentID));
		}
	}
}
