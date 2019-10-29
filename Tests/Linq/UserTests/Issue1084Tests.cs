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
		class Issue1084Person
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
		class Issue1084Personv2
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

		[Table("i1084_student")]
		class Issue1084Student
		{
			[Column] public int    Id            { get; set; }
			[Column] public string Number        { get; set; }
			[Column] public int    StatusBitmask { get; set; }
		}

		[ActiveIssue(1084)]
		[Test]
		public void TestConstructorWithParameters([DataSources] string context)
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

				result.ToList();
			}
		}

		[Test]
		public void TestDefaultConstructor([DataSources] string context)
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
	}
}
