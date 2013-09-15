using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.DDL
{
	[TestFixture]
	public class CreateTableTest : TestBase
	{
		class TestTable
		{
			public int    ID;
			public string Field1;
		}

		[Test]
		public void CreateTable1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.MappingSchema.GetFluentMappingBuilder()
					.Entity<TestTable>()
						.Property(t => t.ID)
							.IsIdentity()
							.IsPrimaryKey()
						.Property(t => t.Field1)
							.HasLength(50);

				try
				{
					db.DropTable<TestTable>();
				}
				catch (Exception)
				{
				}

				var table = db.CreateTable<TestTable>();
				var list = table.ToList();

				db.DropTable<TestTable>();
			}
		}

		[Test]
		public void CreateTable2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.MappingSchema.GetFluentMappingBuilder()
					.Entity<TestTable>()
						.Property(t => t.ID)
							.IsIdentity()
							.IsPrimaryKey()
						.Property(t => t.Field1)
							.HasLength(50);

				try
				{
					db.DropTable<TestTable>();
				}
				catch (Exception)
				{
				}

				var table = db.CreateLocalTempTable<TestTable>();
				var list = table.ToList();

				table.Drop();
			}
		}

		[Test]
		public void CreateLocalTempTable1([DataContexts(
			ProviderName.DB2,
			ExcludeLinqService = true)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.MappingSchema.GetFluentMappingBuilder()
					.Entity<TestTable>()
						.IsLocalTempTable()
						.Property(t => t.ID)
							.IsIdentity()
							.IsPrimaryKey()
						.Property(t => t.Field1)
							.HasLength(50);

				try
				{
					db.DropTable<TestTable>();
				}
				catch (Exception)
				{
				}

				var table = db.CreateLocalTempTable<TestTable>();
				var list = table.ToList();

				table.Drop();
			}
		}

		[Test]
		public void CreateGlobalTempTable1([DataContexts(
			ProviderName.DB2,
			ExcludeLinqService = true)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.MappingSchema.GetFluentMappingBuilder()
					.Entity<TestTable>()
						.IsGlobalTempTable()
						.Property(t => t.ID)
							.IsIdentity()
							.IsPrimaryKey()
						.Property(t => t.Field1)
							.HasLength(50);

				try
				{
					db.DropTable<TestTable>();
				}
				catch (Exception)
				{
				}

				var table = db.CreateTable<TestTable>();
				var list = table.ToList();

				table.Drop();
			}
		}
	}
}
