using System;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;

using NUnit.Framework;
using Tests.Model;

namespace Tests.xUpdate
{
	[TestFixture]
	[Order(10000)]
	public class CreateTempTableTests : TestBase
	{
		class IDTable
		{
			public int ID;
		}

		[Test]
		public void CreateTable1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.DropTable<int>("TempTable", throwExceptionIfNotExists:false);

				using (var tmp = db.CreateTempTable(
					"TempTable",
					db.Parent.Select(p => new IDTable { ID = p.ParentID })))
				{
					var list =
					(
						from p in db.Parent
						join t in tmp on p.ParentID equals t.ID
						select t
					).ToList();
				}
			}
		}

		[Test]
		public void CreateTable2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.DropTable<int>("TempTable", throwExceptionIfNotExists:false);

				using (var tmp = db.CreateTempTable(
					"TempTable",
					db.Parent.Select(p => new { ID = p.ParentID })))
				{
					var list =
					(
						from p in db.Parent
						join t in tmp on p.ParentID equals t.ID
						select t
					).ToList();
				}
			}
		}

		[Test]
		public void CreateTable3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.DropTable<int>("TempTable", throwExceptionIfNotExists:false);

				using (var tmp = db.CreateTempTable(
					"TempTable",
					db.Parent.Select(p => new { ID = p.ParentID }),
					em => em
						.Property(e => e.ID)
							.IsPrimaryKey()))
				{
					var list =
					(
						from p in db.Parent
						join t in tmp on p.ParentID equals t.ID
						select t
					).ToList();
				}
			}
		}

		[Test]
		public void CreateTableEnumerable([DataSources(false)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.DropTable<int>("TempTable", throwExceptionIfNotExists: false);

				using (var tmp = db.CreateTempTable(
					"TempTable",
					db.Parent.Select(p => new IDTable { ID = p.ParentID }).ToList()))
				{
					var list =
					(
						from p in db.Parent
						join t in tmp on p.ParentID equals t.ID
						select t
					).ToList();
				}
			}
		}

		[Test]
		public async Task CreateTableAsync([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.DropTable<int>("TempTable", throwExceptionIfNotExists: false);

#if !NETFRAMEWORK
				await
#endif
				using (var tmp = await db.CreateTempTableAsync(
					"TempTable",
					db.Parent.Select(p => new IDTable { ID = p.ParentID })))
				{
					var list =
					(
						from p in db.Parent
						join t in tmp on p.ParentID equals t.ID
						select t
					).ToList();
				}
			}
		}

		[Test]
		public async Task CreateTableAsyncEnumerable([DataSources(false)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.DropTable<int>("TempTable", throwExceptionIfNotExists: false);

#if !NETFRAMEWORK
				await
#endif
				using (var tmp = await db.CreateTempTableAsync(
					"TempTable",
					db.Parent.Select(p => new IDTable { ID = p.ParentID }).ToList()))
				{
					var list =
					(
						from p in db.Parent
						join t in tmp on p.ParentID equals t.ID
						select t
					).ToList();
				}
			}
		}
	}
}
