using System;
using System.IO;
using LinqToDB;
using NUnit.Framework;

using LinqToDB.Data.DataProvider;

namespace Tests.Create
{
	using Model;

	[TestFixture]
	public class CreateData : TestBase
	{
		static void RunScript(string configString, string divider, string name)
		{
			Console.WriteLine("=== " + name + " === \n");

			var text = File.ReadAllText(@"..\..\..\..\Data\Create Scripts\" + name + ".sql");

			var idx = text.IndexOf("SKIP " + configString + " BEGIN");

			if (idx >= 0)
				text = text.Substring(0, idx) + text.Substring(text.IndexOf("SKIP " + configString + " END"));

			var cmds = text.Replace("\r", "").Replace(divider, "\x1").Split('\x1');

			Exception exception = null;

			using (var db = new TestDbManager(configString))
			{
				foreach (var cmd in cmds)
				{
					var command = cmd.Trim();

					if (command.Length == 0)
						continue;

					try 
					{
						Console.WriteLine(command);
						db.SetCommand(command).ExecuteNonQuery();
						Console.WriteLine("\nOK\n");
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.Message);
						Console.WriteLine("\nFAILED\n");

						if (exception == null)
							exception = ex;
					}
				}

				if (exception != null)
					throw exception;

				db.InsertBatch(new[]
				{
					new LinqDataTypes { ID =  1, MoneyValue =  1.11m, DateTimeValue = new DateTime(2001, 1, 11, 1, 11, 21, 100), BoolValue = true,  GuidValue = new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883"), SmallIntValue =  1 },
					new LinqDataTypes { ID =  2, MoneyValue =  2.49m, DateTimeValue = new DateTime(2005, 5, 15, 5, 15, 25, 500), BoolValue = false, GuidValue = new Guid("bc663a61-7b40-4681-ac38-f9aaf55b706b"), SmallIntValue =  2 },
					new LinqDataTypes { ID =  3, MoneyValue =  3.99m, DateTimeValue = new DateTime(2009, 9, 19, 9, 19, 29,  90), BoolValue = true,  GuidValue = new Guid("d2f970c0-35ac-4987-9cd5-5badb1757436"), SmallIntValue =  3 },
					new LinqDataTypes { ID =  4, MoneyValue =  4.50m, DateTimeValue = new DateTime(2009, 9, 20, 9, 19, 29,  90), BoolValue = false, GuidValue = new Guid("40932fdb-1543-4e4a-ac2c-ca371604fb4b"), SmallIntValue =  4 },
					new LinqDataTypes { ID =  5, MoneyValue =  5.50m, DateTimeValue = new DateTime(2009, 9, 21, 9, 19, 29,  90), BoolValue = true,  GuidValue = new Guid("febe3eca-cb5f-40b2-ad39-2979d312afca"), SmallIntValue =  5 },
					new LinqDataTypes { ID =  6, MoneyValue =  6.55m, DateTimeValue = new DateTime(2009, 9, 22, 9, 19, 29,  90), BoolValue = false, GuidValue = new Guid("8d3c5d1d-47db-4730-9fe7-968f6228a4c0"), SmallIntValue =  6 },
					new LinqDataTypes { ID =  7, MoneyValue =  7.00m, DateTimeValue = new DateTime(2009, 9, 23, 9, 19, 29,  90), BoolValue = true,  GuidValue = new Guid("48094115-83af-46dd-a906-bff26ee21ee2"), SmallIntValue =  7 },
					new LinqDataTypes { ID =  8, MoneyValue =  8.99m, DateTimeValue = new DateTime(2009, 9, 24, 9, 19, 29,  90), BoolValue = false, GuidValue = new Guid("c1139f1f-1335-4cd4-937e-92602f732dd3"), SmallIntValue =  8 },
					new LinqDataTypes { ID =  9, MoneyValue =  9.63m, DateTimeValue = new DateTime(2009, 9, 25, 9, 19, 29,  90), BoolValue = true,  GuidValue = new Guid("46c5c512-3d4b-4cf7-b4e7-1de080789e5d"), SmallIntValue =  9 },
					new LinqDataTypes { ID = 10, MoneyValue = 10.77m, DateTimeValue = new DateTime(2009, 9, 26, 9, 19, 29,  90), BoolValue = false, GuidValue = new Guid("61b2bc55-147f-4b40-93ed-a4aa83602fee"), SmallIntValue = 10 },
					new LinqDataTypes { ID = 11, MoneyValue = 11.45m, DateTimeValue = new DateTime(2009, 9, 27, 9, 19, 29,  90), BoolValue = true,  GuidValue = new Guid("d3021d18-97f0-4dc0-98d0-f0c7df4a1230"), SmallIntValue = 11 },
				});

				db.InsertBatch(new[]
				{
					new Parent { ParentID = 1, Value1 = 1    },
					new Parent { ParentID = 2, Value1 = null },
					new Parent { ParentID = 3, Value1 = 3    },
					new Parent { ParentID = 4, Value1 = null },
					new Parent { ParentID = 5, Value1 = 5    },
					new Parent { ParentID = 6, Value1 = 6    },
					new Parent { ParentID = 7, Value1 = 1    },
				});

				db.InsertBatch(new[]
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
					new Child { ParentID = 7, ChildID = 77 },
				});

				db.InsertBatch(new[]
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
					new GrandChild { ParentID = 4, ChildID = 42, GrandChildID = 424 },
				});
			}
		}

#if !MOBILE
		[Test] public void DB2       () { RunScript(ProviderName.DB2,              "\nGO\n",  "DB2");        }
		[Test] public void Informix  () { RunScript(ProviderName.Informix,         "\nGO\n",  "Informix");   }
		[Test] public void Oracle    () { RunScript(ProviderName.Oracle,           "\n/\n",   "Oracle");     }
		[Test] public void Firebird  () { RunScript(ProviderName.Firebird,         "COMMIT;", "Firebird2");  }
		[Test] public void PostgreSQL() { RunScript(ProviderName.PostgreSQL,       "\nGO\n",  "PostgreSQL"); }
		[Test] public void MySql     () { RunScript(ProviderName.MySql,            "\nGO\n",  "MySql");      }
		[Test] public void Sql2005   () { RunScript(ProviderName.MsSql2005,        "\nGO\n",  "MsSql");      }
		[Test] public void Sybase    () { RunScript(ProviderName.Sybase,           "\nGO\n",  "Sybase");     }
#endif

		[Test] public void Sql2008   () { RunScript(ProviderName.MsSql2008,        "\nGO\n",  "MsSql");      }
		[Test] public void SqlCe     () { RunScript(ProviderName.SqlCe,            "\nGO\n",  "SqlCe");      }
		[Test] public void SqlCeData () { RunScript(ProviderName.SqlCe + ".Data",  "\nGO\n",  "SqlCe");      }
		[Test] public void SQLite    () { RunScript(ProviderName.SQLite,           "\nGO\n",  "SQLite");     }
		[Test] public void SQLiteData() { RunScript(ProviderName.SQLite + ".Data", "\nGO\n",  "SQLite");     }
		[Test] public void Access    () { RunScript(ProviderName.Access,           "\nGO\n",  "Access");     }
		[Test] public void AccessData() { RunScript(ProviderName.Access + ".Data", "\nGO\n",  "Access");     }
	}
}
