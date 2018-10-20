using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

using NUnit.Framework;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;

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

		[Test, NorthwindDataContext]
		public void TraceInfoStepsAreReportedForLinqQuery(string context)
		{
			var steps = GetEnumValues((TraceInfoStep s) => false);

			using (var db = new DataConnection(context))
			{
				db.OnTraceConnection = e => steps[e.TraceInfoStep] = true;
				db.GetTable<Northwind.Category>().ToList();

				Assert.IsTrue(steps[TraceInfoStep.BeforeExecute]);
				Assert.IsTrue(steps[TraceInfoStep.AfterExecute]);
				Assert.IsTrue(steps[TraceInfoStep.Completed]);
				Assert.IsFalse(steps[TraceInfoStep.Error]);
			}
		}

		[Test, NorthwindDataContext]
		public void TraceInfoStepsAreReportedForDataReader(string context)
		{
			var steps = GetEnumValues((TraceInfoStep s) => false);

			using (var db = new DataConnection(context))
			{
				var sql = db.GetTable<Northwind.Category>().SqlText;
				db.OnTraceConnection = e => steps[e.TraceInfoStep] = true;

				using (var reader = db.ExecuteReader(sql))
				{
					reader.Query<Northwind.Category>().ToList();
				}

				Assert.IsTrue(steps[TraceInfoStep.BeforeExecute]);
				Assert.IsTrue(steps[TraceInfoStep.AfterExecute]);
				Assert.IsTrue(steps[TraceInfoStep.Completed]);
				Assert.IsFalse(steps[TraceInfoStep.Error]);
			}
		}
	}
}
