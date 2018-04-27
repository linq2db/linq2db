using System;
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

		[Test, DataContextSource]
		public void Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.DropTable<BackgroundTask>(throwExceptionIfNotExists: false);
				db.CreateTable<BackgroundTask>();
				try
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

					Assert.AreEqual(1, items.Count);
					Assert.AreEqual(1, items[0].PersonID);
					Assert.AreEqual(2, items[0].DurationID);
					Assert.AreEqual(3, items[0].ID);
					Assert.AreEqual(4, items[0].DurationInterval);

				}
				catch 
				{
					db.DropTable<BackgroundTask>(throwExceptionIfNotExists: false);
				}
			}
		}

	}

}
