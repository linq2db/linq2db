using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1373Tests : TestBase
	{
		public class CustomFieldType
		{
			public string Field1 { get; set; } = null!;

			public static CustomFieldType? FromString(string? str)
			{
				if (string.IsNullOrEmpty(str))
					return null;
				return new CustomFieldType { Field1 = str! };
			}

			public override string ToString()
			{
				return Field1;
			}
		}

		[Table("Issue1373Tests")]
		public class Issue1363Record
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column]
			public string? Field1 { get; set; }
		}

		[Table("Issue1373Tests")]
		public class Issue1363CustomRecord
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column]
			public CustomFieldType? Field1 { get; set; }
		}

		[Table("Issue1373Tests")]
		public class Issue1363CustomRecord2
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column(DataType = DataType.NVarChar)]
			public CustomFieldType? Field1 { get; set; }
		}

		[Test]
		public void Test1([DataSources] string context)
		{
			Query.ClearCaches();

			var ms = new MappingSchema();
			ms.SetConvertExpression<string?, CustomFieldType?>(s => CustomFieldType.FromString(s));
			ms.SetConvertExpression<CustomFieldType, DataParameter>(
				_ => new DataParameter(null, _ != null ? _.ToString() : null), false);

			using (var db = GetDataContext(context, ms))
			using (var tbl = db.CreateLocalTable<Issue1363Record>())
			{
				db.Insert(new Issue1363CustomRecord2()
				{
					Id = 1
				});

				db.Insert(new Issue1363CustomRecord2()
				{
					Id = 2,
					Field1 = new CustomFieldType()
				});

				db.Insert(new Issue1363CustomRecord2()
				{
					Id = 3,
					Field1 = new CustomFieldType() { Field1 = "test" }
				});

				AssertDb(db);
			}
		}

		[Test]
		public void Test2([DataSources] string context)
		{
			Query.ClearCaches();

			var ms = new MappingSchema();

			ms.SetConvertExpression<string?, CustomFieldType?>(s => CustomFieldType.FromString(s));
			ms.SetConvertExpression<CustomFieldType, DataParameter>(
				_ => _ == null
					? new DataParameter(null, null, DataType.NVarChar)
					: new DataParameter(null, _.ToString()), false);

			using (var db = GetDataContext(context, ms))
			using (var tbl = db.CreateLocalTable<Issue1363Record>())
			{
				db.Insert(new Issue1363CustomRecord()
				{
					Id = 1
				});

				db.Insert(new Issue1363CustomRecord()
				{
					Id = 2,
					Field1 = new CustomFieldType()
				});

				db.Insert(new Issue1363CustomRecord()
				{
					Id = 3,
					Field1 = new CustomFieldType() { Field1 = "test" }
				});

				AssertDb(db);
			}
		}

		[Test]
		public void Test3([DataSources] string context)
		{
			Query.ClearCaches();

			var ms = new MappingSchema();

			ms.SetConvertExpression<string?, CustomFieldType?>(s => CustomFieldType.FromString(s));
			ms.SetConvertExpression<CustomFieldType, DataParameter>(
				_ => new DataParameter(null, _ == null ? null : _.ToString(), DataType.NVarChar), false);

			using (var db = GetDataContext(context,  ms))
			using (var tbl = db.CreateLocalTable<Issue1363Record>())
			{
				db.Insert(new Issue1363CustomRecord()
				{
					Id = 1
				});

				db.Insert(new Issue1363CustomRecord()
				{
					Id = 2,
					Field1 = new CustomFieldType()
				});

				db.Insert(new Issue1363CustomRecord()
				{
					Id = 3,
					Field1 = new CustomFieldType() { Field1 = "test" }
				});

				AssertDb(db);
			}
		}

		[Test]
		public void TestExpr1([DataSources] string context)
		{
			Query.ClearCaches();

			var ms = new MappingSchema();
			ms.SetConvertExpression<string?, CustomFieldType?>(s => CustomFieldType.FromString(s));
			ms.SetConvertExpression<CustomFieldType, DataParameter>(
				_ => new DataParameter(null, _ != null ? _.ToString() : null), false);

			using (var db = GetDataContext(context, ms))
			using (var tbl = db.CreateLocalTable<Issue1363Record>())
			{
				db.GetTable<Issue1363CustomRecord2>().Insert(() => new Issue1363CustomRecord2()
				{
					Id = 1,
					Field1 = null
				});
				
				db.GetTable<Issue1363CustomRecord2>().Insert(() => new Issue1363CustomRecord2()
				{
					Id = 2,
					Field1 = new CustomFieldType()
				});

				db.GetTable<Issue1363CustomRecord2>().Insert(() => new Issue1363CustomRecord2()
				{
					Id = 3,
					Field1 = new CustomFieldType() { Field1 = "test" }
				});

				AssertDb(db);
			}
		}

		[Test]
		public void TestExpr2([DataSources] string context)
		{
			Query.ClearCaches();

			var ms = new MappingSchema();

			ms.SetConvertExpression<string?, CustomFieldType?>(s => CustomFieldType.FromString(s));
			ms.SetConvertExpression<CustomFieldType?, DataParameter>(
				_ => _ == null
					? new DataParameter(null, null, DataType.NVarChar)
					: new DataParameter(null, _.ToString()), false);

			using (var db = GetDataContext(context, ms))
			using (var tbl = db.CreateLocalTable<Issue1363Record>())
			{
				db.GetTable<Issue1363CustomRecord>().Insert(() => new Issue1363CustomRecord()
				{
					Id = 1,
					Field1 = null
				});

				db.GetTable<Issue1363CustomRecord>().Insert(() => new Issue1363CustomRecord()
				{
					Id = 2,
					Field1 = new CustomFieldType()
				});

				db.GetTable<Issue1363CustomRecord>().Insert(() => new Issue1363CustomRecord()
				{
					Id = 3,
					Field1 = new CustomFieldType() { Field1 = "test" }
				});

				AssertDb(db);
			}
		}

		[Test]
		public void TestExpr3([DataSources] string context)
		{
			Query.ClearCaches();

			var ms = new MappingSchema();

			ms.SetConvertExpression<string?, CustomFieldType?>(s => CustomFieldType.FromString(s));
			ms.SetConvertExpression<CustomFieldType, DataParameter>(
				_ => new DataParameter(null, _ == null ? null : _.ToString(), DataType.NVarChar), false);

			using (var db = GetDataContext(context, ms))
			using (var tbl = db.CreateLocalTable<Issue1363Record>())
			{
				db.GetTable<Issue1363CustomRecord>().Insert(() => new Issue1363CustomRecord()
				{
					Id = 1,
					Field1 = null
				});

				db.GetTable<Issue1363CustomRecord>().Insert(() => new Issue1363CustomRecord()
				{
					Id = 2,
					Field1 = new CustomFieldType()
				});

				db.GetTable<Issue1363CustomRecord>().Insert(() => new Issue1363CustomRecord()
				{
					Id = 3,
					Field1 = new CustomFieldType() { Field1 = "test" }
				});

				AssertDb(db);
			}
		}

		private static void AssertDb(Model.ITestDataContext db)
		{
			var result = db.GetTable<Issue1363CustomRecord>().OrderBy(_ => _.Id).ToArray();
			Assert.That(result, Has.Length.EqualTo(3));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].Id, Is.EqualTo(1));
				Assert.That(result[0].Field1, Is.Null);
				Assert.That(result[1].Id, Is.EqualTo(2));
				Assert.That(result[1].Field1, Is.Null);
				Assert.That(result[2].Id, Is.EqualTo(3));
				Assert.That(result[2].Field1, Is.Not.Null);
			}

			Assert.That(result[2].Field1!.Field1, Is.EqualTo("test"));
		}
	}
}
