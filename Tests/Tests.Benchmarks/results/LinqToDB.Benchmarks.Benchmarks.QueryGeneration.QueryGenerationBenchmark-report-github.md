``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.1198 (1909/November2018Update/19H2)
AMD Ryzen 9 3950X, 1 CPU, 32 logical and 16 physical cores
.NET Core SDK=5.0.100
  [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  Job-WRPQJL : .NET Framework 4.8 (4.8.4250.0), X64 RyuJIT
  Job-UDECFI : .NET Core 2.1.23 (CoreCLR 4.6.29321.03, CoreFX 4.6.29321.01), X64 RyuJIT
  Job-HIPQTH : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                    Method |       Runtime |   DataProvider |       Mean |     Median | Ratio | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------- |-------------- |--------------- |-----------:|-----------:|------:|------:|------:|------:|----------:|
|             **VwSalesByYear** |    **.NET 4.7.2** |         **Access** |   **375.1 μs** |   **374.9 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 |         Access |   360.6 μs |   361.7 μs |  0.96 |     - |     - |     - | 105.75 KB |
|             VwSalesByYear | .NET Core 3.1 |         Access |   433.3 μs |   430.9 μs |  1.16 |     - |     - |     - | 104.86 KB |
|                           |               |                |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 |         Access |   396.9 μs |   396.1 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 |         Access |   377.0 μs |   378.8 μs |  0.95 |     - |     - |     - | 114.24 KB |
|     VwSalesByYearMutation | .NET Core 3.1 |         Access |   458.3 μs |   455.7 μs |  1.16 |     - |     - |     - | 111.59 KB |
|                           |               |                |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 |         Access | 1,284.4 μs | 1,280.0 μs |  1.00 |     - |     - |     - |    488 KB |
| VwSalesByCategoryContains | .NET Core 2.1 |         Access | 1,590.0 μs | 1,549.4 μs |  1.19 |     - |     - |     - | 461.38 KB |
| VwSalesByCategoryContains | .NET Core 3.1 |         Access | 1,777.0 μs | 1,762.1 μs |  1.38 |     - |     - |     - | 457.32 KB |
|                           |               |                |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** |       **Firebird** |   **369.8 μs** |   **371.4 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 |       Firebird |   357.4 μs |   355.7 μs |  0.98 |     - |     - |     - | 106.37 KB |
|             VwSalesByYear | .NET Core 3.1 |       Firebird |   433.1 μs |   431.9 μs |  1.17 |     - |     - |     - | 105.45 KB |
|                           |               |                |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 |       Firebird |   402.2 μs |   404.2 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 |       Firebird |   377.4 μs |   377.9 μs |  0.94 |     - |     - |     - | 115.42 KB |
|     VwSalesByYearMutation | .NET Core 3.1 |       Firebird |   462.2 μs |   460.8 μs |  1.15 |     - |     - |     - | 112.76 KB |
|                           |               |                |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 |       Firebird | 1,177.7 μs | 1,162.7 μs |  1.00 |     - |     - |     - |    472 KB |
| VwSalesByCategoryContains | .NET Core 2.1 |       Firebird | 1,371.9 μs | 1,354.7 μs |  1.16 |     - |     - |     - | 442.39 KB |
| VwSalesByCategoryContains | .NET Core 3.1 |       Firebird | 1,681.6 μs | 1,680.5 μs |  1.43 |     - |     - |     - | 437.66 KB |
|                           |               |                |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** | **MySqlConnector** |   **366.5 μs** |   **365.6 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 | MySqlConnector |   357.9 μs |   357.9 μs |  0.98 |     - |     - |     - | 105.72 KB |
|             VwSalesByYear | .NET Core 3.1 | MySqlConnector |   437.7 μs |   440.3 μs |  1.19 |     - |     - |     - |  104.8 KB |
|                           |               |                |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 | MySqlConnector |   399.1 μs |   396.6 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 | MySqlConnector |   375.2 μs |   376.0 μs |  0.93 |     - |     - |     - | 114.77 KB |
|     VwSalesByYearMutation | .NET Core 3.1 | MySqlConnector |   459.7 μs |   460.9 μs |  1.14 |     - |     - |     - | 112.11 KB |
|                           |               |                |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 | MySqlConnector | 1,033.9 μs | 1,022.4 μs |  1.00 |     - |     - |     - |    400 KB |
| VwSalesByCategoryContains | .NET Core 2.1 | MySqlConnector | 1,220.5 μs | 1,216.0 μs |  1.18 |     - |     - |     - | 370.95 KB |
| VwSalesByCategoryContains | .NET Core 3.1 | MySqlConnector | 1,469.7 μs | 1,471.7 μs |  1.42 |     - |     - |     - | 366.59 KB |
|                           |               |                |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** |     **PostgreSQL** |   **365.4 μs** |   **364.9 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 |     PostgreSQL |   359.7 μs |   361.7 μs |  0.98 |     - |     - |     - | 106.44 KB |
|             VwSalesByYear | .NET Core 3.1 |     PostgreSQL |   434.6 μs |   433.6 μs |  1.19 |     - |     - |     - | 104.84 KB |
|                           |               |                |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 |     PostgreSQL |   413.1 μs |   397.1 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 |     PostgreSQL |   378.0 μs |   377.4 μs |  0.87 |     - |     - |     - |  114.7 KB |
|     VwSalesByYearMutation | .NET Core 3.1 |     PostgreSQL |   459.9 μs |   461.0 μs |  1.07 |     - |     - |     - | 111.99 KB |
|                           |               |                |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 |     PostgreSQL | 1,060.3 μs | 1,058.8 μs |  1.00 |     - |     - |     - |    400 KB |
| VwSalesByCategoryContains | .NET Core 2.1 |     PostgreSQL | 1,245.3 μs | 1,247.8 μs |  1.17 |     - |     - |     - | 373.49 KB |
| VwSalesByCategoryContains | .NET Core 3.1 |     PostgreSQL | 1,535.5 μs | 1,532.6 μs |  1.45 |     - |     - |     - | 371.84 KB |
|                           |               |                |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** | **SQLite.Classic** |   **367.5 μs** |   **366.1 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 | SQLite.Classic |   353.7 μs |   353.5 μs |  0.96 |     - |     - |     - | 105.93 KB |
|             VwSalesByYear | .NET Core 3.1 | SQLite.Classic |   432.2 μs |   432.5 μs |  1.17 |     - |     - |     - |  105.3 KB |
|                           |               |                |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 | SQLite.Classic |   389.0 μs |   387.6 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 | SQLite.Classic |   371.8 μs |   371.0 μs |  0.95 |     - |     - |     - | 115.31 KB |
|     VwSalesByYearMutation | .NET Core 3.1 | SQLite.Classic |   457.3 μs |   458.9 μs |  1.17 |     - |     - |     - | 112.64 KB |
|                           |               |                |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 | SQLite.Classic | 1,051.3 μs | 1,042.1 μs |  1.00 |     - |     - |     - |    400 KB |
| VwSalesByCategoryContains | .NET Core 2.1 | SQLite.Classic | 1,229.4 μs | 1,233.0 μs |  1.17 |     - |     - |     - | 374.44 KB |
| VwSalesByCategoryContains | .NET Core 3.1 | SQLite.Classic | 1,516.0 μs | 1,519.2 μs |  1.44 |     - |     - |     - | 374.13 KB |
|                           |               |                |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** |      **SQLite.MS** |   **369.7 μs** |   **367.4 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 |      SQLite.MS |   355.1 μs |   354.9 μs |  0.96 |     - |     - |     - | 105.82 KB |
|             VwSalesByYear | .NET Core 3.1 |      SQLite.MS |   430.4 μs |   427.7 μs |  1.15 |     - |     - |     - |  105.2 KB |
|                           |               |                |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 |      SQLite.MS |   393.9 μs |   395.2 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 |      SQLite.MS |   373.5 μs |   374.5 μs |  0.95 |     - |     - |     - | 115.22 KB |
|     VwSalesByYearMutation | .NET Core 3.1 |      SQLite.MS |   506.6 μs |   487.4 μs |  1.29 |     - |     - |     - | 112.56 KB |
|                           |               |                |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 |      SQLite.MS | 1,044.6 μs | 1,044.7 μs |  1.00 |     - |     - |     - |    400 KB |
| VwSalesByCategoryContains | .NET Core 2.1 |      SQLite.MS | 1,231.4 μs | 1,229.6 μs |  1.18 |     - |     - |     - | 374.36 KB |
| VwSalesByCategoryContains | .NET Core 3.1 |      SQLite.MS | 1,508.3 μs | 1,506.6 μs |  1.44 |     - |     - |     - | 374.06 KB |
|                           |               |                |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** | **SqlServer.2000** |   **367.6 μs** |   **367.6 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 | SqlServer.2000 |   357.3 μs |   357.6 μs |  0.97 |     - |     - |     - | 104.97 KB |
|             VwSalesByYear | .NET Core 3.1 | SqlServer.2000 |   436.0 μs |   435.1 μs |  1.18 |     - |     - |     - |  101.6 KB |
|                           |               |                |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 | SqlServer.2000 |   396.1 μs |   394.6 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 | SqlServer.2000 |   382.8 μs |   384.6 μs |  0.97 |     - |     - |     - | 114.23 KB |
|     VwSalesByYearMutation | .NET Core 3.1 | SqlServer.2000 |   466.8 μs |   466.9 μs |  1.18 |     - |     - |     - | 110.77 KB |
|                           |               |                |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 | SqlServer.2000 | 1,040.8 μs | 1,034.4 μs |  1.00 |     - |     - |     - |    392 KB |
| VwSalesByCategoryContains | .NET Core 2.1 | SqlServer.2000 | 1,230.0 μs | 1,230.0 μs |  1.18 |     - |     - |     - | 369.01 KB |
| VwSalesByCategoryContains | .NET Core 3.1 | SqlServer.2000 | 1,489.4 μs | 1,489.0 μs |  1.43 |     - |     - |     - | 366.46 KB |
|                           |               |                |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** | **SqlServer.2005** |   **369.4 μs** |   **369.4 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 | SqlServer.2005 |   352.7 μs |   352.8 μs |  0.95 |     - |     - |     - | 107.58 KB |
|             VwSalesByYear | .NET Core 3.1 | SqlServer.2005 |   433.2 μs |   431.3 μs |  1.17 |     - |     - |     - | 104.09 KB |
|                           |               |                |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 | SqlServer.2005 |   396.1 μs |   395.0 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 | SqlServer.2005 |   377.5 μs |   375.8 μs |  0.96 |     - |     - |     - | 114.23 KB |
|     VwSalesByYearMutation | .NET Core 3.1 | SqlServer.2005 |   466.1 μs |   466.9 μs |  1.17 |     - |     - |     - | 110.77 KB |
|                           |               |                |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 | SqlServer.2005 | 1,468.5 μs | 1,463.3 μs |  1.00 |     - |     - |     - |    736 KB |
| VwSalesByCategoryContains | .NET Core 2.1 | SqlServer.2005 | 1,690.0 μs | 1,697.0 μs |  1.15 |     - |     - |     - |  703.9 KB |
| VwSalesByCategoryContains | .NET Core 3.1 | SqlServer.2005 | 2,184.9 μs | 2,195.7 μs |  1.44 |     - |     - |     - | 699.69 KB |
|                           |               |                |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** | **SqlServer.2008** |   **368.9 μs** |   **368.0 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 | SqlServer.2008 |   350.1 μs |   350.4 μs |  0.95 |     - |     - |     - | 104.97 KB |
|             VwSalesByYear | .NET Core 3.1 | SqlServer.2008 |   430.0 μs |   430.1 μs |  1.16 |     - |     - |     - |  101.6 KB |
|                           |               |                |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 | SqlServer.2008 |   391.7 μs |   388.6 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 | SqlServer.2008 |   394.6 μs |   382.2 μs |  1.07 |     - |     - |     - | 114.23 KB |
|     VwSalesByYearMutation | .NET Core 3.1 | SqlServer.2008 |   464.7 μs |   462.4 μs |  1.19 |     - |     - |     - | 110.77 KB |
|                           |               |                |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 | SqlServer.2008 | 1,036.2 μs | 1,037.3 μs |  1.00 |     - |     - |     - |    392 KB |
| VwSalesByCategoryContains | .NET Core 2.1 | SqlServer.2008 | 1,220.7 μs | 1,224.0 μs |  1.18 |     - |     - |     - | 369.01 KB |
| VwSalesByCategoryContains | .NET Core 3.1 | SqlServer.2008 | 1,474.7 μs | 1,470.0 μs |  1.42 |     - |     - |     - | 366.39 KB |
|                           |               |                |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** | **SqlServer.2012** |   **363.5 μs** |   **363.8 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 | SqlServer.2012 |   350.1 μs |   351.1 μs |  0.96 |     - |     - |     - | 104.97 KB |
|             VwSalesByYear | .NET Core 3.1 | SqlServer.2012 |   426.2 μs |   425.2 μs |  1.17 |     - |     - |     - |  101.6 KB |
|                           |               |                |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 | SqlServer.2012 |   398.6 μs |   398.2 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 | SqlServer.2012 |   376.8 μs |   377.1 μs |  0.95 |     - |     - |     - | 114.23 KB |
|     VwSalesByYearMutation | .NET Core 3.1 | SqlServer.2012 |   458.4 μs |   459.0 μs |  1.15 |     - |     - |     - | 110.77 KB |
|                           |               |                |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 | SqlServer.2012 | 1,029.4 μs | 1,030.3 μs |  1.00 |     - |     - |     - |    392 KB |
| VwSalesByCategoryContains | .NET Core 2.1 | SqlServer.2012 | 1,217.8 μs | 1,217.2 μs |  1.18 |     - |     - |     - | 369.01 KB |
| VwSalesByCategoryContains | .NET Core 3.1 | SqlServer.2012 | 1,481.6 μs | 1,479.4 μs |  1.43 |     - |     - |     - | 366.39 KB |
|                           |               |                |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** | **SqlServer.2017** |   **367.2 μs** |   **365.1 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 | SqlServer.2017 |   352.3 μs |   353.0 μs |  0.96 |     - |     - |     - | 104.97 KB |
|             VwSalesByYear | .NET Core 3.1 | SqlServer.2017 |   429.1 μs |   427.7 μs |  1.16 |     - |     - |     - |  101.6 KB |
|                           |               |                |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 | SqlServer.2017 |   391.2 μs |   389.3 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 | SqlServer.2017 |   383.8 μs |   385.9 μs |  0.98 |     - |     - |     - | 114.23 KB |
|     VwSalesByYearMutation | .NET Core 3.1 | SqlServer.2017 |   459.1 μs |   458.9 μs |  1.17 |     - |     - |     - | 110.77 KB |
|                           |               |                |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 | SqlServer.2017 | 1,042.6 μs | 1,037.8 μs |  1.00 |     - |     - |     - |    392 KB |
| VwSalesByCategoryContains | .NET Core 2.1 | SqlServer.2017 | 1,230.6 μs | 1,221.5 μs |  1.19 |     - |     - |     - | 369.01 KB |
| VwSalesByCategoryContains | .NET Core 3.1 | SqlServer.2017 | 1,473.9 μs | 1,472.4 μs |  1.40 |     - |     - |     - | 366.46 KB |
