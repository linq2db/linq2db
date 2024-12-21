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
	using System.Collections;

	using Model;

	using Tests.DataProvider;


	[TestFixture]
	public class TraceTests : TestBase
	{
		private TraceLevel                           OriginalTraceLevel { get; set; }
		private Action<string,string,TraceLevel> OriginalWrite      { get; set; } = null!;

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

		// test could fail for some providers in VS due to designer mode
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

				Assert.That(
					() => db.GetTable<Child>().ToList(),
					Throws.TypeOf<ArgumentException>().Or.TypeOf<InvalidOperationException>());

				Assert.Multiple(() =>
				{
					// steps called once
					Assert.That(counters[TraceInfoStep.Error], Is.EqualTo(1));
					Assert.That(counters[TraceInfoStep.Completed], Is.EqualTo(1));

					// steps never called
					Assert.That(counters[TraceInfoStep.BeforeExecute], Is.EqualTo(0));
					Assert.That(counters[TraceInfoStep.AfterExecute], Is.EqualTo(0));
					Assert.That(counters[TraceInfoStep.MapperCreated], Is.EqualTo(0));
				});
			}
		}

		// test could fail for some providers in VS due to designer mode
		[Test]
		public void TraceInfoErrorsAreReportedForInvalidConnectionStringAsync([DataSources(false)] string context)
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

				Assert.That(
					() => db.GetTable<Child>().ToListAsync(),
					Throws.TypeOf<ArgumentException>().Or.TypeOf<InvalidOperationException>());

				Assert.Multiple(() =>
				{
					// steps called once
					Assert.That(counters[TraceInfoStep.Error], Is.EqualTo(1));
					Assert.That(counters[TraceInfoStep.Completed], Is.EqualTo(1));

					// steps never called
					Assert.That(counters[TraceInfoStep.BeforeExecute], Is.EqualTo(0));
					Assert.That(counters[TraceInfoStep.AfterExecute], Is.EqualTo(0));
					Assert.That(counters[TraceInfoStep.MapperCreated], Is.EqualTo(0));
				});
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
				Assert.Multiple(() =>
				{
					Assert.That(events[TraceInfoStep.AfterExecute]!.Command, Is.SameAs(command));
					Assert.That(events[TraceInfoStep.Completed]!.Command, Is.Null);
					Assert.That(command, Is.Not.Null);

					// steps called once
					Assert.That(counters[TraceInfoStep.BeforeExecute], Is.EqualTo(1));
					Assert.That(counters[TraceInfoStep.AfterExecute], Is.EqualTo(1));
					Assert.That(counters[TraceInfoStep.Completed], Is.EqualTo(1));

					// steps never called
					Assert.That(counters[TraceInfoStep.MapperCreated], Is.EqualTo(0));
					Assert.That(counters[TraceInfoStep.Error], Is.EqualTo(0));
				});
			}
		}

		[Test]
		public void TraceInfoStepsAreReportedForDataReader([NorthwindDataContext] string context)
		{
			var events   = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db = GetDataConnection(context))
			{
				var sql = db.GetTable<Northwind.Category>().ToSqlQuery().Sql;
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
				Assert.Multiple(() =>
				{
					Assert.That(events[TraceInfoStep.AfterExecute]!.Command, Is.SameAs(command));
					Assert.That(events[TraceInfoStep.Completed]!.Command, Is.SameAs(command));
					Assert.That(command, Is.Not.Null);

					// steps called once
					Assert.That(counters[TraceInfoStep.BeforeExecute], Is.EqualTo(1));
					Assert.That(counters[TraceInfoStep.AfterExecute], Is.EqualTo(1));
					Assert.That(counters[TraceInfoStep.Completed], Is.EqualTo(1));

					// steps never called
					Assert.That(counters[TraceInfoStep.MapperCreated], Is.EqualTo(0));
					Assert.That(counters[TraceInfoStep.Error], Is.EqualTo(0));
				});
			}
		}

		[Test]
		public async Task TraceInfoStepsAreReportedForDataReaderAsync([NorthwindDataContext] string context)
		{
			var events   = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db = GetDataConnection(context))
			{
				var sql = db.GetTable<Northwind.Category>().ToSqlQuery().Sql;
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
				Assert.Multiple(() =>
				{
					Assert.That(events[TraceInfoStep.AfterExecute]!.Command, Is.SameAs(command));
					Assert.That(events[TraceInfoStep.Completed]!.Command, Is.SameAs(command));
					Assert.That(command, Is.Not.Null);

					// steps called once
					Assert.That(counters[TraceInfoStep.BeforeExecute], Is.EqualTo(1));
					Assert.That(counters[TraceInfoStep.AfterExecute], Is.EqualTo(1));
					Assert.That(counters[TraceInfoStep.Completed], Is.EqualTo(1));

					// steps never called
					Assert.That(counters[TraceInfoStep.MapperCreated], Is.EqualTo(0));
					Assert.That(counters[TraceInfoStep.Error], Is.EqualTo(0));
				});
			}
		}

		[Test]
		public void TraceInfoStepsAreReportedForSqlQuery([NorthwindDataContext] string context)
		{
			var events = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db = new DataConnection(context))
			{
				var sql = db.GetTable<Northwind.Category>().ToSqlQuery().Sql;
				db.OnTraceConnection = e =>
				{
					events[e.TraceInfoStep] = e;
					counters[e.TraceInfoStep]++;
				};

				db.SetCommand(sql).Query<Northwind.Category>().ToArray();

				// the same command is reported on each step
				var command = events[TraceInfoStep.BeforeExecute]!.Command;
				Assert.Multiple(() =>
				{
					Assert.That(events[TraceInfoStep.AfterExecute]!.Command, Is.SameAs(command));
					Assert.That(events[TraceInfoStep.Completed]!.Command, Is.SameAs(command));
					Assert.That(command, Is.Not.Null);

					// steps called once
					Assert.That(counters[TraceInfoStep.BeforeExecute], Is.EqualTo(1));
					Assert.That(counters[TraceInfoStep.AfterExecute], Is.EqualTo(1));
					Assert.That(counters[TraceInfoStep.Completed], Is.EqualTo(1));

					// steps never called
					Assert.That(counters[TraceInfoStep.MapperCreated], Is.EqualTo(0));
					Assert.That(counters[TraceInfoStep.Error], Is.EqualTo(0));
				});
			}
		}

		[Test]
		public void TraceInfoStepsAreReportedForDataReaderQuery([NorthwindDataContext] string context)
		{
			var events = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db = new DataConnection(context))
			{
				var sql = db.GetTable<Northwind.Category>().ToSqlQuery().Sql;
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
				Assert.Multiple(() =>
				{
					Assert.That(events[TraceInfoStep.AfterExecute]!.Command, Is.SameAs(command));
					Assert.That(events[TraceInfoStep.Completed]!.Command, Is.SameAs(command));
					Assert.That(command, Is.Not.Null);

					// steps called once
					Assert.That(counters[TraceInfoStep.BeforeExecute], Is.EqualTo(1));
					Assert.That(counters[TraceInfoStep.AfterExecute], Is.EqualTo(1));
					Assert.That(counters[TraceInfoStep.Completed], Is.EqualTo(1));

					// steps never called
					Assert.That(counters[TraceInfoStep.MapperCreated], Is.EqualTo(0));
					Assert.That(counters[TraceInfoStep.Error], Is.EqualTo(0));
				});
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
				Assert.Multiple(() =>
				{
					Assert.That(events[TraceInfoStep.AfterExecute]!.Command, Is.SameAs(command));
					Assert.That(events[TraceInfoStep.Completed]!.Command, Is.SameAs(command));
					Assert.That(command, Is.Not.Null);

					// steps called once
					Assert.That(counters[TraceInfoStep.BeforeExecute], Is.EqualTo(1));
					Assert.That(counters[TraceInfoStep.AfterExecute], Is.EqualTo(1));
					Assert.That(counters[TraceInfoStep.Completed], Is.EqualTo(1));

					// steps never called
					Assert.That(counters[TraceInfoStep.MapperCreated], Is.EqualTo(0));
					Assert.That(counters[TraceInfoStep.Error], Is.EqualTo(0));
				});

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
				Assert.Multiple(() =>
				{
					Assert.That(events[TraceInfoStep.AfterExecute]!.Command, Is.SameAs(command));
					Assert.That(events[TraceInfoStep.Completed]!.Command, Is.SameAs(command));
					Assert.That(command, Is.Not.Null);

					// steps called once
					Assert.That(counters[TraceInfoStep.BeforeExecute], Is.EqualTo(1));
					Assert.That(counters[TraceInfoStep.AfterExecute], Is.EqualTo(1));
					Assert.That(counters[TraceInfoStep.Completed], Is.EqualTo(1));

					// steps never called
					Assert.That(counters[TraceInfoStep.MapperCreated], Is.EqualTo(0));
					Assert.That(counters[TraceInfoStep.Error], Is.EqualTo(0));
				});

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
				Assert.Multiple(() =>
				{
					Assert.That(events[TraceInfoStep.AfterExecute]!.Command, Is.SameAs(command));
					Assert.That(events[TraceInfoStep.Completed]!.Command, Is.SameAs(command));
					Assert.That(command, Is.Not.Null);

					// steps called once
					Assert.That(counters[TraceInfoStep.BeforeExecute], Is.EqualTo(1));
					Assert.That(counters[TraceInfoStep.AfterExecute], Is.EqualTo(1));
					Assert.That(counters[TraceInfoStep.Completed], Is.EqualTo(1));

					// steps never called
					Assert.That(counters[TraceInfoStep.MapperCreated], Is.EqualTo(0));
					Assert.That(counters[TraceInfoStep.Error], Is.EqualTo(0));
				});

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
				Assert.Multiple(() =>
				{
					Assert.That(events[TraceInfoStep.AfterExecute]!.Command, Is.SameAs(command));
					Assert.That(events[TraceInfoStep.Completed]!.Command, Is.SameAs(command));
					Assert.That(command, Is.Not.Null);

					// steps called once
					Assert.That(counters[TraceInfoStep.BeforeExecute], Is.EqualTo(1));
					Assert.That(counters[TraceInfoStep.AfterExecute], Is.EqualTo(1));
					Assert.That(counters[TraceInfoStep.Completed], Is.EqualTo(1));

					// steps never called
					Assert.That(counters[TraceInfoStep.MapperCreated], Is.EqualTo(0));
					Assert.That(counters[TraceInfoStep.Error], Is.EqualTo(0));
				});

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
				var sql = db.GetTable<Northwind.Category>().ToSqlQuery().Sql;
				db.OnTraceConnection = e =>
				{
					events[e.TraceInfoStep] = e;
					counters[e.TraceInfoStep]++;
				};

				db.SetCommand(sql).Execute<Northwind.Category>();

				// the same command is reported on each step
				var command = events[TraceInfoStep.BeforeExecute]!.Command;
				Assert.Multiple(() =>
				{
					Assert.That(events[TraceInfoStep.AfterExecute]!.Command, Is.SameAs(command));
					Assert.That(events[TraceInfoStep.Completed]!.Command, Is.SameAs(command));
					Assert.That(command, Is.Not.Null);

					// steps called once
					Assert.That(counters[TraceInfoStep.BeforeExecute], Is.EqualTo(1));
					Assert.That(counters[TraceInfoStep.AfterExecute], Is.EqualTo(1));
					Assert.That(counters[TraceInfoStep.Completed], Is.EqualTo(1));

					// steps never called
					Assert.That(counters[TraceInfoStep.MapperCreated], Is.EqualTo(0));
					Assert.That(counters[TraceInfoStep.Error], Is.EqualTo(0));
				});
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
					Assert.Multiple(() =>
					{
						// Begin transaction command is reported on each step
						Assert.That(events[TraceInfoStep.BeforeExecute]!.CommandText, Is.EqualTo("BeginTransaction"));
						Assert.That(events[TraceInfoStep.AfterExecute]!.CommandText, Is.EqualTo("BeginTransaction"));
					});

					db.SetCommand(@"UPDATE Categories SET CategoryName = CategoryName WHERE 1=2").Execute();
					db.CommitTransaction();

					Assert.Multiple(() =>
					{
						// Commit transaction command is reported on each step
						Assert.That(events[TraceInfoStep.BeforeExecute]!.CommandText, Is.EqualTo("CommitTransaction"));
						Assert.That(events[TraceInfoStep.AfterExecute]!.CommandText, Is.EqualTo("CommitTransaction"));

						// steps called once for BeginTransaction once for Update and once for CommitTransaction
						Assert.That(counters[TraceInfoStep.BeforeExecute], Is.EqualTo(3));
						Assert.That(counters[TraceInfoStep.AfterExecute], Is.EqualTo(3));

						// step called once for Update
						Assert.That(counters[TraceInfoStep.Completed], Is.EqualTo(1));

						// steps never called
						Assert.That(counters[TraceInfoStep.MapperCreated], Is.EqualTo(0));
						Assert.That(counters[TraceInfoStep.Error], Is.EqualTo(0));
					});
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
					Assert.Multiple(() =>
					{
						// Begin transaction command is reported on each step
						Assert.That(events[TraceInfoStep.BeforeExecute]!.CommandText, Is.EqualTo("BeginTransactionAsync"));
						Assert.That(events[TraceInfoStep.AfterExecute]!.CommandText, Is.EqualTo("BeginTransactionAsync"));
					});

					db.SetCommand(@"UPDATE Categories SET CategoryName = CategoryName WHERE 1=2").Execute();
					await db.CommitTransactionAsync();

					Assert.Multiple(() =>
					{
						// Commit transaction command is reported on each step
						Assert.That(events[TraceInfoStep.BeforeExecute]!.CommandText, Is.EqualTo("CommitTransactionAsync"));
						Assert.That(events[TraceInfoStep.AfterExecute]!.CommandText, Is.EqualTo("CommitTransactionAsync"));

						// steps called once for BeginTransaction once for Update and once for CommitTransaction
						Assert.That(counters[TraceInfoStep.BeforeExecute], Is.EqualTo(3));
						Assert.That(counters[TraceInfoStep.AfterExecute], Is.EqualTo(3));

						// step called once for Update
						Assert.That(counters[TraceInfoStep.Completed], Is.EqualTo(1));

						// steps never called
						Assert.That(counters[TraceInfoStep.MapperCreated], Is.EqualTo(0));
						Assert.That(counters[TraceInfoStep.Error], Is.EqualTo(0));
					});
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
					Assert.Multiple(() =>
					{
						// Begin transaction command is reported on each step
						Assert.That(events[TraceInfoStep.BeforeExecute]!.CommandText, Is.EqualTo("BeginTransaction"));
						Assert.That(events[TraceInfoStep.AfterExecute]!.CommandText, Is.EqualTo("BeginTransaction"));
					});

					db.SetCommand(@"UPDATE Categories SET CategoryName = CategoryName WHERE 1=2").Execute();
					db.RollbackTransaction();

					Assert.Multiple(() =>
					{
						// Commit transaction command is reported on each step
						Assert.That(events[TraceInfoStep.BeforeExecute]!.CommandText, Is.EqualTo("RollbackTransaction"));
						Assert.That(events[TraceInfoStep.AfterExecute]!.CommandText, Is.EqualTo("RollbackTransaction"));

						// steps called once for BeginTransaction once for Update and once for CommitTransaction
						Assert.That(counters[TraceInfoStep.BeforeExecute], Is.EqualTo(3));
						Assert.That(counters[TraceInfoStep.AfterExecute], Is.EqualTo(3));

						// step called once for Update
						Assert.That(counters[TraceInfoStep.Completed], Is.EqualTo(1));

						// steps never called
						Assert.That(counters[TraceInfoStep.MapperCreated], Is.EqualTo(0));
						Assert.That(counters[TraceInfoStep.Error], Is.EqualTo(0));
					});
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
					Assert.Multiple(() =>
					{
						// Begin transaction command is reported on each step
						Assert.That(events[TraceInfoStep.BeforeExecute]!.CommandText, Is.EqualTo("BeginTransactionAsync"));
						Assert.That(events[TraceInfoStep.AfterExecute]!.CommandText, Is.EqualTo("BeginTransactionAsync"));
					});

					db.SetCommand(@"UPDATE Categories SET CategoryName = CategoryName WHERE 1=2").Execute();
					await db.RollbackTransactionAsync();

					Assert.Multiple(() =>
					{
						// Commit transaction command is reported on each step
						Assert.That(events[TraceInfoStep.BeforeExecute]!.CommandText, Is.EqualTo("RollbackTransactionAsync"));
						Assert.That(events[TraceInfoStep.AfterExecute]!.CommandText, Is.EqualTo("RollbackTransactionAsync"));

						// steps called once for BeginTransaction once for Update and once for CommitTransaction
						Assert.That(counters[TraceInfoStep.BeforeExecute], Is.EqualTo(3));
						Assert.That(counters[TraceInfoStep.AfterExecute], Is.EqualTo(3));

						// step called once for Update
						Assert.That(counters[TraceInfoStep.Completed], Is.EqualTo(1));

						// steps never called
						Assert.That(counters[TraceInfoStep.MapperCreated], Is.EqualTo(0));
						Assert.That(counters[TraceInfoStep.Error], Is.EqualTo(0));
					});
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
					Assert.Multiple(() =>
					{
						// Begin transaction command is reported on each step
						Assert.That(events[TraceInfoStep.BeforeExecute]!.CommandText, Is.EqualTo("BeginTransaction(ReadCommitted)"));
						Assert.That(events[TraceInfoStep.AfterExecute]!.CommandText, Is.EqualTo("BeginTransaction(ReadCommitted)"));
					});

					db.SetCommand(@"UPDATE Categories SET CategoryName = CategoryName WHERE 1=2").Execute();
					db.CommitTransaction();

					Assert.Multiple(() =>
					{
						// Commit transaction command is reported on each step
						Assert.That(events[TraceInfoStep.BeforeExecute]!.CommandText, Is.EqualTo("CommitTransaction"));
						Assert.That(events[TraceInfoStep.AfterExecute]!.CommandText, Is.EqualTo("CommitTransaction"));

						// steps called once for BeginTransaction once for Update and once for CommitTransaction
						Assert.That(counters[TraceInfoStep.BeforeExecute], Is.EqualTo(3));
						Assert.That(counters[TraceInfoStep.AfterExecute], Is.EqualTo(3));

						// step called once for Update
						Assert.That(counters[TraceInfoStep.Completed], Is.EqualTo(1));

						// steps never called
						Assert.That(counters[TraceInfoStep.MapperCreated], Is.EqualTo(0));
						Assert.That(counters[TraceInfoStep.Error], Is.EqualTo(0));
					});
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
					Assert.Multiple(() =>
					{
						// Begin transaction command is reported on each step
						Assert.That(events[TraceInfoStep.BeforeExecute]!.CommandText, Is.EqualTo("BeginTransactionAsync(ReadCommitted)"));
						Assert.That(events[TraceInfoStep.AfterExecute]!.CommandText, Is.EqualTo("BeginTransactionAsync(ReadCommitted)"));
					});

					db.SetCommand(@"UPDATE Categories SET CategoryName = CategoryName WHERE 1=2").Execute();
					await db.CommitTransactionAsync();

					Assert.Multiple(() =>
					{
						// Commit transaction command is reported on each step
						Assert.That(events[TraceInfoStep.BeforeExecute]!.CommandText, Is.EqualTo("CommitTransactionAsync"));
						Assert.That(events[TraceInfoStep.AfterExecute]!.CommandText, Is.EqualTo("CommitTransactionAsync"));

						// steps called once for BeginTransaction once for Update and once for CommitTransaction
						Assert.That(counters[TraceInfoStep.BeforeExecute], Is.EqualTo(3));
						Assert.That(counters[TraceInfoStep.AfterExecute], Is.EqualTo(3));

						// step called once for Update
						Assert.That(counters[TraceInfoStep.Completed], Is.EqualTo(1));

						// steps never called
						Assert.That(counters[TraceInfoStep.MapperCreated], Is.EqualTo(0));
						Assert.That(counters[TraceInfoStep.Error], Is.EqualTo(0));
					});
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

			Assert.That(builderTraceCalled, Is.True, "because the builder trace should have been called");
		}

		[Test]
		public void TraceSwitchShouldUseDefault()
		{
			var staticTraceLevel = DataConnection.TraceSwitch.Level;

			using (var db = new DataConnection())
			{
				Assert.That(db.TraceSwitchConnection.Level, Is.EqualTo(staticTraceLevel));
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
				Assert.That(db.TraceSwitchConnection.Level, Is.EqualTo(builderTraceLevel));
				Assert.That(db.TraceSwitchConnection.Level, Is.Not.EqualTo(staticTraceLevel));
			}
		}

		[Test]
		public void WriteTraceInstanceShouldUseDefault()
		{
			var wtl = DataConnection.WriteTraceLine;

			var staticWriteCalled = false;
			DataConnection.WriteTraceLine = (s, s1, arg3) => staticWriteCalled = true;

			using (var db = new DataConnection())
			{
				db.WriteTraceLineConnection("", "", TraceLevel.Info);
			}

			Assert.That(staticWriteCalled, Is.True, "because the data connection should have used the static version by default");

			DataConnection.WriteTraceLine = wtl;
		}

		[Test]
		public void WriteTraceInstanceShouldUseFromBuilder()
		{
			var wtl = DataConnection.WriteTraceLine;

			var staticWriteCalled = false;
			DataConnection.WriteTraceLine = (s, s1, arg3) => staticWriteCalled = true;

			var builderWriteCalled = false;
			var builder = new DataOptions()
				.UseTraceWith((s, s1, a3) => builderWriteCalled = true);

			using (var db = new DataConnection(builder))
			{
				db.WriteTraceLineConnection("", "", TraceLevel.Info);
			}

			Assert.Multiple(() =>
			{
				Assert.That(builderWriteCalled, Is.True, "because the data connection should have used the action from the builder");
				Assert.That(staticWriteCalled, Is.False, "because the data connection should have used the action from the builder");
			});

			DataConnection.WriteTraceLine = wtl;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3663")]
		public void Issue3663Test([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context, o => o.UseTracing(Trace));
			db.Person.ToArray();

			db.Query<Person>(db.LastQuery!).ToArray();

			static void Trace(TraceInfo trace)
			{
				_ = trace.Command?.CommandText;
				_ = trace.SqlText;
			}
		}
	}
}
