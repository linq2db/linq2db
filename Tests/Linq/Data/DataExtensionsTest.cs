using System;
using System.Linq;
using JetBrains.Annotations;
using LinqToDB.Mapping;
using NUnit.Framework;

using LinqToDB;
using LinqToDB.Data;

namespace Tests.Data
{
	[TestFixture]
	public class DataExtensionsTest : TestBase
	{
		[Test]
		public void TestScalar1([IncludeDataContexts(ProviderName.SqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var list = conn.Query(rd => rd[0], "SELECT 1").ToList();

				Assert.That(new[] { 1 }, Is.EquivalentTo(list));
			}
		}

		[Test]
		public void TestScalar2([IncludeDataContexts(ProviderName.SqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var list = conn.Query<int>("SELECT 1").ToList();

				Assert.That(new[] { 1 }, Is.EquivalentTo(list));
			}
		}

		[Test]
		public void TestScalar3([IncludeDataContexts(ProviderName.SqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var list = conn.Query<DateTimeOffset>("SELECT CURRENT_TIMESTAMP").ToList();

				Assert.That(list.Count, Is.EqualTo(1));
			}
		}

		class QueryObject
		{
			public int      Column1;
			public DateTime Column2;
		}

		[Test]
		public void TestObject1([IncludeDataContexts(ProviderName.SqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var list = conn.Query<QueryObject>("SELECT 1 as Column1, CURRENT_TIMESTAMP as Column2").ToList();

				Assert.That(list.Count, Is.EqualTo(1));
			}
		}

		[Test]
		public void TestObject2([IncludeDataContexts(ProviderName.SqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var list = conn.Query(
					new
					{
						Column1 = 1,
						Column2 = DateTime.MinValue
					},
					"SELECT 1 as Column1, CURRENT_TIMESTAMP as Column2").ToList();

				Assert.That(list.Count, Is.EqualTo(1));
			}
		}

		[ScalarType(false)]
		struct QueryStruct
		{
			public int      Column1;
			public DateTime Column2;
		}

		[Test]
		public void TestStruct1([IncludeDataContexts(ProviderName.SqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var list = conn.Query<QueryStruct>("SELECT 1 as Column1, CURRENT_TIMESTAMP as Column2").ToList();

				Assert.That(list.Count, Is.EqualTo(1));
			}
		}
	}
}
