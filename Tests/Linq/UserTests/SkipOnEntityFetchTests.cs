using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class SkipOnEntityFetchTests : TestBase
	{
		[Table("Person")]
		public class PersonEx
		{
			[Column(SkipOnEntityFetch = true)]
			public string? FirstName { get; set; }

			[Column]
			public string? LastName { get; set; }

			[Column("PersonID")]
			public int? ID;
		}

		[Test]
		public void SelectFullEntityWithSkipColumn([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var allPeople = db.GetTable<PersonEx>().ToArray();
				var anyGotFirstName = allPeople.Any(p => p.FirstName != null);
				Assert.IsFalse(anyGotFirstName);

				var allPeopleWithCondition = db.GetTable<PersonEx>()
												.Where(p => (p.ID ?? 0) >= 2)
												.ToArray();
				var anyWithCondGotFirstName = allPeopleWithCondition.Any(p => p.FirstName != null);
				Assert.IsFalse(anyWithCondGotFirstName);

				var allAnonymWithExplicitSelect = db.GetTable<PersonEx>()
													.Select(p => new {p.ID, p.FirstName, p.LastName});
				var allAnonymGotFirstName = allAnonymWithExplicitSelect.All(p => p.FirstName != null);
				Assert.IsTrue(allAnonymGotFirstName);

				var allPeopleWithExplicitSelect = db.GetTable<PersonEx>()
													.Select(p => new PersonEx()
													{
														ID = p.ID,
														FirstName = p.FirstName,
														LastName = p.LastName
													});
				var allExplicitGotFirstName = allPeopleWithExplicitSelect.All(p => p.FirstName != null);
				Assert.IsTrue(allExplicitGotFirstName);
			}
		}
	}
}
