using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

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
		public void CreateTable([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.MappingSchema.GetFluentMappingBuilder()
					.Entity<TestTable>()
						.Property(t => t.ID)
							.IsIdentity()
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
	}
}
