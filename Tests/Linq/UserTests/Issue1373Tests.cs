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
			public string Field1 { get; set; }

			public static CustomFieldType FromString(string str)
			{
				if (string.IsNullOrEmpty(str))
					return null;
				return new CustomFieldType { Field1 = str };
			}

			public override string ToString()
			{
				return Field1;
			}
		}

		[Table("Issue1373Tests")]
		public class Issue1363Record
		{
			[PrimaryKey, Identity]
			public int Id { get; set; }

			[Column]
			public string Field1 { get; set; }
		}

		[Table("Issue1373Tests")]
		public class Issue1363CustomRecord
		{
			[PrimaryKey, Identity]
			public int Id { get; set; }

			[Column]
			public CustomFieldType Field1 { get; set; }
		}

		[Test]
		public void Test1([DataSources] string context, [Values] bool addNullCheck)
		{
			Query.ClearCaches();

			var ms = new MappingSchema();
			ms.SetConvertExpression<string, CustomFieldType>(s => CustomFieldType.FromString(s));
			ms.SetConvertExpression<CustomFieldType, DataParameter>(_ => new DataParameter(null, _.ToString()), addNullCheck);

			using (var db = GetDataContext(context, ms))
			using (var tbl = db.CreateLocalTable<Issue1363Record>())
			{
				db.InsertWithIdentity(new Issue1363CustomRecord()
				{
					Field1 = null
				});

				db.InsertWithIdentity(new Issue1363CustomRecord()
				{
					Field1 = new CustomFieldType()
				});

				db.InsertWithIdentity(new Issue1363CustomRecord()
				{
					Field1 = new CustomFieldType() { Field1 = "test" }
				});
			}
		}

		[Test]
		public void Test2([DataSources] string context, [Values] bool addNullCheck)
		{
			Query.ClearCaches();

			var ms = new MappingSchema();
			ms.SetConvertExpression<string, CustomFieldType>(s => CustomFieldType.FromString(s));
			ms.SetConvertExpression<CustomFieldType, DataParameter>(_ => _ == null ? new DataParameter(null, null, DataType.NVarChar) : new DataParameter(null, _.ToString()), addNullCheck);

			using (var db = GetDataContext(context, ms))
			using (var tbl = db.CreateLocalTable<Issue1363Record>())
			{
				db.InsertWithIdentity(new Issue1363CustomRecord()
				{
					Field1 = null
				});

				db.InsertWithIdentity(new Issue1363CustomRecord()
				{
					Field1 = new CustomFieldType()
				});

				db.InsertWithIdentity(new Issue1363CustomRecord()
				{
					Field1 = new CustomFieldType() { Field1 = "test" }
				});
			}
		}

		[Test]
		public void Test3([DataSources] string context, [Values] bool addNullCheck)
		{
			Query.ClearCaches();

			var ms = new MappingSchema();
			ms.SetConvertExpression<string, CustomFieldType>(s => CustomFieldType.FromString(s));
			ms.SetConvertExpression<CustomFieldType, DataParameter>(_ => new DataParameter(null, _ == null ? null : _.ToString(), DataType.NVarChar), addNullCheck);

			using (var db = GetDataContext(context,  ms))
			using (var tbl = db.CreateLocalTable<Issue1363Record>())
			{
				db.InsertWithIdentity(new Issue1363CustomRecord()
				{
					Field1 = null
				});

				db.InsertWithIdentity(new Issue1363CustomRecord()
				{
					Field1 = new CustomFieldType()
				});

				db.InsertWithIdentity(new Issue1363CustomRecord()
				{
					Field1 = new CustomFieldType() { Field1 = "test" }
				});
			}
		}

	}
}
