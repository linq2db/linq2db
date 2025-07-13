# ActivityService (Metrics)

## Overview

The `ActivityService` provides functionality to collect critical `Linq To DB` telemetry data, that can be used to monitor, analyze, and optimize your application.
The `ActivityService` is compatible with the [OpenTelemetry](https://opentelemetry.io/) specification and [System.Diagnostics.DiagnosticSource](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource/) package.

## IActivity interface

The `IActivity` represents a single activity that can be measured.
This is an interface that you need to implement to collect `Linq To DB` telemetry data.

## ActivityBase class

The `ActivityBase` class provides a basic implementation of the `IActivity` interface. You do not have to use this class.
However, it can help you to avoid incompatibility issues in the future if the `IActivity` interface is extended.

## ActivityService class

The `ActivityService` class provides a simple API to register factory methods that create `IActivity` instances or `null` for provided `ActivityID` event.
You can register multiple factory methods.

## ActivityID

The `ActivityID` is a unique identifier of the LinqToDB activity. It is used to identify the activity in the metrics data.

`Linq To DB` contains a large set of telemetry collection points that can be used to collect data. 
Each collection point has a unique `ActivityID` identifier.

## Example

The following example shows how to use the `ActivityService` and `OpenTelemetry` to collect `Linq To DB` telemetry data.

```c#
using System;
using System.Diagnostics;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
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
```

Output:

```
Activity.TraceId:            4ee29f995cd25bba583def846a2aa220
Activity.SpanId:             cd0647218f959924
Activity.TraceFlags:         Recorded
Activity.ParentSpanId:       268635637f43fbd1
Activity.ActivitySourceName: Sample.LinqToDB
Activity.DisplayName:        FinalizeQuery
Activity.Kind:               Internal
Activity.StartTime:          2023-12-28T07:14:45.0200111Z
Activity.Duration:           00:00:00.0644992
Resource associated with Activity:
    service.name: MySample
    service.instance.id: 61b68727-d6bd-43a1-a426-c206851b6bdb
    telemetry.sdk.name: opentelemetry
    telemetry.sdk.language: dotnet
    telemetry.sdk.version: 1.6.0

Activity.TraceId:            4ee29f995cd25bba583def846a2aa220
Activity.SpanId:             8f5842b0c199c668
Activity.TraceFlags:         Recorded
Activity.ParentSpanId:       93fb3a253e06df95
Activity.ActivitySourceName: Sample.LinqToDB
Activity.DisplayName:        BuildSql
Activity.Kind:               Internal
Activity.StartTime:          2023-12-28T07:14:45.4280856Z
Activity.Duration:           00:00:00.0101335
Resource associated with Activity:
    service.name: MySample
    service.instance.id: 61b68727-d6bd-43a1-a426-c206851b6bdb
    telemetry.sdk.name: opentelemetry
    telemetry.sdk.language: dotnet
    telemetry.sdk.version: 1.6.0


...


Activity.TraceId:            45a597dbc0d5b354e18d371b02a101c6
Activity.SpanId:             38cfd4c41f55583b
Activity.TraceFlags:         Recorded
Activity.ActivitySourceName: Sample.LinqToDB
Activity.DisplayName:        ExecuteElementAsync
Activity.Kind:               Internal
Activity.StartTime:          2023-12-28T07:14:46.2702044Z
Activity.Duration:           00:00:00.1555957
Resource associated with Activity:
    service.name: MySample
    service.instance.id: 61b68727-d6bd-43a1-a426-c206851b6bdb
    telemetry.sdk.name: opentelemetry
    telemetry.sdk.language: dotnet
    telemetry.sdk.version: 1.6.0

Count is 91
```
