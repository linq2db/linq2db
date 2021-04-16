using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class ConditionalTests : TestBase
	{
		class ConditionalData
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column]
			public string? StringProp { get; set; }


			public static ConditionalData[] Seed()
			{
				return Enumerable.Range(1, 10)
					.Select(x => new ConditionalData {Id = x, StringProp = x % 3 == 0 ? null : "String" + x})
					.ToArray();
			}
		}

		class TestChildClass
		{
			public string? StringProp { get; set; }
		}

		[Test]
		public void ViaConditionWithNull1([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var data = ConditionalData.Seed();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var query =
					from p in table
					select new
					{
						Id    = p.Id,
						child = p.StringProp == "1" ? null : new TestChildClass {StringProp = p.StringProp}
					};

				query = query.Where(x => x.child.StringProp.Contains("2"));

				AssertQuery(query);
			}
		}


		[Test]
		public void ViaConditionWithNull2([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var data = ConditionalData.Seed();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var query =
					from p in table
					select new
					{
						Id    = p.Id,
						child = p.StringProp == "1" ? new TestChildClass {StringProp = p.StringProp} : null
					};

				query = query.Where(x => x.child.StringProp.Contains("2"));

				AssertQuery(query);
			}
		}

		[Test]
		public void ViaCondition([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var data = ConditionalData.Seed();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var query =
					from p in table
					select new
					{
						Id = p.Id,
						child = p.StringProp == "1"
							? new TestChildClass {StringProp = "2"}
							: new TestChildClass {StringProp = p.StringProp}
					};

				query = query.Where(x => x.child.StringProp.Contains("2"));

				AssertQuery(query);
			}
		}

		[Test]
		public void ViaConditionNull([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var data = ConditionalData.Seed();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var query =
					from p in table
					from p2 in table.Where(p2 => p2.StringProp != null && p2.Id == p.Id).DefaultIfEmpty()
					select new
					{
						Id = p.Id,
						child = p2 == null
							? new TestChildClass {StringProp = "-1"}
							: new TestChildClass {StringProp = p2.StringProp}
					};

				query = query.Where(x => x.child.StringProp == "-1");

				AssertQuery(query);
			}
		}

		[Test]
		public void NestedProperties([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var data = ConditionalData.Seed();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var query =
					from p in table
					select new
					{
						Id = p.Id,
						Sub = p == null
							? new { Prop = new { V = "-1"} }
							: new { Prop = p.StringProp.Contains("1") ? new
							{
								V = "1"
							} : new
							{
								V = "2"
							}}
					};

				query = query.Where(x => x.Sub.Prop.V == "-1");

				AssertQuery(query);
			}
		}

	}
}
