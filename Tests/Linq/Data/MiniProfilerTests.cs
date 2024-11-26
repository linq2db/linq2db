extern alias MySqlConnector;
extern alias MySqlData;

using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Access;
using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.DataProvider.DB2;
using LinqToDB.DataProvider.Firebird;
using LinqToDB.DataProvider.Informix;
using LinqToDB.DataProvider.MySql;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.DataProvider.SapHana;
using LinqToDB.DataProvider.SqlCe;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.DataProvider.Sybase;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;

using FirebirdSql.Data.Types;
using IBM.Data.DB2Types;
using Microsoft.SqlServer.Types;
using NUnit.Framework;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;
using Tests.DataProvider;
using Tests.Model;
#if NETFRAMEWORK
using IBM.Data.Informix;
#endif

using MySqlConnectorDateTime   = MySqlConnector::MySqlConnector.MySqlDateTime;
using MySqlDataDateTime        = MySqlData::MySql.Data.Types.MySqlDateTime;
using MySqlDataDecimal         = MySqlData::MySql.Data.Types.MySqlDecimal;
using MySqlDataMySqlConnection = MySqlData::MySql.Data.MySqlClient.MySqlConnection;

namespace Tests.Data
{
	[TestFixture]
	public class MiniProfilerTests : TestBase
	{
		// IMPORTANT:
		// MiniProfiler initialized by SQLiteMiniprofilerProvider.Init
		// Teardown is not needed as it will break sqlite miniprofiler tests

		[SetUp]
		public void InitTest()
		{
			// to prevent tests interference
			CommandInfo.ClearObjectReaderCache();
		}

		public class MiniProfilerDataContext : DataConnection
		{
			public MiniProfilerDataContext(string configurationString)
#pragma warning disable CA2000 // Dispose objects before losing scope
				: base(GetDataProvider(), GetConnection(configurationString)) { }
#pragma warning restore CA2000 // Dispose objects before losing scope

			private static IDataProvider GetDataProvider()
			{
				return new SqlServerTests.TestSqlServerDataProvider("MiniProfiler." + ProviderName.SqlServer2012, SqlServerVersion.v2012, SqlServerProvider.SystemDataSqlClient);
			}

			private static DbConnection GetConnection(string configurationString)
			{
#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable CS0618 // Type or member is obsolete
				var dbConnection = new SqlConnection(GetConnectionString(configurationString));
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CA2000 // Dispose objects before losing scope
				return new ProfiledDbConnection(dbConnection, MiniProfiler.Current);
			}
		}

		[Test]
		public void Test1([IncludeDataSources(TestProvName.AllNorthwind)] string context)
		{
			using (var mpcon = new MiniProfilerDataContext(context))
			{
				mpcon.GetTable<Northwind.Category>().ToList();
			}
		}

		// provider-specific tests
		// tests must check all code, that use provider-specific functionality for specific provider
		// also test must create new instance of provider, to not benefit from existing instance
		[Test]
		public void TestAccessOleDb([IncludeDataSources(TestProvName.AllAccessOleDb)] string context, [Values] ConnectionType type)
		{
			var unmapped = type == ConnectionType.MiniProfilerNoMappings;
			var connectionString = DataConnection.GetConnectionString(context);
#if NETFRAMEWORK
			using (var db = CreateDataConnection(AccessTools.GetDataProvider(provider: AccessProvider.OleDb, connectionString: connectionString), context, type, cs => new System.Data.OleDb.OleDbConnection(cs)))
#else
			using (var db = CreateDataConnection(AccessTools.GetDataProvider(provider: AccessProvider.OleDb, connectionString: connectionString), context, type, "System.Data.OleDb.OleDbConnection, System.Data.OleDb"))
#endif
			{
				var trace = string.Empty;
				db.OnTraceConnection += (TraceInfo ti) =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						trace = ti.SqlText;
				};

				Assert.Multiple(() =>
				{
					// assert provider-specific parameter type name
					// DateTime, DateTime2 => Date
					// Text => LongVarChar
					// NText => LongVarWChar
					Assert.That(db.Execute<int>("SELECT ID FROM AllTypes WHERE datetimeDataType = @p", new DataParameter("@p", new DateTime(2012, 12, 12, 12, 12, 12), DataType.DateTime)), Is.EqualTo(2));
					Assert.That(trace, Does.Contain("DECLARE @p Date "));
					Assert.That(db.Execute<int>("SELECT ID FROM AllTypes WHERE datetimeDataType = @p", new DataParameter("@p", new DateTime(2012, 12, 12, 12, 12, 12), DataType.DateTime2)), Is.EqualTo(2));
				});
				Assert.Multiple(() =>
				{
					Assert.That(trace, Does.Contain("DECLARE @p Date "));
					Assert.That(db.Execute<int>("SELECT ID FROM AllTypes WHERE textDataType = @p", new DataParameter("@p", "567", DataType.Text)), Is.EqualTo(2));
				});
				Assert.Multiple(() =>
				{
					Assert.That(trace, Does.Contain("DECLARE @p LongVarChar(3)"));
					Assert.That(db.Execute<int>("SELECT ID FROM AllTypes WHERE ntextDataType = @p", new DataParameter("@p", "111", DataType.NText)), Is.EqualTo(2));
				});
				Assert.That(trace, Does.Contain("DECLARE @p LongVarWChar(3)"));
			}
		}

#if NETFRAMEWORK
		[Test]
		public void TestAccessOleDbSchema([IncludeDataSources(TestProvName.AllAccessOleDb)] string context, [Values] ConnectionType type)
		{
			var unmapped = type == ConnectionType.MiniProfilerNoMappings;
			var connectionString = DataConnection.GetConnectionString(context);
			using (var db = CreateDataConnection(AccessTools.GetDataProvider(provider: AccessProvider.OleDb, connectionString: connectionString), context, type, cs => new System.Data.OleDb.OleDbConnection(cs)))
			{
				// TODO: reenable for .net, when issue with OleDb transactions under .net core fixed
				// assert custom schema table access
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);
				Assert.That(schema.Tables.Any(t => t.ForeignKeys.Count > 0), Is.EqualTo(!unmapped));
			}
		}
#endif

		[Test]
		public void TestAccessODBC([IncludeDataSources(TestProvName.AllAccessOdbc)] string context, [Values] ConnectionType type)
		{
			var unmapped = type == ConnectionType.MiniProfilerNoMappings;
			var connectionString = DataConnection.GetConnectionString(context);
#if NETFRAMEWORK
			using (var db = CreateDataConnection(AccessTools.GetDataProvider(provider: AccessProvider.ODBC, connectionString: connectionString), context, type, cs => new System.Data.Odbc.OdbcConnection(cs)))
#else
			using (var db = CreateDataConnection(AccessTools.GetDataProvider(provider: AccessProvider.ODBC, connectionString: connectionString), context, type, "System.Data.Odbc.OdbcConnection, System.Data.Odbc"))
#endif
			{
				var trace = string.Empty;
				db.OnTraceConnection += (TraceInfo ti) =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						trace = ti.SqlText;
				};

				Assert.Multiple(() =>
				{
					// assert provider-specific parameter type name
					// Variant => Binary
					Assert.That(db.Execute<int>("SELECT ID FROM AllTypes WHERE oleObjectDataType = ?", DataParameter.Variant("@p", new byte[] { 5, 6, 7, 8 })), Is.EqualTo(2));
					Assert.That(trace, Does.Contain("DECLARE @p Binary("));
				});
			}
		}

		[Test]
		public void TestSapHanaOdbc([IncludeDataSources(ProviderName.SapHanaOdbc)] string context, [Values] ConnectionType type)
		{
#if NETFRAMEWORK
			using (var db = CreateDataConnection(new SapHanaOdbcDataProvider(), context, type, cs => new System.Data.Odbc.OdbcConnection(cs)))
#else
			using (var db = CreateDataConnection(new SapHanaOdbcDataProvider(), context, type, "System.Data.Odbc.OdbcConnection, System.Data.Odbc"))
#endif
			{
				// provider doesn't use provider-specific API, so we just query schema
				db.DataProvider.GetSchemaProvider().GetSchema(db);
			}
		}

		[Test]
		public void TestFirebird([IncludeDataSources(TestProvName.AllFirebird)] string context, [Values] ConnectionType type)
		{
			var unmapped = type == ConnectionType.MiniProfilerNoMappings;

			Type providerType;

			using (var db = (DataConnection)GetDataContext(context))
			{
				providerType = db.DataProvider.GetType();
			}

			var provider = (FirebirdDataProvider)Activator.CreateInstance(providerType)!;

			using (var db = CreateDataConnection(provider, context, type, "FirebirdSql.Data.FirebirdClient.FbConnection, FirebirdSql.Data.FirebirdClient"))
			{
				var trace = string.Empty;
				db.OnTraceConnection += (TraceInfo ti) =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						trace = ti.SqlText;
				};

				Assert.Multiple(() =>
				{
					// assert provider-specific parameter type name
					Assert.That(db.Execute<int>("SELECT ID FROM \"AllTypes\" WHERE \"nvarcharDataType\" = @p", new DataParameter("@p", "3323", DataType.NVarChar)), Is.EqualTo(2));
					Assert.That(trace, Does.Contain("DECLARE @p VarChar"));
				});

				// just check schema (no api used)
				db.DataProvider.GetSchemaProvider().GetSchema(db);

				// assert api resolved and callable
				FirebirdTools.ClearAllPools();

				// test provider-specific types
				if (context == TestProvName.AllFirebird4Plus)
				{
					var fbDecFloat = new FbDecFloat(BigInteger.Parse("12345"), 5);
					var fbDecFloat1 = db.Execute<FbDecFloat>("SELECT CAST(@p as decfloat) from rdb$database", new DataParameter("@p", fbDecFloat, DataType.DecFloat));
					Assert.That(fbDecFloat1, Is.EqualTo(fbDecFloat));

					var fbZonedDateTime = new FbZonedDateTime(TestData.DateTime4Utc, "UTC");
					var fbZonedDateTime1 = db.Execute<FbZonedDateTime>("SELECT CAST(@p as timestamp with time zone) from rdb$database", new DataParameter("@p", fbZonedDateTime, DataType.DateTimeOffset));
					Assert.That(fbZonedDateTime1, Is.EqualTo(fbZonedDateTime));

					var fbZonedTime = new FbZonedTime(TestData.TimeOfDay4, "UTC");
					var fbZonedTime1 = db.Execute<FbZonedTime>("SELECT CAST(@p as time with time zone) from rdb$database", new DataParameter("@p", fbZonedTime, DataType.TimeTZ));
					Assert.That(fbZonedTime1, Is.EqualTo(fbZonedTime));
				}
			}
		}

		[Test]
		public void TestSqlCe([IncludeDataSources(ProviderName.SqlCe)] string context, [Values] ConnectionType type)
		{
			var unmapped = type == ConnectionType.MiniProfilerNoMappings;
			using (var db = CreateDataConnection(new SqlCeDataProvider(), context, type, DbProviderFactories.GetFactory("System.Data.SqlServerCe.4.0").GetType().Assembly.GetType("System.Data.SqlServerCe.SqlCeConnection")!))
			{
				var trace = string.Empty;
				db.OnTraceConnection += (TraceInfo ti) =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						trace = ti.SqlText;
				};

				Assert.Multiple(() =>
				{
					// assert provider-specific parameter type name
					Assert.That(db.Execute<string>("SELECT Cast(@p as ntext)", new DataParameter("@p", "111", DataType.Text)), Is.EqualTo("111"));
					Assert.That(trace, Does.Contain("DECLARE @p NText"));
					Assert.That(db.Execute<string>("SELECT Cast(@p as ntext)", new DataParameter("@p", "111", DataType.NText)), Is.EqualTo("111"));
				});
				Assert.Multiple(() =>
				{
					Assert.That(trace, Does.Contain("DECLARE @p NText"));
					Assert.That(db.Execute<int>("SELECT ID FROM AllTypes WHERE nvarcharDataType = @p", new DataParameter("@p", "3323", DataType.VarChar)), Is.EqualTo(2));
				});
				Assert.Multiple(() =>
				{
					Assert.That(trace, Does.Contain("DECLARE @p NVarChar"));
					Assert.That(db.Execute<int>("SELECT ID FROM AllTypes WHERE nvarcharDataType = @p", new DataParameter("@p", "3323", DataType.NVarChar)), Is.EqualTo(2));
				});
				Assert.Multiple(() =>
				{
					Assert.That(trace, Does.Contain("DECLARE @p NVarChar"));
					Assert.That(db.Execute<int>("SELECT ID FROM AllTypes WHERE binaryDataType = @p", new DataParameter("@p", new byte[] { 1 }, DataType.Binary)), Is.EqualTo(2));
				});
				Assert.Multiple(() =>
				{
					Assert.That(trace, Does.Contain("DECLARE @p Binary("));
					Assert.That(db.Execute<int>("SELECT ID FROM AllTypes WHERE varbinaryDataType = @p", new DataParameter("@p", new byte[] { 2 }, DataType.VarBinary)), Is.EqualTo(2));
				});
				Assert.Multiple(() =>
				{
					Assert.That(trace, Does.Contain("DECLARE @p VarBinary("));
					Assert.That(db.Execute<byte[]>("SELECT Cast(@p as image)", new DataParameter("@p", new byte[] { 0, 0, 0, 3 }, DataType.Image)), Is.EqualTo(new byte[] { 0, 0, 0, 3 }));
				});
				Assert.That(trace, Does.Contain("DECLARE @p Image("));

				var tsVal = db.Execute<byte[]>("SELECT timestampDataType FROM AllTypes WHERE ID = 2");
				Assert.Multiple(() =>
				{
					Assert.That(db.Execute<int>("SELECT ID FROM AllTypes WHERE timestampDataType = @p", new DataParameter("@p", tsVal, DataType.Timestamp)), Is.EqualTo(2));
					Assert.That(trace, Does.Contain("DECLARE @p Timestamp("));
				});

				// just check schema (no api used)
				db.DataProvider.GetSchemaProvider().GetSchema(db);

				// assert api resolved and callable
				SqlCeTools.CreateDatabase($"TestSqlCe_{TestData.Guid1:N}");
			}
		}

		sealed class MapperExpressionTest1
		{
			public DateTime Value { get; set; }
		}

		sealed class MapperExpressionTest2
		{
			public MySqlDataDateTime Value { get; set; }
		}

		sealed class MapperExpressionTest3
		{
			public object? Value { get; set; }
		}

		// tests support of data reader methods by Mapper.Map (using MySql.Data provider only)
		[Test]
		public void TestMapperMap([IncludeDataSources(TestProvName.AllMySqlData)] string context, [Values] ConnectionType type)
		{
			Type providerType;

			using (var db = (DataConnection)GetDataContext(context))
			{
				providerType = db.DataProvider.GetType();
			}

			var provider = (MySqlDataProvider)Activator.CreateInstance(providerType)!;

			// AllowZeroDateTime is to enable MySqlDateTime type
			using (var db = CreateDataConnection(provider, context, type, "MySql.Data.MySqlClient.MySqlConnection, MySql.Data", ";AllowZeroDateTime=true"))
			{
				var dtValue = new DateTime(2012, 12, 12, 12, 12, 12, 0);

				Assert.Multiple(() =>
				{
					Assert.That(db.FromSql<MapperExpressionTest1>("SELECT Cast({0} as datetime) as Value", new DataParameter("p", dtValue, DataType.DateTime)).Single().Value, Is.EqualTo(dtValue));
					Assert.That(db.FromSql<MapperExpressionTest2>("SELECT Cast({0} as datetime) as Value", new DataParameter("p", dtValue, DataType.DateTime)).Single().Value.Value, Is.EqualTo(dtValue));
				});

				var rawDtValue = db.FromSql<MapperExpressionTest3>("SELECT Cast({0} as datetime) as Value", new DataParameter("p", dtValue, DataType.DateTime)).Single().Value;
				Assert.Multiple(() =>
				{
					Assert.That(rawDtValue is MySqlDataDateTime, Is.True);
					Assert.That(((MySqlDataDateTime)rawDtValue!).Value, Is.EqualTo(dtValue));
				});
			}
		}

		sealed class LinqMySqlDataProvider : MySqlDataProvider
		{
			private readonly Func<string, DbConnection> _connectionFactory;
			public LinqMySqlDataProvider(MySqlVersion version, Func<string, DbConnection> connectionFactory)
				: base(ProviderName.MySql, version, MySqlProvider.MySqlData)
			{
				_connectionFactory = connectionFactory;
			}

			protected override DbConnection CreateConnectionInternal(string connectionString)
			{
				return _connectionFactory(connectionString);
			}
		}

		// tests support of data reader methods by LinqService
		// full of hacks to made test work as expected
		[Test]
		public void TestLinqService([IncludeDataSources(true, TestProvName.AllMySqlData)] string context, [Values] ConnectionType type)
		{
			var provider = GetProviderName(context, out var isLinq);
			MySqlVersion version;
			using (var db = (DataConnection)GetDataContext(provider))
			{
				version = ((MySqlDataProvider)db.DataProvider).Version;
			}

			const string testContext = "test-linq-service-reader";

			// hacks to make remote context to work new custom dataprovider instance
			var cs = DataConnection.GetConnectionString(provider);
			DataConnection.AddOrSetConfiguration(testContext, cs + ";AllowZeroDateTime=true", testContext);
			DataConnection.AddDataProvider(testContext, new LinqMySqlDataProvider(version, cs =>
			{
				var cn = new MySqlDataMySqlConnection(cs);

				switch (type)
				{
					case ConnectionType.MiniProfilerNoMappings:
					case ConnectionType.MiniProfiler:
						if (MiniProfiler.Current == null)
							MiniProfiler.DefaultOptions.StartProfiler();
						Assert.That(MiniProfiler.Current, Is.Not.Null);
						return new ProfiledDbConnection(cn, MiniProfiler.Current);
				}

				return cn;
			}));

			// TODO: probably we should add serialization for supported provider-specific types to provider's mapping schema
			var ms = new MappingSchema();
			ms.SetConvertExpression<MySqlDataDateTime, string>(value => value.Value.ToBinary().ToString(CultureInfo.InvariantCulture));
			ms.SetConvertExpression<string, MySqlDataDateTime>(value => new MySqlDataDateTime(DateTime.FromBinary(long.Parse(value, CultureInfo.InvariantCulture))));

			using (var db = GetDataContext(testContext + (isLinq ? LinqServiceSuffix : null), ms))
			{
				if (type == ConnectionType.MiniProfiler)
					db.AddInterceptor(UnwrapProfilerInterceptor.Instance);

				var dtValue = new DateTime(2012, 12, 12, 12, 12, 12, 0);

				Assert.Multiple(() =>
				{
					// ExecuteReader
					Assert.That(db.FromSql<MapperExpressionTest1>("SELECT Cast({0} as datetime) as Value", new DataParameter("p", dtValue, DataType.DateTime)).Single().Value, Is.EqualTo(dtValue));
					Assert.That(db.FromSql<MapperExpressionTest2>("SELECT Cast({0} as datetime) as Value", new DataParameter("p", dtValue, DataType.DateTime)).Single().Value.Value, Is.EqualTo(dtValue));
				});

				// TODO: doesn't work due to object use, probably we should add type to remote context data
				//var rawDtValue = db.FromSql<MapperExpressionTest3>("SELECT Cast(@p as datetime) as Value", new DataParameter("@p", dtValue, DataType.DateTime)).Single().Value;
				//Assert.True(rawDtValue is MySqlDataDateTime);
				//Assert.AreEqual(dtValue, ((MySqlDataDateTime)rawDtValue).Value);
			}
		}

		[Test]
		public void TestMySqlData([IncludeDataSources(TestProvName.AllMySqlData)] string context, [Values] ConnectionType type)
		{
			Type providerType;

			using (var db = (DataConnection)GetDataContext(context))
			{
				providerType = db.DataProvider.GetType();
			}

			var provider = (MySqlDataProvider)Activator.CreateInstance(providerType)!;

			var unmapped = type == ConnectionType.MiniProfilerNoMappings;
			// AllowZeroDateTime is to enable MySqlDateTime type
			using (var db = CreateDataConnection(provider, context, type, "MySql.Data.MySqlClient.MySqlConnection, MySql.Data", ";AllowZeroDateTime=true"))
			{
				var trace = string.Empty;
				db.OnTraceConnection += (TraceInfo ti) =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						trace = ti.SqlText;
				};

				// test provider-specific type readers
				// (using both SetProviderField and SetToTypeField registrations)
				var decValue = 123.456m;

				// not valid for wrapped reader, because MySqlDataDecimal cannot be constructed
				MySqlDataDecimal mysqlDecValue = default;
				if (type != ConnectionType.MiniProfilerNoMappings)
				{
					mysqlDecValue = db.Execute<MySqlDataDecimal>("SELECT Cast(@p as decimal(6, 3))", new DataParameter("@p", decValue, DataType.Decimal));
					Assert.That(mysqlDecValue.Value, Is.EqualTo(decValue));
				}

				var rawDecValue = db.Execute<object>("SELECT Cast(@p as decimal(6, 3))", new DataParameter("@p", decValue, DataType.Decimal));
				Assert.Multiple(() =>
				{
					Assert.That(rawDecValue is decimal, Is.True);
					Assert.That((decimal)rawDecValue, Is.EqualTo(decValue));
				});

				var dtValue = new DateTime(2012, 12, 12, 12, 12, 12, 0);
				Assert.That(db.Execute<MySqlDataDateTime>("SELECT Cast(@p as datetime)", new DataParameter("@p", dtValue, DataType.DateTime)).Value, Is.EqualTo(dtValue));
				var rawDtValue = db.Execute<object>("SELECT Cast(@p as datetime)", new DataParameter("@p", dtValue, DataType.DateTime));
				Assert.Multiple(() =>
				{
					Assert.That(rawDtValue is MySqlDataDateTime, Is.True);
					Assert.That(((MySqlDataDateTime)rawDtValue).Value, Is.EqualTo(dtValue));

					// test readers + mapper.map
					Assert.That(db.FromSql<MapperExpressionTest1>("SELECT Cast(@p as datetime) as Value", new DataParameter("@p", dtValue, DataType.DateTime)).Single().Value, Is.EqualTo(dtValue));
					Assert.That(db.FromSql<MapperExpressionTest2>("SELECT Cast(@p as datetime) as Value", new DataParameter("@p", dtValue, DataType.DateTime)).Single().Value.Value, Is.EqualTo(dtValue));
				});
				rawDtValue = db.FromSql<MapperExpressionTest3>("SELECT Cast(@p as datetime) as Value", new DataParameter("@p", dtValue, DataType.DateTime)).Single().Value!;
				Assert.Multiple(() =>
				{
					Assert.That(rawDtValue is MySqlDataDateTime, Is.True);
					Assert.That(((MySqlDataDateTime)rawDtValue).Value, Is.EqualTo(dtValue));
				});

				// test provider-specific parameter values
				if (type == ConnectionType.MiniProfilerNoMappings)
					decValue = 0;

				Assert.Multiple(() =>
				{
					Assert.That(db.Execute<decimal>("SELECT Cast(@p as decimal(6, 3))", new DataParameter("@p", mysqlDecValue, DataType.Decimal)), Is.EqualTo(decValue));
					Assert.That(db.Execute<decimal>("SELECT Cast(@p as decimal(6, 3))", new DataParameter("@p", mysqlDecValue, DataType.VarNumeric)), Is.EqualTo(decValue));
					Assert.That(db.Execute<DateTime>("SELECT Cast(@p as datetime)", new DataParameter("@p", new MySqlDataDateTime(dtValue), DataType.Date)), Is.EqualTo(dtValue));
					Assert.That(db.Execute<DateTime>("SELECT Cast(@p as datetime)", new DataParameter("@p", new MySqlDataDateTime(dtValue), DataType.DateTime)), Is.EqualTo(dtValue));
					Assert.That(db.Execute<DateTime>("SELECT Cast(@p as datetime)", new DataParameter("@p", new MySqlDataDateTime(dtValue), DataType.DateTime2)), Is.EqualTo(dtValue));

					// assert provider-specific parameter type name
					Assert.That(db.Execute<int>("SELECT ID FROM AllTypes WHERE tinyintDataType = @p", new DataParameter("@p", (sbyte)111, DataType.SByte)), Is.EqualTo(2));
					Assert.That(trace, Does.Contain("DECLARE @p Byte "));
				});

				// just check schema (no api used)
				db.DataProvider.GetSchemaProvider().GetSchema(db);
			}
		}

		[Test]
		public async Task TestMySqlConnector([IncludeDataSources(TestProvName.AllMySqlConnector)] string context, [Values] ConnectionType type)
		{
			Type providerType;

			using (var db = (DataConnection)GetDataContext(context))
			{
				providerType = db.DataProvider.GetType();
			}

			var provider = (MySqlDataProvider)Activator.CreateInstance(providerType)!;

			var unmapped = type == ConnectionType.MiniProfilerNoMappings;
			var connectionTypeName = "MySqlConnector.MySqlConnection, MySqlConnector";
			using (var db = CreateDataConnection(provider, context, type, connectionTypeName, ";AllowZeroDateTime=true"))
			{
				var trace = string.Empty;
				db.OnTraceConnection += (TraceInfo ti) =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						trace = ti.SqlText;
				};

				// test provider-specific type readers
				var dtValue = new DateTime(2012, 12, 12, 12, 12, 12, 0);
				Assert.That(db.Execute<MySqlConnectorDateTime>("SELECT Cast(@p as datetime)", new DataParameter("@p", dtValue, DataType.DateTime)).GetDateTime(), Is.EqualTo(dtValue));
				Assert.That(db.Execute<MySqlConnectorDateTime>("SELECT Cast(@p as datetime)", new DataParameter("@p", dtValue, DataType.DateTime)).GetDateTime(), Is.EqualTo(dtValue));
				var rawDtValue = db.Execute<object>("SELECT Cast(@p as datetime)", new DataParameter("@p", dtValue, DataType.DateTime));
				Assert.Multiple(() =>
				{
					Assert.That(rawDtValue is MySqlConnectorDateTime, Is.True);
					Assert.That(((MySqlConnectorDateTime)rawDtValue).GetDateTime(), Is.EqualTo(dtValue));
				});

				// test provider-specific parameter values
				using (new DisableBaseline("Output (datetime format) is culture-/system-dependent"))
				{
					Assert.Multiple(() =>
					{
						Assert.That(db.Execute<DateTime>("SELECT Cast(@p as datetime)", new DataParameter("@p", new MySqlConnectorDateTime(dtValue), DataType.Date)), Is.EqualTo(dtValue));
						Assert.That(db.Execute<DateTime>("SELECT Cast(@p as datetime)", new DataParameter("@p", new MySqlConnectorDateTime(dtValue), DataType.DateTime)), Is.EqualTo(dtValue));
						Assert.That(db.Execute<DateTime>("SELECT Cast(@p as datetime)", new DataParameter("@p", new MySqlConnectorDateTime(dtValue), DataType.DateTime2)), Is.EqualTo(dtValue));
					});
				}

				Assert.Multiple(() =>
				{
					// assert provider-specific parameter type name
					Assert.That(db.Execute<int>("SELECT ID FROM AllTypes WHERE tinyintDataType = @p", new DataParameter("@p", (sbyte)111, DataType.SByte)), Is.EqualTo(2));
					Assert.That(trace, Does.Contain("DECLARE @p Byte "));
				});

				// bulk copy
				MySqlTestUtils.EnableNativeBulk(db, context);
				try
				{
					db.BulkCopy(
						new BulkCopyOptions() { BulkCopyType = BulkCopyType.ProviderSpecific },
						Enumerable.Range(0, 1000).Select(n => new MySqlTests.AllTypeBaseProviderSpecific() { ID = 2000 + n }));

					Assert.That(trace.Contains("INSERT BULK"), Is.EqualTo(!unmapped));
				}
				finally
				{
					db.GetTable<MySqlTests.AllTypeBaseProviderSpecific>().Delete(p => p.ID >= 2000);
				}

				// async bulk copy
				try
				{
					await db.BulkCopyAsync(
						new BulkCopyOptions() { BulkCopyType = BulkCopyType.ProviderSpecific },
						Enumerable.Range(0, 1000).Select(n => new MySqlTests.AllTypeBaseProviderSpecific() { ID = 2000 + n }));

					Assert.That(trace.Contains("INSERT ASYNC BULK"), Is.EqualTo(!unmapped));
				}
				finally
				{
					await db.GetTable<MySqlTests.AllTypeBaseProviderSpecific>().DeleteAsync(p => p.ID >= 2000);
				}

				// just check schema (no api used)
				db.DataProvider.GetSchemaProvider().GetSchema(db);
			}
		}

		[Test]
		public void TestSystemSqlite([IncludeDataSources(ProviderName.SQLiteClassic)] string context, [Values] ConnectionType type)
		{
			using (var db = CreateDataConnection(SQLiteTools.GetDataProvider(SQLiteProvider.System), context, type, "System.Data.SQLite.SQLiteConnection, System.Data.SQLite"))
			{
				// just check schema (no api used)
				db.DataProvider.GetSchemaProvider().GetSchema(db);
			}
		}

		[Test]
		public void TestMicrosoftSqlite([IncludeDataSources(ProviderName.SQLiteMS)] string context, [Values] ConnectionType type)
		{
			using (var db = CreateDataConnection(SQLiteTools.GetDataProvider(SQLiteProvider.Microsoft), context, type, "Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite"))
			{
				// just check schema (no api used)
				db.DataProvider.GetSchemaProvider().GetSchema(db);
			}
		}

		sealed class TestDB2LUWDataProvider : DB2DataProvider
		{
			public TestDB2LUWDataProvider()
				: base(ProviderName.DB2LUW, DB2Version.LUW)
			{
			}
		}

		[Test]
		public void TestDB2([IncludeDataSources(ProviderName.DB2)] string context, [Values] ConnectionType type)
		{
			var unmapped = type == ConnectionType.MiniProfilerNoMappings;
			using (var db = CreateDataConnection(new TestDB2LUWDataProvider(), context, type, $"{DB2ProviderAdapter.ClientNamespace}.DB2Connection, {DB2ProviderAdapter.AssemblyName}"))
			{
				var trace = string.Empty;
				db.OnTraceConnection += (TraceInfo ti) =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						trace = ti.SqlText;
				};

				// we have DB2 tests for all types, so here we will test only one type (they all look the same)
				// test provider-specific type readers
				var longValue = -12335L;
				Assert.That(db.Execute<DB2Int64>("SELECT Cast(@p as bigint) FROM SYSIBM.SYSDUMMY1", new DataParameter("p", longValue, DataType.Int64)).Value, Is.EqualTo(longValue));
				var rawValue = db.Execute<object>("SELECT Cast(@p as bigint) FROM SYSIBM.SYSDUMMY1", new DataParameter("p", longValue, DataType.Int64));
				Assert.Multiple(() =>
				{
					// DB2DataReader returns provider-specific types only if asked explicitly
					Assert.That(rawValue is long, Is.True);
					Assert.That((long)rawValue, Is.EqualTo(longValue));

					// test provider-specific parameter values
					Assert.That(db.Execute<long>("SELECT Cast(@p as bigint) FROM SYSIBM.SYSDUMMY1", new DataParameter("p", new DB2Int64(longValue), DataType.Int64)), Is.EqualTo(longValue));

					//// assert provider-specific parameter type name
					Assert.That(db.Execute<int>("SELECT ID FROM AllTypes WHERE blobDataType = @p", new DataParameter("p", new byte[] { 50, 51, 52 }, DataType.Blob)), Is.EqualTo(2));
					Assert.That(trace, Does.Contain("DECLARE @p Blob("));
				});

				// bulk copy
				try
				{
					db.BulkCopy(
						new BulkCopyOptions() { BulkCopyType = BulkCopyType.ProviderSpecific },
						Enumerable.Range(0, 1000).Select(n => new ALLTYPE() { ID = 2000 + n }));

					Assert.That(trace.Contains("INSERT BULK"), Is.EqualTo(!unmapped));
				}
				finally
				{
					db.GetTable<ALLTYPE>().Delete(p => p.ID >= 2000);
				}

				// just check schema (no api used)
				db.DataProvider.GetSchemaProvider().GetSchema(db);

				// test connection server type property
				var cs = DataConnection.GetConnectionString(GetProviderName(context, out var _));
				using (var cn = DB2ProviderAdapter.Instance.CreateConnection(cs))
				{
					cn.Open();

					Assert.That(DB2ProviderAdapter.Instance.ConnectionWrapper(cn).eServerType, Is.EqualTo(DB2ProviderAdapter.DB2ServerTypes.DB2_UW));
				}
			}
		}

		[Test]
		public async Task TestRetryPolicy([IncludeDataSources(TestProvName.AllSqlServer)] string context, [Values] ConnectionType type)
		{
			Configuration.RetryPolicy.Factory = connection => new SqlServerRetryPolicy();
			try
			{
				await TestSqlServer(context, type);
			}
			finally
			{
				Configuration.RetryPolicy.Factory = null;
			}
		}

		[Test]
		public async Task TestSqlServer([IncludeDataSources(TestProvName.AllSqlServer)] string context, [Values] ConnectionType type)
		{
			string providerName;
			SqlServerVersion version;
			using (var db = (DataConnection)GetDataContext(context))
			{
				providerName = db.DataProvider.Name;
				version = ((SqlServerDataProvider)db.DataProvider).Version;
			}

			var tvpSupported = version >= SqlServerVersion.v2008;
			var hierarchyidSupported = version >= SqlServerVersion.v2008;

			var unmapped = type == ConnectionType.MiniProfilerNoMappings;
			using (new DisableBaseline("TODO: debug reason for inconsistent bulk copy sql"))
#if NETFRAMEWORK
			using (var db = CreateDataConnection(new SqlServerTests.TestSqlServerDataProvider(providerName, version, SqlServerProvider.SystemDataSqlClient), context, type, typeof(SqlConnection)))
#else
			using (var db = CreateDataConnection(new SqlServerTests.TestSqlServerDataProvider(providerName, version, SqlServerProvider.SystemDataSqlClient), context, type, "System.Data.SqlClient.SqlConnection, System.Data.SqlClient"))
#endif
			{
				var trace = string.Empty;
				db.OnTraceConnection += (TraceInfo ti) =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						trace = ti.SqlText;
				};

				var testValue = -1.2335m;
				Assert.That(db.Execute<SqlMoney>("SELECT Cast(@p as money)", new DataParameter("@p", testValue, DataType.Money)).Value, Is.EqualTo(testValue));
				var rawValue = db.Execute<object>("SELECT Cast(@p as money)", new DataParameter("@p", testValue, DataType.Money));
				Assert.Multiple(() =>
				{
					Assert.That(rawValue is decimal, Is.True);
					Assert.That((decimal)rawValue, Is.EqualTo(testValue));

					// test provider-specific parameter values
					Assert.That(db.Execute<decimal>("SELECT Cast(@p as money)", new DataParameter("@p", new SqlMoney(testValue), DataType.Money)), Is.EqualTo(testValue));

					//// assert provider-specific parameter type name
					Assert.That(db.Execute<int>("SELECT ID FROM AllTypes WHERE smalldatetimeDataType = @p", new DataParameter("@p", new DateTime(2012, 12, 12, 12, 12, 00), DataType.SmallDateTime)), Is.EqualTo(2));
					Assert.That(trace, Does.Contain("DECLARE @p SmallDateTime "));
				});

				if (hierarchyidSupported)
				{
					//// assert UDT type name
					var hid = SqlHierarchyId.Parse("/1/3/");
					Assert.Multiple(() =>
					{
						Assert.That(db.Execute<SqlHierarchyId>("SELECT Cast(@p as hierarchyid)", new DataParameter("@p", hid, DataType.Udt)), Is.EqualTo(hid));
						Assert.That(trace, Does.Contain("DECLARE @p hierarchyid -- Udt"));
						Assert.That(db.Execute<object>("SELECT Cast(@p as hierarchyid)", new DataParameter("@p", hid, DataType.Udt)), Is.EqualTo(hid));
					});
				}

				if (tvpSupported)
				{
					//// assert TVP type name
					var record     = SqlServerTestUtils.TestUDTData[0];
					var parameter  = new DataParameter("p", SqlServerTestUtils.GetSqlDataRecords()) { DbType = SqlServerTypesTests.TYPE_NAME };
					var readRecord = (from r in db.FromSql<SqlServerTestUtils.TVPRecord>($"select * from {parameter}")
									  where r.Id == record.Id
									  select new SqlServerTestUtils.TVPRecord() { Id = record.Id, Name = record.Name }).Single();

					Assert.Multiple(() =>
					{
						Assert.That(readRecord.Id, Is.EqualTo(record.Id));
						Assert.That(readRecord.Name, Is.EqualTo(record.Name));
						Assert.That(trace, Does.Contain($"DECLARE @p {SqlServerTypesTests.TYPE_NAME} "));
					});
				}

				// bulk copy
				TestBulkCopy();
				using (var tr = db.BeginTransaction())
					TestBulkCopy();
				// async bulk copy
				await TestBulkCopyAsync();
				using (var tr = db.BeginTransaction())
					await TestBulkCopyAsync();

				// test schema type name escaping
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);
				if (tvpSupported)
				{
					var proc = schema.Procedures.FirstOrDefault(p => p.ProcedureName == "TableTypeTestProc")!;
					Assert.That(proc, Is.Not.Null);
					Assert.That(proc.Parameters[0].SchemaType, Is.EqualTo("[dbo].[TestTableType]"));
				}

				// test SqlException handing
				Assert.That(SqlServerTransientExceptionDetector.IsHandled(new InvalidOperationException(), out var errors), Is.False);
				Exception? sex = null;
				try
				{
					db.Execute<object>("SELECT 1 / 0");
				}
				catch (Exception ex)
				{
					sex = ex;
				}

				Assert.Multiple(() =>
				{
					Assert.That(SqlServerTransientExceptionDetector.IsHandled(sex!, out errors), Is.True);
					Assert.That(errors!.Count(), Is.EqualTo(1));
					Assert.That(errors!.Single(), Is.EqualTo(8134));
				});

				var cs = DataConnection.GetConnectionString(GetProviderName(context, out var _));

				// test MARS not set
				Assert.That(db.IsMarsEnabled, Is.EqualTo(cs.ToLowerInvariant().Contains("multipleactiveresultsets=true")));

				// test server version
				using (var cn = ((SqlServerDataProvider)db.DataProvider).Adapter.CreateConnection(cs))
				{
					cn.Open();

					Assert.That(cn.ServerVersion, Is.Not.Null);
				}

				void TestBulkCopy()
				{
					try
					{
						long copied = 0;
						var options = new BulkCopyOptions()
						{
							BulkCopyType       = BulkCopyType.ProviderSpecific,
							NotifyAfter        = 500,
							RowsCopiedCallback = arg => copied = arg.RowsCopied
						};

						db.BulkCopy(
							options,
							Enumerable.Range(0, 1000).Select(n => new SqlServerTests.AllTypes() { ID = 2000 + n }));

						Assert.Multiple(() =>
						{
							Assert.That(trace.Contains("INSERT BULK"), Is.EqualTo(!unmapped));
							Assert.That(copied, Is.EqualTo(1000));
						});
					}
					finally
					{
						db.GetTable<SqlServerTests.AllTypes>().Delete(p => p.ID >= 3);

						// test quotation works
						Assert.That(trace, Does.Contain("[AllTypes]"));
					}
				}

				async Task TestBulkCopyAsync()
				{
					try
					{
						long copied = 0;
						var options = new BulkCopyOptions()
						{
							BulkCopyType       = BulkCopyType.ProviderSpecific,
							NotifyAfter        = 500,
							RowsCopiedCallback = arg => copied = arg.RowsCopied
						};

						await db.BulkCopyAsync(
							options,
							Enumerable.Range(0, 1000).Select(n => new SqlServerTests.AllTypes() { ID = 2000 + n }));

						Assert.Multiple(() =>
						{
							Assert.That(trace.Contains("INSERT ASYNC BULK"), Is.EqualTo(!unmapped));
							Assert.That(copied, Is.EqualTo(1000));
						});
					}
					finally
					{
						await db.GetTable<SqlServerTests.AllTypes>().DeleteAsync(p => p.ID >= 2000);

						// test quotation works
						Assert.That(trace, Does.Contain("[AllTypes]"));
					}
				}
			}
		}

		[Test]
		public async Task TestSqlServerMS([IncludeDataSources(TestProvName.AllSqlServer)] string context, [Values] ConnectionType type)
		{
			string providerName;
			SqlServerVersion version;
			using (var db = (DataConnection)GetDataContext(context))
			{
				providerName = db.DataProvider.Name;
				version      = ((SqlServerDataProvider)db.DataProvider).Version;
			}

			var tvpSupported = version >= SqlServerVersion.v2008;

			var unmapped = type == ConnectionType.MiniProfilerNoMappings;
			using (new DisableBaseline("TODO: debug reason for inconsistent bulk copy sql"))
			using (var db = CreateDataConnection(new SqlServerTests.TestSqlServerDataProvider(providerName, version, SqlServerProvider.MicrosoftDataSqlClient), context, type, "Microsoft.Data.SqlClient.SqlConnection, Microsoft.Data.SqlClient"))
			{
				var trace = string.Empty;
				db.OnTraceConnection += (TraceInfo ti) =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						trace = ti.SqlText;
				};

				var testValue = -1.2335m;
				Assert.That(db.Execute<SqlMoney>("SELECT Cast(@p as money)", new DataParameter("@p", testValue, DataType.Money)).Value, Is.EqualTo(testValue));
				var rawValue = db.Execute<object>("SELECT Cast(@p as money)", new DataParameter("@p", testValue, DataType.Money));
				Assert.Multiple(() =>
				{
					Assert.That(rawValue is decimal, Is.True);
					Assert.That((decimal)rawValue, Is.EqualTo(testValue));

					// test provider-specific parameter values
					Assert.That(db.Execute<decimal>("SELECT Cast(@p as money)", new DataParameter("@p", new SqlMoney(testValue), DataType.Money)), Is.EqualTo(testValue));

					//// assert provider-specific parameter type name
					Assert.That(db.Execute<int>("SELECT ID FROM AllTypes WHERE smalldatetimeDataType = @p", new DataParameter("@p", new DateTime(2012, 12, 12, 12, 12, 00), DataType.SmallDateTime)), Is.EqualTo(2));
					Assert.That(trace, Does.Contain("DECLARE @p SmallDateTime "));
				});

				// not supported by provider
				// assert UDT type name
				//var hid = SqlHierarchyId.Parse("/1/");
				//Assert.AreEqual(hid, db.Execute<SqlHierarchyId>("SELECT Cast(@p as hierarchyid)", new DataParameter("@p", hid, DataType.Udt)));
				//Assert.True(trace.Contains("DECLARE @p hierarchyid -- Udt"));
				//Assert.AreEqual(hid, db.Execute<object>("SELECT Cast(@p as hierarchyid)", new DataParameter("@p", hid, DataType.Udt)));

				if (tvpSupported)
				{
					//// assert TVP type name
					var record     = SqlServerTestUtils.TestUDTData[0];
					var parameter  = new DataParameter("p", SqlServerTestUtils.GetSqlDataRecordsMS()) { DbType = SqlServerTypesTests.TYPE_NAME };
					var readRecord = (from r in db.FromSql<SqlServerTestUtils.TVPRecord>($"select * from {parameter}")
									  where r.Id == record.Id
									  select new SqlServerTestUtils.TVPRecord() { Id = record.Id, Name = record.Name }).Single();

					Assert.Multiple(() =>
					{
						Assert.That(readRecord.Id, Is.EqualTo(record.Id));
						Assert.That(readRecord.Name, Is.EqualTo(record.Name));
						Assert.That(trace, Does.Contain($"DECLARE @p {SqlServerTypesTests.TYPE_NAME} "));
					});
				}

				// bulk copy
				TestBulkCopy();
				using (var tr = db.BeginTransaction())
					TestBulkCopy();

				// async bulk copy
				await TestBulkCopyAsync();
				using (var tr = db.BeginTransaction())
					await TestBulkCopyAsync();

				// test schema type name escaping
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);
				if (tvpSupported)
				{
					var proc = schema.Procedures.FirstOrDefault(p => p.ProcedureName == "TableTypeTestProc")!;
					Assert.That(proc, Is.Not.Null);
					Assert.That(proc.Parameters[0].SchemaType, Is.EqualTo("[dbo].[TestTableType]"));
				}

				// test SqlException handing
				Assert.That(SqlServerTransientExceptionDetector.IsHandled(new InvalidOperationException(), out var errors), Is.False);
				Exception? sex = null;
				try
				{
					db.Execute<object>("SELECT 1 / 0");
				}
				catch (Exception ex)
				{
					sex = ex;
				}

				Assert.Multiple(() =>
				{
					Assert.That(SqlServerTransientExceptionDetector.IsHandled(sex!, out errors), Is.True);
					Assert.That(errors!.Count(), Is.EqualTo(1));
					Assert.That(errors!.Single(), Is.EqualTo(8134));
				});

				var cs = DataConnection.GetConnectionString(GetProviderName(context, out var _));

				// test MARS not set
				Assert.That(db.IsMarsEnabled, Is.EqualTo(cs.ToLowerInvariant().Contains("multipleactiveresultsets=true")));

				// test server version
				using (var cn = ((SqlServerDataProvider)db.DataProvider).Adapter.CreateConnection(cs))
				{
					cn.Open();

					Assert.That(cn.ServerVersion, Is.Not.Null);
				}

				void TestBulkCopy()
				{
					try
					{
						long copied = 0;
						var options = new BulkCopyOptions()
						{
							BulkCopyType       = BulkCopyType.ProviderSpecific,
							NotifyAfter        = 500,
							RowsCopiedCallback = arg => copied = arg.RowsCopied
						};

						db.BulkCopy(
							options,
							Enumerable.Range(0, 1000).Select(n => new SqlServerTests.AllTypes() { ID = 2000 + n }));

						Assert.Multiple(() =>
						{
							Assert.That(trace.Contains("INSERT BULK"), Is.EqualTo(!unmapped));
							Assert.That(copied, Is.EqualTo(1000));
						});
					}
					finally
					{
						db.GetTable<SqlServerTests.AllTypes>().Delete(p => p.ID >= 2000);

						// test quotation works
						Assert.That(trace, Does.Contain("[AllTypes]"));
					}
				}

				async Task TestBulkCopyAsync()
				{
					try
					{
						long copied = 0;
						var options = new BulkCopyOptions()
						{
							BulkCopyType       = BulkCopyType.ProviderSpecific,
							NotifyAfter        = 500,
							RowsCopiedCallback = arg => copied = arg.RowsCopied
						};

						await db.BulkCopyAsync(
							options,
							Enumerable.Range(0, 1000).Select(n => new SqlServerTests.AllTypes() { ID = 2000 + n }));

						Assert.Multiple(() =>
						{
							Assert.That(trace.Contains("INSERT ASYNC BULK"), Is.EqualTo(!unmapped));
							Assert.That(copied, Is.EqualTo(1000));
						});
					}
					finally
					{
						await db.GetTable<SqlServerTests.AllTypes>().DeleteAsync(p => p.ID >= 2000);

						// test quotation works
						Assert.That(trace, Does.Contain("[AllTypes]"));
					}
				}
			}
		}

		[Test]
		public async Task TestSapHanaNative([IncludeDataSources(ProviderName.SapHanaNative)] string context, [Values] ConnectionType type)
		{
			var unmapped = type == ConnectionType.MiniProfilerNoMappings;
#if NETFRAMEWORK
			using (var db = CreateDataConnection(new SapHanaNativeDataProvider(), context, type, DbProviderFactories.GetFactory("Sap.Data.Hana").GetType().Assembly.GetType("Sap.Data.Hana.HanaConnection")!))
#else
			using (var db = CreateDataConnection(new SapHanaNativeDataProvider(), context, type, "Sap.Data.Hana.HanaConnection, Sap.Data.Hana.Core.v2.1"))
#endif
			{
				var trace = string.Empty;
				db.OnTraceConnection += (TraceInfo ti) =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						trace = ti.SqlText;
				};

				var binaryValue = new byte[] { 1, 2, 3 };
				Assert.Multiple(() =>
				{
					Assert.That(db.Execute<byte[]>("SELECT cast(:p as blob) from dummy", new DataParameter("p", binaryValue, DataType.Image)), Is.EqualTo(binaryValue));
					Assert.That(trace, Does.Contain("DECLARE @p Binary("));
					Assert.That(db.Execute<byte[]>("SELECT cast(:p as varbinary) from dummy", new DataParameter("p", binaryValue, DataType.Binary)), Is.EqualTo(binaryValue));
				});
				Assert.That(trace, Does.Contain("DECLARE @p Binary("));
				var textValue = "test";
				Assert.Multiple(() =>
				{
					Assert.That(db.Execute<string>("SELECT cast(:p as text) from dummy", new DataParameter("p", textValue, DataType.Text)), Is.EqualTo(textValue));
					Assert.That(trace, Does.Contain("DECLARE @p NVarChar("));
				});
				var ntextValue = "тест";
				Assert.Multiple(() =>
				{
					Assert.That(db.Execute<string>("SELECT cast(:p as nclob) from dummy", new DataParameter("p", ntextValue, DataType.NText)), Is.EqualTo(ntextValue));
					Assert.That(trace, Does.Contain("DECLARE @p  -- Xml"));
				});

				// bulk copy without and with transaction
				TestBulkCopy();
				using (var tr = db.BeginTransaction())
					TestBulkCopy();

				// async bulk copy without and with transaction
				await TestBulkCopyAsync();
				using (var tr = db.BeginTransaction())
					await TestBulkCopyAsync();

				// test schema type name escaping
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);

				void TestBulkCopy()
				{
					try
					{
						long copied = 0;
						var options = new BulkCopyOptions()
						{
							BulkCopyType       = BulkCopyType.ProviderSpecific,
							NotifyAfter        = 500,
							RowsCopiedCallback = arg => copied = arg.RowsCopied
						};

						db.BulkCopy(
							options,
							Enumerable.Range(0, 1000).Select(n => new SapHanaTests.AllType() { ID = 2000 + n }));

						Assert.Multiple(() =>
						{
							Assert.That(trace.Contains("INSERT BULK"), Is.EqualTo(!unmapped));
							Assert.That(copied, Is.EqualTo(1000));
						});
					}
					finally
					{
						db.GetTable<SapHanaTests.AllType>().Delete(p => p.ID >= 2000);
					}
				}

				async Task TestBulkCopyAsync()
				{
					try
					{
						long copied = 0;
						var options = new BulkCopyOptions()
						{
							BulkCopyType       = BulkCopyType.ProviderSpecific,
							NotifyAfter        = 500,
							RowsCopiedCallback = arg => copied = arg.RowsCopied
						};

						await db.BulkCopyAsync(
							options,
							Enumerable.Range(0, 1000).Select(n => new SapHanaTests.AllType() { ID = 2000 + n }));

						Assert.Multiple(() =>
						{
#if NETFRAMEWORK
							Assert.That(trace.Contains("INSERT ASYNC BULK"), Is.EqualTo(!unmapped));
#else
						Assert.That(trace.Contains("INSERT BULK"), Is.EqualTo(!unmapped));
#endif

							Assert.That(copied, Is.EqualTo(1000));
						});
					}
					finally
					{
						await db.GetTable<SapHanaTests.AllType>().DeleteAsync(p => p.ID >= 2000);
					}
				}
			}
		}

		[Test]
		public void TestSybaseNative([IncludeDataSources(ProviderName.Sybase)] string context, [Values] ConnectionType type)
		{
			var unmapped = type == ConnectionType.MiniProfilerNoMappings;
			using (var db = CreateDataConnection(SybaseTools.GetDataProvider(SybaseProvider.Unmanaged), context, type, DbProviderFactories.GetFactory("Sybase.Data.AseClient").GetType().Assembly.GetType("Sybase.Data.AseClient.AseConnection")!))
			{
				var trace = string.Empty;
				db.OnTraceConnection += (TraceInfo ti) =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						trace = ti.SqlText;
				};

				var ntextValue = "тест";
				Assert.Multiple(() =>
				{
					Assert.That(db.Execute<string>("SELECT @p", new DataParameter("p", ntextValue, DataType.NText)), Is.EqualTo(ntextValue));
					Assert.That(trace, Does.Contain("DECLARE @p Unitext("));
				});

				// bulk copy without and with transaction
				TestBulkCopy();
				using (var tr = db.BeginTransaction())
					TestBulkCopy();

				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);

				void TestBulkCopy()
				{
					try
					{
						long copied = 0;
						var options = new BulkCopyOptions()
						{
							BulkCopyType       = BulkCopyType.ProviderSpecific,
							NotifyAfter        = 100,
							RowsCopiedCallback = arg => copied = arg.RowsCopied,
							KeepIdentity       = true
						};

						db.BulkCopy(
							options,
							Enumerable.Range(0, 500).Select(n => new SybaseTests.AllType() { ID = 2000 + n, bitDataType = true }));

						Assert.Multiple(() =>
						{
							Assert.That(trace.Contains("INSERT BULK"), Is.EqualTo(!unmapped));
							Assert.That(copied, Is.EqualTo(500));
						});
					}
					finally
					{
						db.GetTable<SybaseTests.AllType>().Delete(p => p.ID >= 2000);
					}
				}
			}
		}

		[Test]
		public void TestSybaseManaged([IncludeDataSources(ProviderName.SybaseManaged)] string context, [Values] ConnectionType type)
		{
			var unmapped = type == ConnectionType.MiniProfilerNoMappings;
			using (var db = CreateDataConnection(SybaseTools.GetDataProvider(SybaseProvider.DataAction), context, type, "AdoNetCore.AseClient.AseConnection, AdoNetCore.AseClient"))
			{
				var trace = string.Empty;
				db.OnTraceConnection += (TraceInfo ti) =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						trace = ti.SqlText;
				};

				var ntextValue = "тест";
				Assert.Multiple(() =>
				{
					Assert.That(db.Execute<string>("SELECT @p", new DataParameter("p", ntextValue, DataType.NText)), Is.EqualTo(ntextValue));
					Assert.That(trace, Does.Contain("DECLARE @p Unitext("));
				});

				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);
			}
		}

#if NETFRAMEWORK
		[Test]
		public void TestInformixIFX([IncludeDataSources(ProviderName.Informix)] string context, [Values] ConnectionType type)
		{
			var unmapped  = type == ConnectionType.MiniProfilerNoMappings;
			var provider  = new TestInformixDataProvider(ProviderName.Informix, InformixProvider.Informix);
			using (var db = CreateDataConnection(provider, context, type, "IBM.Data.Informix.IfxConnection, IBM.Data.Informix"))
			{
				var trace = string.Empty;
				db.OnTraceConnection += (TraceInfo ti) =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						trace = ti.SqlText;
				};

				// IfxType type name test
				try
				{
					// ifx parameters not suported for select list, so we will just check generated trace
					db.Execute<string>("SELECT FIRST 1 ? FROM SYSTABLES", new DataParameter("p", "qqq", DataType.NText));
				}
				catch
				{
				}
				Assert.That(trace, Does.Contain("DECLARE @p Clob("));

				// provider-specific type classes
				if (!provider.Adapter.IsIDSProvider)
				{
					var ifxTSVal = db.Execute<IfxTimeSpan>("SELECT FIRST 1 intervalDataType FROM ALLTYPES WHERE intervalDataType IS NOT NULL");
					Assert.That(db.Execute<IfxTimeSpan>("SELECT FIRST 1 intervalDataType FROM ALLTYPES WHERE intervalDataType  = ?", new DataParameter("@p", ifxTSVal, DataType.Time)), Is.EqualTo(ifxTSVal));
					var rawValue = db.Execute<object>("SELECT FIRST 1 intervalDataType FROM ALLTYPES WHERE intervalDataType  = ?", new DataParameter("@p", ifxTSVal, DataType.Time));
					Assert.Multiple(() =>
					{
						Assert.That(rawValue is TimeSpan, Is.True);
						Assert.That(rawValue, Is.EqualTo((TimeSpan)ifxTSVal));
					});
				}
				else
				{
					var dateTimeValue = db.Execute<IfxDateTime>("SELECT FIRST 1 datetimeDataType FROM ALLTYPES WHERE ID = 2");
					Assert.That(db.Execute<IfxDateTime>("SELECT FIRST 1 datetimeDataType FROM ALLTYPES WHERE datetimeDataType  = ?", new DataParameter("@p", dateTimeValue, DataType.DateTime)), Is.EqualTo(dateTimeValue));
					var rawValue = db.Execute<object>("SELECT FIRST 1 datetimeDataType FROM ALLTYPES WHERE datetimeDataType  = ?", new DataParameter("@p", dateTimeValue, DataType.DateTime));
					Assert.Multiple(() =>
					{
						Assert.That(rawValue is DateTime, Is.True);
						Assert.That(rawValue, Is.EqualTo((DateTime)dateTimeValue));
					});
				}

				// bulk copy (transaction not supported)
				if (provider.Adapter.IsIDSProvider)
					TestBulkCopy();

				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);

				void TestBulkCopy()
				{
					try
					{
						long copied = 0;
						var options = new BulkCopyOptions()
						{
							BulkCopyType       = BulkCopyType.ProviderSpecific,
							NotifyAfter        = 500,
							RowsCopiedCallback = arg => copied = arg.RowsCopied,
							KeepIdentity       = !unmapped
						};

						db.BulkCopy(
							options,
							Enumerable.Range(0, 1000).Select(n => new InformixTests.AllType() { ID = 2000 + n }));

						Assert.Multiple(() =>
						{
							Assert.That(trace.Contains("INSERT BULK"), Is.EqualTo(!unmapped));
							Assert.That(copied, Is.EqualTo(1000));
						});
					}
					finally
					{
						db.GetTable<InformixTests.AllType>().Delete(p => p.ID >= 2000);
					}
				}
			}
		}
#endif

		sealed class TestInformixDataProvider : InformixDataProvider
		{
			public TestInformixDataProvider(string providerName, InformixProvider provider)
				: base(providerName, provider)
			{
			}
		}

		[Test]
		public void TestInformixDB2([IncludeDataSources(ProviderName.InformixDB2)] string context, [Values] ConnectionType type)
		{
			var unmapped = type == ConnectionType.MiniProfilerNoMappings;
			var provider = new TestInformixDataProvider(ProviderName.InformixDB2, InformixProvider.DB2);
			using (var db = CreateDataConnection(provider, context, type, $"{DB2ProviderAdapter.ClientNamespace}.DB2Connection, {DB2ProviderAdapter.AssemblyName}"))
			{
				var trace = string.Empty;
				db.OnTraceConnection += (TraceInfo ti) =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						trace = ti.SqlText;
				};

				// DB2Type type name test
				try
				{
					// ifx parameters not suported for select list, so we will just check generated trace
					db.Execute<string>("SELECT FIRST 1 @p FROM SYSTABLES", new DataParameter("@p", "qqq", DataType.NText));
				}
				catch { }

				Assert.That(trace, Does.Contain("DECLARE @p Clob("));

				// provider-specific type classes
				var dateTimeValue = db.Execute<DB2DateTime>("SELECT FIRST 1 datetimeDataType FROM ALLTYPES WHERE ID = 2");
				Assert.That(db.Execute<DB2DateTime>("SELECT FIRST 1 datetimeDataType FROM ALLTYPES WHERE datetimeDataType  = ?", new DataParameter("@p", dateTimeValue, DataType.DateTime)), Is.EqualTo(dateTimeValue));
				var rawValue = db.Execute<object>("SELECT FIRST 1 datetimeDataType FROM ALLTYPES WHERE datetimeDataType  = ?", new DataParameter("@p", dateTimeValue, DataType.DateTime));
				Assert.Multiple(() =>
				{
					Assert.That(rawValue is DateTime, Is.True);
					Assert.That(rawValue, Is.EqualTo((DateTime)dateTimeValue));
				});

				// bulk copy (transaction not supported)
				if (provider.Adapter.DB2BulkCopy != null)
					TestBulkCopy();

				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);

				void TestBulkCopy()
				{
					try
					{
						long copied = 0;
						var options = new BulkCopyOptions()
						{
							BulkCopyType       = BulkCopyType.ProviderSpecific,
							NotifyAfter        = 500,
							RowsCopiedCallback = arg => copied = arg.RowsCopied,
							KeepIdentity       = !unmapped
						};

						db.BulkCopy(
							options,
							Enumerable.Range(0, 1000).Select(n => new InformixTests.AllType() { ID = 2000 + n }));

						Assert.Multiple(() =>
						{
							Assert.That(trace.Contains("INSERT BULK"), Is.EqualTo(!unmapped));
							Assert.That(copied, Is.EqualTo(1000));
						});
					}
					finally
					{
						db.GetTable<InformixTests.AllType>().Delete(p => p.ID >= 2000);
					}
				}
			}
		}

#if NETFRAMEWORK
		[Test]
		public void TestOracleNative([IncludeDataSources(TestProvName.AllOracleNative)] string context, [Values] ConnectionType type)
		{
			var wrapped   = type == ConnectionType.MiniProfilerNoMappings || type == ConnectionType.MiniProfiler;
			var unmapped  = type == ConnectionType.MiniProfilerNoMappings;

			OracleDataProvider provider;
			Type connectionType;
			using (var db = GetDataConnection(context))
			{
				provider = new OracleTests.TestOracleDataProvider(db.DataProvider.Name, ((OracleDataProvider)db.DataProvider).Provider, ((OracleDataProvider)db.DataProvider).Version);
				connectionType = ((OracleDataProvider)db.DataProvider).Adapter.ConnectionType;
			}

			using (var db = CreateDataConnection(provider, context, type, connectionType))
			{
				var trace = string.Empty;
				db.OnTraceConnection += (TraceInfo ti) =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						trace = ti.SqlText;
				};

				var commandInterceptor = new SaveWrappedCommandInterceptor(wrapped);
				db.AddInterceptor(commandInterceptor);

				var ntextValue = "тест";
				Assert.Multiple(() =>
				{
					Assert.That(db.Execute<string>("SELECT :p FROM SYS.DUAL", new DataParameter("p", ntextValue, DataType.NText)), Is.EqualTo(ntextValue));
					Assert.That(trace, Does.Contain("DECLARE @p NClob "));
				});

				// provider-specific type classes and readers
				var decValue = 123.45m;
				var decimalValue = db.Execute<Oracle.DataAccess.Types.OracleDecimal>("SELECT :p FROM SYS.DUAL", new DataParameter("p", decValue, DataType.Decimal));
				Assert.That((decimal)decimalValue, Is.EqualTo(decValue));
				var rawValue = db.Execute<object>("SELECT :p FROM SYS.DUAL", new DataParameter("p", decValue, DataType.Decimal));
				Assert.Multiple(() =>
				{
					Assert.That(rawValue is decimal, Is.True);
					Assert.That((decimal)rawValue, Is.EqualTo(decValue));
				});

				// OracleTimeStampTZ parameter creation and conversion to DateTimeOffset
				var dtoVal = DateTimeOffset.Now;
				var dtoValue = db.Execute<DateTimeOffset>("SELECT :p FROM SYS.DUAL", new DataParameter("p", dtoVal, DataType.DateTimeOffset) { Precision = 6});
				dtoVal = dtoVal.AddTicks(-1 * (dtoVal.Ticks % 10));
				Assert.Multiple(() =>
				{
					Assert.That(dtoValue, Is.EqualTo(dtoVal));
					Assert.That(commandInterceptor.Parameters[0].Value.GetType(), Is.EqualTo(((OracleDataProvider)db.DataProvider).Adapter.OracleTimeStampTZType));
				});

				// bulk copy without transaction (transaction not supported)
				TestBulkCopy();

				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);
				// ToLower, because native prodiver returns it lowercased
				Assert.That(schema.Database.ToUpperInvariant(), Is.EqualTo(unmapped ? string.Empty : TestUtils.GetServerName(db, context).ToUpperInvariant()));
				//schema.DataSource not asserted, as it returns db hostname

				// dbcommand properties
				db.DisposeCommand();

				db.Execute<DateTimeOffset>("SELECT :p FROM SYS.DUAL", new DataParameter("p", dtoVal, DataType.DateTimeOffset));

				dynamic cmd = commandInterceptor.Command!;
				if (unmapped)
				{
					Assert.That(cmd.BindByName, Is.False);
					Assert.That(cmd.InitialLONGFetchSize, Is.Zero);
					Assert.That(cmd.ArrayBindCount, Is.Zero);
				}
				else
				{
					Assert.That(cmd.BindByName, Is.True);
					Assert.That(cmd.InitialLONGFetchSize, Is.EqualTo(-1));
					Assert.That(cmd.ArrayBindCount, Is.Zero);
				}

				void TestBulkCopy()
				{
					using (db.CreateLocalTable<OracleBulkCopyTable>())
					{
						long copied = 0;
						var options = new BulkCopyOptions()
						{
							BulkCopyType       = BulkCopyType.ProviderSpecific,
							NotifyAfter        = 500,
							RowsCopiedCallback = arg => copied = arg.RowsCopied,
							KeepIdentity       = true
						};

						db.BulkCopy(
							options,
							Enumerable.Range(0, 1000).Select(n => new OracleBulkCopyTable() { ID = 2000 + n }));

						Assert.Multiple(() =>
						{
							Assert.That(trace.Contains("INSERT BULK"), Is.EqualTo(!unmapped));
							Assert.That(copied, Is.EqualTo(1000));
						});
					}
				}
			}
		}
#endif

		[Test]
		public void TestOracleManaged([IncludeDataSources(TestProvName.AllOracleManaged)] string context, [Values] ConnectionType type)
		{
			var wrapped   = type == ConnectionType.MiniProfilerNoMappings || type == ConnectionType.MiniProfiler;
			var unmapped  = type == ConnectionType.MiniProfilerNoMappings;

			OracleDataProvider provider;
			using (var db = GetDataConnection(context))
				provider = new OracleTests.TestOracleDataProvider(db.DataProvider.Name, ((OracleDataProvider)db.DataProvider).Provider, ((OracleDataProvider)db.DataProvider).Version);

			using (var db = CreateDataConnection(provider, context, type, "Oracle.ManagedDataAccess.Client.OracleConnection, Oracle.ManagedDataAccess"))
			{
				var trace = string.Empty;
				db.OnTraceConnection += (TraceInfo ti) =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						trace = ti.SqlText;
				};

				var commandInterceptor = new SaveWrappedCommandInterceptor(wrapped);
				db.AddInterceptor(commandInterceptor);

				var ntextValue = "тест";
				Assert.Multiple(() =>
				{
					Assert.That(db.Execute<string>("SELECT :p FROM SYS.DUAL", new DataParameter("p", ntextValue, DataType.NText)), Is.EqualTo(ntextValue));
					Assert.That(trace, Does.Contain("DECLARE @p NClob "));
				});

				// provider-specific type classes and readers
				var decValue = 123.45m;
				var decimalValue = db.Execute<Oracle.ManagedDataAccess.Types.OracleDecimal>("SELECT :p FROM SYS.DUAL", new DataParameter("p", decValue, DataType.Decimal));
				Assert.That((decimal)decimalValue, Is.EqualTo(decValue));
				var rawValue = db.Execute<object>("SELECT :p FROM SYS.DUAL", new DataParameter("p", decValue, DataType.Decimal));
				Assert.Multiple(() =>
				{
					Assert.That(rawValue is decimal, Is.True);
					Assert.That((decimal)rawValue, Is.EqualTo(decValue));
				});

				// OracleTimeStampTZ parameter creation and conversion to DateTimeOffset
				var dtoVal = TestData.DateTimeOffset;

				// it is possible to define working reader expression for unmapped wrapper (at least for MiniProfiler)
				// but it doesn't make sense to do it righ now without user request
				// especially taking into account that more proper way is to define mappings
				if (!unmapped)
				{
					var dtoValue = db.Execute<DateTimeOffset>("SELECT :p FROM SYS.DUAL", new DataParameter("p", dtoVal, DataType.DateTimeOffset) { Precision = 6 });
					dtoVal = dtoVal.AddTicks(-1 * (dtoVal.Ticks % 10));
					Assert.Multiple(() =>
					{
						Assert.That(dtoValue, Is.EqualTo(dtoVal));
						Assert.That(commandInterceptor.Parameters[0].Value!.GetType()!, Is.EqualTo(((OracleDataProvider)db.DataProvider).Adapter.OracleTimeStampTZType));
					});
				}

				// bulk copy without transaction (transaction not supported)
				TestBulkCopy();

				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);
				Assert.That(schema.Database.ToUpperInvariant(), Is.EqualTo(unmapped ? string.Empty : TestUtils.GetServerName(db, context).ToUpperInvariant()));
				//schema.DataSource not asserted, as it returns db hostname

				// dbcommand properties
				db.DisposeCommand();

				// starting from v23 those properties available only on non-disposed command
				void assertCommand(DbCommand command)
				{
					dynamic cmd = type == ConnectionType.Raw ? command : ((dynamic)command).WrappedCommand;

					if (unmapped)
					{
						Assert.That(cmd.BindByName, Is.False);
						Assert.That(cmd.InitialLONGFetchSize, Is.Zero);
						Assert.That(cmd.ArrayBindCount, Is.Zero);
					}
					else
					{
						Assert.That(cmd.BindByName, Is.True);
						Assert.That(cmd.InitialLONGFetchSize, Is.EqualTo(-1));
						Assert.That(cmd.ArrayBindCount, Is.Zero);
					}
				}

				commandInterceptor.OnCommandSet += assertCommand;

				db.Execute<DateTimeOffset>("SELECT :p FROM SYS.DUAL", new DataParameter("p", dtoVal, DataType.DateTimeOffset));

				commandInterceptor.OnCommandSet -= assertCommand;

				void TestBulkCopy()
				{
					using (db.CreateLocalTable<OracleBulkCopyTable>())
					{
						long copied = 0;
						var options = new BulkCopyOptions()
						{
							BulkCopyType       = BulkCopyType.ProviderSpecific,
							NotifyAfter        = 500,
							RowsCopiedCallback = arg => copied = arg.RowsCopied,
							KeepIdentity       = true
						};

						db.BulkCopy(
							options,
							Enumerable.Range(0, 1000).Select(n => new OracleBulkCopyTable() { ID = 2000 + n }));

						Assert.Multiple(() =>
						{
							Assert.That(trace.Contains("INSERT BULK"), Is.EqualTo(!unmapped));
							Assert.That(copied, Is.EqualTo(1000));
						});
					}
				}
			}
		}

		[ActiveIssue(Configuration = TestProvName.Oracle21DevartDirect)]
		[Test]
		public void TestOracleDevart([IncludeDataSources(TestProvName.AllOracleDevart)] string context, [Values] ConnectionType type)
		{
			var wrapped   = type == ConnectionType.MiniProfilerNoMappings || type == ConnectionType.MiniProfiler;
			var unmapped  = type == ConnectionType.MiniProfilerNoMappings;

			OracleDataProvider provider;
			using (var db = GetDataConnection(context))
				provider = new OracleTests.TestOracleDataProvider(db.DataProvider.Name, ((OracleDataProvider)db.DataProvider).Provider, ((OracleDataProvider)db.DataProvider).Version);

			using (var db = CreateDataConnection(provider, context, type, "Devart.Data.Oracle.OracleConnection, Devart.Data.Oracle"))
			{
				var trace = string.Empty;
				db.OnTraceConnection += (TraceInfo ti) =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						trace = ti.SqlText;
				};

				var commandInterceptor = new SaveWrappedCommandInterceptor(wrapped);
				db.AddInterceptor(commandInterceptor);

				var ntextValue = "тест";
				Assert.Multiple(() =>
				{
					Assert.That(db.Execute<string>("SELECT :p FROM SYS.DUAL", new DataParameter("p", ntextValue, DataType.NText)), Is.EqualTo(ntextValue));
					Assert.That(trace, Does.Contain("DECLARE @p NClob(4) "));
				});

				// provider-specific type classes and readers
				var decValue = 123.45m;
				var decimalValue = db.Execute<Devart.Data.Oracle.OracleNumber>("SELECT :p FROM SYS.DUAL", new DataParameter("p", decValue, DataType.Decimal));
				Assert.That((decimal)decimalValue, Is.EqualTo(decValue));
				var rawValue = db.Execute<object>("SELECT :p FROM SYS.DUAL", new DataParameter("p", decValue, DataType.Decimal));
				Assert.Multiple(() =>
				{
					Assert.That(rawValue is decimal, Is.True);
					Assert.That((decimal)rawValue, Is.EqualTo(decValue));
				});

				// OracleTimeStampTZ parameter creation and conversion to DateTimeOffset
				var dtoVal = TestData.DateTimeOffset;

				// it is possible to define working reader expression for unmapped wrapper (at least for MiniProfiler)
				// but it doesn't make sense to do it righ now without user request
				// especially taking into account that more proper way is to define mappings
				if (!unmapped)
				{
					var dtoValue = db.Execute<DateTimeOffset>("SELECT :p FROM SYS.DUAL", new DataParameter("p", dtoVal, DataType.DateTimeOffset) { Precision = 6 });
					dtoVal = dtoVal.AddTicks(-1 * (dtoVal.Ticks % 10));
					Assert.Multiple(() =>
					{
						Assert.That(dtoValue, Is.EqualTo(dtoVal));
						Assert.That(commandInterceptor.Parameters[0].Value!.GetType()!, Is.EqualTo(((OracleDataProvider)db.DataProvider).Adapter.OracleTimeStampType));
					});
				}

				TestBulkCopy();
				using (var tr = db.BeginTransaction())
					TestBulkCopy();

				// dbcommand properties
				db.DisposeCommand();

				db.Execute<DateTimeOffset>("SELECT :p FROM SYS.DUAL", new DataParameter("p", dtoVal, DataType.DateTimeOffset));

				dynamic cmd = commandInterceptor.Command!;
				Assert.That(cmd.PassParametersByName, Is.Not.EqualTo(unmapped));

				void TestBulkCopy()
				{
					using (db.CreateLocalTable<OracleBulkCopyTable>())
					{
						long copied = 0;
						var options = new BulkCopyOptions()
						{
							BulkCopyType       = BulkCopyType.ProviderSpecific,
							NotifyAfter        = 500,
							RowsCopiedCallback = arg => copied = arg.RowsCopied,
							KeepIdentity       = true
						};

						db.BulkCopy(
							options,
							Enumerable.Range(0, 1000).Select(n => new OracleBulkCopyTable() { ID = 2000 + n }));

						Assert.Multiple(() =>
						{
							Assert.That(trace.Contains("INSERT BULK"), Is.EqualTo(!unmapped));
							Assert.That(copied, Is.EqualTo(1000));
						});
					}
				}
			}
		}

		[Table]
		public class OracleBulkCopyTable
		{
			[Column]
			public int ID { get; set; }
		}

		[Test]
		public async Task TestPostgreSQL([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values] ConnectionType type)
		{
			var wrapped = type == ConnectionType.MiniProfilerNoMappings || type == ConnectionType.MiniProfiler;
			var unmapped = type == ConnectionType.MiniProfilerNoMappings;

			Type providerType;

			using (var db = (DataConnection)GetDataContext(context))
			{
				providerType = db.DataProvider.GetType();
			}

			var provider = (PostgreSQLDataProvider)Activator.CreateInstance(providerType)!;

			using (var db = CreateDataConnection(provider, context, type, "Npgsql.NpgsqlConnection, Npgsql"))
			{
				// needed for proper AllTypes columns mapping
				db.AddMappingSchema(new MappingSchema(context));

				var trace = string.Empty;
				db.OnTraceConnection += (TraceInfo ti) =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						trace = ti.SqlText;
				};

				var jsonValue = /*lang=json,strict*/ "{ \"x\": 1 }";
				Assert.Multiple(() =>
				{
					Assert.That(db.Execute<string>("SELECT @p", new DataParameter("@p", jsonValue, DataType.Json)), Is.EqualTo(jsonValue));
					Assert.That(trace, Does.Contain("DECLARE @p Json"));
				});

				// provider-specific type classes and readers
				var interval = TimeSpan.FromSeconds(-1234);
				var nValue = db.Execute<NpgsqlTypes.NpgsqlInterval>("SELECT @p", new DataParameter("@p", interval, DataType.Interval));
				Assert.That(TimeSpan.FromTicks(nValue.Time * 10), Is.EqualTo(interval));
				var rawValue = db.Execute<object>("SELECT @p", new DataParameter("@p", interval, DataType.Interval));
				Assert.Multiple(() =>
				{
					Assert.That(rawValue is TimeSpan, Is.True);
					Assert.That((TimeSpan)rawValue, Is.EqualTo(interval));
				});

				// bulk copy without and with transaction
				TestBulkCopy();
				using (var tr = db.BeginTransaction())
					TestBulkCopy();

				// async bulk copy without and with transaction
				await TestBulkCopyAsync();
				using (var tr = db.BeginTransaction())
					await TestBulkCopyAsync();

				// provider types support by schema
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);
				var allTypes = schema.Tables.Where(t => t.TableName == "AllTypes").SingleOrDefault()!;
				Assert.That(allTypes, Is.Not.Null);
				var tsColumn = allTypes.Columns.Where(c => c.ColumnName == "intervalDataType").SingleOrDefault()!;
				Assert.That(tsColumn, Is.Not.Null);
				Assert.Multiple(() =>
				{
					Assert.That(tsColumn.ProviderSpecificType, Is.EqualTo("NpgsqlInterval"));

					// provider properties
					Assert.That(provider.HasMacAddr8, Is.EqualTo(true));
				});

				// type name generation from provider type
				using (db.CreateLocalTable<TestPostgreSQLTypeName>())
					Assert.That(trace, Does.Contain("\"Column\" circle     NULL"));

				// test server version
				var serverVersion = db.Execute<int>("SHOW server_version_num");
				var cs            = DataConnection.GetConnectionString(GetProviderName(context, out var _));
				using (var cn     = ((PostgreSQLDataProvider)db.DataProvider).Adapter.CreateConnection(cs))
				{
					cn.Open();

					var version = ((PostgreSQLDataProvider)db.DataProvider).Adapter.ConnectionWrapper(cn).PostgreSqlVersion;

					Assert.That(version.Major, Is.EqualTo(serverVersion / 10000));

					// machine-readable version number... sure
					if (version.Major == 9)
						Assert.That(version.Minor, Is.EqualTo((serverVersion / 100) % 100));
					else
						Assert.That(version.Minor, Is.EqualTo(serverVersion % 100));

				}

				void TestBulkCopy()
				{
					try
					{
						long copied = 0;
						var options = new BulkCopyOptions()
						{
							BulkCopyType       = BulkCopyType.ProviderSpecific,
							NotifyAfter        = 500,
							RowsCopiedCallback = arg => copied = arg.RowsCopied,
							KeepIdentity       = true
						};

						db.BulkCopy(
							options,
							Enumerable.Range(0, 1000).Select(n => new PostgreSQLTests.AllTypes() { ID = 2000 + n }));

						Assert.Multiple(() =>
						{
							Assert.That(trace.Contains("INSERT BULK"), Is.EqualTo(!unmapped));
							Assert.That(copied, Is.EqualTo(1000));
						});
					}
					finally
					{
						db.GetTable<PostgreSQLTests.AllTypes>().Delete(p => p.ID >= 2000);
					}
				}

				async Task TestBulkCopyAsync()
				{
					try
					{
						long copied = 0;
						var options = new BulkCopyOptions()
						{
							BulkCopyType       = BulkCopyType.ProviderSpecific,
							NotifyAfter        = 500,
							RowsCopiedCallback = arg => copied = arg.RowsCopied,
							KeepIdentity       = true
						};

						await db.BulkCopyAsync(
							options,
							Enumerable.Range(0, 1000).Select(n => new PostgreSQLTests.AllTypes() { ID = 2000 + n }));

						Assert.Multiple(() =>
						{
							Assert.That(trace.Contains("INSERT ASYNC BULK"), Is.EqualTo(!unmapped));
							Assert.That(copied, Is.EqualTo(1000));
						});
					}
					finally
					{
						await db.GetTable<PostgreSQLTests.AllTypes>().DeleteAsync(p => p.ID >= 2000);
					}
				}
			}
		}

		[Table]
		public class TestPostgreSQLTypeName
		{
			[Column]
			public NpgsqlTypes.NpgsqlCircle? Column { get; set; }
		}

		internal sealed class TestClickHouseDataProvider : ClickHouseDataProvider
		{
			public TestClickHouseDataProvider(string providerName, ClickHouseProvider provider)
				: base(providerName, provider)
			{
			}
		}

		[Table]
		public class ClickHouseBulkCopyTable
		{
			[Column]
			public int ID { get; set; }
		}

		[Test]
		public async ValueTask TestClickHouse([IncludeDataSources(TestProvName.AllClickHouse)] string context, [Values] ConnectionType type)
		{
			var unmapped = type == ConnectionType.MiniProfilerNoMappings;

			ClickHouseDataProvider provider;
			using (var db = GetDataConnection(context))
				provider = new TestClickHouseDataProvider(db.DataProvider.Name, ((ClickHouseDataProvider)db.DataProvider).Provider);

			using (var db = CreateDataConnection(provider, context, type, provider.Adapter.ConnectionType))
			{
				var trace = string.Empty;
				db.OnTraceConnection += (TraceInfo ti) =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						trace = ti.SqlText;
				};

				// native bulk copy not supported for mysql interface
				if (!context.IsAnyOf(ProviderName.ClickHouseMySql))
				{
					TestBulkCopy();
					await TestBulkCopyAsync();
				}

				void TestBulkCopy()
				{
					using (db.CreateLocalTable<ClickHouseBulkCopyTable>())
					{
						long copied  = 0;
						var  options = GetDefaultBulkCopyOptions(context) with
							{
								BulkCopyType       = BulkCopyType.ProviderSpecific,
								NotifyAfter        = 500,
								RowsCopiedCallback = arg => copied = arg.RowsCopied
							};

						db.BulkCopy(
							options,
							Enumerable.Range(0, 1000).Select(n => new ClickHouseBulkCopyTable() { ID = 2000 + n }));

						// Client provider supports only async API
						if (context.IsAnyOf(ProviderName.ClickHouseClient))
							Assert.That(trace.Contains("INSERT ASYNC BULK"), Is.EqualTo(!unmapped));
						else
							Assert.That(trace.Contains("INSERT INTO"), Is.EqualTo(true));
						Assert.That(copied, Is.EqualTo(1000));
					}
				}

				async Task TestBulkCopyAsync()
				{
					using (db.CreateLocalTable<ClickHouseBulkCopyTable>())
					{
						long copied  = 0;
						var  options = GetDefaultBulkCopyOptions(context) with
							{
								BulkCopyType       = BulkCopyType.ProviderSpecific,
								NotifyAfter        = 500,
								RowsCopiedCallback = arg => copied = arg.RowsCopied
							};

						await db.BulkCopyAsync(
							options,
							Enumerable.Range(0, 1000).Select(n => new ClickHouseBulkCopyTable() { ID = 2000 + n }));

						if (context.IsAnyOf(ProviderName.ClickHouseClient))
							Assert.That(trace.Contains("INSERT ASYNC BULK"), Is.EqualTo(!unmapped));
						else
							Assert.That(trace.Contains("INSERT INTO"), Is.EqualTo(true));
						Assert.That(copied, Is.EqualTo(1000));
					}
				}
			}
		}

		public enum ConnectionType
		{
			Raw,
			MiniProfiler,
			MiniProfilerNoMappings
		}

		private DataConnection CreateDataConnection(IDataProvider provider, string context, ConnectionType type, Type connectionType, string? csExtra = null)
		{
			return CreateDataConnection(provider, context, type, cs => (DbConnection)Activator.CreateInstance(connectionType, cs)!, csExtra);
		}

		private DataConnection CreateDataConnection(IDataProvider provider, string context, ConnectionType type, string connectionTypeName, string? csExtra = null)
		{
			return CreateDataConnection(provider, context, type, cs => (DbConnection)Activator.CreateInstance(Type.GetType(connectionTypeName, true)!, cs)!, csExtra);
		}

		private DataConnection CreateDataConnection(IDataProvider provider, string context, ConnectionType type, Func<string, DbConnection> connectionFactory, string? csExtra = null)
		{
			var db = new DataConnection(provider, options =>
			{
				// don't create connection using provider, or it will initialize types
				var cn = connectionFactory(DataConnection.GetConnectionString(context) + csExtra);

				switch (type)
				{
					case ConnectionType.MiniProfilerNoMappings      :
					case ConnectionType.MiniProfiler                :
						Assert.That(MiniProfiler.Current, Is.Not.Null);
						return new ProfiledDbConnection(cn, MiniProfiler.Current);
				}

				return cn;
			});

			switch (type)
			{
				case ConnectionType.MiniProfiler:
					db.AddInterceptor(UnwrapProfilerInterceptor.Instance);
					break;
			}

			return db;
		}
	}
}
