using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1900Tests : TestBase
	{
		[Table("PERSON_1900")]
		public class PersonTest
		{
			[Column("AGE")]
			public int Age {get; set;}
			[Column("NAME")]
			public string? Name {get; set;}

			[ExpressionMethod(nameof(GetTypeExpr), IsColumn = true)]
			public int Type {get; set;}

			public static Expression<Func<PersonTest, int>> GetTypeExpr()
			{
				return p => Sql.Property<int>(p, "OPTIONS") & 0xF;
			}
		}

		[Table("PERSON_1900")]
		public class PersonPrototype
		{
			[Column("AGE")]
			public int Age {get; set;}
			[Column("NAME")]
			public string? Name {get; set;}

			[Column("OPTIONS")]
			public int Options { get; set; }
		}

		[Test]
		public void TestDynamicInColumns([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(new []{ new PersonPrototype
			{
				Name = "Some",
				Age = 10,
				Options = 4
			}}))
			{
				var result = db.GetTable<PersonTest>().Where(p => p.Type == 4 || p.Type == 5).ToList();
				Assert.That(result, Has.Count.EqualTo(1));
			}
		}
	}
}
