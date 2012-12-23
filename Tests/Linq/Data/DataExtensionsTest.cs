using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

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

		[Test]
		public void TestObject3([IncludeDataContexts(ProviderName.SqlServer)] string context)
		{
			var arr1 = new byte[] { 48, 57 };
			var arr2 = new byte[] { 42 };

			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Query<byte[]>("SELECT @p", new { p = arr1 }).First(), Is.EqualTo(arr1));
				Assert.That(conn.Query<byte[]>("SELECT @p", new { p = arr2 }).First(), Is.EqualTo(arr2));
			}
		}

		[Test]
		public void TestObject4([IncludeDataContexts(ProviderName.SqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Query<int>("SELECT @p", new { p = 1 }).First(), Is.EqualTo(1));
			}
		}

		[Test]
		public void TestObject5([IncludeDataContexts(ProviderName.SqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Query<string>(
					"SELECT @p",
					new
					{
						p  = new DataParameter { DataType = DataType.VarChar, Value = "123" },
						p1 = 1
					}).First(), Is.EqualTo("123"));
			}
		}

		[Test]
		public void TestObject6([IncludeDataContexts(ProviderName.SqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Query<string>(
					"SELECT @p",
					new
					{
						p1 = new DataParameter { Name = "p", DataType = DataType.Char, Value = "123" },
						p2 = 1
					}).First(), Is.EqualTo("123"));
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
