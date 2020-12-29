using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2743Tests : TestBase
	{
		[Table]
		class MessageEventDTO
		{
			[PrimaryKey]
			public int Id { get; set; }
		}

		class MessageEventCombinedWithoutTranslationDTO
		{
			public virtual MessageEventDTO? MessageEventDTO { get; set; }

		}

		[Test]
		public void IssueTestInsertViaSelect([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<MessageEventDTO>())
				using (db.CreateLocalTable<MessageEventDTO>(tableName: "MessageEventDTOTb2"))
				{
					var evtQry = db.GetTable<MessageEventDTO>();
					var q = from evt in evtQry
							select new MessageEventCombinedWithoutTranslationDTO
							{
								MessageEventDTO = evt,
							};

					var queryDelete = q.Select(x => x.MessageEventDTO);

					var destination = db.GetTable<MessageEventDTO>().TableName( "MessageEventDTOTb2");

					queryDelete.Insert(destination, x => x);
				}
			}
		}

		[Test]
		public void IssueTestDeleteViaSelect([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable<MessageEventDTO>())
				using (db.CreateLocalTable<MessageEventDTO>(tableName: "MessageEventDTOTb2"))
				{
					var evtQry = db.GetTable<MessageEventDTO>();
					var q = from evt in evtQry
							select new MessageEventCombinedWithoutTranslationDTO
							{
								MessageEventDTO = evt,
							};

					var queryDelete = q.Select(x => x.MessageEventDTO);

					var rows = db.Delete(queryDelete);
				}
			}
		}
	}
}
