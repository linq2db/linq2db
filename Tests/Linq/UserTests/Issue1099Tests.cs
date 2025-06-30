using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	public class Issue1099Tests : TestBase
	{
		public class BackgroundTask : IBackgroundTask
		{
			public int? ID { get; set; }
			public int DurationID { get; set; }
			public int DurationInterval { get; set; }
			public int? PersonID { get; set; }
		}

		public interface IBackgroundTask
		{
			[Column]
			int? ID { get; set; }
			[Column]
			int DurationID { get; set; }
			[Column]
			int DurationInterval { get; set; }
			[Column]
			int? PersonID { get; set; }
		}

		[Test]
		public void Test([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<BackgroundTask>())
				{
					var personId = 1;

					db.Insert(new BackgroundTask
					{
						PersonID = 1,
						DurationID = 2,
						ID = 3,
						DurationInterval = 4
					});

					IQueryable<IBackgroundTask> tasks = db.GetTable<BackgroundTask>();

					var query = from task in tasks
						where task.PersonID == personId
						select task;

					var items = query.ToList();

					Assert.That(items, Has.Count.EqualTo(1));
					using (Assert.EnterMultipleScope())
					{
						Assert.That(items[0].PersonID, Is.EqualTo(1));
						Assert.That(items[0].DurationID, Is.EqualTo(2));
						Assert.That(items[0].ID, Is.EqualTo(3));
						Assert.That(items[0].DurationInterval, Is.EqualTo(4));
					}
				}
			}
		}

	}

}
