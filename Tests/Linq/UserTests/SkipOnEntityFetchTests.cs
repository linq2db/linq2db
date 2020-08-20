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
			[Column("PersonID", SkipOnEntityFetch=true)]
			public int? ID;

			[Column]
			public string? FirstName { get; set; }

			[Column]
			public string? LastName { get; set; }
		}

		public void SelectFullEntityWithSkipColumn([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var allPeople = db.GetTable<PersonEx>().ToArray();
				var anyGotId = allPeople.Any(p => p.ID != null);
				Assert.IsFalse(anyGotId);

				var allPeopleWithCondition = db.GetTable<PersonEx>()
												.Where(p => (p.ID ?? 0) >= 2)
												.ToArray();
				var anyWithCondGotId = allPeopleWithCondition.Any(p => p.ID != null);
				Assert.IsFalse(anyWithCondGotId);

				var allAnonymWithExplicitSelect = db.GetTable<PersonEx>()
													.Select(p => new {p.ID, p.FirstName,p.LastName});
				var allAnonymGotId = allAnonymWithExplicitSelect.All(p => p.ID != null);
				Assert.IsTrue(allAnonymGotId);

				var allPeopleWithExplicitSelect = db.GetTable<PersonEx>()
													.Select(p => new PersonEx()
													{
														ID = p.ID,
														FirstName = p.FirstName,
														LastName = p.LastName
													});
				var allExplicitGotId = allPeopleWithExplicitSelect.All(p => p.ID != null);
				Assert.IsTrue(allExplicitGotId);
			}
		}
	}
}
