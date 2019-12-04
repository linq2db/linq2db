using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1990Tests : TestBase
	{
		public class Issue
		{
			public int Id { get; set; }

			public string Name { get; set; }

			public static void Map(FluentMappingBuilder fmb)
			{
				fmb.Entity<Issue>()
					.HasTableName("issues")
					.Property(x => x.Name).HasColumnName("name").IsNullable()
					.Property(x => x.Id).HasColumnName("id").IsPrimaryKey();
			}
		}

		[Test]
		public void Test([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();
			var builder = ms.GetFluentMappingBuilder();
			Issue.Map(builder);
			using (var dc = GetDataContext(context, ms))
			{
				try { dc.DropTable<Issue>(); } catch { }
				dc.CreateTable<Issue>();
				var tagname = "";
				var query = from i in dc.GetTable<Issue>()
					where
						i.Id == 478356 
						&& !string.IsNullOrEmpty(tagname) ? i.Name == tagname : true
					select new { Issue = i };
				query.ToList();
				var sql = ((DataConnection)dc).LastQuery;

				Assert.IsTrue(sql.Contains("478356"));
			}
		}
	}
}
