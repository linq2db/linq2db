using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;

using LinqToDB.Data;

namespace Tests.Data
{
#if !NETSTANDARD1_6
	using System.Configuration;
#endif

	using Model;

	[TestFixture]
	public class TraceTests
	{
		private TraceLevel OriginalTraceLevel { get; set; }

		[OneTimeSetUp]
		public void SetTraceInfoLevel()
		{
			OriginalTraceLevel = DataConnection.TraceSwitch.Level;
			DataConnection.TraceSwitch.Level = TraceLevel.Info;
		}

		[OneTimeTearDown]
		public void RestoreOriginalTraceLevel()
		{
			DataConnection.TraceSwitch.Level = OriginalTraceLevel;
		}

		private IDictionary<TEnum, TValue> GetEnumValues<TEnum, TValue>(Func<TEnum, TValue> factory)
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
			var events = GetEnumValues((TraceInfoStep s) => default(TraceInfo));
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
				var command = events[TraceInfoStep.BeforeExecute].Command;
				Assert.AreSame(command, events[TraceInfoStep.AfterExecute].Command);
				Assert.AreSame(command, events[TraceInfoStep.Completed].Command);
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

				using (var reader = db.ExecuteReader(sql))
				{
					var _ = reader.Query<Northwind.Category>().ToList();
				}

				// the same command is reported on each step
				var command = events[TraceInfoStep.BeforeExecute].Command;
				Assert.AreSame(command, events[TraceInfoStep.AfterExecute].Command);
				Assert.AreSame(command, events[TraceInfoStep.Completed].Command);
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

				using (var reader = await new CommandInfo(db, sql).ExecuteReaderAsync())
				{
					await reader.QueryToListAsync<Northwind.Category>();
				}

				// the same command is reported on each step
				var command = events[TraceInfoStep.BeforeExecute].Command;
				Assert.AreSame(command, events[TraceInfoStep.AfterExecute].Command);
				Assert.AreSame(command, events[TraceInfoStep.Completed].Command);
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
	}
}
