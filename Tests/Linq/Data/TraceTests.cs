using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.Data
{
	using Model;
	using Tools;

	[TestFixture]
	public class TraceTests : TestBase
	{
		private TraceLevel                           OriginalTraceLevel { get; set; }
		private Action<string?, string?, TraceLevel> OriginalWrite      { get; set; } = null!;


		[OneTimeSetUp]
		public void SetTraceInfoLevel()
		{
			OriginalTraceLevel               = DataConnection.TraceSwitch.Level;
			OriginalWrite                    = DataConnection.WriteTraceLine;
			DataConnection.TraceSwitch.Level = TraceLevel.Info;
		}

		[OneTimeTearDown]
		public void RestoreOriginalTraceLevel()
		{
			DataConnection.TraceSwitch.Level = OriginalTraceLevel;
			DataConnection.WriteTraceLine    = OriginalWrite;
		}

		private IDictionary<TEnum, TValue> GetEnumValues<TEnum, TValue>(Func<TEnum, TValue> factory)
			where TEnum : notnull
		{
			var steps =
				from s in Enum.GetValues(typeof(TEnum)).OfType<TEnum>()
				select new
				{
					Enum = s,
					Value = factory(s)
				};

			return steps.ToDictionary(s => s.Enum, s => s.Value);
		}

		[Test]
		public void TraceInfoErrorsAreReportedForInvalidConnectionString([DataSources(false)] string context)
		{
			var events   = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db0 = (TestDataConnection)GetDataContext(context))
			using (var db  = new DataContext(db0.DataProvider.Name, "BAD"))
			{
				db.OnTraceConnection = e =>
				{
					events[e.TraceInfoStep] = e;
					counters[e.TraceInfoStep]++;
				};

				NUnitAssert.ThrowsAny(() => db.GetTable<Child>().ToList(), typeof(ArgumentException), typeof(InvalidOperationException));

				// steps called once
				Assert.AreEqual(1, counters[TraceInfoStep.Error]);
				Assert.AreEqual(1, counters[TraceInfoStep.Completed]);

				// steps never called
				Assert.AreEqual(0, counters[TraceInfoStep.BeforeExecute]);
				Assert.AreEqual(0, counters[TraceInfoStep.AfterExecute]);
				Assert.AreEqual(0, counters[TraceInfoStep.MapperCreated]);
			}
		}

		[Test]
		public async Task TraceInfoErrorsAreReportedForInvalidConnectionStringAsync([DataSources(false)] string context)
		{
			var events   = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db0 = (TestDataConnection)GetDataContext(context))
			using (var db  = new DataContext(db0.DataProvider.Name, "BAD"))
			{
				db.OnTraceConnection = e =>
				{
					events[e.TraceInfoStep] = e;
					counters[e.TraceInfoStep]++;
				};

				await NUnitAssert.ThrowsAnyAsync(() => db.GetTable<Child>().ToListAsync(), typeof(ArgumentException), typeof(InvalidOperationException));

				// steps called once
				Assert.AreEqual(1, counters[TraceInfoStep.Error]);
				Assert.AreEqual(1, counters[TraceInfoStep.Completed]);

				// steps never called
				Assert.AreEqual(0, counters[TraceInfoStep.BeforeExecute]);
				Assert.AreEqual(0, counters[TraceInfoStep.AfterExecute]);
				Assert.AreEqual(0, counters[TraceInfoStep.MapperCreated]);
			}
		}

		[Test]
		public void TraceInfoStepsAreReportedForLinqQuery([NorthwindDataContext] string context)
		{
			var events   = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db = GetDataConnection(context))
			{
				db.OnTraceConnection = e =>
				{
					events[e.TraceInfoStep] = e;
					counters[e.TraceInfoStep]++;
				};

				var _ = db.GetTable<Northwind.Category>().ToList();

				// the same command is reported on each step
				var command = events[TraceInfoStep.BeforeExecute]!.Command;
				Assert.AreSame(command, events[TraceInfoStep.AfterExecute]!.Command);
				Assert.That(events[TraceInfoStep.Completed]!.Command, Is.Null);
				Assert.NotNull(command);

				// steps called once
				Assert.AreEqual(1, counters[TraceInfoStep.BeforeExecute]);
				Assert.AreEqual(1, counters[TraceInfoStep.AfterExecute]);
				Assert.AreEqual(1, counters[TraceInfoStep.Completed]);

				// steps never called
				Assert.AreEqual(0, counters[TraceInfoStep.MapperCreated]);
				Assert.AreEqual(0, counters[TraceInfoStep.Error]);
			}
		}

		[Test]
		public void TraceInfoStepsAreReportedForDataReader([NorthwindDataContext] string context)
		{
			var events   = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db = GetDataConnection(context))
			{
				var sql = db.GetTable<Northwind.Category>().SqlText;
				db.OnTraceConnection = e =>
				{
					events[e.TraceInfoStep] = e;
					counters[e.TraceInfoStep]++;
				};

				using (var reader = db.ExecuteReader(sql))
				{
					var _ = reader.Query<Northwind.Category>().ToList();
				}

				// the same command is reported on each step
				var command = events[TraceInfoStep.BeforeExecute]!.Command;
				Assert.AreSame(command, events[TraceInfoStep.AfterExecute]!.Command);
				Assert.AreSame(command, events[TraceInfoStep.Completed]!.Command);
				Assert.NotNull(command);

				// steps called once
				Assert.AreEqual(1, counters[TraceInfoStep.BeforeExecute]);
				Assert.AreEqual(1, counters[TraceInfoStep.AfterExecute]);
				Assert.AreEqual(1, counters[TraceInfoStep.Completed]);

				// steps never called
				Assert.AreEqual(0, counters[TraceInfoStep.MapperCreated]);
				Assert.AreEqual(0, counters[TraceInfoStep.Error]);
			}
		}

		[Test]
		public async Task TraceInfoStepsAreReportedForDataReaderAsync([NorthwindDataContext] string context)
		{
			var events   = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db = GetDataConnection(context))
			{
				var sql = db.GetTable<Northwind.Category>().SqlText;
				db.OnTraceConnection = e =>
				{
					events[e.TraceInfoStep] = e;
					counters[e.TraceInfoStep]++;
				};

				await using (var reader = await new CommandInfo(db, sql).ExecuteReaderAsync())
				{
					await reader.QueryToListAsync<Northwind.Category>();
				}

				// the same command is reported on each step
				var command = events[TraceInfoStep.BeforeExecute]!.Command;
				Assert.AreSame(command, events[TraceInfoStep.AfterExecute]!.Command);
				Assert.AreSame(command, events[TraceInfoStep.Completed]!.Command);
				Assert.NotNull(command);

				// steps called once
				Assert.AreEqual(1, counters[TraceInfoStep.BeforeExecute]);
				Assert.AreEqual(1, counters[TraceInfoStep.AfterExecute]);
				Assert.AreEqual(1, counters[TraceInfoStep.Completed]);

				// steps never called
				Assert.AreEqual(0, counters[TraceInfoStep.MapperCreated]);
				Assert.AreEqual(0, counters[TraceInfoStep.Error]);
			}
		}

		[Test]
		public void TraceInfoStepsAreReportedForSqlQuery([NorthwindDataContext] string context)
		{
			var events = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db = new DataConnection(context))
			{
				var sql = db.GetTable<Northwind.Category>().SqlText;
				db.OnTraceConnection = e =>
				{
					events[e.TraceInfoStep] = e;
					counters[e.TraceInfoStep]++;
				};

				db.SetCommand(sql).Query<Northwind.Category>().ToArray();

				// the same command is reported on each step
				var command = events[TraceInfoStep.BeforeExecute]!.Command;
				Assert.AreSame(command, events[TraceInfoStep.AfterExecute]!.Command);
				Assert.AreSame(command, events[TraceInfoStep.Completed]!.Command);
				Assert.NotNull(command);

				// steps called once
				Assert.AreEqual(1, counters[TraceInfoStep.BeforeExecute]);
				Assert.AreEqual(1, counters[TraceInfoStep.AfterExecute]);
				Assert.AreEqual(1, counters[TraceInfoStep.Completed]);

				// steps never called
				Assert.AreEqual(0, counters[TraceInfoStep.MapperCreated]);
				Assert.AreEqual(0, counters[TraceInfoStep.Error]);
			}
		}


		[Test]
		public void TraceInfoStepsAreReportedForDataReaderQuery([NorthwindDataContext] string context)
		{
			var events = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db = new DataConnection(context))
			{
				var sql = db.GetTable<Northwind.Category>().SqlText;
				db.OnTraceConnection = e =>
				{
					events[e.TraceInfoStep] = e;
					counters[e.TraceInfoStep]++;
				};

				using (var reader = db.SetCommand(sql).ExecuteReader())
				{
					reader.Query<Northwind.Category>().ToArray();
				}

				// the same command is reported on each step
				var command = events[TraceInfoStep.BeforeExecute]!.Command;
				Assert.AreSame(command, events[TraceInfoStep.AfterExecute]!.Command);
				Assert.AreSame(command, events[TraceInfoStep.Completed]!.Command);
				Assert.NotNull(command);

				// steps called once
				Assert.AreEqual(1, counters[TraceInfoStep.BeforeExecute]);
				Assert.AreEqual(1, counters[TraceInfoStep.AfterExecute]);
				Assert.AreEqual(1, counters[TraceInfoStep.Completed]);

				// steps never called
				Assert.AreEqual(0, counters[TraceInfoStep.MapperCreated]);
				Assert.AreEqual(0, counters[TraceInfoStep.Error]);
			}
		}

		[Test]
		public void TraceInfoStepsAreReportedForLinqUpdate([NorthwindDataContext] string context)
		{
			var events = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db = new DataConnection(context))
			using (db.BeginTransaction())
			{
				db.OnTraceConnection = e =>
				{
					events[e.TraceInfoStep] = e;
					counters[e.TraceInfoStep]++;
				};

				db.GetTable<Northwind.Category>()
					.Set(c => c.CategoryName, c => c.CategoryName)
					.Update();

				// the same command is reported on each step
				var command = events[TraceInfoStep.BeforeExecute]!.Command;
				Assert.AreSame(command, events[TraceInfoStep.AfterExecute]!.Command);
				Assert.AreSame(command, events[TraceInfoStep.Completed]!.Command);
				Assert.NotNull(command);

				// steps called once
				Assert.AreEqual(1, counters[TraceInfoStep.BeforeExecute]);
				Assert.AreEqual(1, counters[TraceInfoStep.AfterExecute]);
				Assert.AreEqual(1, counters[TraceInfoStep.Completed]);

				// steps never called
				Assert.AreEqual(0, counters[TraceInfoStep.MapperCreated]);
				Assert.AreEqual(0, counters[TraceInfoStep.Error]);

				db.RollbackTransaction();
			}
		}

		[Test]
		public void TraceInfoStepsAreReportedForSqlUpdate([NorthwindDataContext] string context)
		{
			var events = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db = new DataConnection(context))
			using (db.BeginTransaction())
			{
				db.OnTraceConnection = e =>
				{
					events[e.TraceInfoStep] = e;
					counters[e.TraceInfoStep]++;
				};

				db.SetCommand(@"UPDATE Categories SET CategoryName = CategoryName WHERE 1=2").Execute();

				// the same command is reported on each step
				var command = events[TraceInfoStep.BeforeExecute]!.Command;
				Assert.AreSame(command, events[TraceInfoStep.AfterExecute]!.Command);
				Assert.AreSame(command, events[TraceInfoStep.Completed]!.Command);
				Assert.NotNull(command);

				// steps called once
				Assert.AreEqual(1, counters[TraceInfoStep.BeforeExecute]);
				Assert.AreEqual(1, counters[TraceInfoStep.AfterExecute]);
				Assert.AreEqual(1, counters[TraceInfoStep.Completed]);

				// steps never called
				Assert.AreEqual(0, counters[TraceInfoStep.MapperCreated]);
				Assert.AreEqual(0, counters[TraceInfoStep.Error]);

				db.RollbackTransaction();
			}
		}

		[Test]
		public void TraceInfoStepsAreReportedForSqlInsert([IncludeDataSources(false, TestProvName.AllSQLiteNorthwind)] string context)
		{
			var events = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db = new DataConnection(context))
			using (db.BeginTransaction())
			{
				db.OnTraceConnection = e =>
				{
					events[e.TraceInfoStep] = e;
					counters[e.TraceInfoStep]++;
				};

				db.SetCommand(@"INSERT INTO Categories(CategoryID, CategoryName) VALUES(1024, '1024')").Execute();

				// the same command is reported on each step
				var command = events[TraceInfoStep.BeforeExecute]!.Command;
				Assert.AreSame(command, events[TraceInfoStep.AfterExecute]!.Command);
				Assert.AreSame(command, events[TraceInfoStep.Completed]!.Command);
				Assert.NotNull(command);

				// steps called once
				Assert.AreEqual(1, counters[TraceInfoStep.BeforeExecute]);
				Assert.AreEqual(1, counters[TraceInfoStep.AfterExecute]);
				Assert.AreEqual(1, counters[TraceInfoStep.Completed]);

				// steps never called
				Assert.AreEqual(0, counters[TraceInfoStep.MapperCreated]);
				Assert.AreEqual(0, counters[TraceInfoStep.Error]);

				db.RollbackTransaction();
			}
		}

		[Test]
		public void TraceInfoStepsAreReportedForSqlDelete([NorthwindDataContext] string context)
		{
			var events = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db = new DataConnection(context))
			using (db.BeginTransaction())
			{
				db.OnTraceConnection = e =>
				{
					events[e.TraceInfoStep] = e;
					counters[e.TraceInfoStep]++;
				};

				db.SetCommand(@"DELETE FROM Categories WHERE CategoryID = 1024").Execute();

				// the same command is reported on each step
				var command = events[TraceInfoStep.BeforeExecute]!.Command;
				Assert.AreSame(command, events[TraceInfoStep.AfterExecute]!.Command);
				Assert.AreSame(command, events[TraceInfoStep.Completed]!.Command);
				Assert.NotNull(command);

				// steps called once
				Assert.AreEqual(1, counters[TraceInfoStep.BeforeExecute]);
				Assert.AreEqual(1, counters[TraceInfoStep.AfterExecute]);
				Assert.AreEqual(1, counters[TraceInfoStep.Completed]);

				// steps never called
				Assert.AreEqual(0, counters[TraceInfoStep.MapperCreated]);
				Assert.AreEqual(0, counters[TraceInfoStep.Error]);

				db.RollbackTransaction();
			}
		}

		[Test]
		public void TraceInfoStepsAreReportedForExecuteObject([NorthwindDataContext] string context)
		{
			var events = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db = new DataConnection(context))
			{
				var sql = db.GetTable<Northwind.Category>().SqlText;
				db.OnTraceConnection = e =>
				{
					events[e.TraceInfoStep] = e;
					counters[e.TraceInfoStep]++;
				};

				db.SetCommand(sql).Execute<Northwind.Category>();

				// the same command is reported on each step
				var command = events[TraceInfoStep.BeforeExecute]!.Command;
				Assert.AreSame(command, events[TraceInfoStep.AfterExecute]!.Command);
				Assert.AreSame(command, events[TraceInfoStep.Completed]!.Command);
				Assert.NotNull(command);

				// steps called once
				Assert.AreEqual(1, counters[TraceInfoStep.BeforeExecute]);
				Assert.AreEqual(1, counters[TraceInfoStep.AfterExecute]);
				Assert.AreEqual(1, counters[TraceInfoStep.Completed]);

				// steps never called
				Assert.AreEqual(0, counters[TraceInfoStep.MapperCreated]);
				Assert.AreEqual(0, counters[TraceInfoStep.Error]);
			}
		}

		[Test]
		public void TraceInfoStepsAreReportedForCommitedTransaction([NorthwindDataContext] string context)
		{
			var events = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db = new DataConnection(context))
			{
				db.OnTraceConnection = e =>
				{
					events[e.TraceInfoStep] = e;
					counters[e.TraceInfoStep]++;
				};
				using (db.BeginTransaction())
				{
					// Begin transaction command is reported on each step
					Assert.AreEqual("BeginTransaction", events[TraceInfoStep.BeforeExecute]!.CommandText);
					Assert.AreEqual("BeginTransaction", events[TraceInfoStep.AfterExecute]!.CommandText);

					db.SetCommand(@"UPDATE Categories SET CategoryName = CategoryName WHERE 1=2").Execute();
					db.CommitTransaction();

					// Commit transaction command is reported on each step
					Assert.AreEqual("CommitTransaction", events[TraceInfoStep.BeforeExecute]!.CommandText);
					Assert.AreEqual("CommitTransaction", events[TraceInfoStep.AfterExecute]!.CommandText);

					// steps called once for BeginTransaction once for Update and once for CommitTransaction
					Assert.AreEqual(3, counters[TraceInfoStep.BeforeExecute]);
					Assert.AreEqual(3, counters[TraceInfoStep.AfterExecute]);

					// step called once for Update
					Assert.AreEqual(1, counters[TraceInfoStep.Completed]);

					// steps never called
					Assert.AreEqual(0, counters[TraceInfoStep.MapperCreated]);
					Assert.AreEqual(0, counters[TraceInfoStep.Error]);
				}
			}

		}

		[Test]
		public async Task TraceInfoStepsAreReportedForCommitedTransactionAsync([NorthwindDataContext] string context)
		{
			var events = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db = new DataConnection(context))
			{
				db.OnTraceConnection = e =>
				{
					events[e.TraceInfoStep] = e;
					counters[e.TraceInfoStep]++;
				};
				using (await db.BeginTransactionAsync())
				{
					// Begin transaction command is reported on each step
					Assert.AreEqual("BeginTransactionAsync", events[TraceInfoStep.BeforeExecute]!.CommandText);
					Assert.AreEqual("BeginTransactionAsync", events[TraceInfoStep.AfterExecute]!.CommandText);

					db.SetCommand(@"UPDATE Categories SET CategoryName = CategoryName WHERE 1=2").Execute();
					await db.CommitTransactionAsync();

					// Commit transaction command is reported on each step
					Assert.AreEqual("CommitTransactionAsync", events[TraceInfoStep.BeforeExecute]!.CommandText);
					Assert.AreEqual("CommitTransactionAsync", events[TraceInfoStep.AfterExecute]!.CommandText);

					// steps called once for BeginTransaction once for Update and once for CommitTransaction
					Assert.AreEqual(3, counters[TraceInfoStep.BeforeExecute]);
					Assert.AreEqual(3, counters[TraceInfoStep.AfterExecute]);

					// step called once for Update
					Assert.AreEqual(1, counters[TraceInfoStep.Completed]);

					// steps never called
					Assert.AreEqual(0, counters[TraceInfoStep.MapperCreated]);
					Assert.AreEqual(0, counters[TraceInfoStep.Error]);
				}
			}
		}

		[Test]
		public void TraceInfoStepsAreReportedForRolledbackTransaction([NorthwindDataContext] string context)
		{
			var events = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db = new DataConnection(context))
			{
				db.OnTraceConnection = e =>
				{
					events[e.TraceInfoStep] = e;
					counters[e.TraceInfoStep]++;
				};
				using (db.BeginTransaction())
				{
					// Begin transaction command is reported on each step
					Assert.AreEqual("BeginTransaction", events[TraceInfoStep.BeforeExecute]!.CommandText);
					Assert.AreEqual("BeginTransaction", events[TraceInfoStep.AfterExecute]!.CommandText);

					db.SetCommand(@"UPDATE Categories SET CategoryName = CategoryName WHERE 1=2").Execute();
					db.RollbackTransaction();

					// Commit transaction command is reported on each step
					Assert.AreEqual("RollbackTransaction", events[TraceInfoStep.BeforeExecute]!.CommandText);
					Assert.AreEqual("RollbackTransaction", events[TraceInfoStep.AfterExecute]!.CommandText);

					// steps called once for BeginTransaction once for Update and once for CommitTransaction
					Assert.AreEqual(3, counters[TraceInfoStep.BeforeExecute]);
					Assert.AreEqual(3, counters[TraceInfoStep.AfterExecute]);

					// step called once for Update
					Assert.AreEqual(1, counters[TraceInfoStep.Completed]);

					// steps never called
					Assert.AreEqual(0, counters[TraceInfoStep.MapperCreated]);
					Assert.AreEqual(0, counters[TraceInfoStep.Error]);
				}
			}

		}

		[Test]
		public async Task TraceInfoStepsAreReportedForRolledbackTransactionAsync([NorthwindDataContext] string context)
		{
			var events = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db = new DataConnection(context))
			{
				db.OnTraceConnection = e =>
				{
					events[e.TraceInfoStep] = e;
					counters[e.TraceInfoStep]++;
				};
				using (await db.BeginTransactionAsync())
				{
					// Begin transaction command is reported on each step
					Assert.AreEqual("BeginTransactionAsync", events[TraceInfoStep.BeforeExecute]!.CommandText);
					Assert.AreEqual("BeginTransactionAsync", events[TraceInfoStep.AfterExecute]!.CommandText);

					db.SetCommand(@"UPDATE Categories SET CategoryName = CategoryName WHERE 1=2").Execute();
					await db.RollbackTransactionAsync();

					// Commit transaction command is reported on each step
					Assert.AreEqual("RollbackTransactionAsync", events[TraceInfoStep.BeforeExecute]!.CommandText);
					Assert.AreEqual("RollbackTransactionAsync", events[TraceInfoStep.AfterExecute]!.CommandText);

					// steps called once for BeginTransaction once for Update and once for CommitTransaction
					Assert.AreEqual(3, counters[TraceInfoStep.BeforeExecute]);
					Assert.AreEqual(3, counters[TraceInfoStep.AfterExecute]);

					// step called once for Update
					Assert.AreEqual(1, counters[TraceInfoStep.Completed]);

					// steps never called
					Assert.AreEqual(0, counters[TraceInfoStep.MapperCreated]);
					Assert.AreEqual(0, counters[TraceInfoStep.Error]);
				}
			}
		}

		[Test]
		public void TraceInfoStepsAreReportedForBeginTransactionIlosationLevel([NorthwindDataContext(
#if NETFRAMEWORK
			excludeSqlite: false, excludeSqliteMs: true
#endif
			)] string context)
		{
			var events = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db = new DataConnection(context))
			{
				db.OnTraceConnection = e =>
				{
					events[e.TraceInfoStep] = e;
					counters[e.TraceInfoStep]++;
				};
				using (db.BeginTransaction(IsolationLevel.ReadCommitted))
				{
					// Begin transaction command is reported on each step
					Assert.AreEqual("BeginTransaction(ReadCommitted)", events[TraceInfoStep.BeforeExecute]!.CommandText);
					Assert.AreEqual("BeginTransaction(ReadCommitted)", events[TraceInfoStep.AfterExecute]!.CommandText);

					db.SetCommand(@"UPDATE Categories SET CategoryName = CategoryName WHERE 1=2").Execute();
					db.CommitTransaction();

					// Commit transaction command is reported on each step
					Assert.AreEqual("CommitTransaction", events[TraceInfoStep.BeforeExecute]!.CommandText);
					Assert.AreEqual("CommitTransaction", events[TraceInfoStep.AfterExecute]!.CommandText);

					// steps called once for BeginTransaction once for Update and once for CommitTransaction
					Assert.AreEqual(3, counters[TraceInfoStep.BeforeExecute]);
					Assert.AreEqual(3, counters[TraceInfoStep.AfterExecute]);

					// step called once for Update
					Assert.AreEqual(1, counters[TraceInfoStep.Completed]);

					// steps never called
					Assert.AreEqual(0, counters[TraceInfoStep.MapperCreated]);
					Assert.AreEqual(0, counters[TraceInfoStep.Error]);
				}
			}

		}

		[Test]
		public async Task TraceInfoStepsAreReportedForBeginTransactionIlosationLevelAsync([NorthwindDataContext(
#if NETFRAMEWORK
			excludeSqlite: false, excludeSqliteMs: true
#endif
			)] string context)
		{
			var events = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db = new DataConnection(context))
			{
				db.OnTraceConnection = e =>
				{
					events[e.TraceInfoStep] = e;
					counters[e.TraceInfoStep]++;
				};
				using (await db.BeginTransactionAsync(IsolationLevel.ReadCommitted))
				{
					// Begin transaction command is reported on each step
					Assert.AreEqual("BeginTransactionAsync(ReadCommitted)", events[TraceInfoStep.BeforeExecute]!.CommandText);
					Assert.AreEqual("BeginTransactionAsync(ReadCommitted)", events[TraceInfoStep.AfterExecute]!.CommandText);

					db.SetCommand(@"UPDATE Categories SET CategoryName = CategoryName WHERE 1=2").Execute();
					await db.CommitTransactionAsync();

					// Commit transaction command is reported on each step
					Assert.AreEqual("CommitTransactionAsync", events[TraceInfoStep.BeforeExecute]!.CommandText);
					Assert.AreEqual("CommitTransactionAsync", events[TraceInfoStep.AfterExecute]!.CommandText);

					// steps called once for BeginTransaction once for Update and once for CommitTransaction
					Assert.AreEqual(3, counters[TraceInfoStep.BeforeExecute]);
					Assert.AreEqual(3, counters[TraceInfoStep.AfterExecute]);

					// step called once for Update
					Assert.AreEqual(1, counters[TraceInfoStep.Completed]);

					// steps never called
					Assert.AreEqual(0, counters[TraceInfoStep.MapperCreated]);
					Assert.AreEqual(0, counters[TraceInfoStep.Error]);
				}
			}
		}

		[Test]
		public void OnTraceConnectionShouldUseFromBuilder()
		{
			bool builderTraceCalled = false;
			var builder = new DataOptions().UseTracing(info => builderTraceCalled = true);

			using (var db = new DataConnection(builder))
			{
				db.OnTraceConnection(new TraceInfo(db, TraceInfoStep.BeforeExecute, TraceOperation.BuildMapping, false));
			}

			Assert.True(builderTraceCalled, "because the builder trace should have been called");
		}

		[Test]
		public void TraceSwitchShouldUseDefault()
		{
			var staticTraceLevel = DataConnection.TraceSwitch.Level;

			using (var db = new DataConnection())
			{
				Assert.AreEqual(staticTraceLevel, db.TraceSwitchConnection.Level);
			}
		}

		[Test]
		public void TraceSwitchShouldUseFromBuilder()
		{
			var staticTraceLevel = DataConnection.TraceSwitch.Level;
			var builderTraceLevel = staticTraceLevel + 1;
			var builder = new DataOptions().UseTraceLevel(builderTraceLevel);

			using (var db = new DataConnection(builder))
			{
				Assert.AreEqual(builderTraceLevel, db.TraceSwitchConnection.Level);
				Assert.AreNotEqual(staticTraceLevel, db.TraceSwitchConnection.Level);
			}
		}

		[Test]
		public void WriteTraceInstanceShouldUseDefault()
		{
			var staticWriteCalled = false;
			DataConnection.WriteTraceLine = (s, s1, arg3) => staticWriteCalled = true;

			using (var db = new DataConnection())
			{
				db.WriteTraceLineConnection(null, null, TraceLevel.Info);
			}

			Assert.True(staticWriteCalled, "because the data connection should have used the static version by default");
		}

		[Test]
		public void WriteTraceInstanceShouldUseFromBuilder()
		{
			var staticWriteCalled = false;
			DataConnection.WriteTraceLine = (s, s1, arg3) => staticWriteCalled = true;

			var builderWriteCalled = false;
			var builder = new DataOptions()
				.UseTraceWith((s, s1, a3) => builderWriteCalled = true);

			using (var db = new DataConnection(builder))
			{
				db.WriteTraceLineConnection(null, null, TraceLevel.Info);
			}

			Assert.True(builderWriteCalled, "because the data connection should have used the action from the builder");
			Assert.False(staticWriteCalled, "because the data connection should have used the action from the builder");
		}
	}
}
