using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.PostgreSQL;
using Npgsql;
using NUnit.Framework;
using System.Linq;

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
		public void GeneralTest([DataSources(ProviderName.Sybase, ProviderName.OracleNative)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.MappingSchema.GetFluentMappingBuilder()
					.Entity<Client>()
						.HasTableName("Issue1438")
						.Property(x => x.Id)
							.IsPrimaryKey()
							.IsIdentity();

				using (var tbl = db.CreateLocalTable<Client>())
				{
					var id = db.InsertWithInt32Identity(new Client()
					{
						Has = true
					});

					var record = tbl.Where(_ => _.Id == id).Single();

					Assert.AreEqual(id, record.Id);
					Assert.True(record.Has);
				}
			}
		}

		[Test]
		public void SpecificTest([IncludeDataSources(false, ProviderName.PostgreSQL)] string context, [Values] bool avoidProviderSpecificApi)
		{
			using (new AvoidSpecificDataProviderAPI(avoidProviderSpecificApi))
			{
				var provider = new PostgreSQLDataProvider(PostgreSQLVersion.v95);
				var cs = DataConnection.GetConnectionString(context);
				using (var cn = new NpgsqlConnection(cs))
				using (var db = new DataConnection(provider, cn))
				{
					db.MappingSchema.GetFluentMappingBuilder()
						.Entity<Client>()
							.HasTableName("Issue1438")
							.Property(x => x.Id)
								.IsPrimaryKey()
								.IsIdentity();

					using (var tbl = db.CreateLocalTable<Client>())
					{
						var id = db.InsertWithInt32Identity(new Client()
						{
							Has = true
						});

						var record = tbl.Where(_ => _.Id == id).Single();

						Assert.AreEqual(id, record.Id);
						Assert.True(record.Has);
					}
				}
			}
		}
	}
}
