using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Internal.Async;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	public class Issue4414Tests : TestBase
	{
		[Table("MessageDto4414")]
		class MessageDto
		{
			[PrimaryKey]
			public int       Id       { get; set; }
			[Column]
			public int       Key      { get; set; }
			[Column]
			public DateTime? Consumed { get; set; }
			[Column]
			public string?   Payload  { get; set; }
		}

		[Test]
		public async Task UpdateWithOutputOrderByNoReturning([IncludeDataSources(TestProvName.AllSqlServer2012Plus, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			using var table = db.CreateLocalTable<MessageDto>();

			var items = await table
				.Where(x => !x.Consumed.HasValue)
				.OrderBy(x => x.Id)
				.Take(1)
				.UpdateWithOutputAsync(
					_ => new MessageDto() { Consumed = Sql.CurrentTimestamp },
					(d, i) => new { i.Id, i.Key, i.Payload }).ToListAsync();
		}
	}
}
