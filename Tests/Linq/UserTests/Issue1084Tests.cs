using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Linq;

namespace Tests.UserTests
{
	public class Issue1084Tests : TestBase
	{
		[Table("i1084_person")]
		sealed class Issue1084Person
		{
			public Issue1084Person(Issue1084Person k)
			{
			}

			[Column] public int Id            { get; set; }
			[Column] public int Number        { get; set; }
			[Column] public int StatusBitmask { get; set; }

			public bool IsBlocked        { get; set; }
			public bool IsBlockedStudent { get; set; }
		}

		[Table("i1084_person")]
		sealed class Issue1084Personv2
		{
			public Issue1084Personv2()
			{
			}

			[Column] public int Id            { get; set; }
			[Column] public int Number        { get; set; }
			[Column] public int StatusBitmask { get; set; }

			public bool IsBlocked        { get; set; }
			public bool IsBlockedStudent { get; set; }
		}

		[Table("i1084_person")]
		sealed class Issue1084Personv3
		{
			public bool Default;
			public bool Copy;
			public Issue1084Personv3()
			{
				Default = true;
			}

			public Issue1084Personv3(Issue1084Personv3 k)
			{
				Copy = true;
			}

			[Column] public int Id            { get; set; }
			[Column] public int Number        { get; set; }
			[Column] public int StatusBitmask { get; set; }

			public bool IsBlocked        { get; set; }
			public bool IsBlockedStudent { get; set; }

			public static Issue1084Personv3[] Data { get; } = new[]
			{
				new Issue1084Personv3() { Id = 1, Number = 1 },
				new Issue1084Personv3() { Id = 2, Number = 2 }
			};
		}

		[Table("i1084_student")]
		sealed class Issue1084Student
		{
			[Column] public int     Id            { get; set; }
			[Column] public string? Number        { get; set; }
			[Column] public int     StatusBitmask { get; set; }

			public static Issue1084Student[] Data { get; } = new[]
			{
				new Issue1084Student() { Id = 1, Number = "1" },
				new Issue1084Student() { Id = 2, Number = "2" }
			};
		}

		[Test]
		public void TestInstantiation([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Issue1084Personv3.Data))
			using (db.CreateLocalTable(Issue1084Student.Data))
			{
				var result = from k in db.GetTable<Issue1084Personv3>()
							 join ks in db.GetTable<Issue1084Student>() on
								Tuple.Create(k.Id, k.Number.ToString()) equals Tuple.Create(ks.Id, ks.Number)
								into joinedTable
							 from g in joinedTable.DefaultIfEmpty()
							 select new Issue1084Personv3(k)
							 {
								 IsBlocked        = (k.StatusBitmask & 0x80) != 0,
								 IsBlockedStudent = (g.StatusBitmask & 0x80) != 0
							 };

				var res = result.ToList();
				Assert.That(res, Has.Count.EqualTo(2));
				Assert.Multiple(() =>
				{
					Assert.That(res[0].Default, Is.False);
					Assert.That(res[1].Default, Is.False);
					Assert.That(res[0].Copy, Is.True);
					Assert.That(res[1].Copy, Is.True);
				});
			}
		}

		[Test]
		public void TestTupleFactoryWithConstructorWithParameters([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Issue1084Person>())
			using (db.CreateLocalTable<Issue1084Student>())
			{
				var result = from k  in db.GetTable<Issue1084Person>()
							 join ks in db.GetTable<Issue1084Student>() on
								Tuple.Create(k.Id, k.Number.ToString()) equals Tuple.Create(ks.Id, ks.Number)
								into joinedTable
							 from g in joinedTable.DefaultIfEmpty()
							 select new Issue1084Person(k)
							 {
								 IsBlocked        = (k.StatusBitmask & 0x80) != 0,
								 IsBlockedStudent = (g.StatusBitmask & 0x80) != 0
							 };

				// because for k we need default constructor, which is missing
				Assert.Throws<InvalidOperationException>(() => result.ToList());
			}
		}

		[Test]
		public void TestTupleFactoryWithDefaultConstructor([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Issue1084Personv2>())
			using (db.CreateLocalTable<Issue1084Student>())
			{
				var result = from k  in db.GetTable<Issue1084Person>()
							 join ks in db.GetTable<Issue1084Student>() on
								Tuple.Create(k.Id, k.Number.ToString()) equals Tuple.Create(ks.Id, ks.Number)
								into joinedTable
							 from g in joinedTable.DefaultIfEmpty()
							 select new Issue1084Personv2()
							 {
								 IsBlocked        = (k.StatusBitmask & 0x80) != 0,
								 IsBlockedStudent = (g.StatusBitmask & 0x80) != 0
							 };

				result.ToList();
			}
		}

		[Test]
		public void TestTupleConstructorWithDefaultConstructor([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Issue1084Personv2>())
			using (db.CreateLocalTable<Issue1084Student>())
			{
				var result = from k in db.GetTable<Issue1084Person>()
							 join ks in db.GetTable<Issue1084Student>() on
								Tuple.Create(k.Id, k.Number.ToString()) equals Tuple.Create(ks.Id, ks.Number)
								into joinedTable
							 from g in joinedTable.DefaultIfEmpty()
							 select new Issue1084Personv2()
							 {
								 IsBlocked = (k.StatusBitmask & 0x80) != 0,
								 IsBlockedStudent = (g.StatusBitmask & 0x80) != 0
							 };

				result.ToList();
			}
		}
	}
}
