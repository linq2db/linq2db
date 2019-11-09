extern alias MySqlData;
extern alias MySqlConnector;

using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Access;
using LinqToDB.DataProvider.Firebird;
using LinqToDB.DataProvider.MySql;
using LinqToDB.DataProvider.SapHana;
using LinqToDB.DataProvider.SqlCe;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;
using NUnit.Framework;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;
using Tests.Model;

using MySqlDataDateTime           = MySqlData::MySql.Data.Types.MySqlDateTime;
using MySqlDataDecimal            = MySqlData::MySql.Data.Types.MySqlDecimal;
using MySqlConnectorDateTime      = MySqlConnector::MySql.Data.Types.MySqlDateTime;
using  MySqlDataMySqlConnection   = MySqlData::MySql.Data.MySqlClient.MySqlConnection;
using System.Globalization;

namespace Tests.Data
{
	[TestFixture]
	public class MiniProfilerTests : TestBase
	{
		[OneTimeSetUp]
		public void Init()
		{
#if NET46
			MiniProfiler.Settings.ProfilerProvider = new SingletonProfilerProvider();
			MiniProfiler.Start();
#else
			MiniProfiler.DefaultOptions.StartProfiler();
#endif
		}

		[OneTimeTearDown]
		public void Shutdown()
		{
#if NET46
			MiniProfiler.Stop(true);
#else
			MiniProfiler.Current.Stop(true);
#endif
		}

		[SetUp]
		public void InitTest()
		{
			// to prevent tests interference
			CommandInfo.ClearObjectReaderCache();
		}

		public class MiniProfilerDataContext : DataConnection
		{
			public MiniProfilerDataContext(string configurationString)
				: base(GetDataProvider(), GetConnection(configurationString)) { }

			private static IDataProvider GetDataProvider()
			{
				return new SqlServerDataProvider("MiniProfiler." + ProviderName.SqlServer2000, SqlServerVersion.v2012);
			}

			private static IDbConnection GetConnection(string configurationString)
			{
				LinqToDB.Common.Configuration.AvoidSpecificDataProviderAPI = true;
				var dbConnection = new SqlConnection(GetConnectionString(configurationString));
				return new ProfiledDbConnection(dbConnection, MiniProfiler.Current);
			}
		}

		[Test]
		public void Test1([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			try
			{
				using (var mpcon = new MiniProfilerDataContext(context))
				{
					mpcon.GetTable<Northwind.Category>().ToList();
				}
			}
			finally
			{
				LinqToDB.Common.Configuration.AvoidSpecificDataProviderAPI = false;
			}
		}

		// provider-specific tests
		// tests must check all code, that use provider-specific functionality for specific provider
		// also test must create new instance of provider, to not benefit from existing instance
		[Test]
		public void TestAccess([IncludeDataSources(ProviderName.Access)] string context, [Values] ConnectionType type, [Values] bool avoidApi)
		{
			var unmapped = type == ConnectionType.MiniProfilerNoMappings;// || type == ConnectionType.SimpleMiniProfilerNoMappings;
			using (new AvoidSpecificDataProviderAPI(avoidApi))
			{
#if NET46
				using (var db = CreateDataConnection(new AccessDataProvider(), context, type, cs => new System.Data.OleDb.OleDbConnection(cs)))
#else
				using (var db = CreateDataConnection(new AccessDataProvider(), context, type, "System.Data.OleDb.OleDbConnection, System.Data.OleDb"))
#endif
				{
					var trace = string.Empty;
					db.OnTraceConnection += (TraceInfo ti) =>
					{
						if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
							trace = ti.SqlText;
					};

					// assert provider-specific parameter type name
					// DateTime, DateTime2 => Date
					// Text => LongVarChar
					// NText => LongVarWChar
					Assert.AreEqual(2, db.Execute<int>("SELECT ID FROM AllTypes WHERE datetimeDataType = @p", new DataParameter("@p", new DateTime(2012, 12, 12, 12, 12, 12), DataType.DateTime)));
					Assert.True(trace.Contains("DECLARE @p Date "));
					Assert.AreEqual(2, db.Execute<int>("SELECT ID FROM AllTypes WHERE datetimeDataType = @p", new DataParameter("@p", new DateTime(2012, 12, 12, 12, 12, 12), DataType.DateTime2)));
					Assert.True(trace.Contains("DECLARE @p Date "));
					Assert.AreEqual(2, db.Execute<int>("SELECT ID FROM AllTypes WHERE textDataType = @p", new DataParameter("@p", "567", DataType.Text)));
					Assert.True(trace.Contains("DECLARE @p LongVarChar(3)"));
					Assert.AreEqual(2, db.Execute<int>("SELECT ID FROM AllTypes WHERE ntextDataType = @p", new DataParameter("@p", "111", DataType.NText)));
					Assert.True(trace.Contains("DECLARE @p LongVarWChar(3)"));

					// TODO: reenable, when transactions fixed
#if NET46
					// assert custom schema table access
					var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);
					Assert.AreEqual(!unmapped, schema.Tables.Any(t => t.ForeignKeys.Any()));
#endif
				}
			}
		}

		[Test]
		public void TestSapHanaOdbc([IncludeDataSources(ProviderName.SapHanaOdbc)] string context, [Values] ConnectionType type, [Values] bool avoidApi)
		{
			using (new AvoidSpecificDataProviderAPI(avoidApi))
			{
#if NET46
				using (var db = CreateDataConnection(new SapHanaOdbcDataProvider(), context, type, cs => new System.Data.Odbc.OdbcConnection(cs)))
#else
				using (var db = CreateDataConnection(new SapHanaOdbcDataProvider(), context, type, "System.Data.Odbc.OdbcConnection, System.Data.Odbc"))
#endif
				{
					// provider doesn't use provider-specific API, so we just query schema
					db.DataProvider.GetSchemaProvider().GetSchema(db, TestUtils.GetDefaultSchemaOptions(context));
				}
			}
		}

		[Test]
		public void TestFirebird([IncludeDataSources(TestProvName.AllFirebird)] string context, [Values] ConnectionType type, [Values] bool avoidApi)
		{
			var unmapped = type == ConnectionType.MiniProfilerNoMappings;// || type == ConnectionType.SimpleMiniProfilerNoMappings;
			using (new AvoidSpecificDataProviderAPI(avoidApi))
			{
				using (var db = CreateDataConnection(new FirebirdDataProvider(), context, type, "FirebirdSql.Data.FirebirdClient.FbConnection, FirebirdSql.Data.FirebirdClient"))
				{
					var trace = string.Empty;
					db.OnTraceConnection += (TraceInfo ti) =>
					{
						if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
							trace = ti.SqlText;
					};

					// assert provider-specific parameter type name
					Assert.AreEqual(2, db.Execute<int>("SELECT ID FROM AllTypes WHERE nvarcharDataType = @p", new DataParameter("@p", "3323", DataType.NVarChar)));
					Assert.True(trace.Contains("DECLARE @p VarChar"));

					// just check schema (no api used)
					db.DataProvider.GetSchemaProvider().GetSchema(db, TestUtils.GetDefaultSchemaOptions(context));

					// assert api resolved and callable
					// unfortunatelly it uses pre-created provider instance, so it doesn't test this call
					// properly when called with other tests (tested manually)
					// actually possible to test with nunit plugin with appdomain test isolation, but meh
					FirebirdTools.ClearAllPools();
				}
			}
		}

		[Test]
		public void TestSqlCe([IncludeDataSources(ProviderName.SqlCe)] string context, [Values] ConnectionType type, [Values] bool avoidApi)
		{
			var unmapped = type == ConnectionType.MiniProfilerNoMappings;// || type == ConnectionType.SimpleMiniProfilerNoMappings;
			using (new AvoidSpecificDataProviderAPI(avoidApi))
			{
				using (var db = CreateDataConnection(new SqlCeDataProvider(), context, type, DbProviderFactories.GetFactory("System.Data.SqlServerCe.4.0").GetType().Assembly.GetType("System.Data.SqlServerCe.SqlCeConnection")))
				{
					var trace = string.Empty;
					db.OnTraceConnection += (TraceInfo ti) =>
					{
						if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
							trace = ti.SqlText;
					};

					// assert provider-specific parameter type name
					Assert.AreEqual("111", db.Execute<string>("SELECT Cast(@p as ntext)", new DataParameter("@p", "111", DataType.Text)));
					Assert.True(trace.Contains("DECLARE @p NText"));
					Assert.AreEqual("111", db.Execute<string>("SELECT Cast(@p as ntext)", new DataParameter("@p", "111", DataType.NText)));
					Assert.True(trace.Contains("DECLARE @p NText"));
					Assert.AreEqual(2, db.Execute<int>("SELECT ID FROM AllTypes WHERE nvarcharDataType = @p", new DataParameter("@p", "3323", DataType.VarChar)));
					Assert.True(trace.Contains("DECLARE @p NVarChar"));
					Assert.AreEqual(2, db.Execute<int>("SELECT ID FROM AllTypes WHERE nvarcharDataType = @p", new DataParameter("@p", "3323", DataType.NVarChar)));
					Assert.True(trace.Contains("DECLARE @p NVarChar"));
					Assert.AreEqual(2, db.Execute<int>("SELECT ID FROM AllTypes WHERE binaryDataType = @p", new DataParameter("@p", new byte[] { 1 }, DataType.Binary)));
					Assert.True(trace.Contains("DECLARE @p Binary("));
					Assert.AreEqual(2, db.Execute<int>("SELECT ID FROM AllTypes WHERE varbinaryDataType = @p", new DataParameter("@p", new byte[] { 2 }, DataType.VarBinary)));
					Assert.True(trace.Contains("DECLARE @p VarBinary("));
					Assert.AreEqual(new byte[] { 0, 0, 0, 3 }, db.Execute<byte[]>("SELECT Cast(@p as image)", new DataParameter("@p", new byte[] { 0, 0, 0, 3 }, DataType.Image)));
					Assert.True(trace.Contains("DECLARE @p Image("));

					var tsVal = db.Execute<byte[]>("SELECT timestampDataType FROM AllTypes WHERE ID = 2");
					Assert.AreEqual(2, db.Execute<int>("SELECT ID FROM AllTypes WHERE timestampDataType = @p", new DataParameter("@p", tsVal, DataType.Timestamp)));
					Assert.True(trace.Contains("DECLARE @p Timestamp("));

					// just check schema (no api used)
					db.DataProvider.GetSchemaProvider().GetSchema(db, TestUtils.GetDefaultSchemaOptions(context));


					// assert api resolved and callable
					SqlCeTools.CreateDatabase($"TestSqlCe_{Guid.NewGuid():N}");
				}
			}
		}

		class MapperExpressionTest1
		{
			public DateTime Value { get; set; }
		}

		class MapperExpressionTest2
		{
			public MySqlDataDateTime Value { get; set; }
		}

		class MapperExpressionTest3
		{
			public object Value { get; set; }
		}

		// tests support of data reader methods by Mapper.Map (using MySql.Data provider only)
		[Test]
		public void TestMapperMap([IncludeDataSources(TestProvName.AllMySqlData)] string context, [Values] ConnectionType type, [Values] bool avoidApi)
		{
			using (new AvoidSpecificDataProviderAPI(avoidApi))
			{
				// AllowZeroDateTime is to enable MySqlDateTime type
				using (var db = CreateDataConnection(new MySqlDataProvider(ProviderName.MySqlOfficial), context, type, "MySql.Data.MySqlClient.MySqlConnection, MySql.Data", ";AllowZeroDateTime=true"))
				{
					var dtValue = new DateTime(2012, 12, 12, 12, 12, 12, 0);

					Assert.AreEqual(dtValue, db.FromSql<MapperExpressionTest1>("SELECT Cast(@p as datetime) as Value", new DataParameter("@p", dtValue, DataType.DateTime)).Single().Value);
					Assert.AreEqual(dtValue, db.FromSql<MapperExpressionTest2>("SELECT Cast(@p as datetime) as Value", new DataParameter("@p", dtValue, DataType.DateTime)).Single().Value.Value);
					var rawDtValue = db.FromSql<MapperExpressionTest3>("SELECT Cast(@p as datetime) as Value", new DataParameter("@p", dtValue, DataType.DateTime)).Single().Value;
					Assert.True(rawDtValue is MySqlDataDateTime);
					Assert.AreEqual(dtValue, ((MySqlDataDateTime)rawDtValue).Value);
				}
			}
		}

		public class LinqMySqlDataProvider : MySqlDataProvider
		{
			private readonly Func<string, IDbConnection> _connectionFactory;
			public LinqMySqlDataProvider(Func<string, IDbConnection> connectionFactory)
				: base(ProviderName.MySqlOfficial)
			{
				_connectionFactory = connectionFactory;
			}

			protected override IDbConnection CreateConnectionInternal(string connectionString)
			{
				return _connectionFactory(connectionString);
			}
		}

		// tests support of data reader methods by LinqService
		// full of hacks to made test work as expected
		[Test]
		public void TestLinqService([IncludeDataSources(true, ProviderName.MySql)] string context, [Values] ConnectionType type)
		{
			var provider = GetProviderName(context, out var isLinq);
			// not interested in non-remote case
			//if (!isLinq)
			//return;

			const string testContext = "test-linq-service-reader";

			// hacks to make remote context to work new custom dataprovider instance
			var cs = DataConnection.GetConnectionString(provider);
			DataConnection.AddOrSetConfiguration(testContext, cs + ";AllowZeroDateTime=true", testContext);
			DataConnection.AddDataProvider(testContext,  new LinqMySqlDataProvider(cs =>
			{
				var cn = new MySqlDataMySqlConnection(cs);

				switch (type)
				{
					case ConnectionType.MiniProfilerNoMappings:
					case ConnectionType.MiniProfiler:
						Assert.IsNotNull(MiniProfiler.Current);
						return new ProfiledDbConnection(cn, MiniProfiler.Current);
				}

				return cn;
			}));

			// TODO: probably we should add serialization for supported provider-specific types to provider's mapping schema
			var ms = new MappingSchema();
			ms.SetConvertExpression<MySqlDataDateTime, string>(value => value.Value.ToBinary().ToString(CultureInfo.InvariantCulture));
			ms.SetConvertExpression<string, MySqlDataDateTime>(value => new MySqlDataDateTime(DateTime.FromBinary(long.Parse(value, CultureInfo.InvariantCulture))));
			switch (type)
			{
				case ConnectionType.MiniProfiler:
					ms.SetConvertExpression<ProfiledDbConnection, IDbConnection>(db => db.WrappedConnection);
					ms.SetConvertExpression<ProfiledDbDataReader, IDataReader>(db => db.WrappedReader);
					break;
			}

			using (var db = GetDataContext(testContext + (isLinq ? ".LinqService" : null), ms))
			{
				var dtValue = new DateTime(2012, 12, 12, 12, 12, 12, 0);

				// ExecuteReader
				Assert.AreEqual(dtValue, db.FromSql<MapperExpressionTest1>("SELECT Cast(@p as datetime) as Value", new DataParameter("@p", dtValue, DataType.DateTime)).Single().Value);
				Assert.AreEqual(dtValue, db.FromSql<MapperExpressionTest2>("SELECT Cast(@p as datetime) as Value", new DataParameter("@p", dtValue, DataType.DateTime)).Single().Value.Value);
				// TODO: doesn't work due to object use, probably we should add type to remote context data
				//var rawDtValue = db.FromSql<MapperExpressionTest3>("SELECT Cast(@p as datetime) as Value", new DataParameter("@p", dtValue, DataType.DateTime)).Single().Value;
				//Assert.True(rawDtValue is MySqlDataDateTime);
				//Assert.AreEqual(dtValue, ((MySqlDataDateTime)rawDtValue).Value);
			}
		}

		[Test]
		public void TestMySqlData([IncludeDataSources(TestProvName.AllMySqlData)] string context, [Values] ConnectionType type, [Values] bool avoidApi)
		{
			var unmapped = type == ConnectionType.MiniProfilerNoMappings;// || type == ConnectionType.SimpleMiniProfilerNoMappings;
			using (new AvoidSpecificDataProviderAPI(avoidApi))
			{
				// AllowZeroDateTime is to enable MySqlDateTime type
				using (var db = CreateDataConnection(new MySqlDataProvider(ProviderName.MySqlOfficial), context, type, "MySql.Data.MySqlClient.MySqlConnection, MySql.Data", ";AllowZeroDateTime=true"))
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
						Assert.AreEqual(decValue, mysqlDecValue.Value);
					}

					var rawDecValue = db.Execute<object>("SELECT Cast(@p as decimal(6, 3))", new DataParameter("@p", decValue, DataType.Decimal));
					Assert.True(rawDecValue is decimal);
					Assert.AreEqual(decValue, (decimal)rawDecValue);

					var dtValue = new DateTime(2012, 12, 12, 12, 12, 12, 0);
					Assert.AreEqual(dtValue, db.Execute<MySqlDataDateTime>("SELECT Cast(@p as datetime)", new DataParameter("@p", dtValue, DataType.DateTime)).Value);
					var rawDtValue = db.Execute<object>("SELECT Cast(@p as datetime)", new DataParameter("@p", dtValue, DataType.DateTime));
					Assert.True(rawDtValue is MySqlDataDateTime);
					Assert.AreEqual(dtValue, ((MySqlDataDateTime)rawDtValue).Value);

					// test readers + mapper.map
					Assert.AreEqual(dtValue, db.FromSql<MapperExpressionTest1>("SELECT Cast(@p as datetime) as Value", new DataParameter("@p", dtValue, DataType.DateTime)).Single().Value);
					Assert.AreEqual(dtValue, db.FromSql<MapperExpressionTest2>("SELECT Cast(@p as datetime) as Value", new DataParameter("@p", dtValue, DataType.DateTime)).Single().Value.Value);
					rawDtValue = db.FromSql<MapperExpressionTest3>("SELECT Cast(@p as datetime) as Value", new DataParameter("@p", dtValue, DataType.DateTime)).Single().Value;
					Assert.True(rawDtValue is MySqlDataDateTime);
					Assert.AreEqual(dtValue, ((MySqlDataDateTime)rawDtValue).Value);

					// test provider-specific parameter values
					if (type == ConnectionType.MiniProfilerNoMappings)
					{
						decValue = 0;
					}
					Assert.AreEqual(decValue, db.Execute<decimal>("SELECT Cast(@p as decimal(6, 3))", new DataParameter("@p", mysqlDecValue, DataType.Decimal)));
					Assert.AreEqual(decValue, db.Execute<decimal>("SELECT Cast(@p as decimal(6, 3))", new DataParameter("@p", mysqlDecValue, DataType.VarNumeric)));
					Assert.AreEqual(dtValue, db.Execute<DateTime>("SELECT Cast(@p as datetime)", new DataParameter("@p", new MySqlDataDateTime(dtValue), DataType.Date)));
					Assert.AreEqual(dtValue, db.Execute<DateTime>("SELECT Cast(@p as datetime)", new DataParameter("@p", new MySqlDataDateTime(dtValue), DataType.DateTime)));
					Assert.AreEqual(dtValue, db.Execute<DateTime>("SELECT Cast(@p as datetime)", new DataParameter("@p", new MySqlDataDateTime(dtValue), DataType.DateTime2)));

					// assert provider-specific parameter type name
					Assert.AreEqual(2, db.Execute<int>("SELECT ID FROM AllTypes WHERE tinyintDataType = @p", new DataParameter("@p", (sbyte)111, DataType.SByte)));
					Assert.True(trace.Contains("DECLARE @p Byte "));

					// just check schema (no api used)
					db.DataProvider.GetSchemaProvider().GetSchema(db, TestUtils.GetDefaultSchemaOptions(context));
				}
			}
		}

		[Test]
		public void TestMySqlConnector([IncludeDataSources(ProviderName.MySqlConnector)] string context, [Values] ConnectionType type, [Values] bool avoidApi)
		{
			var unmapped = type == ConnectionType.MiniProfilerNoMappings;// || type == ConnectionType.SimpleMiniProfilerNoMappings;
			using (new AvoidSpecificDataProviderAPI(avoidApi))
			{
				using (var db = CreateDataConnection(new MySqlDataProvider(ProviderName.MySqlConnector), context, type, "MySql.Data.MySqlClient.MySqlConnection, MySqlConnector", ";AllowZeroDateTime=true"))
				{
					var trace = string.Empty;
					db.OnTraceConnection += (TraceInfo ti) =>
					{
						if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
							trace = ti.SqlText;
					};

					// test provider-specific type readers
					var dtValue = new DateTime(2012, 12, 12, 12, 12, 12, 0);
					Assert.AreEqual(dtValue, db.Execute<MySqlConnectorDateTime>("SELECT Cast(@p as datetime)", new DataParameter("@p", dtValue, DataType.DateTime)).GetDateTime());
					Assert.AreEqual(dtValue, db.Execute<MySqlConnectorDateTime>("SELECT Cast(@p as datetime)", new DataParameter("@p", dtValue, DataType.DateTime)).GetDateTime());
					var rawDtValue = db.Execute<object>("SELECT Cast(@p as datetime)", new DataParameter("@p", dtValue, DataType.DateTime));
					Assert.True(rawDtValue is MySqlConnectorDateTime);
					Assert.AreEqual(dtValue, ((MySqlConnectorDateTime)rawDtValue).GetDateTime());

					// test provider-specific parameter values
					Assert.AreEqual(dtValue, db.Execute<DateTime>("SELECT Cast(@p as datetime)", new DataParameter("@p", new MySqlConnectorDateTime(dtValue), DataType.Date)));
					Assert.AreEqual(dtValue, db.Execute<DateTime>("SELECT Cast(@p as datetime)", new DataParameter("@p", new MySqlConnectorDateTime(dtValue), DataType.DateTime)));
					Assert.AreEqual(dtValue, db.Execute<DateTime>("SELECT Cast(@p as datetime)", new DataParameter("@p", new MySqlConnectorDateTime(dtValue), DataType.DateTime2)));

					// assert provider-specific parameter type name
					Assert.AreEqual(2, db.Execute<int>("SELECT ID FROM AllTypes WHERE tinyintDataType = @p", new DataParameter("@p", (sbyte)111, DataType.SByte)));
					Assert.True(trace.Contains("DECLARE @p Byte "));

					// just check schema (no api used)
					db.DataProvider.GetSchemaProvider().GetSchema(db, TestUtils.GetDefaultSchemaOptions(context));
				}
			}
		}

		public enum ConnectionType
		{
			Raw,
			MiniProfiler,
			MiniProfilerNoMappings,
			// disabled for now, as it makes things too complicated
			//SimpleMiniProfiler,
			//SimpleMiniProfilerNoMappings
		}

		private DataConnection CreateDataConnection(IDataProvider provider, string context, ConnectionType type, Type connectionType, string csExtra = null)
		{
			return CreateDataConnection(provider, context, type, cs => (IDbConnection)Activator.CreateInstance(connectionType, cs), csExtra);
		}

		private DataConnection CreateDataConnection(IDataProvider provider, string context, ConnectionType type, string connectionTypeName, string csExtra = null)
		{
			return CreateDataConnection(provider, context, type, cs => (IDbConnection)Activator.CreateInstance(Type.GetType(connectionTypeName), cs), csExtra);
		}

		private DataConnection CreateDataConnection(IDataProvider provider, string context, ConnectionType type, Func<string, IDbConnection> connectionFactory, string csExtra = null)
		{
			var ms = new MappingSchema();
			DataConnection db = null;
			db = new DataConnection(provider, () =>
			{
				// don't create connection using provider, or it will initialize types
				var cn = connectionFactory(DataConnection.GetConnectionString(context) + csExtra);

				switch (type)
				{
					case ConnectionType.MiniProfilerNoMappings      :
					case ConnectionType.MiniProfiler                :
						Assert.IsNotNull(MiniProfiler.Current);
						return new ProfiledDbConnection((DbConnection)cn, MiniProfiler.Current);
					//case ConnectionType.SimpleMiniProfilerNoMappings:
					//case ConnectionType.SimpleMiniProfiler          :
					//	return new SimpleProfiledConnection(cn, MiniProfiler.Current);
				}

				return cn;
			});

			switch (type)
			{
				case ConnectionType.MiniProfiler:
					ms.SetConvertExpression<ProfiledDbConnection, IDbConnection>(db => db.WrappedConnection);
					ms.SetConvertExpression<ProfiledDbDataReader, IDataReader>(db => db.WrappedReader);
					break;
				//case ConnectionType.SimpleMiniProfiler:
				//	ms.SetConvertExpression<SimpleProfiledConnection, IDbConnection>(db => db.WrappedConnection);
				//	break;
			}

			db.AddMappingSchema(ms);

			return db;
		}
	}
}
