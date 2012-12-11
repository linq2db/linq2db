using System;
using System.Linq;

using NUnit.Framework;

using LinqToDB;
using LinqToDB.Data;

namespace Tests.Data
{
	[TestFixture]
	public class DataExtensionsTest : TestBase
	{
		[Test]
		public void Test1([IncludeDataContexts(ProviderName.SqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var list = conn.Query(rd => rd[0], "SELECT 1").ToList();

				Assert.That(new[] { 1 }, Is.EquivalentTo(list));
			}
		}

		[Test]
		public void Test2([IncludeDataContexts(ProviderName.SqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var list = conn.Query<int>("SELECT 1").ToList();

				Assert.That(new[] { 1 }, Is.EquivalentTo(list));
			}
		}

		[Test]
		public void Test3([IncludeDataContexts(ProviderName.SqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var list = conn.Query<DateTimeOffset>("SELECT CURRENT_TIMESTAMP").ToList();

				Assert.That(list.Count, Is.EqualTo(1));
			}
		}
	}
}
