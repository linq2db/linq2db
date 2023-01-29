using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Common.Internal;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;

using Microsoft.Data.SqlClient;

using NUnit.Framework;

namespace Tests.Infrastructure
{
	using System.Net;
	using System.Threading.Tasks;
	using Model;

	[TestFixture]
	public class DataOptionsTests : TestBase
	{
		[Test]
		public void LinqOptionsTest()
		{
			var lo1 = LinqToDB.Common.Configuration.Linq.Options with { GuardGrouping = false };
			var lo2 = lo1 with { GuardGrouping = true };

			Assert.That(((IConfigurationID)lo1).ConfigurationID, Is.Not.EqualTo(((IConfigurationID)lo2).ConfigurationID));
		}

		[Test]
		public void OnTraceTest()
		{
			string? s1 = null;

			{
				using var db = new TestDataConnection(options => options.WithOptions<QueryTraceOptions>(o => o with
				{
					OnTrace = ti => s1 = ti.SqlText
				}));

				_child = db.Child.ToList();

				Assert.NotNull(s1);
			}

			{
				s1 = null;

				using var db = new TestDataConnection();

				_child = db.Child.ToList();

				Assert.IsNull(s1);
			}
		}

		[Test]
		public void OnTraceTest2()
		{
			string? s1 = null;

			using var db = new TestDataConnection();

			_child = db.Child.ToList();

			Assert.IsNull(s1);

			using var db1 = new TestDataConnection(db.Options
				.UseConnection   (db.DataProvider, db.Connection, false)
				.UseMappingSchema(db.MappingSchema)
				.WithOptions<QueryTraceOptions>(o => o with
				{
					OnTrace = ti => s1 = ti.SqlText
				}));


			_child = db1.Child.ToList();

			Assert.NotNull(s1);
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

				Assert.True(syncBeforeCalled);
				Assert.True(syncAfterCalled);
				Assert.False(asyncBeforeCalled);
				Assert.False(asyncAfterCalled);
			}

			syncBeforeCalled  = false;
			asyncBeforeCalled = false;
			syncAfterCalled   = false;
			asyncAfterCalled  = false;
			using (var dc = new DataConnection(options))
			{
				await dc.GetTable<Person>().ToListAsync();

				Assert.False(syncBeforeCalled);
				Assert.False(syncAfterCalled);
				Assert.True(asyncBeforeCalled);
				Assert.True(asyncAfterCalled);
			}

			syncBeforeCalled  = false;
			asyncBeforeCalled = false;
			syncAfterCalled   = false;
			asyncAfterCalled  = false;
			using (var dc = new DataContext(options))
			{
				dc.GetTable<Person>().ToList();

				Assert.True(syncBeforeCalled);
				Assert.True(syncAfterCalled);
				Assert.False(asyncBeforeCalled);
				Assert.False(asyncAfterCalled);
			}

			syncBeforeCalled  = false;
			asyncBeforeCalled = false;
			syncAfterCalled   = false;
			asyncAfterCalled  = false;
			using (var dc = new DataContext(options))
			{
				await dc.GetTable<Person>().ToListAsync();

				Assert.False(syncBeforeCalled);
				Assert.False(syncAfterCalled);
				Assert.True(asyncBeforeCalled);
				Assert.True(asyncAfterCalled);
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

				Assert.True(beforeCalled);
				Assert.True(afterCalled);
			}

			beforeCalled = false;
			afterCalled  = false;
			using (var dc = new DataConnection(options))
			{
				await dc.GetTable<Person>().ToListAsync();

				Assert.True(beforeCalled);
				Assert.True(afterCalled);
			}

			beforeCalled = false;
			afterCalled  = false;
			using (var dc = new DataContext(options))
			{
				dc.GetTable<Person>().ToList();

				Assert.True(beforeCalled);
				Assert.True(afterCalled);
			}

			beforeCalled = false;
			afterCalled  = false;
			using (var dc = new DataContext(options))
			{
				await dc.GetTable<Person>().ToListAsync();

				Assert.True(beforeCalled);
				Assert.True(afterCalled);
			}
		}
	}
}
