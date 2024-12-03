using System.Linq;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3432Tests : TestBase
	{
		[Table]
		public class Task
		{
			[Column] public int     AdminPartyId { get; set; }
			[Column] public string? Description  { get; set; }
		}

		[Table]
		public class Party
		{
			[Column] public int     Id   { get; set; }
			[Column] public string? Name { get; set; }
		}

		[Table]
		public class PartyAccess
		{
			[Column] public int     PartyId { get; set; }
			[Column] public string? Role    { get; set; }
		}

		[Test]
		public void OuterApplyOptimization([DataSources] string context)
		{
			const string Admin = "Admin";

			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Task>();
			using var t2 = db.CreateLocalTable<Party>();
			using var t3 = db.CreateLocalTable<PartyAccess>();

			var query =
				from task in db.GetTable<Task>()
				from party in db.GetTable<Party>()
					.Where(p => task.AdminPartyId == p.Id || db.GetTable<PartyAccess>().Any(pa => pa.PartyId == p.Id && pa.Role == Admin))
					.DefaultIfEmpty()
				select new { task.Description, party.Name };

			_ = query.ToArray();
		}
	}
}
