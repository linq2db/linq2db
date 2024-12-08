using System;
using System.Data.Common;
using System.IO;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.Access;
using LinqToDB.DataProvider.Informix;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.SchemaProvider;

using NUnit.Framework;

using Tests;
using Tests.Model;

// for unknown reason order doesn't help on ubuntu 18, so namespace were removed and class name changed to be first in
// sort order
[TestFixture]
[Category(TestCategory.Create)]
[Order(-1)]
// ReSharper disable once InconsistentNaming
// ReSharper disable once TestClassNameSuffixWarning
public class a_CreateData : TestBase
{
	void RunScript(string configString, string divider, string name, Action<DbConnection>? action = null, string? databaseName = null)
	{
		TestContext.Out.WriteLine("=== " + name + " === \n");

		var scriptFolder = Path.Combine(Path.GetFullPath("."), "Database", "Create Scripts");
		TestContext.Out.WriteLine("Script folder exists: {1}; {0}", scriptFolder, Directory.Exists(scriptFolder));

		var sqlFileName  = Path.GetFullPath(Path.Combine(scriptFolder, Path.ChangeExtension(name, "sql")));
		TestContext.Out.WriteLine("Sql file exists: {1}; {0}", sqlFileName, File.Exists(sqlFileName));

		var text = File.ReadAllText(sqlFileName);

		while (true)
		{
			var idx = text.IndexOf($"SKIP {configString} BEGIN", StringComparison.Ordinal);

			if (idx >= 0)
				text = text[..idx] + text[text.IndexOf($"SKIP {configString} END", idx, StringComparison.Ordinal)..];
			else
				break;
		}

		text = string.Join(Environment.NewLine,
			text.Split('\n')
			.Select(l => l.Trim('\r', '\n'))
			.Select(l =>
			{
				var idx = l.IndexOf("-- SKIP ", StringComparison.Ordinal);
				return idx >= 0 ? l[..idx] : l;
			}));

		Exception? exception = null;

		using (var db = GetDataConnection(configString))
		{
			if (configString.IsAnyOf(TestProvName.AllOracleNative))
			{
				// we need this to avoid errors in trigger creation when native provider
				// recognize ":NEW" as parameter
				db.AddInterceptor(new BindByNameOracleCommandInterceptor());
			}
			//db.CommandTimeout = 20;

			var database = databaseName ?? db.Connection.Database;

			var cmds = text
				.Replace("{DBNAME}", database)
				.Replace("\r",    "")
				.Replace(divider, "\x1")
				.Split  ('\x1')
				.Select (c => c.Trim())
				.Where  (c => !string.IsNullOrEmpty(c))
				.ToArray();

			if (DataConnection.TraceSwitch.TraceInfo)
				TestContext.Out.WriteLine("Commands count: {0}", cmds.Length);

			foreach (var command in cmds)
			{
				try
				{
					if (DataConnection.TraceSwitch.TraceInfo)
						TestContext.Out.WriteLine(command);

					db.Execute(command);

					if (DataConnection.TraceSwitch.TraceInfo)
						TestContext.Out.WriteLine("\nOK\n");
				}
				catch (Exception ex)
				{
					if (DataConnection.TraceSwitch.TraceError)
					{
						if (!DataConnection.TraceSwitch.TraceInfo)
							TestContext.Out.WriteLine(command);

						var isDrop =
							command.TrimStart().StartsWith("DROP")          ||
							command.TrimStart().Contains("DROP PACKAGE ")   ||
							command.TrimStart().Contains("DROP PROCEDURE ") ||
							command.TrimStart().StartsWith("CALL DROP");

						TestContext.Out.WriteLine(ex.Message);

						if (isDrop)
						{
							TestContext.Out.WriteLine("\nnot too OK\n");
						}
						else
						{
							TestContext.Out.WriteLine("\nFAILED\n");

#pragma warning disable CA1508 // Avoid dead conditional code
							exception ??= ex;
#pragma warning restore CA1508 // Avoid dead conditional code
						}
					}
				}
			}

			if (DataConnection.TraceSwitch.TraceInfo)
				TestContext.Out.WriteLine("\nBulkCopy LinqDataTypes\n");

			var options = GetDefaultBulkCopyOptions(configString);

			db.BulkCopy(
				options,
				new []
				{
					new LinqDataTypes2 { ID =  1, MoneyValue =  1.11m, DateTimeValue = new DateTime(2001,  1,  11,  1, 11, 21, 100), BoolValue = true,  GuidValue = new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883"), SmallIntValue =  1, StringValue = null, BigIntValue = 1 },
					new LinqDataTypes2 { ID =  2, MoneyValue =  2.49m, DateTimeValue = new DateTime(2005,  5,  15,  5, 15, 25, 500), BoolValue = false, GuidValue = new Guid("bc663a61-7b40-4681-ac38-f9aaf55b706b"), SmallIntValue =  2, StringValue = "",   BigIntValue = 2 },
					new LinqDataTypes2 { ID =  3, MoneyValue =  3.99m, DateTimeValue = new DateTime(2009,  9,  19,  9, 19, 29,  90), BoolValue = true,  GuidValue = new Guid("d2f970c0-35ac-4987-9cd5-5badb1757436"), SmallIntValue =  3, StringValue = "1"  },
					new LinqDataTypes2 { ID =  4, MoneyValue =  4.50m, DateTimeValue = new DateTime(2009,  9,  20,  9, 19, 29,  90), BoolValue = false, GuidValue = new Guid("40932fdb-1543-4e4a-ac2c-ca371604fb4b"), SmallIntValue =  4, StringValue = "2"  },
					new LinqDataTypes2 { ID =  5, MoneyValue =  5.50m, DateTimeValue = new DateTime(2009,  9,  20,  9, 19, 29,  90), BoolValue = true,  GuidValue = new Guid("febe3eca-cb5f-40b2-ad39-2979d312afca"), SmallIntValue =  5, StringValue = "3"  },
					new LinqDataTypes2 { ID =  6, MoneyValue =  6.55m, DateTimeValue = new DateTime(2009,  9,  22,  9, 19, 29,  90), BoolValue = false, GuidValue = new Guid("8d3c5d1d-47db-4730-9fe7-968f6228a4c0"), SmallIntValue =  6, StringValue = "4"  },
					new LinqDataTypes2 { ID =  7, MoneyValue =  7.00m, DateTimeValue = new DateTime(2009,  9,  23,  9, 19, 29,  90), BoolValue = true,  GuidValue = new Guid("48094115-83af-46dd-a906-bff26ee21ee2"), SmallIntValue =  7, StringValue = "5"  },
					new LinqDataTypes2 { ID =  8, MoneyValue =  8.99m, DateTimeValue = new DateTime(2009,  9,  24,  9, 19, 29,  90), BoolValue = false, GuidValue = new Guid("c1139f1f-1335-4cd4-937e-92602f732dd3"), SmallIntValue =  8, StringValue = "6"  },
					new LinqDataTypes2 { ID =  9, MoneyValue =  9.63m, DateTimeValue = new DateTime(2009,  9,  25,  9, 19, 29,  90), BoolValue = true,  GuidValue = new Guid("46c5c512-3d4b-4cf7-b4e7-1de080789e5d"), SmallIntValue =  9, StringValue = "7"  },
					new LinqDataTypes2 { ID = 10, MoneyValue = 10.77m, DateTimeValue = new DateTime(2009,  9,  26,  9, 19, 29,  90), BoolValue = false, GuidValue = new Guid("61b2bc55-147f-4b40-93ed-a4aa83602fee"), SmallIntValue = 10, StringValue = "8"  },
					new LinqDataTypes2 { ID = 11, MoneyValue = 11.45m, DateTimeValue = new DateTime(2009,  9,  27,  0,  0,  0,   0), BoolValue = true,  GuidValue = new Guid("d3021d18-97f0-4dc0-98d0-f0c7df4a1230"), SmallIntValue = 11, StringValue = "9"  },
					new LinqDataTypes2 { ID = 12, MoneyValue = 11.45m, DateTimeValue = new DateTime(2012, 11,   7, 19, 19, 29,  90), BoolValue = true,  GuidValue = new Guid("03021d18-97f0-4dc0-98d0-f0c7df4a1230"), SmallIntValue = 12, StringValue = "0"  }
				});

			if (DataConnection.TraceSwitch.TraceInfo)
				TestContext.Out.WriteLine("\nBulkCopy Parent\n");

			db.BulkCopy(
				options,
				new []
				{
					new Parent { ParentID = 1, Value1 = 1    },
					new Parent { ParentID = 2, Value1 = null },
					new Parent { ParentID = 3, Value1 = 3    },
					new Parent { ParentID = 4, Value1 = null },
					new Parent { ParentID = 5, Value1 = 5    },
					new Parent { ParentID = 6, Value1 = 6    },
					new Parent { ParentID = 7, Value1 = 1    }
				});

			if (DataConnection.TraceSwitch.TraceInfo)
				TestContext.Out.WriteLine("\nBulkCopy Child\n");

			db.BulkCopy(
				options,
				new []
				{
					new Child { ParentID = 1, ChildID = 11 },
					new Child { ParentID = 2, ChildID = 21 },
					new Child { ParentID = 2, ChildID = 22 },
					new Child { ParentID = 3, ChildID = 31 },
					new Child { ParentID = 3, ChildID = 32 },
					new Child { ParentID = 3, ChildID = 33 },
					new Child { ParentID = 4, ChildID = 41 },
					new Child { ParentID = 4, ChildID = 42 },
					new Child { ParentID = 4, ChildID = 43 },
					new Child { ParentID = 4, ChildID = 44 },
					new Child { ParentID = 6, ChildID = 61 },
					new Child { ParentID = 6, ChildID = 62 },
					new Child { ParentID = 6, ChildID = 63 },
					new Child { ParentID = 6, ChildID = 64 },
					new Child { ParentID = 6, ChildID = 65 },
					new Child { ParentID = 6, ChildID = 66 },
					new Child { ParentID = 7, ChildID = 77 }
				});

			if (DataConnection.TraceSwitch.TraceInfo)
				TestContext.Out.WriteLine("\nBulkCopy GrandChild\n");

			db.BulkCopy(
				options,
				new []
				{
					new GrandChild { ParentID = 1, ChildID = 11, GrandChildID = 111 },
					new GrandChild { ParentID = 2, ChildID = 21, GrandChildID = 211 },
					new GrandChild { ParentID = 2, ChildID = 21, GrandChildID = 212 },
					new GrandChild { ParentID = 2, ChildID = 22, GrandChildID = 221 },
					new GrandChild { ParentID = 2, ChildID = 22, GrandChildID = 222 },
					new GrandChild { ParentID = 3, ChildID = 31, GrandChildID = 311 },
					new GrandChild { ParentID = 3, ChildID = 31, GrandChildID = 312 },
					new GrandChild { ParentID = 3, ChildID = 31, GrandChildID = 313 },
					new GrandChild { ParentID = 3, ChildID = 32, GrandChildID = 321 },
					new GrandChild { ParentID = 3, ChildID = 32, GrandChildID = 322 },
					new GrandChild { ParentID = 3, ChildID = 32, GrandChildID = 323 },
					new GrandChild { ParentID = 3, ChildID = 33, GrandChildID = 331 },
					new GrandChild { ParentID = 3, ChildID = 33, GrandChildID = 332 },
					new GrandChild { ParentID = 3, ChildID = 33, GrandChildID = 333 },
					new GrandChild { ParentID = 4, ChildID = 41, GrandChildID = 411 },
					new GrandChild { ParentID = 4, ChildID = 41, GrandChildID = 412 },
					new GrandChild { ParentID = 4, ChildID = 41, GrandChildID = 413 },
					new GrandChild { ParentID = 4, ChildID = 41, GrandChildID = 414 },
					new GrandChild { ParentID = 4, ChildID = 42, GrandChildID = 421 },
					new GrandChild { ParentID = 4, ChildID = 42, GrandChildID = 422 },
					new GrandChild { ParentID = 4, ChildID = 42, GrandChildID = 423 },
					new GrandChild { ParentID = 4, ChildID = 42, GrandChildID = 424 }
				});


			db.BulkCopy(
				options,
				new[]
				{
					new InheritanceParent2 {InheritanceParentId = 1, TypeDiscriminator = null, Name = null },
					new InheritanceParent2 {InheritanceParentId = 2, TypeDiscriminator = 1,    Name = null },
					new InheritanceParent2 {InheritanceParentId = 3, TypeDiscriminator = 2,    Name = "InheritanceParent2" }
				});

			db.BulkCopy(
				options,
				new[]
				{
					new InheritanceChild2() {InheritanceChildId = 1, TypeDiscriminator = null, InheritanceParentId = 1, Name = null },
					new InheritanceChild2() {InheritanceChildId = 2, TypeDiscriminator = 1,    InheritanceParentId = 2, Name = null },
					new InheritanceChild2() {InheritanceChildId = 3, TypeDiscriminator = 2,    InheritanceParentId = 3, Name = "InheritanceParent2" }
				});

			action?.Invoke(db.Connection);

			if (exception != null)
				throw exception;
		}
	}

	void RunScript(CreateDataScript script)
	{
		RunScript(script.ConfigString, script.Divider, script.Name, script.Action, script.Database);
	}

	[Test, Order(0)]
	public void CreateDatabase([CreateDatabaseSources] string context)
	{
		switch (context)
		{
			case string when context.IsAnyOf(TestProvName.AllFirebird)   : RunScript(context,          "COMMIT;", "Firebird", FirebirdAction);    break;
			case string when context.IsAnyOf(TestProvName.AllPostgreSQL) : RunScript(context,          "\nGO\n",  "PostgreSQL");                  break;
			case string when context.IsAnyOf(TestProvName.AllMySql)      : RunScript(context,          "\nGO\n",  "MySql");                       break;
			case string when context.IsAnyOf(TestProvName.AllSqlServer)  : RunScript(context,          "\nGO\n",  "SqlServer");                   break;
			case string when context.IsAnyOf(TestProvName.AllSQLiteBase) : RunScript(context,          "\nGO\n",  "SQLite",   SQLiteAction);
			                                                               RunScript(context+ ".Data", "\nGO\n",  "SQLite",   SQLiteAction);      break;
			case string when context.IsAnyOf(TestProvName.AllSQLiteMP)   : RunScript(context,          "\nGO\n",  "SQLite",   SQLiteAction);      break;
			case string when context.IsAnyOf(TestProvName.AllOracle)     : RunScript(context,          "\n/\n",   "Oracle",   OracleAction);      break;
			case string when context.IsAnyOf(TestProvName.AllSybase)     : RunScript(context,          "\nGO\n",  "Sybase");                      break;
			case ProviderName.Informix                                   : RunScript(context,          "\nGO\n",  "Informix", InformixAction);    break;
			case ProviderName.InformixDB2                                : RunScript(context,          "\nGO\n",  "Informix", InformixDB2Action); break;
			case ProviderName.DB2                                        : RunScript(context,          "\nGO\n",  "DB2");                         break;
			case string when context.IsAnyOf(TestProvName.AllSapHana)    : RunScript(context,          ";;\n"  ,  "SapHana");                     break;
			case ProviderName.Access                                     : RunScript(context,          "\nGO\n",  "Access",   AccessAction);
			                                                               RunScript(context+ ".Data", "\nGO\n",  "Access",   AccessAction);      break;
			case ProviderName.AccessOdbc                                 : RunScript(context,          "\nGO\n",  "Access",   AccessODBCAction);
			                                                               RunScript(context+ ".Data", "\nGO\n",  "Access",   AccessODBCAction);  break;
			case ProviderName.SqlCe                                      : RunScript(context,          "\nGO\n",  "SqlCe");
			                                                               RunScript(context+ ".Data", "\nGO\n",  "SqlCe");                       break;
			case string when context.IsAnyOf(TestProvName.AllClickHouse) : RunScript(context,          "\nGO\n",  "ClickHouse");                  break;
			default                                                      :
				var script = CustomizationSupport.Interceptor.InterceptCreateData(context);
				if (script != null)
				{
					RunScript(script);
					break;
				}
				throw new InvalidOperationException(context);
		}
	}

	static void AccessODBCAction(DbConnection connection)
	{

		using (var conn = AccessTools.CreateDataConnection(connection, AccessProvider.ODBC))
		{
			conn.Execute(@"
				INSERT INTO AllTypes
				(
					bitDataType, decimalDataType, smallintDataType, intDataType,tinyintDataType, moneyDataType, floatDataType, realDataType,
					datetimeDataType,
					charDataType, varcharDataType, textDataType, ncharDataType, nvarcharDataType, ntextDataType,
					binaryDataType, varbinaryDataType, imageDataType, oleobjectDataType,
					uniqueidentifierDataType
				)
				VALUES
				(
					1, 2222222, 25555, 7777777, 100, 100000, 20.31, 16.2,
					?,
					'1', '234', '567', '23233', '3323', '111',
					?, ?, ?, ?,
					?
				)",
				new
				{
					datetimeDataType         = new DateTime(2012, 12, 12, 12, 12, 12),

					binaryDataType           = new byte[] { 1, 2, 3, 4 },
					varbinaryDataType        = new byte[] { 1, 2, 3, 5 },
					imageDataType            = new byte[] { 3, 4, 5, 6 },
					oleobjectDataType        = new byte[] { 5, 6, 7, 8 },

					uniqueidentifierDataType = new Guid("{6F9619FF-8B86-D011-B42D-00C04FC964FF}"),
				});
		}
	}

	static void AccessAction(DbConnection connection)
	{
		using (var conn = AccessTools.CreateDataConnection(connection, AccessProvider.OleDb))
		{
			conn.Execute(@"
				INSERT INTO AllTypes
				(
					bitDataType, decimalDataType, smallintDataType, intDataType,tinyintDataType, moneyDataType, floatDataType, realDataType,
					datetimeDataType,
					charDataType, varcharDataType, textDataType, ncharDataType, nvarcharDataType, ntextDataType,
					binaryDataType, varbinaryDataType, imageDataType, oleobjectDataType,
					uniqueidentifierDataType
				)
				VALUES
				(
					1, 2222222, 25555, 7777777, 100, 100000, 20.31, 16.2,
					@datetimeDataType,
					'1', '234', '567', '23233', '3323', '111',
					@binaryDataType, @varbinaryDataType, @imageDataType, @oleobjectDataType,
					@uniqueidentifierDataType
				)",
				new
				{
					datetimeDataType = new DateTime(2012, 12, 12, 12, 12, 12),

					binaryDataType    = new byte[] { 1, 2, 3, 4 },
					varbinaryDataType = new byte[] { 1, 2, 3, 5 },
					imageDataType     = new byte[] { 3, 4, 5, 6 },
					oleobjectDataType = new byte[] { 5, 6, 7, 8 },

					uniqueidentifierDataType = new Guid("{6F9619FF-8B86-D011-B42D-00C04FC964FF}"),
				});
		}
	}

	void FirebirdAction(DbConnection connection)
	{
		using (var conn = LinqToDB.DataProvider.Firebird.FirebirdTools.CreateDataConnection(connection))
		{
			conn.Execute(@"
				UPDATE ""Person""
				SET
					""FirstName"" = @FIRSTNAME,
					""LastName""  = @LASTNAME
				WHERE ""PersonID"" = 4",
				new
				{
					FIRSTNAME = "Jürgen",
					LASTNAME  = "König",
				});

			using var _ = new DisableBaseline("Non-deterministic database cleanup");
			var sp      = conn.DataProvider.GetSchemaProvider();
			var schema  = sp.GetSchema(conn, new GetSchemaOptions { GetProcedures = false });

			foreach (var table in schema.Tables)
			{
				if (table.TableName!.StartsWith("Animals") ||
					table.TableName!.StartsWith("Eyes")    ||
					table.TableName!.StartsWith("xxPatient"))
				{
					conn.Execute($"DROP TABLE \"{table.TableName}\"");
				}
			}
		}
	}

	static void SQLiteAction(DbConnection connection)
	{
		using (var conn = SQLiteTools.CreateDataConnection(connection, SQLiteProvider.AutoDetect))
		{
			conn.Execute(@"
				UPDATE AllTypes
				SET
					binaryDataType           = @binaryDataType,
					varbinaryDataType        = @varbinaryDataType,
					imageDataType            = @imageDataType,
					uniqueidentifierDataType = @uniqueidentifierDataType
				WHERE ID = 2",
				new
				{
					binaryDataType           = new byte[] { 1 },
					varbinaryDataType        = new byte[] { 2 },
					imageDataType            = new byte[] { 0, 0, 0, 3 },
					uniqueidentifierDataType = new Guid("{6F9619FF-8B86-D011-B42D-00C04FC964FF}"),
				});
		}
	}

	static void OracleAction(DbConnection connection)
	{
		using (var conn = LinqToDB.DataProvider.Oracle.OracleTools.CreateDataConnection(connection, LinqToDB.DataProvider.Oracle.OracleVersion.v11, LinqToDB.DataProvider.Oracle.OracleProvider.Managed))
		{
			// if file is not configured under windows we assume
			// oracle is run from linux docker image
			// and test file created at /home/oracle/bfile.txt location
			if (0 == conn.Execute<int>(@"select dbms_lob.fileexists(bfilename('DATA_DIR', 'bfile.txt')) from dual"))
			{
				conn.Execute("CREATE OR REPLACE DIRECTORY DATA_DIR AS '/home/oracle'");
				conn.Execute("UPDATE \"AllTypes\" SET \"bfileDataType\" = bfilename('DATA_DIR', 'bfile.txt') WHERE \"ID\" = 2");
			}
		}
	}

	static void InformixAction(DbConnection connection)
	{
		using (var conn = InformixTools.CreateDataConnection(connection, InformixProvider.Informix))
		{
			conn.Execute(@"
				UPDATE AllTypes
				SET
					byteDataType = ?,
					textDataType = ?
				WHERE ID = 2",
				new
				{
					blob = new byte[] { 1, 2 },
					text = "BBBBB"
				});
		}
	}

	static void InformixDB2Action(DbConnection connection)
	{
		using (var conn = InformixTools.CreateDataConnection(connection, InformixProvider.DB2))
		{
			conn.Execute(@"
				UPDATE AllTypes
				SET
					byteDataType = ?,
					textDataType = ?
				WHERE ID = 2",
				new
				{
					blob = new byte[] { 1, 2 },
					text = "BBBBB"
				});
		}
	}
}
