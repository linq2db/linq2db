﻿using System.Linq;
using System.Net;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using Microsoft.Data.SqlClient;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Infrastructure
{
	[TestFixture]
	public class DataOptionsTests : TestBase
	{
		[Test]
		public void LinqOptionsTest()
		{
			var lo1 = Configuration.Linq.Options with { GuardGrouping = false };
			var lo2 = lo1 with { GuardGrouping = true };

			Assert.That(((IConfigurationID)lo1).ConfigurationID, Is.Not.EqualTo(((IConfigurationID)lo2).ConfigurationID));
		}

		[Test]
		public void OnTraceTest()
		{
			string? s1 = null;

			{
				using var db = new TestDataConnection(options => options.UseTracing(ti => s1 = ti.SqlText));

				_child = db.Child.ToList();

				Assert.That(s1, Is.Not.Null);
			}

			{
				s1 = null;

				using var db = new TestDataConnection();

				_child = db.Child.ToList();

				Assert.That(s1, Is.Null);
			}
		}

		[Test]
		public void OnTraceTest2()
		{
			string? s1 = null;

			using var db = new TestDataConnection();

			_child = db.Child.ToList();

			Assert.That(s1, Is.Null);

			var connection = db.TryGetDbConnection();
			Assert.That(connection, Is.Not.Null);

			using var db1 = new TestDataConnection(db.Options
				.UseConnection   (db.DataProvider, connection, false)
				.UseMappingSchema(db.MappingSchema)
				.UseTracing(ti => s1 = ti.SqlText));

			_child = db1.Child.ToList();

			Assert.That(s1, Is.Not.Null);
		}

		[Test]
		public async ValueTask OnBeforeAfterConnectionOpenedTest([IncludeDataSources(TestProvName.SqlServer2022MS)] string context)
		{
			var cs = DataConnection.GetConnectionString(context);

			var builder = new SqlConnectionStringBuilder(cs);

			if (string.IsNullOrEmpty(builder.Password))
				Assert.Inconclusive("SQL Credentials required");

			var password = new NetworkCredential("", builder.Password).SecurePassword;
			password.MakeReadOnly();
			var creds = new SqlCredential(builder.UserID, password);

			builder.Remove("UserID");
			builder.Remove("User ID");
			builder.Remove("Password");

			var cleanCs = builder.ToString();

			var syncBeforeCalled  = false;
			var asyncBeforeCalled = false;
			var syncAfterCalled   = false;
			var asyncAfterCalled  = false;

			var options = new DataOptions()
				.UseSqlServer(cleanCs, SqlServerVersion.v2022, SqlServerProvider.MicrosoftDataSqlClient)
				.UseBeforeConnectionOpened(cn =>
				{
					((SqlConnection)cn).Credential = creds;
					syncBeforeCalled = true;
				},
				(cn, t) =>
				{
					((SqlConnection)cn).Credential = creds;
					asyncBeforeCalled = true;
					return Task.CompletedTask;
				})
				.UseAfterConnectionOpened(_ =>
				{
					syncAfterCalled = true;
				},
				(_, _) =>
				{
					asyncAfterCalled = true;
					return Task.CompletedTask;
				});

			using (var dc = new DataConnection(options))
			{
				dc.GetTable<Person>().ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(syncBeforeCalled, Is.True);
					Assert.That(syncAfterCalled, Is.True);
					Assert.That(asyncBeforeCalled, Is.False);
					Assert.That(asyncAfterCalled, Is.False);
				}
			}

			syncBeforeCalled  = false;
			asyncBeforeCalled = false;
			syncAfterCalled   = false;
			asyncAfterCalled  = false;
			using (var dc = new DataConnection(options))
			{
				await dc.GetTable<Person>().ToListAsync();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(syncBeforeCalled, Is.False);
					Assert.That(syncAfterCalled, Is.False);
					Assert.That(asyncBeforeCalled, Is.True);
					Assert.That(asyncAfterCalled, Is.True);
				}
			}

			syncBeforeCalled  = false;
			asyncBeforeCalled = false;
			syncAfterCalled   = false;
			asyncAfterCalled  = false;
			using (var dc = new DataContext(options))
			{
				dc.GetTable<Person>().ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(syncBeforeCalled, Is.True);
					Assert.That(syncAfterCalled, Is.True);
					Assert.That(asyncBeforeCalled, Is.False);
					Assert.That(asyncAfterCalled, Is.False);
				}
			}

			syncBeforeCalled  = false;
			asyncBeforeCalled = false;
			syncAfterCalled   = false;
			asyncAfterCalled  = false;
			using (var dc = new DataContext(options))
			{
				await dc.GetTable<Person>().ToListAsync();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(syncBeforeCalled, Is.False);
					Assert.That(syncAfterCalled, Is.False);
					Assert.That(asyncBeforeCalled, Is.True);
					Assert.That(asyncAfterCalled, Is.True);
				}
			}

			// test sync only handlers

			var beforeCalled = false;
			var afterCalled  = false;

			options = new DataOptions()
				.UseSqlServer(cleanCs, SqlServerVersion.v2022, SqlServerProvider.MicrosoftDataSqlClient)
				.UseBeforeConnectionOpened(cn =>
				{
					((SqlConnection)cn).Credential = creds;
					beforeCalled = true;
				})
				.UseAfterConnectionOpened(_ =>
				{
					afterCalled = true;
				});

			using (var dc = new DataConnection(options))
			{
				dc.GetTable<Person>().ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(beforeCalled, Is.True);
					Assert.That(afterCalled, Is.True);
				}
			}

			beforeCalled = false;
			afterCalled  = false;
			using (var dc = new DataConnection(options))
			{
				await dc.GetTable<Person>().ToListAsync();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(beforeCalled, Is.True);
					Assert.That(afterCalled, Is.True);
				}
			}

			beforeCalled = false;
			afterCalled  = false;
			using (var dc = new DataContext(options))
			{
				dc.GetTable<Person>().ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(beforeCalled, Is.True);
					Assert.That(afterCalled, Is.True);
				}
			}

			beforeCalled = false;
			afterCalled  = false;
			using (var dc = new DataContext(options))
			{
				await dc.GetTable<Person>().ToListAsync();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(beforeCalled, Is.True);
					Assert.That(afterCalled, Is.True);
				}
			}

			// test use from provider detector
			var beforeCallCnt = 0;
			var afterCallCnt  = 0;
			options = new DataOptions()
				.UseSqlServer(cleanCs, SqlServerVersion.AutoDetect, SqlServerProvider.MicrosoftDataSqlClient)
				.UseBeforeConnectionOpened(cn =>
				{
					((SqlConnection)cn).Credential = creds;
					beforeCallCnt++;
				})
				.UseAfterConnectionOpened(_ =>
				{
					afterCallCnt++;
				});

			using (var dc = new DataConnection(options))
			{
				dc.GetTable<Person>().ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(beforeCallCnt, Is.EqualTo(2));
					Assert.That(afterCallCnt, Is.EqualTo(2));
				}
			}
		}

		sealed class EntityDescriptorTable
		{
			public int Id { get; }
		}

		[Test]
		public void OnEntityDescriptorCreatedTest([DataSources(false)] string context)
		{
			MappingSchema.ClearCache();
			var globalTriggered = false;
			var localTriggrered = false;

			MappingSchema.EntityDescriptorCreatedCallback = (_, _) =>
			{
				globalTriggered = true;
			};

			try

			{
				// global handler set
				using (var db = GetDataContext(context))
				{
					_ = db.GetTable<EntityDescriptorTable>().ToSqlQuery();
				}

				using (Assert.EnterMultipleScope())
				{
					Assert.That(globalTriggered, Is.True);
					Assert.That(localTriggrered, Is.False);
				}

				globalTriggered = false;

				// local handler set
				MappingSchema.ClearCache();
				using (var db = GetDataContext(context, options => options.UseOnEntityDescriptorCreated((_, _) =>
				{
					localTriggrered = true;
				})))
				{
					_ = db.GetTable<EntityDescriptorTable>().ToSqlQuery();
				}

				using (Assert.EnterMultipleScope())
				{
					Assert.That(globalTriggered, Is.False);
					Assert.That(localTriggrered, Is.True);
				}

				localTriggrered = false;

				// descriptor cached
				using (var db = GetDataContext(context))
				{
					_ = db.GetTable<EntityDescriptorTable>().ToSqlQuery();
				}

				using (Assert.EnterMultipleScope())
				{
					Assert.That(globalTriggered, Is.False);
					Assert.That(localTriggrered, Is.False);
				}

				// cache miss
				using (var db = GetDataContext(context, new MappingSchema("name1")))
				{
					_ = db.GetTable<EntityDescriptorTable>().ToSqlQuery();
				}

				using (Assert.EnterMultipleScope())
				{
					Assert.That(globalTriggered, Is.True);
					Assert.That(localTriggrered, Is.False);
				}

				globalTriggered = false;

				// no handlers
				MappingSchema.EntityDescriptorCreatedCallback = null;
				using (var db = GetDataContext(context, new MappingSchema("name2")))
				{
					_ = db.GetTable<EntityDescriptorTable>().ToSqlQuery();
				}

				using (Assert.EnterMultipleScope())
				{
					Assert.That(globalTriggered, Is.False);
					Assert.That(localTriggrered, Is.False);
				}
			}
			finally
			{
				MappingSchema.EntityDescriptorCreatedCallback = null;
			}
		}

		private static void OverloadsNotTest()
		{
			// this is compile-time "test" to ensure configuration overloads with default parameters
			// doesn't conflict with each other when default parameters not specified

			var connectionString = "fake";

			new DataOptions()

				.UseSqlCe()
				.UseSqlCe(connectionString)

				.UseFirebird()
				.UseFirebird(connectionString)

				.UsePostgreSQL()
				.UsePostgreSQL(o => o)
				.UsePostgreSQL(connectionString)
				.UsePostgreSQL(connectionString, o => o)

				.UseDB2()
				.UseDB2(o => o)
				.UseDB2(connectionString)
				.UseDB2(connectionString, o => o)

				.UseSqlServer()
				.UseSqlServer(o => o)
				.UseSqlServer(connectionString)
				.UseSqlServer(connectionString, o => o)

				.UseMySql()
				.UseMySql(o => o)
				.UseMySql(connectionString)
				.UseMySql(connectionString, o => o)

				.UseOracle()
				.UseOracle(o => o)
				.UseOracle(connectionString)
				.UseOracle(connectionString, o => o)

				.UseSQLite()
				.UseSQLite(o => o)
				.UseSQLite(connectionString)
				.UseSQLite(connectionString, o => o)

				.UseAccess()
				.UseAccess(o => o)
				.UseAccess(connectionString)
				.UseAccess(connectionString, o => o)

				.UseInformix()
				.UseInformix(o => o)
				.UseInformix(connectionString)
				.UseInformix(connectionString, o => o)

				.UseSapHana()
				.UseSapHana(o => o)
				.UseSapHana(connectionString)
				.UseSapHana(connectionString, o => o)

				.UseAse()
				.UseAse(o => o)
				.UseAse(connectionString)
				.UseAse(connectionString, o => o)

				.UseClickHouse()
				.UseClickHouse(o => o)
				.UseClickHouse(connectionString)
				.UseClickHouse(connectionString, o => o)
				;
		}

		[Test]
		public void UseCommandTimeoutTest()
		{
			using var db = new TestDataConnection(o => o.UseCommandTimeout(30));

			var commandTimeout = db.CommandTimeout;
			var optionsID      = ((IConfigurationID)db.Options).ConfigurationID;
			var dbID           = ((IConfigurationID)db).        ConfigurationID;

			using (db.UseOptions<DataContextOptions>(o => o with { CommandTimeout = 45 }))
			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.CommandTimeout,                              Is.EqualTo(45));
				Assert.That(((IConfigurationID)db.Options).ConfigurationID, Is.Not.EqualTo(optionsID));
				Assert.That(((IConfigurationID)db).ConfigurationID,         Is.Not.EqualTo(dbID));
			}

			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.CommandTimeout,                              Is.EqualTo(commandTimeout));
				Assert.That(((IConfigurationID)db.Options).ConfigurationID, Is.EqualTo(optionsID));
				Assert.That(((IConfigurationID)db).ConfigurationID,         Is.EqualTo(dbID));
			}

			db.CommandTimeout = 15;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.CommandTimeout, Is.EqualTo(15));
				Assert.That(((IConfigurationID)db.Options).ConfigurationID, Is.EqualTo(optionsID));
				Assert.That(((IConfigurationID)db).ConfigurationID, Is.EqualTo(dbID));
			}

			using (db.UseOptions<DataContextOptions>(o => o with { CommandTimeout = 25 }))
			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.CommandTimeout, Is.EqualTo(25));
				Assert.That(((IConfigurationID)db.Options).ConfigurationID, Is.Not.EqualTo(optionsID));
				Assert.That(((IConfigurationID)db).ConfigurationID, Is.Not.EqualTo(dbID));
			}

			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.CommandTimeout, Is.EqualTo(15));
				Assert.That(((IConfigurationID)db.Options).ConfigurationID, Is.EqualTo(optionsID));
				Assert.That(((IConfigurationID)db).ConfigurationID, Is.EqualTo(dbID));
			}

			db.ResetCommandTimeout();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.CommandTimeout, Is.EqualTo(-1));
				Assert.That(((IConfigurationID)db.Options).ConfigurationID, Is.EqualTo(optionsID));
				Assert.That(((IConfigurationID)db).ConfigurationID, Is.EqualTo(dbID));
			}

			using (db.UseOptions<DataContextOptions>(o => o with { CommandTimeout = 35 }))
			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.CommandTimeout, Is.EqualTo(35));
				Assert.That(((IConfigurationID)db.Options).ConfigurationID, Is.Not.EqualTo(optionsID));
				Assert.That(((IConfigurationID)db).ConfigurationID, Is.Not.EqualTo(dbID));
			}

			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.CommandTimeout, Is.EqualTo(-1));
				Assert.That(((IConfigurationID)db.Options).ConfigurationID, Is.EqualTo(optionsID));
				Assert.That(((IConfigurationID)db).ConfigurationID, Is.EqualTo(dbID));
			}
		}

		[Test]
		public void UseCommandTimeoutOnContextTest()
		{
			using var db = new DataContext(new DataOptions().UseCommandTimeout(30));

			var commandTimeout = db.CommandTimeout;
			var optionsID      = ((IConfigurationID)db.Options).ConfigurationID;
			var dbID           = ((IConfigurationID)db).        ConfigurationID;

			using (db.UseOptions<DataContextOptions>(o => o with { CommandTimeout = 45 }))
			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.CommandTimeout, Is.EqualTo(45));
				Assert.That(((IConfigurationID)db.Options).ConfigurationID, Is.Not.EqualTo(optionsID));
				Assert.That(((IConfigurationID)db).ConfigurationID, Is.Not.EqualTo(dbID));
			}

			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.CommandTimeout, Is.EqualTo(commandTimeout));
				Assert.That(((IConfigurationID)db.Options).ConfigurationID, Is.EqualTo(optionsID));
				Assert.That(((IConfigurationID)db).ConfigurationID, Is.EqualTo(dbID));
			}

			db.CommandTimeout = 15;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.CommandTimeout, Is.EqualTo(15));
				Assert.That(((IConfigurationID)db.Options).ConfigurationID, Is.EqualTo(optionsID));
				Assert.That(((IConfigurationID)db).ConfigurationID, Is.EqualTo(dbID));
			}

			using (db.UseOptions<DataContextOptions>(o => o with { CommandTimeout = 25 }))
			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.CommandTimeout, Is.EqualTo(25));
				Assert.That(((IConfigurationID)db.Options).ConfigurationID, Is.Not.EqualTo(optionsID));
				Assert.That(((IConfigurationID)db).ConfigurationID, Is.Not.EqualTo(dbID));
			}

			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.CommandTimeout, Is.EqualTo(15));
				Assert.That(((IConfigurationID)db.Options).ConfigurationID, Is.EqualTo(optionsID));
				Assert.That(((IConfigurationID)db).ConfigurationID, Is.EqualTo(dbID));
			}

			db.ResetCommandTimeout();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.CommandTimeout, Is.EqualTo(-1));
				Assert.That(((IConfigurationID)db.Options).ConfigurationID, Is.EqualTo(optionsID));
				Assert.That(((IConfigurationID)db).ConfigurationID, Is.EqualTo(dbID));
			}

			using (db.UseOptions<DataContextOptions>(o => o with { CommandTimeout = 35 }))
			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.CommandTimeout, Is.EqualTo(35));
				Assert.That(((IConfigurationID)db.Options).ConfigurationID, Is.Not.EqualTo(optionsID));
				Assert.That(((IConfigurationID)db).ConfigurationID, Is.Not.EqualTo(dbID));
			}

			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.CommandTimeout, Is.EqualTo(-1));
				Assert.That(((IConfigurationID)db.Options).ConfigurationID, Is.EqualTo(optionsID));
				Assert.That(((IConfigurationID)db).ConfigurationID, Is.EqualTo(dbID));
			}
		}

		[Test]
		public void UseOptimizeJoinsTest()
		{
			using var db = new TestDataConnection(o => o.UseOptimizeJoins(false));

			var param     = db.Options.LinqOptions.OptimizeJoins;
			var optionsID = ((IConfigurationID)db.Options).ConfigurationID;
			var dbID      = ((IConfigurationID)db).        ConfigurationID;

			using (db.UseOptions(o => o
				.WithOptions<LinqOptions>    (co => co with { OptimizeJoins = true })
				.WithOptions<BulkCopyOptions>(bo => bo with { BulkCopyType = BulkCopyType.RowByRow })))
			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.Options.LinqOptions.OptimizeJoins,           Is.Not.EqualTo(param));
				Assert.That(((IConfigurationID)db.Options).ConfigurationID, Is.Not.EqualTo(optionsID));
				Assert.That(((IConfigurationID)db).ConfigurationID,         Is.Not.EqualTo(dbID));
			}

			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.Options.LinqOptions.OptimizeJoins,           Is.EqualTo(param));
				Assert.That(((IConfigurationID)db.Options).ConfigurationID, Is.EqualTo(optionsID));
				Assert.That(((IConfigurationID)db).ConfigurationID,         Is.EqualTo(dbID));
			}
		}

		[Test]
		public void UseCompareNullsTest()
		{
			using var db = new TestDataConnection(o => o.UseCompareNulls(CompareNulls.LikeSqlExceptParameters));

			var param     = db.Options.LinqOptions.CompareNulls;
			var optionsID = ((IConfigurationID)db.Options).ConfigurationID;
			var dbID      = ((IConfigurationID)db).        ConfigurationID;

			using (db.UseLinqOptions(o => o with { CompareNulls = param }))
			{
				AssertState();
			}

			AssertState();

			void AssertState()
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(db.Options.LinqOptions.CompareNulls,            Is.EqualTo(param));
					Assert.That(((IConfigurationID)db.Options).ConfigurationID, Is.EqualTo(optionsID));
					Assert.That(((IConfigurationID)db).ConfigurationID,         Is.EqualTo(dbID));
				}
			}
		}

		[Test]
		public void TryUseConfigurationStringTest()
		{
			using var db = new TestDataConnection();
			Assert.Throws<LinqToDBException>(
				() => { using (db.UseOptions(o => o.UseConfiguration("new config"))) { } },
				"ConfigurationString cannot be changed.");
		}

		[Test]
		public void TryUseConnectionStringTest()
		{
			using var db = new TestDataConnection();
			Assert.Throws<LinqToDBException>(
				() => { using (db.UseOptions(o => o.UseConnectionString("new config"))) { } },
				"ConnectionString cannot be changed.");
	}

		[Test]
		public void TryUseProviderNameTest()
		{
			using var db = new TestDataConnection();
			Assert.Throws<LinqToDBException>(
				() => { using (db.UseOptions(o => o.UseProvider("new provider"))) { } },
				"ProviderName cannot be changed.");
}

		[Test]
		public void TryWithDbConnectionTest()
		{
			using var db = new TestDataConnection();
			Assert.Throws<LinqToDBException>(
				() => { using (db.UseOptions(o => o.WithOptions<ConnectionOptions>(co => co.WithDbConnection(db.OpenDbConnection())))) { } },
				"DbConnection cannot be changed.");
		}

		[Test]
		public void TryWithDbTransactionTest()
		{
			using var db = new TestDataConnection();

			db.BeginTransaction();

			Assert.Throws<LinqToDBException>(
				() => { using (db.UseOptions(o => o.WithOptions<ConnectionOptions>(co => co.WithDbTransaction(db.Transaction!)))) { } },
				"DbTransaction cannot be changed.");
		}

		[Test]
		public void TryWithDisposeConnectionTest()
		{
			using var db = new TestDataConnection();
			Assert.Throws<LinqToDBException>(
				() => { using (db.UseOptions(o => o.WithOptions<ConnectionOptions>(co => co.WithDisposeConnection(true)))) { } },
				"DisposeConnection cannot be changed.");
		}

		[Test]
		public void TryUseDataProviderTest()
		{
			using var db = new TestDataConnection();
			Assert.Throws<LinqToDBException>(
				() => { using (db.UseOptions(o => o.UseSqlServer(SqlServerVersion.v2022, SqlServerProvider.MicrosoftDataSqlClient))) { } },
				"DataProvider cannot be changed.");
		}

		[Test]
		public void TryUseDataProviderFactoryTest()
		{
			using var db = new TestDataConnection();
			Assert.Throws<LinqToDBException>(
				() => { using (db.UseOptions(o => o.UseSqlServer("connection string"))) { } },
				"DataProviderFactory cannot be changed.");
		}

		[Test]
		public void TryWithConnectionFactoryTest()
		{
			using var db = new TestDataConnection();
			Assert.Throws<LinqToDBException>(
				() => { using (db.UseOptions(o => o.WithOptions<ConnectionOptions>(co => co.WithConnectionFactory(_ => db.OpenDbConnection())))) { } },
				"ConnectionFactory cannot be changed.");
		}

		[Test]
		public void TryWithOnEntityDescriptorCreatedTest()
		{
			using var db = new TestDataConnection();
			Assert.Throws<LinqToDBException>(
				() => { using (db.UseOptions(o => o.WithOptions<ConnectionOptions>(co => co.WithOnEntityDescriptorCreated((schema, descriptor) => { })))) { } },
				"OnEntityDescriptorCreated cannot be changed.");
		}
	}
}
