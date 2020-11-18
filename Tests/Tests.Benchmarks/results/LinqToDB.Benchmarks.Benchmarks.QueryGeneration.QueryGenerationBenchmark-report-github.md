``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.1198 (1909/November2018Update/19H2)
AMD Ryzen 9 3950X, 1 CPU, 32 logical and 16 physical cores
.NET Core SDK=5.0.100
  [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  Job-RZNSOE : .NET Framework 4.8 (4.8.4250.0), X64 RyuJIT
  Job-HJHMJA : .NET Core 2.1.23 (CoreCLR 4.6.29321.03, CoreFX 4.6.29321.01), X64 RyuJIT
  Job-QJYWEE : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT

Jit=RyuJit  Platform=X64  WarmupCount=0  

```
|                    Method |       Runtime |   DataProvider |       Mean |     Median | Ratio | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------- |-------------- |--------------- |-----------:|-----------:|------:|------:|------:|------:|----------:|
|             **VwSalesByYear** |    **.NET 4.7.2** |         **Access** |   **372.6 μs** |   **369.1 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 |         Access |   361.4 μs |   361.6 μs |  0.97 |     - |     - |     - | 106.45 KB |
|             VwSalesByYear | .NET Core 3.1 |         Access |   430.4 μs |   430.4 μs |  1.15 |     - |     - |     - | 102.95 KB |
|                           |               |                |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 |         Access |   392.4 μs |   391.9 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 |         Access |   375.0 μs |   375.2 μs |  0.96 |     - |     - |     - | 112.87 KB |
|     VwSalesByYearMutation | .NET Core 3.1 |         Access |   466.8 μs |   468.5 μs |  1.18 |     - |     - |     - | 110.79 KB |
|                           |               |                |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 |         Access | 1,414.4 μs | 1,392.6 μs |  1.00 |     - |     - |     - |    560 KB |
| VwSalesByCategoryContains | .NET Core 2.1 |         Access | 1,586.0 μs | 1,559.3 μs |  1.12 |     - |     - |     - | 532.55 KB |
| VwSalesByCategoryContains | .NET Core 3.1 |         Access | 1,910.1 μs | 1,904.4 μs |  1.38 |     - |     - |     - |  533.8 KB |
|                           |               |                |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** |       **Firebird** |   **414.0 μs** |   **385.1 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 |       Firebird |   361.3 μs |   362.2 μs |  0.87 |     - |     - |     - | 107.07 KB |
|             VwSalesByYear | .NET Core 3.1 |       Firebird |   433.0 μs |   432.5 μs |  1.11 |     - |     - |     - | 103.55 KB |
|                           |               |                |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 |       Firebird |   416.9 μs |   404.6 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 |       Firebird |   377.6 μs |   377.3 μs |  0.88 |     - |     - |     - | 114.05 KB |
|     VwSalesByYearMutation | .NET Core 3.1 |       Firebird |   455.3 μs |   454.9 μs |  1.06 |     - |     - |     - | 111.96 KB |
|                           |               |                |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 |       Firebird | 1,239.4 μs | 1,222.3 μs |  1.00 |     - |     - |     - |    512 KB |
| VwSalesByCategoryContains | .NET Core 2.1 |       Firebird | 1,437.4 μs | 1,407.3 μs |  1.16 |     - |     - |     - | 485.95 KB |
| VwSalesByCategoryContains | .NET Core 3.1 |       Firebird | 1,731.9 μs | 1,714.3 μs |  1.40 |     - |     - |     - | 486.84 KB |
|                           |               |                |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** | **MySqlConnector** |   **366.0 μs** |   **364.8 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 | MySqlConnector |   358.0 μs |   358.1 μs |  0.98 |     - |     - |     - | 106.42 KB |
|             VwSalesByYear | .NET Core 3.1 | MySqlConnector |   430.8 μs |   431.4 μs |  1.18 |     - |     - |     - |  102.9 KB |
|                           |               |                |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 | MySqlConnector |   396.6 μs |   394.3 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 | MySqlConnector |   381.6 μs |   380.3 μs |  0.96 |     - |     - |     - |  113.4 KB |
|     VwSalesByYearMutation | .NET Core 3.1 | MySqlConnector |   455.4 μs |   454.9 μs |  1.13 |     - |     - |     - | 111.31 KB |
|                           |               |                |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 | MySqlConnector | 1,096.5 μs | 1,089.8 μs |  1.00 |     - |     - |     - |    424 KB |
| VwSalesByCategoryContains | .NET Core 2.1 | MySqlConnector | 1,266.9 μs | 1,248.2 μs |  1.16 |     - |     - |     - | 393.04 KB |
| VwSalesByCategoryContains | .NET Core 3.1 | MySqlConnector | 1,556.9 μs | 1,546.5 μs |  1.42 |     - |     - |     - | 394.61 KB |
|                           |               |                |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** |     **PostgreSQL** |   **364.6 μs** |   **362.3 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 |     PostgreSQL |   352.5 μs |   353.0 μs |  0.97 |     - |     - |     - | 105.08 KB |
|             VwSalesByYear | .NET Core 3.1 |     PostgreSQL |   429.4 μs |   426.3 μs |  1.18 |     - |     - |     - | 102.94 KB |
|                           |               |                |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 |     PostgreSQL |   389.7 μs |   390.8 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 |     PostgreSQL |   381.2 μs |   382.4 μs |  0.98 |     - |     - |     - | 113.99 KB |
|     VwSalesByYearMutation | .NET Core 3.1 |     PostgreSQL |   501.3 μs |   506.4 μs |  1.42 |     - |     - |     - |  111.2 KB |
|                           |               |                |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 |     PostgreSQL | 1,102.7 μs | 1,104.2 μs |  1.00 |     - |     - |     - |    424 KB |
| VwSalesByCategoryContains | .NET Core 2.1 |     PostgreSQL | 1,306.9 μs | 1,282.0 μs |  1.20 |     - |     - |     - | 396.24 KB |
| VwSalesByCategoryContains | .NET Core 3.1 |     PostgreSQL | 1,563.6 μs | 1,555.5 μs |  1.41 |     - |     - |     - |  397.8 KB |
|                           |               |                |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** | **SQLite.Classic** |   **366.8 μs** |   **366.8 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 | SQLite.Classic |   352.5 μs |   353.2 μs |  0.96 |     - |     - |     - | 106.63 KB |
|             VwSalesByYear | .NET Core 3.1 | SQLite.Classic |   427.3 μs |   425.8 μs |  1.16 |     - |     - |     - |  103.4 KB |
|                           |               |                |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 | SQLite.Classic |   397.3 μs |   397.1 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 | SQLite.Classic |   370.3 μs |   370.7 μs |  0.94 |     - |     - |     - | 113.94 KB |
|     VwSalesByYearMutation | .NET Core 3.1 | SQLite.Classic |   455.5 μs |   455.2 μs |  1.15 |     - |     - |     - | 111.84 KB |
|                           |               |                |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 | SQLite.Classic | 1,091.7 μs | 1,087.6 μs |  1.00 |     - |     - |     - |    424 KB |
| VwSalesByCategoryContains | .NET Core 2.1 | SQLite.Classic | 1,275.3 μs | 1,258.0 μs |  1.17 |     - |     - |     - | 396.84 KB |
| VwSalesByCategoryContains | .NET Core 3.1 | SQLite.Classic | 1,579.0 μs | 1,570.4 μs |  1.44 |     - |     - |     - | 402.14 KB |
|                           |               |                |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** |      **SQLite.MS** |   **367.3 μs** |   **364.6 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 |      SQLite.MS |   353.3 μs |   352.4 μs |  0.96 |     - |     - |     - | 106.52 KB |
|             VwSalesByYear | .NET Core 3.1 |      SQLite.MS |   439.1 μs |   437.9 μs |  1.19 |     - |     - |     - | 103.29 KB |
|                           |               |                |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 |      SQLite.MS |   389.1 μs |   390.1 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 |      SQLite.MS |   370.8 μs |   373.6 μs |  0.95 |     - |     - |     - | 113.84 KB |
|     VwSalesByYearMutation | .NET Core 3.1 |      SQLite.MS |   471.5 μs |   469.1 μs |  1.22 |     - |     - |     - | 111.77 KB |
|                           |               |                |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 |      SQLite.MS | 1,088.6 μs | 1,090.0 μs |  1.00 |     - |     - |     - |    424 KB |
| VwSalesByCategoryContains | .NET Core 2.1 |      SQLite.MS | 1,290.0 μs | 1,266.0 μs |  1.21 |     - |     - |     - | 396.77 KB |
| VwSalesByCategoryContains | .NET Core 3.1 |      SQLite.MS | 1,575.8 μs | 1,572.4 μs |  1.55 |     - |     - |     - | 402.08 KB |
|                           |               |                |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** | **SqlServer.2000** |   **364.7 μs** |   **364.0 μs** |  **1.00** |     **-** |     **-** |     **-** |    **120 KB** |
|             VwSalesByYear | .NET Core 2.1 | SqlServer.2000 |   458.2 μs |   451.4 μs |  1.31 |     - |     - |     - | 103.63 KB |
|             VwSalesByYear | .NET Core 3.1 | SqlServer.2000 |   438.5 μs |   437.8 μs |  1.21 |     - |     - |     - | 102.74 KB |
|                           |               |                |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 | SqlServer.2000 |   400.5 μs |   397.1 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 | SqlServer.2000 |   392.2 μs |   392.1 μs |  0.97 |     - |     - |     - | 112.01 KB |
|     VwSalesByYearMutation | .NET Core 3.1 | SqlServer.2000 |   465.3 μs |   466.6 μs |  1.17 |     - |     - |     - | 110.75 KB |
|                           |               |                |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 | SqlServer.2000 | 1,106.3 μs | 1,097.9 μs |  1.00 |     - |     - |     - |    416 KB |
| VwSalesByCategoryContains | .NET Core 2.1 | SqlServer.2000 | 1,476.0 μs | 1,455.5 μs |  1.37 |     - |     - |     - | 395.38 KB |
| VwSalesByCategoryContains | .NET Core 3.1 | SqlServer.2000 | 1,546.3 μs | 1,543.0 μs |  1.39 |     - |     - |     - | 391.13 KB |
|                           |               |                |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** | **SqlServer.2005** |   **364.3 μs** |   **364.6 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 | SqlServer.2005 |   353.0 μs |   353.2 μs |  0.97 |     - |     - |     - | 106.23 KB |
|             VwSalesByYear | .NET Core 3.1 | SqlServer.2005 |   436.5 μs |   437.3 μs |  1.20 |     - |     - |     - | 105.23 KB |
|                           |               |                |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 | SqlServer.2005 |   398.2 μs |   396.8 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 | SqlServer.2005 |   376.4 μs |   376.4 μs |  0.95 |     - |     - |     - | 112.01 KB |
|     VwSalesByYearMutation | .NET Core 3.1 | SqlServer.2005 |   462.8 μs |   464.9 μs |  1.16 |     - |     - |     - | 110.75 KB |
|                           |               |                |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 | SqlServer.2005 | 1,586.8 μs | 1,586.4 μs |  1.00 |     - |     - |     - |    816 KB |
| VwSalesByCategoryContains | .NET Core 2.1 | SqlServer.2005 | 1,861.3 μs | 1,850.5 μs |  1.18 |     - |     - |     - | 793.66 KB |
| VwSalesByCategoryContains | .NET Core 3.1 | SqlServer.2005 | 2,347.4 μs | 2,352.9 μs |  1.48 |     - |     - |     - | 787.89 KB |
|                           |               |                |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** | **SqlServer.2008** |   **364.5 μs** |   **364.5 μs** |  **1.00** |     **-** |     **-** |     **-** |    **120 KB** |
|             VwSalesByYear | .NET Core 2.1 | SqlServer.2008 |   354.6 μs |   355.4 μs |  0.97 |     - |     - |     - | 103.63 KB |
|             VwSalesByYear | .NET Core 3.1 | SqlServer.2008 |   435.7 μs |   435.0 μs |  1.20 |     - |     - |     - | 102.74 KB |
|                           |               |                |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 | SqlServer.2008 |   396.2 μs |   394.7 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 | SqlServer.2008 |   380.0 μs |   382.6 μs |  0.96 |     - |     - |     - | 112.01 KB |
|     VwSalesByYearMutation | .NET Core 3.1 | SqlServer.2008 |   465.5 μs |   465.4 μs |  1.17 |     - |     - |     - | 110.75 KB |
|                           |               |                |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 | SqlServer.2008 | 1,092.3 μs | 1,091.0 μs |  1.00 |     - |     - |     - |    416 KB |
| VwSalesByCategoryContains | .NET Core 2.1 | SqlServer.2008 | 1,296.5 μs | 1,275.6 μs |  1.19 |     - |     - |     - | 395.38 KB |
| VwSalesByCategoryContains | .NET Core 3.1 | SqlServer.2008 | 1,560.4 μs | 1,562.2 μs |  1.42 |     - |     - |     - | 391.13 KB |
|                           |               |                |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** | **SqlServer.2012** |   **427.8 μs** |   **397.5 μs** |  **1.00** |     **-** |     **-** |     **-** |    **120 KB** |
|             VwSalesByYear | .NET Core 2.1 | SqlServer.2012 |   353.8 μs |   353.5 μs |  0.79 |     - |     - |     - | 103.63 KB |
|             VwSalesByYear | .NET Core 3.1 | SqlServer.2012 |   442.1 μs |   439.8 μs |  0.97 |     - |     - |     - | 102.74 KB |
|                           |               |                |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 | SqlServer.2012 |   398.1 μs |   396.2 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 | SqlServer.2012 |   378.9 μs |   378.7 μs |  0.95 |     - |     - |     - | 112.01 KB |
|     VwSalesByYearMutation | .NET Core 3.1 | SqlServer.2012 |   465.1 μs |   465.2 μs |  1.16 |     - |     - |     - | 110.75 KB |
|                           |               |                |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 | SqlServer.2012 | 1,094.3 μs | 1,085.2 μs |  1.00 |     - |     - |     - |    416 KB |
| VwSalesByCategoryContains | .NET Core 2.1 | SqlServer.2012 | 1,294.3 μs | 1,274.3 μs |  1.19 |     - |     - |     - | 395.38 KB |
| VwSalesByCategoryContains | .NET Core 3.1 | SqlServer.2012 | 1,545.3 μs | 1,548.9 μs |  1.40 |     - |     - |     - | 391.13 KB |
|                           |               |                |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** | **SqlServer.2017** |   **365.4 μs** |   **364.9 μs** |  **1.00** |     **-** |     **-** |     **-** |    **120 KB** |
|             VwSalesByYear | .NET Core 2.1 | SqlServer.2017 |   353.9 μs |   355.6 μs |  0.97 |     - |     - |     - | 103.63 KB |
|             VwSalesByYear | .NET Core 3.1 | SqlServer.2017 |   443.0 μs |   441.9 μs |  1.22 |     - |     - |     - | 102.74 KB |
|                           |               |                |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 | SqlServer.2017 |   396.8 μs |   396.9 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 | SqlServer.2017 |   378.0 μs |   379.9 μs |  0.96 |     - |     - |     - | 112.01 KB |
|     VwSalesByYearMutation | .NET Core 3.1 | SqlServer.2017 |   462.1 μs |   461.6 μs |  1.16 |     - |     - |     - | 110.75 KB |
|                           |               |                |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 | SqlServer.2017 | 1,089.6 μs | 1,086.0 μs |  1.00 |     - |     - |     - |    416 KB |
| VwSalesByCategoryContains | .NET Core 2.1 | SqlServer.2017 | 1,301.7 μs | 1,286.7 μs |  1.20 |     - |     - |     - | 395.38 KB |
| VwSalesByCategoryContains | .NET Core 3.1 | SqlServer.2017 | 1,559.4 μs | 1,557.4 μs |  1.42 |     - |     - |     - | 391.13 KB |
