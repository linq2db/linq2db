using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.Mapping;

using Npgsql;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1438Tests : TestBase
	{
		public class Client
		{
			public int Id { get; set; }

			public bool Has { get; set; }
		}

		[Test]
		public void GeneralTest([DataSources(TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<Client>()
					.HasTableName("Issue1438")
					.Property(x => x.Id)
						.IsPrimaryKey()
						.IsIdentity()
				.Build();

			using (var db = GetDataContext(context, ms))
			{
				using (var tbl = db.CreateLocalTable<Client>())
				{
					var id = db.InsertWithInt32Identity(new Client()
					{
						Has = true
					});

					var record = tbl.Where(_ => _.Id == id).Single();
					using (Assert.EnterMultipleScope())
					{
						Assert.That(record.Id, Is.EqualTo(id));
						Assert.That(record.Has, Is.True);
					}
				}
			}
		}

		[Test]
		public void SpecificTest([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values] bool avoidProviderSpecificApi)
		{
			var provider = PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v95);
			var cs       = DataConnection.GetConnectionString(context);

			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<Client>()
					.HasTableName("Issue1438")
					.Property(x => x.Id)
						.IsPrimaryKey()
						.IsIdentity()
				.Build();

			using (var cn = new NpgsqlConnection(cs))
			using (var db = new DataConnection(new DataOptions().UseConnection(provider, cn)))
			{
				db.AddMappingSchema(ms);
				using (var tbl = db.CreateLocalTable<Client>())
				{
					var id = db.InsertWithInt32Identity(new Client()
					{
						Has = true
					});

					var record = tbl.Where(_ => _.Id == id).Single();
					using (Assert.EnterMultipleScope())
					{
						Assert.That(record.Id, Is.EqualTo(id));
						Assert.That(record.Has, Is.True);
					}
				}
			}
		}
	}
}
