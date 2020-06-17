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
		class SampleClass1 : ISample
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ChildEntitity.ParentId), CanBeNull = true)]
			public IEnumerable<ChildEntitity> SomeEntities { get; set; }

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
		class SampleClass2 : ISample
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ChildEntitity.ParentId), CanBeNull = true)]
			public IEnumerable<ChildEntitity> SomeEntities { get; set; }

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
		class ChildEntitity
		{
			[Column] public int  Id       { get; set; }
			[Column] public int? ParentId { get; set; }

			[Column] public int  SubId   { get; set; }


			[Association(ThisKey = nameof(SubId), OtherKey = nameof(SubEntitity.Id), CanBeNull = true)]
			public SubEntitity SubItem { get; set; }

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
		class SubEntitity
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

			Assert.That(result.Length, Is.EqualTo(10));

			foreach (var item in result)
			{
				var subEntities = item.SomeEntities.ToArray();
				if (item.Id % 3 == 0)
					Assert.That(subEntities.Length, Is.EqualTo(2));
				else
					Assert.That(subEntities.Length, Is.EqualTo(0));

				Assert.That(subEntities.Any(s => s.SubItem == null), Is.False);
			}
		}

		[Test]
		public void AssociationSelect([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (new AllowMultipleQuery())
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
	}
}
