using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5458Tests : TestBase
	{
		[Table]
		public class MessageEventDTO
		{
			[Column, PrimaryKey] public int EventID { get; set; }
			[Column] public Guid MessageId { get; set; }
			[Column] public Guid? MessageClassId { get; set; }
			[Column] public string? MessageKey { get; set; }
			[Column] public string? MessageLanguage1 { get; set; }
			[Column] public string? MessageLanguage2 { get; set; }
			[Column] public string? MessageLanguage3 { get; set; }
			[Column] public string? MessageLanguage4 { get; set; }
			[Column] public string? TranslatedMessage1 { get; set; }
			[Column] public string? TranslatedMessage2 { get; set; }
			[Column] public string? TranslatedMessage3 { get; set; }
			[Column] public string? TranslatedMessage4 { get; set; }
		}

		[Table]
		public class MessageDTO
		{
			[Column, PrimaryKey] public Guid Id { get; set; }
			[Column] public Guid? AvailabilityGroupId { get; set; }
			[Column] public string? TextName { get; set; }
		}

		[Table]
		public class MessageClassDTO
		{
			[Column, PrimaryKey] public Guid Id { get; set; }
		}


		[Table]
		public class AvailabilityGroupDTO
		{
			[Column, PrimaryKey] public Guid Id { get; set; }
		}

		[Table]
		public class RefMessageEventAvailabilityGroupDTO
		{
			[Column, PrimaryKey] public int EventId { get; set; }
			[Column] public Guid AvailabilityGroupId { get; set; }
		}

		[Table]
		public class MessageAdditionalInformationDTO
		{
			[Column, PrimaryKey] public int EventId { get; set; }
			[Column] public string? Language { get; set; }
			[Column] public string? MessageKey { get; set; }
		}

		[Table]
		public class MessageEventCombinedDTO
		{
			public MessageEventDTO? MessageEventDTO { get; set; }
			public MessageClassDTO? MessageClassDTO { get; set; }
			public MessageDTO? MessageDTO { get; set; }
			public string? Language { get; set; }
			public string? TranslatedMessage { get; set; }
			public AvailabilityGroupDTO? AvailabilityGroup { get; set; }
			public MessageAdditionalInformationDTO? MessageAdditionalInformationDTO { get; set; }
		}

		[Test]
		public void UnionAllCausesWrongQuery([IncludeDataSources(TestProvName.AllSqlServer2019Plus)] string context)
		{
			using var db    = GetDataContext(context);
			using var table1 = db.CreateLocalTable<MessageDTO>([new MessageDTO { Id = TestData.Guid1, TextName = "a"}]);
			using var table2 = db.CreateLocalTable<MessageClassDTO>([new MessageClassDTO { Id = TestData.Guid2 }]);
			using var table3 = db.CreateLocalTable<AvailabilityGroupDTO>();
			using var table4 = db.CreateLocalTable<RefMessageEventAvailabilityGroupDTO>();
			using var table5 = db.CreateLocalTable<AvailabilityGroupDTO>();
			using var table6 = db.CreateLocalTable<MessageAdditionalInformationDTO>();
			using var table7 = db.CreateLocalTable<MessageEventDTO>([new MessageEventDTO { EventID = 123, MessageClassId = TestData.Guid2, MessageId= TestData.Guid1, MessageLanguage1 = "de" }]);
			using var table8 = db.CreateLocalTable<MessageEventDTO>("Common_MessageSystem_EventsA");

			var language = "de";
			var useArchiveTable = true;

			var evtQry = (IQueryable<MessageEventDTO>)db.GetTable<MessageEventDTO>();
			if (useArchiveTable)
				evtQry = evtQry.UnionAll(((ITable<MessageEventDTO>)db.GetTable<MessageEventDTO>()).TableName("Common_MessageSystem_EventsA"));
			var q = from evt in evtQry
					join msg in db.GetTable<MessageDTO>() on evt.MessageId equals msg.Id
					join mclass in db.GetTable<MessageClassDTO>() on evt.MessageClassId equals mclass.Id
					join a in db.GetTable<AvailabilityGroupDTO>() on msg.AvailabilityGroupId equals a.Id into aj
					from a in aj.DefaultIfEmpty()
					join aRef in db.GetTable<RefMessageEventAvailabilityGroupDTO>() on evt.EventID equals aRef.EventId into aRefj
					from aRef in aRefj.DefaultIfEmpty()
					join a2 in db.GetTable<AvailabilityGroupDTO>() on aRef.AvailabilityGroupId equals a2.Id into a2j
					from a2 in a2j.DefaultIfEmpty()
					join msgAddI in db.GetTable<MessageAdditionalInformationDTO>().Where(m => m.Language == language) on evt.MessageKey equals msgAddI.MessageKey into msgAddI2
					from msgAddI in msgAddI2.DefaultIfEmpty()
					select new MessageEventCombinedDTO
					{
						MessageEventDTO = evt,
						MessageClassDTO = mclass,
						MessageDTO = msg,
						Language = language,
						TranslatedMessage = !string.IsNullOrEmpty(evt.MessageLanguage1)
							? (evt.MessageLanguage4 == language ? evt.TranslatedMessage4 : (evt.MessageLanguage3 == language ? evt.TranslatedMessage3 : (evt.MessageLanguage2 == language ? evt.TranslatedMessage2 : evt.TranslatedMessage1)))
							: msg.TextName,
						AvailabilityGroup = a ?? a2,
						MessageAdditionalInformationDTO = msgAddI
					};

			var actual = q
				.ToList();

			Assert.That(actual, Has.Count.EqualTo(1));
		}
	}
}
