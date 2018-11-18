using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

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
	}
}
