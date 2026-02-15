using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1403Tests : TestBase
	{
		public abstract class ModelBase
		{
		}

		[Table("Issue1403Tests_1")]
		public class MyClass1 : ModelBase
		{
			[Column("event_id", Order = 1), PrimaryKey, NotNull]
			public int EventId { get; set; }

			[Column("event_description", Order = 2), NotNull]
			public string EventDescription { get; set; } = null!;
		}

		[Table("Issue1403Tests_2")]
		public class MyClass2
		{
			[Column("event_id", Order = 1), PrimaryKey, NotNull]
			public int EventId { get; set; }

			[Column("event_description", Order = 2), NotNull]
			public string EventDescription { get; set; } = null!;
		}

		[Table("Issue1403Tests_3")]
		public class MyClass3 : ModelBase
		{
			[Column("event_id"), PrimaryKey, NotNull]
			public int EventId { get; set; }

			[Column("event_description"), NotNull]
			public string EventDescription { get; set; } = null!;
		}

		[Test]
		public void Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<MyClass1>();
			table.Insert(() => new MyClass1()
			{
				EventId = 1,
				EventDescription = "New event"
			});

			var events = table.First();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(events.EventId, Is.EqualTo(1));
				Assert.That(events.EventDescription, Is.EqualTo("New event"));
			}
		}

		[Test]
		public void Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<MyClass2>();
			table.Insert(() => new MyClass2()
			{
				EventId = 1,
				EventDescription = "New event"
			});

			var events = table.First();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(events.EventId, Is.EqualTo(1));
				Assert.That(events.EventDescription, Is.EqualTo("New event"));
			}
		}

		[Test]
		public void Test3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<MyClass3>();
			table.Insert(() => new MyClass3()
			{
				EventId = 1,
				EventDescription = "New event"
			});

			var events = table.First();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(events.EventId, Is.EqualTo(1));
				Assert.That(events.EventDescription, Is.EqualTo("New event"));
			}
		}
	}
}
