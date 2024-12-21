using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class AbstractionTests : TestBase
	{
		interface ISample
		{
			int Id    { get; set; }
			int Value { get; set; }

			IEnumerable<ChildEntitity> SomeEntities { get; set; }
		}

		[Table]
		sealed class SampleClass1 : ISample
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ChildEntitity.ParentId), CanBeNull = true)]
			public IEnumerable<ChildEntitity> SomeEntities { get; set; } = null!;

			public static SampleClass1[] Seed()
			{
				return Enumerable.Range(1, 10)
					.Select(i => new SampleClass1
					{
						Id = i,
						Value = i * 1000
					})
					.ToArray();
			}
		}

		[Table]
		sealed class SampleClass2 : ISample
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ChildEntitity.ParentId), CanBeNull = true)]
			public IEnumerable<ChildEntitity> SomeEntities { get; set; } = null!;

			public static SampleClass2[] Seed()
			{
				return Enumerable.Range(1, 10)
					.Select(i => new SampleClass2
					{
						Id = i,
						Value = i * 1000
					})
					.ToArray();
			}
		}

		[Table]
		sealed class ChildEntitity
		{
			[Column] public int  Id       { get; set; }
			[Column] public int? ParentId { get; set; }

			[Column] public int  SubId   { get; set; }

			[Association(ThisKey = nameof(SubId), OtherKey = nameof(SubEntitity.Id), CanBeNull = true)]
			public SubEntitity SubItem { get; set; } = null!;

			public static ChildEntitity[] Seed()
			{
				return Enumerable.Range(1, 100)
					.Select(i => new ChildEntitity
					{
						Id = i,
						SubId = i,
						ParentId = (i - 1) / 10 + 1
					})
					.ToArray();
			}
		}

		[Table]
		sealed class SubEntitity
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }

			public static SubEntitity[] Seed()
			{
				return Enumerable.Range(1, 100)
					.Select(i => new SubEntitity
					{
						Id = i,
						Value = i * 20
					})
					.ToArray();
			}
		}

		static void GenericTest<T>(IDataContext db)
		where T: class, ISample
		{
			var result = db.GetTable<T>()
				.LoadWith(e => e.SomeEntities.Where(_ => _.ParentId % 3 == 0).OrderBy(_ => _.Id).Take(2))
				.ThenLoad(c => c.SubItem)
				.ToArray();

			Assert.That(result, Has.Length.EqualTo(10));

			foreach (var item in result)
			{
				var subEntities = item.SomeEntities.ToArray();
				if (item.Id % 3 == 0)
					Assert.That(subEntities, Has.Length.EqualTo(2));
				else
					Assert.That(subEntities, Is.Empty);

				Assert.That(subEntities.Any(s => s.SubItem == null), Is.False);
			}
		}

		[Test]
		public void AssociationSelect([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(SampleClass1.Seed()))
			using (db.CreateLocalTable(SampleClass2.Seed()))
			using (db.CreateLocalTable(ChildEntitity.Seed()))
			using (db.CreateLocalTable(SubEntitity.Seed()))
			{
				GenericTest<SampleClass1>(db);
				GenericTest<SampleClass2>(db);
			}
		}

		abstract class EntityBase
		{
			[PrimaryKey] public int ID { get; set; }
		}

		[Table]
		sealed class Entity : EntityBase
		{
			[Column] public int Value { get; set; }
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/848")]
		public void InsertUsingRuntimeType([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Entity>();

			EntityBase entity = new Entity()
			{
				ID    = 1,
				Value = 2
			};

			db.Insert(entity);

			var record = tb.Single();

			Assert.Multiple(() =>
			{
				Assert.That(record.ID, Is.EqualTo(1));
				Assert.That(record.Value, Is.EqualTo(2));
			});
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/848")]
		public void UpdateUsingRuntimeType([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Entity>();

			var entity = new Entity()
			{
				ID    = 1,
				Value = 2
			};

			db.Insert(entity);

			EntityBase newEntity = new Entity()
			{
				ID    = 1,
				Value = 3
			};

			db.Update(newEntity);

			var record = tb.Single();

			Assert.Multiple(() =>
			{
				Assert.That(record.ID, Is.EqualTo(1));
				Assert.That(record.Value, Is.EqualTo(3));
			});
		}

		[Table("Parent")]
		abstract class DetailsBase
		{
			[Column("ParentID")] public int ID { get; }
		}

		class Details : DetailsBase
		{
			[Column("Value1")] public int? Value { get; }
		}

		class Projection
		{
			public int ID { get; set; }
			public DetailsBase Details { get; set; } = null!;
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1473")]
		public void TransientAbstractMapping([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query = from proj in (from b in (from b in db.GetTable<DetailsBase>() where b.ID == 2 || b.ID == 3 select b)
									  join impl in db.GetTable<Details>() on b.ID equals impl.ID
									  select new { b, Impl = impl })
									  orderby proj.b.ID
						select new Projection { ID = proj.b.ID, Details = proj.Impl };

			var result = query.ToList();

			Assert.That(result, Has.Count.EqualTo(2));

			Assert.Multiple(() =>
			{
				Assert.That(result[0].ID, Is.EqualTo(2));
				Assert.That(result[0].Details.ID, Is.EqualTo(2));
				Assert.That(result[0].Details, Is.TypeOf<Details>());
				Assert.That(((Details)result[0].Details).Value, Is.Null);

				Assert.That(result[1].ID, Is.EqualTo(3));
				Assert.That(result[1].Details.ID, Is.EqualTo(3));
				Assert.That(result[1].Details, Is.TypeOf<Details>());
				Assert.That(((Details)result[1].Details).Value, Is.EqualTo(3));
			});
		}

		class Projection2
		{
			public int ID               { get; set; }
			public Model.Parent Details { get; set; } = null!;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1473")]
		public void MappingWithoutAbstracts([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			// query Details instead of DetailsBase
			var query = from proj in (from b in (from b in db.Parent where b.ParentID == 2 || b.ParentID == 3 select b)
									  join impl in db.Parent on b.ParentID equals impl.ParentID
									  select new { b, Impl = impl })
						orderby proj.b.ParentID
						select new Projection2 { ID = proj.b.ParentID, Details = proj.Impl };

			var result = query.ToList();

			Assert.That(result, Has.Count.EqualTo(2));

			Assert.Multiple(() =>
			{
				Assert.That(result[0].ID, Is.EqualTo(2));
				Assert.That(result[0].Details.ParentID, Is.EqualTo(2));
				Assert.That(result[0].Details.Value1, Is.Null);

				Assert.That(result[1].ID, Is.EqualTo(3));
				Assert.That(result[1].Details.ParentID, Is.EqualTo(3));
				Assert.That(result[1].Details.Value1, Is.EqualTo(3));
			});
		}
	}
}
