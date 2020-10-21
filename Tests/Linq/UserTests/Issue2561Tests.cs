using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2561Tests : TestBase
	{
		enum Issue2561ScriptType
		{
			Type1
		}

		class Issue2561Class
		{
			public Guid Id { get; set; }
			public string? Name { get; set; }
			public long Archive { get; set; }
			public Issue2561ScriptType ScriptType { get; set; }
			public string? Script { get; set; }
		}

		[Test]
		public void TestOracle([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			ReservedWords.Add("ARCHIVE", ProviderName.OracleManaged);
			ReservedWords.Add("ARCHIVE", ProviderName.Oracle);
			ReservedWords.Add("ARCHIVE", ProviderName.OracleNative);

			var createTable = "CREATE TABLE Common_Scripts (Id RAW(16), \"Archive\" NUMBER(20,0) NOT NULL, Name NVARCHAR2(255) NOT NULL, ScriptType NUMBER(10,0) NOT NULL, Script NCLOB NULL)";
			var ms = new MappingSchema();

			ms.GetFluentMappingBuilder()
				.Entity<Issue2561Class>()
				.HasTableName("Common_Scripts")
				.Property(e => e.Id).IsPrimaryKey()
				.Property(e => e.Archive).IsPrimaryKey()				
				.Property(e => e.Name)
				.Property(e => e.ScriptType)
				.Property(e => e.Script).HasDataType(DataType.NText);


			using (var db = GetDataContext(context, ms))
			{
				db.DropTable<Issue2561Class>(throwExceptionIfNotExists: false);
				((DataConnection)db).Execute(createTable);

				var l = db.GetTable<Issue2561Class>().ToList();

				var dto = new Issue2561Class() {Id=Guid.NewGuid(), Script="aa".PadLeft(2500,'a') };
				db.Insert(new[] { dto });

				var c = "aaa";
				var q = db.GetTable<Issue2561Class>()
						  .Where(x =>
									x.Archive == 0 &&
									x.ScriptType == Issue2561ScriptType.Type1 &&
									x.Script == c)
						  .FirstOrDefault();

				c = "aaa".PadLeft(2000, 'b');
				var q2 = db.GetTable<Issue2561Class>()
						  .Where(x =>
									x.Archive == 0 &&
									x.ScriptType == Issue2561ScriptType.Type1 &&
									x.Script == c)
						  .FirstOrDefault();
			}
		}

		[Test]
		public void TestPostgres([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var ms = new MappingSchema();

			ms.GetFluentMappingBuilder()
				.Entity<Issue2561Class>()
				.HasTableName("Common_Scripts")
				.Property(e => e.Id).IsPrimaryKey()
				.Property(e => e.Archive).IsPrimaryKey()
				.Property(e => e.Name)
				.Property(e => e.ScriptType)
				.Property(e => e.Script).HasDataType(DataType.NText);


			using (var db = GetDataContext(context, ms))
			{
				db.DropTable<Issue2561Class>(throwExceptionIfNotExists: false);
				db.CreateTable<Issue2561Class>();

				var l = db.GetTable<Issue2561Class>().ToList();

				var dto = new Issue2561Class() {Id=Guid.NewGuid(), Script="aa".PadLeft(2500,'a') };
				db.Insert(new[] { dto });

				var b = "b";
				var c = "aaa";
				var q = db.GetTable<Issue2561Class>()
						  .Where(x =>
									x.Archive == 0 &&
									x.ScriptType == Issue2561ScriptType.Type1 &&
									x.Script == c)
						  .FirstOrDefault();

				c = "aaa".PadLeft(2000, 'b');
				var q2 = db.GetTable<Issue2561Class>()
						  .Where(x =>
									x.Archive == 0 &&
									x.ScriptType == Issue2561ScriptType.Type1 &&
									x.Script == c)
						  .FirstOrDefault();
			}
		}
	}
}
