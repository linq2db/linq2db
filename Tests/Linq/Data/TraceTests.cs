using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB.Configuration;
using NUnit.Framework;

using LinqToDB.Data;

namespace Tests.Data
{
	using Model;

	[TestFixture]
	public class TraceTests
	{
		private TraceLevel                           OriginalTraceLevel { get; set; }
		private Action<TraceInfo>                    OriginalOnTrace    { get; set; } = null!;
		private Action<string?, string?, TraceLevel> OriginalWrite      { get; set; } = null!;


		[OneTimeSetUp]
		public void SetTraceInfoLevel()
		{
			using (var db = new DataConnection())
			{
				//gets the default static on trace so it'll be reset after the tests are done
				OriginalOnTrace = db.OnTraceConnection;
			}

			OriginalTraceLevel               = DataConnection.TraceSwitch.Level;
			OriginalWrite                    = DataConnection.WriteTraceLine;
			DataConnection.TraceSwitch.Level = TraceLevel.Info;
		}

		[OneTimeTearDown]
		public void RestoreOriginalTraceLevel()
		{
			DataConnection.TraceSwitch.Level = OriginalTraceLevel;
			DataConnection.OnTrace           = OriginalOnTrace;
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
		public void TraceInfoStepsAreReportedForLinqQuery([NorthwindDataContext] string context)
		{
			var events   = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db = new DataConnection(context))
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
		public void TraceInfoStepsAreReportedForDataReader([NorthwindDataContext] string context)
		{
			var events   = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
			var counters = GetEnumValues((TraceInfoStep s) => 0);

			using (var db = new DataConnection(context))
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

			using (var db = new DataConnection(context))
			{
				var sql = db.GetTable<Northwind.Category>().SqlText;
				db.OnTraceConnection = e =>
				{
					events[e.TraceInfoStep] = e;
					counters[e.TraceInfoStep]++;
				};

				using (var reader = await new CommandInfo(db, sql).ExecuteReaderAsync())
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
		public void OnTraceConnectionShouldUseStatic()
		{
			bool traceCalled = false;
			DataConnection.OnTrace = info => traceCalled = true;

			using (var db = new DataConnection())
			{
				db.OnTraceConnection(new TraceInfo(db, TraceInfoStep.BeforeExecute));
			}
			Assert.True(traceCalled);
		}

		[Test]
		public void OnTraceConnectionShouldUseFromBuilder()
		{
			bool defaultTraceCalled = false;
			DataConnection.OnTrace = info => defaultTraceCalled = true;

			bool builderTraceCalled = false;
			var builder = new LinqToDbConnectionOptionsBuilder().WithTracing(info => builderTraceCalled = true);

			using (var db = new DataConnection(builder.Build()))
			{
				db.OnTraceConnection(new TraceInfo(db, TraceInfoStep.BeforeExecute));
			}

			Assert.True(builderTraceCalled, "because the builder trace should have been called");
			Assert.False(defaultTraceCalled, "because the static trace should not have been called");
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
			var builder = new LinqToDbConnectionOptionsBuilder().WithTraceLevel(builderTraceLevel);

			using (var db = new DataConnection(builder.Build()))
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
			var builder = new LinqToDbConnectionOptionsBuilder()
				.WriteTraceWith((s, s1, a3) => builderWriteCalled = true);

			using (var db = new DataConnection(builder.Build()))
			{
				db.WriteTraceLineConnection(null, null, TraceLevel.Info);
			}

			Assert.True(builderWriteCalled, "because the data connection should have used the action from the builder");
			Assert.False(staticWriteCalled, "because the data connection should have used the action from the builder");
		}
	}
}
