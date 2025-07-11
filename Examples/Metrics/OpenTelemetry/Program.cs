using System;
using System.Diagnostics;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Mapping;
using LinqToDB.Metrics;

using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpenTelemetryExample
{
	static class Program
	{
		static async Task Main()
		{
			using var tracerProvider = Sdk.CreateTracerProviderBuilder()
				.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MySample"))
				.AddSource("Sample.LinqToDB")
				.AddConsoleExporter()
				.Build();

			ActivitySource.AddActivityListener(_activityListener);

			// Register the factory method that creates LinqToDBActivity instances.
			//
			ActivityService.AddFactory(LinqToDBActivity.Create);

			{
				await using var db = new DataConnection(new DataOptions().UseSQLite("Data Source=Northwind.MS.sqlite", SQLiteProvider.Microsoft));

				await db.CreateTableAsync<Customer>(tableOptions:TableOptions.CheckExistence);

				var count = await db.GetTable<Customer>().CountAsync();

				Console.WriteLine($"Count is {count}");
			}
		}

		static readonly ActivitySource   _activitySource   = new("Sample.LinqToDB");
		static readonly ActivityListener _activityListener = new()
		{
			ShouldListenTo      = _ => true,
			SampleUsingParentId = (ref ActivityCreationOptions<string>          _) => ActivitySamplingResult.AllData,
			Sample              = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
		};

		// This class is used to collect LinqToDB telemetry data.
		//
		sealed class LinqToDBActivity : ActivityBase
		{
			readonly Activity _activity;

			LinqToDBActivity(ActivityID id, Activity activity) : base(id)
			{
				_activity = activity;
			}

			public override void Dispose()
			{
				_activity.Dispose();
			}

			public override IActivity AddTag(ActivityTagID key, object? value)
			{
				_activity.SetTag(key.ToString(), value);
				return this;
			}

			// This method is called by the ActivityService to create an instance of the LinqToDBActivity class.
			//
			public static IActivity? Create(ActivityID id)
			{
				var a = _activitySource.StartActivity(id.ToString());
				return a == null ? null : new LinqToDBActivity(id, a);
			}
		}

		[Table(Name="Customers")]
		public sealed class Customer
		{
			[PrimaryKey]      public string CustomerID  = null!;
			[Column, NotNull] public string CompanyName = null!;
		}
	}
}
