using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class SkipOnEntityFetchTests : TestBase
	{
		[Table("Person")]
		public class PersonEx
		{
			[Column("PersonID", SkipOnEntityFetch = true)]
			public int? ID;

			[Column]
			public string? FirstName { get; set; }

			[Column]
			public string? LastName { get; set; }
		}

		public class Attachment
		{
			[Column, PrimaryKey]
			public long Id { get; set; }

			[Column]
			public long QuestionSetId { get; set; }

			[Column(SkipOnEntityFetch = true)]
			public byte[] Content { get; set; } = null!;

			[Column]
			public string Question { get; set; } = null!;

			[Column]
			public string FileName { get; set; } = null!;

			[Column]
			public long FileSize { get; set; }

			[Column]
			public string ContentType { get; set; } = null!;
		}

		[Test]
		public void SelectFullEntityWithSkipColumn([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var allPeople = db.GetTable<PersonEx>().ToArray();
				var anyGotId = allPeople.Any(p => p.ID != null);
				Assert.That(anyGotId, Is.False);

				var allPeopleWithCondition = db.GetTable<PersonEx>()
					.Where(p => (p.ID ?? 0) >= 2)
					.ToArray();
				var anyWithCondGotId = allPeopleWithCondition.Any(p => p.ID != null);
				Assert.That(anyWithCondGotId, Is.False);

				var allAnonymWithExplicitSelect = db.GetTable<PersonEx>()
					.Select(p => new { p.ID, p.FirstName, p.LastName });
				var allAnonymGotId = allAnonymWithExplicitSelect.All(p => p.ID != null);
				Assert.That(allAnonymGotId, Is.True);

				var allPeopleWithExplicitSelect = db.GetTable<PersonEx>()
					.Select(p => new PersonEx { ID = p.ID, FirstName = p.FirstName, LastName = p.LastName });
				var allExplicitGotId = allPeopleWithExplicitSelect.All(p => p.ID != null);
				Assert.That(allExplicitGotId, Is.True);
			}
		}

		[Test]
		public void FirstOrDefaultTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var data = new[]
			{
				new Attachment
				{
					Content = new byte[] { 1, 2, 3 },
					ContentType = "Some",
					FileName = "SomeFile",
					FileSize = 128,
					Id = 1,
					Question = "SomeQuestion",
					QuestionSetId = 11
				}
			};

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var attachment = db.GetTable<Attachment>().FirstOrDefault(x => x.Id == 1);

				Assert.That(attachment, Is.Not.Null);
				Assert.That(attachment!.Content, Is.Null);
			}
		}

	}
}
