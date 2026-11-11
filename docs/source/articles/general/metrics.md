---
uid: Metrics
---

# ActivityService (Metrics)<!-- omit in toc -->

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

## Configuration.TraceMaterializationActivity

The `Configuration.TraceMaterializationActivity` property can be used to enable or disable tracing of object materialization activity. 
It can significantly break performance if tracing consumer performs slow, so it is disabled by default.

## Example

The following example shows how to use the `ActivityService` and `OpenTelemetry` to collect `Linq To DB` telemetry data.

```c#
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

## Tools

The `LinqToDB.Tools` package contains two activity factories `ActivityHierarchy` and `ActivityStatistics`
that can be used to generate `Linq To DB` activity reports. An exsample of how to use these factories 
can be found [here](https://github.com/linq2db/linq2db/tree/metrics/Examples/Metrics/Tools).

The following is an output of this example:

```
Count is 91
LinqToDB call hierarchy:

CreateTable
  FinalizeQuery
  Execute NonQuery
    BuildSql
    Connection Open
    Command ExecuteNonQuery

IQueryProvider.Execute<T>
  GetQuery
    Find
      Expose
      Find
    Create
      Build
        BuildSequence
          CanBuild (22)
          Build
            BuildSequence
              CanBuild
              Build
        ReorderBuilders
        BuildQuery
          FinalizeQuery
  Execute Element
    BuildSql
    Command ExecuteReader
    Materialization

Connection Dispose


LinqToDB statistics:

Count : 77
+----------------------------------------+------------------+-----------+------------------+---------+
| Name                                   | Elapsed          | CallCount | TimePerCall      | Percent |
+----------------------------------------+------------------+-----------+------------------+---------+
| IQueryProvider.Execute<T>              | 00:00:00.1279609 |         1 | 00:00:00.1279609 |  35.85% |
| IQueryProvider.Execute                 | 00:00:00         |         0 | 00:00:00         |         |
| IQueryProvider.GetEnumerator<T>        | 00:00:00         |         0 | 00:00:00         |         |
| IQueryProvider.GetEnumerator           | 00:00:00         |         0 | 00:00:00         |         |
|   GetQuery                             | 00:00:00.0748878 |         1 | 00:00:00.0748878 |  20.98% |
|     Find                               | 00:00:00.0084079 |         1 | 00:00:00.0084079 |   2.36% |
|       Expose                           | 00:00:00.0081031 |         1 | 00:00:00.0081031 |   2.27% |
|       Find                             | 00:00:00.0002990 |         1 | 00:00:00.0002990 |   0.08% |
|     Create                             | 00:00:00.0664771 |         1 | 00:00:00.0664771 |  18.63% |
|       Build                            | 00:00:00.0450327 |         1 | 00:00:00.0450327 |  12.62% |
|         BuildSequence                  | 00:00:00.0083066 |         2 | 00:00:00.0041533 |   2.33% |
|           CanBuild                     | 00:00:00.0000421 |        23 | 00:00:00.0000018 |   0.01% |
|           Build                        | 00:00:00.0080319 |         2 | 00:00:00.0040159 |   2.25% |
|         ReorderBuilders                | 00:00:00.0004451 |         1 | 00:00:00.0004451 |   0.12% |
|         BuildQuery                     | 00:00:00.0382906 |         1 | 00:00:00.0382906 |  10.73% |
|           FinalizeQuery                | 00:00:00.0590411 |         2 | 00:00:00.0295205 |  16.54% |
|   GetIEnumerable                       | 00:00:00         |         0 | 00:00:00         |         |
| Execute                                | 00:00:00.2289324 |         3 | 00:00:00.0763108 |  64.15% |
|   Execute Query                        | 00:00:00         |         0 | 00:00:00         |         |
|   Execute Query Async                  | 00:00:00         |         0 | 00:00:00         |         |
|   Execute Element                      | 00:00:00.0513571 |         1 | 00:00:00.0513571 |  14.39% |
|   Execute Element Async                | 00:00:00         |         0 | 00:00:00         |         |
|   Execute Scalar                       | 00:00:00         |         0 | 00:00:00         |         |
|   Execute Scalar Async                 | 00:00:00         |         0 | 00:00:00         |         |
|   Execute Scalar 2                     | 00:00:00         |         0 | 00:00:00         |         |
|   Execute Scalar 2 Async               | 00:00:00         |         0 | 00:00:00         |         |
|   Execute NonQuery                     | 00:00:00.0511693 |         1 | 00:00:00.0511693 |  14.34% |
|   Execute NonQuery Async               | 00:00:00         |         0 | 00:00:00         |         |
|   Execute NonQuery 2                   | 00:00:00         |         0 | 00:00:00         |         |
|   Execute NonQuery 2 Async             | 00:00:00         |         0 | 00:00:00         |         |
|   CreateTable                          | 00:00:00.1264060 |         1 | 00:00:00.1264060 |  35.42% |
|   CreateTable Async                    | 00:00:00         |         0 | 00:00:00         |         |
|   DropTable                            | 00:00:00         |         0 | 00:00:00         |         |
|   DropTable Async                      | 00:00:00         |         0 | 00:00:00         |         |
|   Delete Object                        | 00:00:00         |         0 | 00:00:00         |         |
|   Delete Object Async                  | 00:00:00         |         0 | 00:00:00         |         |
|   Insert Object                        | 00:00:00         |         0 | 00:00:00         |         |
|   Insert Object Async                  | 00:00:00         |         0 | 00:00:00         |         |
|   InsertOrReplace Object               | 00:00:00         |         0 | 00:00:00         |         |
|   InsertOrReplace Object Async         | 00:00:00         |         0 | 00:00:00         |         |
|   InsertWithIdentity Object            | 00:00:00         |         0 | 00:00:00         |         |
|   InsertWithIdentity Object Async      | 00:00:00         |         0 | 00:00:00         |         |
|   Update Object                        | 00:00:00         |         0 | 00:00:00         |         |
|   Update Object Async                  | 00:00:00         |         0 | 00:00:00         |         |
|   BulkCopy                             | 00:00:00         |         0 | 00:00:00         |         |
|   BulkCopy Async                       | 00:00:00         |         0 | 00:00:00         |         |
|     BuildSql                           | 00:00:00.0383149 |         2 | 00:00:00.0191574 |  10.74% |
|   SQL Execute                          | 00:00:00         |         0 | 00:00:00         |         |
|   SQL Execute<T>                       | 00:00:00         |         0 | 00:00:00         |         |
|   SQL ExecuteCustom                    | 00:00:00         |         0 | 00:00:00         |         |
|   SQL ExecuteAsync                     | 00:00:00         |         0 | 00:00:00         |         |
|   SQL ExecuteAsync<T>                  | 00:00:00         |         0 | 00:00:00         |         |
|     ADO.NET                            | 00:00:00.0100436 |         4 | 00:00:00.0025109 |   2.81% |
|       Connection Open                  | 00:00:00.0041363 |         1 | 00:00:00.0041363 |   1.16% |
|       Connection OpenAsync             | 00:00:00         |         0 | 00:00:00         |         |
|       Connection Close                 | 00:00:00         |         0 | 00:00:00         |         |
|       Connection CloseAsync            | 00:00:00         |         0 | 00:00:00         |         |
|       Connection Dispose               | 00:00:00.0009168 |         1 | 00:00:00.0009168 |   0.26% |
|       Connection DisposeAsync          | 00:00:00         |         0 | 00:00:00         |         |
|       Connection BeginTransaction      | 00:00:00         |         0 | 00:00:00         |         |
|       Connection BeginTransactionAsync | 00:00:00         |         0 | 00:00:00         |         |
|       Transaction Commit               | 00:00:00         |         0 | 00:00:00         |         |
|       Transaction CommitAsync          | 00:00:00         |         0 | 00:00:00         |         |
|       Transaction Rollback             | 00:00:00         |         0 | 00:00:00         |         |
|       Transaction RollbackAsync        | 00:00:00         |         0 | 00:00:00         |         |
|       Transaction Dispose              | 00:00:00         |         0 | 00:00:00         |         |
|       Transaction DisposeAsync         | 00:00:00         |         0 | 00:00:00         |         |
|       Command ExecuteScalar            | 00:00:00         |         0 | 00:00:00         |         |
|       Command ExecuteScalarAsync       | 00:00:00         |         0 | 00:00:00         |         |
|       Command ExecuteReader            | 00:00:00.0003941 |         1 | 00:00:00.0003941 |   0.11% |
|       Command ExecuteReaderAsync       | 00:00:00         |         0 | 00:00:00         |         |
|       Command ExecuteNonQuery          | 00:00:00.0045964 |         1 | 00:00:00.0045964 |   1.29% |
|       Command ExecuteNonQueryAsync     | 00:00:00         |         0 | 00:00:00         |         |
|     OnTraceInternal                    | 00:00:00         |         0 | 00:00:00         |         |
|     Materialization                    | 00:00:00.0005660 |         1 | 00:00:00.0005660 |   0.16% |
|   GetSqlText                           | 00:00:00         |         0 | 00:00:00         |         |
| Total                                  | 00:00:00.3568933 |         4 | 00:00:00.0892233 | 100.00% |
+----------------------------------------+------------------+-----------+------------------+---------+
```
