using System;
using System.Data;
using System.IO;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
#if !NETSTANDARD
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
		static void RunScript(string configString, string divider, string name, Action<IDbConnection> action = null)
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
				.Replace("\r",    "")
				.Replace(divider, "\x1")
				.Split  ('\x1')
				.Select (c => c.Trim())
				.Where  (c => !string.IsNullOrEmpty(c))
				.ToArray();

			Console.WriteLine("Commands count: {0}", cmds.Length);

			Exception exception = null;

			using (var db = new TestDataConnection(configString))
			{
				//db.CommandTimeout = 20;

				foreach (var command in cmds)
				{
					try 
					{
						Console.WriteLine(command);
						db.Execute(command);
						Console.WriteLine("\nOK\n");
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.Message);

						if (command.TrimStart().StartsWith("DROP"))
							Console.WriteLine("\nnot too OK\n");
						else
						{
							Console.WriteLine("\nFAILED\n");

							if (exception == null)
								exception = ex;
						}
					}
				}

				if (exception != null)
					throw exception;

				Console.WriteLine("\nBulkCopy LinqDataTypes\n");

				var options = new BulkCopyOptions
				{
#if MONO
					BulkCopyType = BulkCopyType.MultipleRows
#endif						
				};

				db.BulkCopy(
					options,
					new []
					{
						new LinqDataTypes { ID =  1, MoneyValue =  1.11m, DateTimeValue = new DateTime(2001,  1,  11,  1, 11, 21, 100), BoolValue = true,  GuidValue = new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883"), SmallIntValue =  1, StringValue = null },
						new LinqDataTypes { ID =  2, MoneyValue =  2.49m, DateTimeValue = new DateTime(2005,  5,  15,  5, 15, 25, 500), BoolValue = false, GuidValue = new Guid("bc663a61-7b40-4681-ac38-f9aaf55b706b"), SmallIntValue =  2, StringValue = ""   },
						new LinqDataTypes { ID =  3, MoneyValue =  3.99m, DateTimeValue = new DateTime(2009,  9,  19,  9, 19, 29,  90), BoolValue = true,  GuidValue = new Guid("d2f970c0-35ac-4987-9cd5-5badb1757436"), SmallIntValue =  3, StringValue = "1"  },
						new LinqDataTypes { ID =  4, MoneyValue =  4.50m, DateTimeValue = new DateTime(2009,  9,  20,  9, 19, 29,  90), BoolValue = false, GuidValue = new Guid("40932fdb-1543-4e4a-ac2c-ca371604fb4b"), SmallIntValue =  4, StringValue = "2"  },
						new LinqDataTypes { ID =  5, MoneyValue =  5.50m, DateTimeValue = new DateTime(2009,  9,  20,  9, 19, 29,  90), BoolValue = true,  GuidValue = new Guid("febe3eca-cb5f-40b2-ad39-2979d312afca"), SmallIntValue =  5, StringValue = "3"  },
						new LinqDataTypes { ID =  6, MoneyValue =  6.55m, DateTimeValue = new DateTime(2009,  9,  22,  9, 19, 29,  90), BoolValue = false, GuidValue = new Guid("8d3c5d1d-47db-4730-9fe7-968f6228a4c0"), SmallIntValue =  6, StringValue = "4"  },
						new LinqDataTypes { ID =  7, MoneyValue =  7.00m, DateTimeValue = new DateTime(2009,  9,  23,  9, 19, 29,  90), BoolValue = true,  GuidValue = new Guid("48094115-83af-46dd-a906-bff26ee21ee2"), SmallIntValue =  7, StringValue = "5"  },
						new LinqDataTypes { ID =  8, MoneyValue =  8.99m, DateTimeValue = new DateTime(2009,  9,  24,  9, 19, 29,  90), BoolValue = false, GuidValue = new Guid("c1139f1f-1335-4cd4-937e-92602f732dd3"), SmallIntValue =  8, StringValue = "6"  },
						new LinqDataTypes { ID =  9, MoneyValue =  9.63m, DateTimeValue = new DateTime(2009,  9,  25,  9, 19, 29,  90), BoolValue = true,  GuidValue = new Guid("46c5c512-3d4b-4cf7-b4e7-1de080789e5d"), SmallIntValue =  9, StringValue = "7"  },
						new LinqDataTypes { ID = 10, MoneyValue = 10.77m, DateTimeValue = new DateTime(2009,  9,  26,  9, 19, 29,  90), BoolValue = false, GuidValue = new Guid("61b2bc55-147f-4b40-93ed-a4aa83602fee"), SmallIntValue = 10, StringValue = "8"  },
						new LinqDataTypes { ID = 11, MoneyValue = 11.45m, DateTimeValue = new DateTime(2009,  9,  27,  0,  0,  0,   0), BoolValue = true,  GuidValue = new Guid("d3021d18-97f0-4dc0-98d0-f0c7df4a1230"), SmallIntValue = 11, StringValue = "9"  },
						new LinqDataTypes { ID = 12, MoneyValue = 11.45m, DateTimeValue = new DateTime(2012, 11,   7, 19, 19, 29,  90), BoolValue = true,  GuidValue = new Guid("03021d18-97f0-4dc0-98d0-f0c7df4a1230"), SmallIntValue = 12, StringValue = "0"  }
					});

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
						new InheritanceParent2() {InheritanceParentId = 1, TypeDiscriminator = null, Name = null },
						new InheritanceParent2() {InheritanceParentId = 2, TypeDiscriminator = 1,    Name = null },
						new InheritanceParent2() {InheritanceParentId = 3, TypeDiscriminator = 2,    Name = "InheritanceParent2" }
					});

				db.BulkCopy(
					options,
					new[]
					{
						new InheritanceChild2() {InheritanceChildId = 1, TypeDiscriminator = null, InheritanceParentId = 1, Name = null },
						new InheritanceChild2() {InheritanceChildId = 2, TypeDiscriminator = 1,    InheritanceParentId = 2, Name = null },
						new InheritanceChild2() {InheritanceChildId = 3, TypeDiscriminator = 2,    InheritanceParentId = 3, Name = "InheritanceParent2" }
					});

				if (action != null)
					action(db.Connection);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.DB2)]           public void DB2          (string ctx) { RunScript(ctx,          "\nGO\n",  "DB2");           }
		[Test, IncludeDataContextSource(ProviderName.Informix)]      public void Informix     (string ctx) { RunScript(ctx,          "\nGO\n",  "Informix", InformixAction); }
		[Test, IncludeDataContextSource(ProviderName.OracleManaged)] public void Oracle       (string ctx) { RunScript(ctx,          "\n/\n",   "Oracle");        }
		[Test, IncludeDataContextSource(ProviderName.Firebird)]      public void Firebird     (string ctx) { RunScript(ctx,          "COMMIT;", "Firebird", FirebirdAction); }
		[Test, IncludeDataContextSource(ProviderName.PostgreSQL)]    public void PostgreSQL   (string ctx) { RunScript(ctx,          "\nGO\n",  "PostgreSQL");    }
		[Test, IncludeDataContextSource(ProviderName.MySql)]         public void MySql        (string ctx) { RunScript(ctx,          "\nGO\n",  "MySql");         }
		[Test, IncludeDataContextSource(TestProvName.MySql57)]       public void MySql57      (string ctx) { RunScript(ctx,          "\nGO\n",  "MySql");         }
		[Test, IncludeDataContextSource(TestProvName.MariaDB)]       public void MariaDB      (string ctx) { RunScript(ctx,          "\nGO\n",  "MySql");         }
		[Test, IncludeDataContextSource(ProviderName.SqlServer2000)] public void Sql2000      (string ctx) { RunScript(ctx,          "\nGO\n",  "SqlServer2000"); }
		[Test, IncludeDataContextSource(ProviderName.SqlServer2005)] public void Sql2005      (string ctx) { RunScript(ctx,          "\nGO\n",  "SqlServer");     }
		[Test, IncludeDataContextSource(ProviderName.Sybase)]        public void Sybase       (string ctx) { RunScript(ctx,          "\nGO\n",  "Sybase");        }
		[Test, IncludeDataContextSource(ProviderName.SqlServer2008)] public void Sql2008      (string ctx) { RunScript(ctx,          "\nGO\n",  "SqlServer");     }
		[Test, IncludeDataContextSource(ProviderName.SqlServer2012)] public void Sql2012      (string ctx) { RunScript(ctx,          "\nGO\n",  "SqlServer");     }
		[Test, IncludeDataContextSource(ProviderName.SqlServer2014)] public void Sql2014      (string ctx) { RunScript(ctx,          "\nGO\n",  "SqlServer");     }
		[Test, IncludeDataContextSource(TestProvName.SqlAzure)]      public void SqlAzure2012 (string ctx) { RunScript(ctx,          "\nGO\n",  "SqlServer");     }
		[Test, IncludeDataContextSource(ProviderName.SqlCe)]         public void SqlCe        (string ctx) { RunScript(ctx,          "\nGO\n",  "SqlCe");         }
		[Test, IncludeDataContextSource(ProviderName.SqlCe)]         public void SqlCeData    (string ctx) { RunScript(ctx+ ".Data", "\nGO\n",  "SqlCe");         }
		[Test, IncludeDataContextSource(TestProvName.SQLiteMs)]      public void SQLiteMs     (string ctx) { RunScript(ctx,          "\nGO\n",  "SQLite",   SQLiteAction); }
		[Test, IncludeDataContextSource(ProviderName.SQLite)]        public void SQLite       (string ctx) { RunScript(ctx,          "\nGO\n",  "SQLite",   SQLiteAction); }
		[Test, IncludeDataContextSource(ProviderName.SQLite)]        public void SQLiteData   (string ctx) { RunScript(ctx+ ".Data", "\nGO\n",  "SQLite",   SQLiteAction); }
		[Test, IncludeDataContextSource(ProviderName.Access)]        public void Access       (string ctx) { RunScript(ctx,          "\nGO\n",  "Access",   AccessAction); }
		[Test, IncludeDataContextSource(ProviderName.Access)]        public void AccessData   (string ctx) { RunScript(ctx+ ".Data", "\nGO\n",  "Access",   AccessAction); }
		[Test, IncludeDataContextSource(ProviderName.SapHana)]       public void SapHana      (string ctx) { RunScript(ctx,          ";;\n"  ,  "SapHana");       }

		static void AccessAction(IDbConnection connection)
		{
#if !NETSTANDARD
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
#endif
		}

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
