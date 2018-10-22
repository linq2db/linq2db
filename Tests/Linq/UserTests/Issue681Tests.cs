using System.Linq;

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
using System.ServiceModel;
#endif

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue681Tests : TestBase
	{
		[Table("LinqDataTypes")]
		class TestTable
		{
			[Column("ID")]
			public int ID { get; set; }
		}

		// for SAP HANA cross-server queries see comments how to configure SAP HANA in TestUtils.GetServerName() method
		[Test]
		public void TestTableFQN(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			var throws = false;
			string serverName;
			string schemaName;
			string dbName;

			using (var db = GetDataContext(context))
			{
				if (withServer && (!withDatabase || !withSchema) && (context.Contains("SqlServer") || context.Contains("Azure")))
				{
					// SQL Server FQN requires schema and db components for linked-server query
					throws = true;
				}

				if (withServer && !withDatabase && (context.Contains("SapHana") || context.Contains("Informix")))
				{
					// SAP HANA and Informix require db name for linked server queries
					throws = true;
				}

				if (withDatabase && !withSchema && context.Contains("DB2"))
				{
					throws = true;
				}

				using (new DisableLogging())
				{
					serverName = withServer   ? TestUtils.GetServerName(db)   : null;
					dbName     = withDatabase ? TestUtils.GetDatabaseName(db) : null;
					schemaName = withSchema   ? TestUtils.GetSchemaName(db)   : null;
				}

				var table = db.GetTable<TestTable>();

				if (withServer)   table = table.ServerName  (serverName);
				if (withDatabase) table = table.DatabaseName(dbName);
				if (withSchema)   table = table.SchemaName  (schemaName);

				if (throws && context.Contains(".LinqService"))
				{
#if !NETSTANDARD1_6 && !NETSTANDARD2_0
					Assert.Throws<FaultException<ExceptionDetail>>(() => table.ToList());
#endif
				}
				else if (throws)
					Assert.Throws<LinqToDBException>(() => table.ToList());
				else
					table.ToList();
			}
		}
	}
}
