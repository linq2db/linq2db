using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5405Tests : TestBase
	{
		[Table]
		public class MessageEventDTO
		{
			[Column, PrimaryKey]
			public Guid Id { get; set; }

			[Column]
			public Guid MessageId { get; set; }
		}

		[Table]
		public class MessageDTO
		{
			[Column, PrimaryKey]
			public Guid Id { get; set; }
		}

		public class MessageEventCombinedWithoutTranslationDTO
		{
			public MessageEventDTO? Evt { get; set; }

			public MessageDTO? Msg { get; set; }
		}

		[Test]
		public void DeleteWithProjection([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			using var evtTable = db.CreateLocalTable<MessageEventDTO>([new MessageEventDTO() { Id = TestData.Guid1, MessageId = TestData.Guid2 }]);
			using var refTable = db.CreateLocalTable<MessageDTO>([new MessageDTO() { Id = TestData.Guid2 }]);

			var q = from evt in db.GetTable<MessageEventDTO>()
					join msg in db.GetTable<MessageDTO>() on evt.MessageId equals msg.Id into msgg
					from msg in msgg.DefaultIfEmpty()
					select new MessageEventCombinedWithoutTranslationDTO
					{
						Evt = evt,
						Msg = msg
					};

			var res = q.Delete();

			Assert.That(res, Is.EqualTo(1));
		}
	}
}
