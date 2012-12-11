using System;
using System.Linq;

using NUnit.Framework;

using LinqToDB.Data;

namespace Tests.Data
{
	[TestFixture]
	public class DataExtensionsTest
	{
		[Test]
		public void Test1()
		{
			using (var conn = new DataConnection("SqlServer"))
			{
				var list = conn.Query(rd => rd[0], "SELECT 1").ToList();

				Assert.That(new[] { 1 }, Is.EquivalentTo(list));
			}
		}

		[Test]
		public void Test2()
		{
			using (var conn = new DataConnection("SqlServer"))
			{
				var list = conn.Query<int>("SELECT 1").ToList();

				Assert.That(new[] { 1 }, Is.EquivalentTo(list));
			}
		}

		[Test]
		public void Test3()
		{
			using (var conn = new DataConnection("SqlServer"))
			{
				var list = conn.Query<DateTimeOffset>("SELECT CURRENT_TIMESTAMP").ToList();

				Assert.That(list.Count, Is.EqualTo(1));
			}
		}
	}
}
