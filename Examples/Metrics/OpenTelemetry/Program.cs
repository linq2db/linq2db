using System;
using System.Diagnostics;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.Tools;

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
				await using var db = new DataConnection(new DataOptions().UseSQLiteMicrosoft("Data Source=Northwind.MS.sqlite"));

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
		sealed class LinqToDBActivity : IActivity
		{
			readonly Activity _activity;

			LinqToDBActivity(Activity activity)
			{
				_activity = activity;
			}

			public void Dispose()
			{
				_activity.Dispose();
			}

			public ValueTask DisposeAsync()
			{
				Dispose();
				return default;
			}

			// This method is called by the ActivityService to create an instance of the LinqToDBActivity class.
			//
			public static IActivity? Create(ActivityID id)
			{
				var a = _activitySource.StartActivity(id.ToString());
				return a == null ? null : new LinqToDBActivity(a);
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
