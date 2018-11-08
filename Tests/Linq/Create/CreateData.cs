using System;
using System.Data;
using System.IO;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
using LinqToDB.DataProvider.Access;
#endif

using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Tests._Create
{
	using Model;

	[TestFixture]
	[Category("Create")]
	// ReSharper disable once InconsistentNaming
	// ReSharper disable once TestClassNameSuffixWarning
	public class _CreateData : TestBase
	{
		static void RunScript(string configString, string divider, string name, Action<IDbConnection> action = null, string database = null)
		{
			Console.WriteLine("=== " + name + " === \n");

			var scriptFolder = Path.Combine(Path.GetFullPath("."), "Database", "Create Scripts");
			Console.WriteLine("Script folder exists: {1}; {0}", scriptFolder, Directory.Exists(scriptFolder));

			var sqlFileName  = Path.GetFullPath(Path.Combine(scriptFolder, Path.ChangeExtension(name, "sql")));
			Console.WriteLine("Sql file exists: {1}; {0}", sqlFileName, File.Exists(sqlFileName));

			var text = File.ReadAllText(sqlFileName);

			while (true)
			{
				var idx = text.IndexOf("SKIP " + configString + " BEGIN");

				if (idx >= 0)
					text = text.Substring(0, idx) + text.Substring(text.IndexOf("SKIP " + configString + " END", idx));
				else
					break;
			}

			var cmds = text
				.Replace("{DBNAME}", database)
				.Replace("\r",    "")
				.Replace(divider, "\x1")
				.Split  ('\x1')
				.Select (c => c.Trim())
				.Where  (c => !string.IsNullOrEmpty(c))
				.ToArray();

			if (DataConnection.TraceSwitch.TraceInfo)
				Console.WriteLine("Commands count: {0}", cmds.Length);

			Exception exception = null;

			using (var db = new TestDataConnection(configString))
			{
				//db.CommandTimeout = 20;

				foreach (var command in cmds)
				{
					try
					{
						if (DataConnection.TraceSwitch.TraceInfo)
							Console.WriteLine(command);

						if (configString == ProviderName.OracleNative)
						{
							// we need this to avoid errors in trigger creation when native provider
							// recognize ":NEW" as parameter
							var cmd = db.CreateCommand();
							cmd.CommandText = command;
							((dynamic)cmd).BindByName = false;
							cmd.ExecuteNonQuery();
						}
						else
							db.Execute(command);

						if (DataConnection.TraceSwitch.TraceInfo)
							Console.WriteLine("\nOK\n");
					}
					catch (Exception ex)
					{
						if (DataConnection.TraceSwitch.TraceError)
						{
							if (!DataConnection.TraceSwitch.TraceInfo)
								Console.WriteLine(command);

							var isDrop =
								command.TrimStart().StartsWith("DROP") ||
								command.TrimStart().StartsWith("CALL DROP");

#if APPVEYOR
							if (!isDrop)
#endif
							Console.WriteLine(ex.Message);

							if (isDrop)
							{
#if !APPVEYOR
								Console.WriteLine("\nnot too OK\n");
#endif
							}
							else
							{
								Console.WriteLine("\nFAILED\n");

								if (exception == null)
									exception = ex;
							}

						}
					}
				}

				if (exception != null)
					throw exception;

				if (DataConnection.TraceSwitch.TraceInfo)
					Console.WriteLine("\nBulkCopy LinqDataTypes\n");

				var options = new BulkCopyOptions();

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
					Console.WriteLine("\nBulkCopy Parent\n");

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
					Console.WriteLine("\nBulkCopy Child\n");

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
					Console.WriteLine("\nBulkCopy GrandChild\n");

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
			}
		}

		[Test]
		public void CreateDatabase([DataSources(false, ProviderName.OracleNative)] string context)
		{
			switch (context)
			{
				case ProviderName.Firebird      : RunScript(context,          "COMMIT;", "Firebird", FirebirdAction);       break;
				case TestProvName.Firebird3     : RunScript(context,          "COMMIT;", "Firebird", FirebirdAction);       break;
				case ProviderName.PostgreSQL    : RunScript(context,          "\nGO\n",  "PostgreSQL");                     break;
				case ProviderName.MySql         : RunScript(context,          "\nGO\n",  "MySql");                          break;
				case TestProvName.MySql57       : RunScript(context,          "\nGO\n",  "MySql");                          break;
				case TestProvName.MariaDB       : RunScript(context,          "\nGO\n",  "MySql");                          break;
				case ProviderName.SqlServer2000 : RunScript(context,          "\nGO\n",  "SqlServer2000");                  break;
				case ProviderName.SqlServer2005 : RunScript(context,          "\nGO\n",  "SqlServer");                      break;
				case ProviderName.SqlServer2008 : RunScript(context,          "\nGO\n",  "SqlServer");                      break;
				case ProviderName.SqlServer2012 : RunScript(context,          "\nGO\n",  "SqlServer");                      break;
				case ProviderName.SqlServer2014 : RunScript(context,          "\nGO\n",  "SqlServer");                      break;
				case TestProvName.SqlAzure      : RunScript(context,          "\nGO\n",  "SqlServer");                      break;
				case ProviderName.SQLiteMS      : RunScript(context,          "\nGO\n",  "SQLite",   SQLiteAction);
				                                  RunScript(context+ ".Data", "\nGO\n",  "SQLite",   SQLiteAction);         break;
#if !NETSTANDARD1_6
				case ProviderName.OracleManaged : RunScript(context,          "\n/\n",   "Oracle");                         break;
#endif
#if !NETSTANDARD1_6 && !NETSTANDARD2_0
				case ProviderName.SQLiteClassic : RunScript(context,          "\nGO\n",  "SQLite",   SQLiteAction);
				                                  RunScript(context+ ".Data", "\nGO\n",  "SQLite",   SQLiteAction);         break;
				case ProviderName.Sybase        : RunScript(context,          "\nGO\n",  "Sybase",   null, "TestData");     break;
				case ProviderName.SybaseManaged : RunScript(context,          "\nGO\n",  "Sybase",   null, "TestDataCore"); break;
				case ProviderName.DB2           : RunScript(context,          "\nGO\n",  "DB2");                            break;
				case ProviderName.Informix      : RunScript(context,          "\nGO\n",  "Informix", InformixAction);       break;
				case ProviderName.SqlCe         : RunScript(context,          "\nGO\n",  "SqlCe");
				                                  RunScript(context+ ".Data", "\nGO\n",  "SqlCe");                          break;
				case ProviderName.Access        : RunScript(context,          "\nGO\n",  "Access",   AccessAction);
				                                  RunScript(context+ ".Data", "\nGO\n",  "Access",   AccessAction);         break;
				case ProviderName.SapHana       : RunScript(context,          ";;\n"  ,  "SapHana");                        break;
#endif
				default: throw new InvalidOperationException(context);
			}
		}

#if !NETSTANDARD1_6 && !NETSTANDARD2_0

		static void AccessAction(IDbConnection connection)
		{

			using (var conn = AccessTools.CreateDataConnection(connection))
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
						datetimeDataType         = new DateTime(2012, 12, 12, 12, 12, 12),

						binaryDataType           = new byte[] { 1, 2, 3, 4 },
						varbinaryDataType        = new byte[] { 1, 2, 3, 5 },
						imageDataType            = new byte[] { 3, 4, 5, 6 },
						oleobjectDataType        = new byte[] { 5, 6, 7, 8 },

						uniqueidentifierDataType = new Guid("{6F9619FF-8B86-D011-B42D-00C04FC964FF}"),
					});
			}
		}

#endif

		void FirebirdAction(IDbConnection connection)
		{
			using (var conn = LinqToDB.DataProvider.Firebird.FirebirdTools.CreateDataConnection(connection))
			{
				conn.Execute(@"
					UPDATE PERSON
					SET
						FIRSTNAME = @FIRSTNAME,
						LASTNAME  = @LASTNAME
					WHERE PERSONID = 4",
					new
					{
						FIRSTNAME = "Jürgen",
						LASTNAME  = "König",
					});
			}
		}

		static void SQLiteAction(IDbConnection connection)
		{
			using (var conn = LinqToDB.DataProvider.SQLite.SQLiteTools.CreateDataConnection(connection))
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

		static void InformixAction(IDbConnection connection)
		{
			using (var conn = LinqToDB.DataProvider.Informix.InformixTools.CreateDataConnection(connection))
			{
				conn.Execute(@"
					UPDATE AllTypes
					SET
						byteDataType = ?
					WHERE ID = 2",
					new
					{
						blob = new byte[] { 1, 2 },
					});
			}
		}
	}
}
