using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class InterfaceTests : TestBase
	{
		[Test]
		public void Test([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent2
					group p by p.ParentID into gr
					select new
					{
						Count = gr.Count()
					};

				var _ = q.ToList();
			}
		}

		#region Issue 4082
		public interface IIdentifiable
		{
			int Id { get; }
		}

		[Table]
		public class UserAccount : IIdentifiable
		{
			[PrimaryKey] public int     Id   { get; set; }
			[Column    ] public string? Name { get; set; }
		}

		[Test]
		public void Issue4082([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			using var db = GetDataContext(context);
			using var t = db.CreateLocalTable<UserAccount>(new[]{ new UserAccount() { Id = 1, Name = "old_name" }, new UserAccount() { Id = 2, Name = "old_name" } });

			var results = ((IQueryable<IIdentifiable>)db.GetTable<UserAccount>())
				.Where(x => x.Id == 1)
				.ToArray();

			Assert.AreEqual(1, results.Length);
			Assert.AreEqual(1, results[0].Id);
		}
		#endregion
	}
}
