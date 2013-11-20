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
		public void CreateLocalTempTable1([IncludeDataContexts(
			ProviderName.SqlServer2008,
			//ProviderName.DB2,
			ExcludeLinqService = true)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.MappingSchema.GetFluentMappingBuilder()
					.Entity<TestTable>()
						.Property(t => t.Field1)
							.HasLength(50);

				const string tableName = "TestTable";

				try
				{
					switch (context)
					{
						case ProviderName.SqlServer2008 : db.DropTable<TestTable>("#" + tableName); break;
						default                         : db.DropTable<TestTable>(tableName);       break;
					}
				}
				catch (Exception)
				{
				}

				ITable<TestTable> table;

				switch (context)
				{
					case ProviderName.SqlServer2008 :
						table = db.CreateTable<TestTable>("#" + tableName);
						break;
					case ProviderName.DB2 :
						table = db.CreateTable<TestTable>(statementHeader:"DECLARE GLOBAL TEMPORARY TABLE SESSION.{0}");
						break;
					default:
						throw new InvalidOperationException();
				}

				var list = table.ToList();

				table.Drop();
			}
		}
	}
}
