using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1965Tests : TestBase
	{
		[Table("Issue1965Person")]
		public class Issue1965Person
		{
			[Column]
			public int Id {get; set;}
			[Column]
			public string? Name {get; set;}
			[Column]
			public int Age {get; set;}
		}

		[Table("Chipcard")]
		public class Chipcard
		{
			[Column]
			public int PersonId {get; set;}
			[Column]
			public DateTime ValidationEnd {get; set;}	
	
//			[Association(ThisKey="PersonId", OtherKey="Id", CanBeNull=true, Relationship=Relationship.ManyToOne, KeyName="FK_CHIPCARD")]
//			public Person PersonData {get; set;}
	
			[Association(QueryExpressionMethod = nameof(Chipcard_Person), CanBeNull = true)]
			public Issue1965Person? PersonData {get; set;}
	
			public static Expression<Func<Chipcard, IDataContext, IQueryable<Issue1965Person>>> Chipcard_Person()
			{
				return (c, db) => db.GetTable<Issue1965Person>().Where(p => p.Id == c.PersonId);
			}
		}

		[Test]
		public void SampleSelectTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Issue1965Person>())
			using (db.CreateLocalTable<Chipcard>())
			{
				var q = db.GetTable<Chipcard>().LoadWith(c => c.PersonData);
	
				var result = q.Where(ka => ka.PersonData != null).ToList();			}
		}
	}
}
