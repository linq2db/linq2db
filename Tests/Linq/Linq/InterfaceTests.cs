using System.Linq;
using LinqToDB;
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

		#region Issue 3034
		interface IA
		{
			int Id { get; set; }
		}

		interface IB : IA
		{
			string Name { get; set; }
		}

		sealed class MyTable
		{
			public int     Id   { get; set; }
			public string? Name { get; set; }
		}

		[Test]
		public void Issue3034([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			using var db = GetDataContext(context);
			using var t = db.CreateLocalTable<MyTable>(new[]{ new MyTable() { Id = 1, Name = "old_name" }, new MyTable() { Id = 2, Name = "old_name" } });

			db.GetTable<IB>().TableName("MyTable")
				.Where(x => x.Id == 1)
				.Set(x => x.Name, x => "new_name")
				.Update();

			var results = t.OrderBy(r => r.Id).ToArray();

			Assert.AreEqual(2, results.Length);
			Assert.AreEqual(1, results[0].Id);
			Assert.AreEqual("new_name", results[0].Name);
			Assert.AreEqual(2, results[1].Id);
			Assert.AreEqual("old_name", results[1].Name);
		}
		#endregion
	}
}
