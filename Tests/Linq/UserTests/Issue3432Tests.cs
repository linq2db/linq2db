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

		[Ignore("Not more applicable. Optimizer choose APPLY join when needed automatically.")]
		[Test]
		public void OuterApplyOptimization([IncludeDataSources(TestProvName.AllSqlServer)] string context, [Values]bool preferApply)
		{
			const string Admin = "Admin";

			using(new PreferApply(preferApply))
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Task>())
			using (db.CreateLocalTable<Party>())
			using (db.CreateLocalTable<PartyAccess>())
			{
				var query =
					from task in db.GetTable<Task>()
					from party in db.GetTable<Party>()
						.Where(p => task.AdminPartyId == p.Id ||
						            db.GetTable<PartyAccess>().Any(pa => pa.PartyId == p.Id && pa.Role == Admin))
						.DefaultIfEmpty()
					select new { task.Description, party.Name };

				_ = query.ToArray();
				var sql = query.ToString();

				if (preferApply)
				{
					sql.Should().Contain("OUTER APPLY");
				}
				else
				{
					sql.Should().NotContain("OUTER APPLY");
				}
			}
		}
	}
}
