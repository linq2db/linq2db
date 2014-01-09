using System;
using System.Linq;

using NUnit.Framework;

namespace Tests.Exceptions
{
	[TestFixture]
	public class Aggregtion : TestBase
	{
		[Test, DataContextSource]
		[ExpectedException(typeof(InvalidOperationException))]
		public void NonNullableMax1(string context)
		{
			using (var db = GetDataContext(context))
				db.Parent.Where(_ => _.ParentID < 0).Max(_ => _.ParentID);
		}

		[Test, DataContextSource]
		public void NonNullableMax2(string context)
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

		[Test, DataContextSource]
		[ExpectedException(typeof(InvalidOperationException))]
		public void NonNullableAverage(string context)
		{
			using (var db = GetDataContext(context))
				db.Parent.Where(_ => _.ParentID < 0).Average(_ => _.ParentID);
		}
	}
}
