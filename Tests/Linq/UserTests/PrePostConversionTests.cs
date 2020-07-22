using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class PrePostConversionTests : TestBase
	{
		[Table]
		public class ValuesTable
		{
			[Column, PrimaryKey]
			public long Id { get; set; }

			[Column]
			public int SomeValue1 { get; set; }

			[Column]
			public int SomeValue2 { get; set; }
		}

		[Test]
		public void TestInsertDynamic([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();

			ms.SetConvertExpression<int, int>(v => v * 2);
			
			using (var db = GetDataContext(context, ms))
			using (var t = db.CreateLocalTable<ValuesTable>())
			{
				var param = 1;
				t.Insert(() => new ValuesTable()
				{
					Id = 1,
					SomeValue1 = 1,
					SomeValue2 = param,
				});

				var record = t.Single();

				Assert.AreEqual(4, record.SomeValue1);
				Assert.AreEqual(4, record.SomeValue2);
			}
		}

		[Test]
		public void TestInsertObject([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();

			ms.SetConvertExpression<int, int>(v => v * 2);
			
			using (var db = GetDataContext(context, ms))
			using (var t = db.CreateLocalTable<ValuesTable>())
			{
				var param = 1;
				db.Insert(new ValuesTable
				{
					Id = 1,
					SomeValue1 = 1,
					SomeValue2 = param,
				});

				var record = t.Single();

				Assert.AreEqual(4, record.SomeValue1);
				Assert.AreEqual(4, record.SomeValue2);
			}
		}


		[Test]
		public void TestUpdateObject([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();

			ms.SetConvertExpression<int, int>(v => v * 2);
			
			using (var db = GetDataContext(context, ms))
			using (var t = db.CreateLocalTable<ValuesTable>())
			{
				db.Insert(new ValuesTable
				{
					Id = 1,
					SomeValue1 = 1,
					SomeValue2 = 1,
				});

				var record = t.Single();

				db.Update(record);

				record = t.Single();

				Assert.AreEqual(16, record.SomeValue1);
				Assert.AreEqual(16, record.SomeValue2);
			}
		}


		[Test]
		public void TestUpdateSet([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();

			ms.SetConvertExpression<int, int>(v => v * 2);
			
			using (var db = GetDataContext(context, ms))
			using (var t = db.CreateLocalTable<ValuesTable>())
			{
				db.Insert(new ValuesTable
				{
					Id = 1,
					SomeValue1 = 1,
					SomeValue2 = 1,
				});

				t.Set(r => r.SomeValue1, 4)
					.Set(r => r.SomeValue2, () => 2)
					.Update();

				var record = t.Single();

				Assert.AreEqual(16, record.SomeValue1);
				Assert.AreEqual(8, record.SomeValue2);

				var param = 4;
				t.Set(r => r.SomeValue2, () => param)
					.Update();

				record = t.Single();

				Assert.AreEqual(16, record.SomeValue1);
				Assert.AreEqual(16, record.SomeValue2);
			}
		}


	}
}
