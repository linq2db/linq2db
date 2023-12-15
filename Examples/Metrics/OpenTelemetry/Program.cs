using System;
using System.Diagnostics;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Mapping;
using LinqToDB.Tools;

using Microsoft.Extensions.ObjectPool;

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

			ActivityService.AddFactory(LinqToDBActivity.Create);

			{
				await using var db = SQLiteTools.CreateDataConnection("Data Source=Northwind.MS.sqlite", ProviderName.SQLiteMS);

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

		sealed class LinqToDBActivity : ActivityBase
		{
			Activity? _activity;

			public override void Dispose()
			{
				_activity!.Dispose();
				_activity = null;

				_activityPool.Return(this);
			}

			static readonly ObjectPool<LinqToDBActivity> _activityPool =
				new DefaultObjectPool<LinqToDBActivity>(new DefaultPooledObjectPolicy<LinqToDBActivity>(), 100);

			public static IActivity? Create(ActivityID id)
			{
				var a = _activitySource.StartActivity(id.ToString());

				if (a == null)
					return null;

				var l2db = _activityPool.Get();

				l2db._activity = a;

				return l2db;
			}
		}

#pragma warning disable CA1812

		[Table(Name="Customers")]
		public sealed class Customer
		{
			[PrimaryKey]      public string CustomerID  = null!;
			[Column, NotNull] public string CompanyName = null!;
		}
	}
}
