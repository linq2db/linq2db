using System.Linq;

using Shouldly;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3186Tests : TestBase
	{
		[Table]
		sealed class element_services
		{
			[Column(IsPrimaryKey = true, Length = 100, CanBeNull = false)]
			public string id { get; set; } = null!;

			[Column(CanBeNull = false)] 
			public bool is_process_service { get; set; }

			[Column(CanBeNull = false)] 
			public bool is_deleted { get; set; }

			public static element_services[] TestData()
			{
				return new[]
				{
					new element_services { id = "TestProcessService", is_process_service = true },
					new element_services { id = "TestElementService", is_process_service = false }
				};
			}
		}

		[Table]
		sealed class component_categories
		{
			[Column(IsPrimaryKey = true, Length = 100, CanBeNull = false)]
			public string id { get; set; } = null!;

			[Column(Length = 100, CanBeNull = false)]
			public string service_id { get; set; } = null!;

			[Column(CanBeNull = false)] 
			public bool is_deleted { get; set; }

			public static component_categories[] TestData()
			{
				return new[]
				{
					new component_categories { id = "TestProcessCategory1", service_id = "TestProcessService" },
					new component_categories { id = "TestProcessCategory2", service_id = "TestProcessService" },
					new component_categories { id = "TestElementCategory1", service_id = "TestElementService" },
					new component_categories { id = "TestElementCategory2", service_id = "TestElementService" },
				};
			}
		}

		[Table]
		sealed class Components
		{
			[Column(IsPrimaryKey = true, Length = 100, CanBeNull = false)]
			public string id { get; set; } = null!;

			[Column(Length = 100, CanBeNull = false)]
			public string category_id { get; set; } = null!;

			[Column(Length = 100, CanBeNull = false)]
			public string service_id { get; set; } = null!;

			[Column(CanBeNull = false)] 
			public bool is_deleted { get; set; }

			public static Components[] TestData()
			{
				return new []
				{
					new Components{ id = "TestProcessComponent1", category_id = "TestProcessCategory1", service_id = "TestProcessService" },
					new Components{ id = "TestProcessComponent2", category_id = "TestProcessCategory2", service_id = "TestProcessService" },
					new Components{ id = "TestElementComponent1", category_id = "TestElementCategory1", service_id = "TestElementService" },
					new Components{ id = "TestElementComponent2", category_id = "TestElementCategory2", service_id = "TestElementService" },
				};
			}
		}

		[Test]
		public void UpdateWhenTableSecond([DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(element_services.TestData()))
			using (db.CreateLocalTable(component_categories.TestData()))
			using (db.CreateLocalTable(Components.TestData()))
			{
				var query = db.GetTable<element_services>()
					.Where(ie => ie.id == "TestProcessService")
					.InnerJoin(db.GetTable<component_categories>(),
						(sr, ctg) => sr.id == ctg.service_id,
						(sr, ctg) => ctg);

				query.InnerJoin(db.GetTable<Components>(),
						(ct, cm) => ct.id == cm.category_id && !cm.is_deleted,
						(ct, cm) => ct
					).Set(r => r.is_deleted, true)
					.Update();

				db.GetTable<component_categories>().Where(x => x.is_deleted && x.service_id == "TestProcessService").ToList().Count.ShouldBe(2);
				db.GetTable<component_categories>().Where(x => !x.is_deleted && x.service_id != "TestProcessService").ToList().Count.ShouldBe(2);
			}
		}

		[Test]
		public void UpdateWhenTableFirst([DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(element_services.TestData()))
			using (db.CreateLocalTable(component_categories.TestData()))
			using (db.CreateLocalTable(Components.TestData()))
			{
				var query = db.GetTable<component_categories>()
					.InnerJoin(db.GetTable<element_services>()
							.Where(ie => ie.id == "TestProcessService"),
						(ctg, sr) => sr.id == ctg.service_id,
						(ctg, sr) => ctg);

				query.InnerJoin(db.GetTable<Components>(),
						(ct, cm) => ct.id == cm.category_id && !cm.is_deleted,
						(ct, cm) => ct
					).Set(r => r.is_deleted, true)
					.Update();

				db.GetTable<component_categories>().Where(x => x.is_deleted && x.service_id == "TestProcessService").ToList().Count.ShouldBe(2);
				db.GetTable<component_categories>().Where(x => !x.is_deleted && x.service_id != "TestProcessService").ToList().Count.ShouldBe(2);
			}
		}

		[Test]
		public void UpdateWhenTableFirstWithLeftJoin([DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(element_services.TestData()))
			using (db.CreateLocalTable(component_categories.TestData()))
			using (db.CreateLocalTable(Components.TestData()))
			{
				var query = db.GetTable<component_categories>()
					.InnerJoin(db.GetTable<element_services>()
							.Where(ie => ie.id == "TestProcessService"),
						(ctg, sr) => sr.id == ctg.service_id,
						(ctg, sr) => ctg);

				query.LeftJoin(db.GetTable<Components>(),
						(ct, cm) => ct.id == cm.category_id && !cm.is_deleted,
						(ct, cm) => ct
					).Set(r => r.is_deleted, true)
					.Update();

				db.GetTable<component_categories>().Where(x => x.is_deleted && x.service_id == "TestProcessService").ToList().Count.ShouldBe(2);
				db.GetTable<component_categories>().Where(x => !x.is_deleted && x.service_id != "TestProcessService").ToList().Count.ShouldBe(2);
			}
		}

	}
}
