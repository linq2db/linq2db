``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.1198 (1909/November2018Update/19H2)
AMD Ryzen 9 3950X, 1 CPU, 32 logical and 16 physical cores
.NET Core SDK=5.0.100
  [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  Job-BEZTAO : .NET Framework 4.8 (4.8.4250.0), X64 RyuJIT
  Job-TFLCPZ : .NET Core 2.1.23 (CoreCLR 4.6.29321.03, CoreFX 4.6.29321.01), X64 RyuJIT
  Job-WVQBEJ : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                Method |       Runtime |   DataProvider |     Mean |   Median | Ratio | Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------- |-------------- |--------------- |---------:|---------:|------:|------:|------:|------:|----------:|
|         **VwSalesByYear** |    **.NET 4.7.2** |         **Access** | **425.0 μs** | **424.6 μs** |  **1.00** |     **-** |     **-** |     **-** |    **136 KB** |
|         VwSalesByYear | .NET Core 2.1 |         Access | 407.8 μs | 407.8 μs |  0.96 |     - |     - |     - | 108.44 KB |
|         VwSalesByYear | .NET Core 3.1 |         Access | 502.0 μs | 502.2 μs |  1.18 |     - |     - |     - | 108.56 KB |
|                       |               |                |          |          |       |       |       |       |           |
| VwSalesByYearMutation |    .NET 4.7.2 |         Access | 446.9 μs | 445.0 μs |  1.00 |     - |     - |     - |    136 KB |
| VwSalesByYearMutation | .NET Core 2.1 |         Access | 431.8 μs | 432.6 μs |  0.97 |     - |     - |     - | 116.82 KB |
| VwSalesByYearMutation | .NET Core 3.1 |         Access | 511.8 μs | 511.5 μs |  1.15 |     - |     - |     - | 114.95 KB |
|                       |               |                |          |          |       |       |       |       |           |
|         **VwSalesByYear** |    **.NET 4.7.2** |       **Firebird** | **442.4 μs** | **431.9 μs** |  **1.00** |     **-** |     **-** |     **-** |    **136 KB** |
|         VwSalesByYear | .NET Core 2.1 |       Firebird | 407.2 μs | 406.3 μs |  0.94 |     - |     - |     - | 109.88 KB |
|         VwSalesByYear | .NET Core 3.1 |       Firebird | 485.3 μs | 487.4 μs |  1.11 |     - |     - |     - |    110 KB |
|                       |               |                |          |          |       |       |       |       |           |
| VwSalesByYearMutation |    .NET 4.7.2 |       Firebird | 440.7 μs | 441.7 μs |  1.00 |     - |     - |     - |    144 KB |
| VwSalesByYearMutation | .NET Core 2.1 |       Firebird | 429.7 μs | 431.6 μs |  0.98 |     - |     - |     - | 118.84 KB |
| VwSalesByYearMutation | .NET Core 3.1 |       Firebird | 506.3 μs | 507.8 μs |  1.16 |     - |     - |     - | 117.29 KB |
|                       |               |                |          |          |       |       |       |       |           |
|         **VwSalesByYear** |    **.NET 4.7.2** | **MySqlConnector** | **415.8 μs** | **414.2 μs** |  **1.00** |     **-** |     **-** |     **-** |    **136 KB** |
|         VwSalesByYear | .NET Core 2.1 | MySqlConnector | 408.7 μs | 408.1 μs |  0.98 |     - |     - |     - | 109.23 KB |
|         VwSalesByYear | .NET Core 3.1 | MySqlConnector | 488.8 μs | 488.8 μs |  1.17 |     - |     - |     - | 109.35 KB |
|                       |               |                |          |          |       |       |       |       |           |
| VwSalesByYearMutation |    .NET 4.7.2 | MySqlConnector | 438.7 μs | 437.8 μs |  1.00 |     - |     - |     - |    144 KB |
| VwSalesByYearMutation | .NET Core 2.1 | MySqlConnector | 433.4 μs | 434.0 μs |  1.00 |     - |     - |     - |  118.2 KB |
| VwSalesByYearMutation | .NET Core 3.1 | MySqlConnector | 510.1 μs | 510.1 μs |  1.16 |     - |     - |     - | 116.64 KB |
|                       |               |                |          |          |       |       |       |       |           |
|         **VwSalesByYear** |    **.NET 4.7.2** |     **PostgreSQL** | **433.8 μs** | **426.3 μs** |  **1.00** |     **-** |     **-** |     **-** |    **136 KB** |
|         VwSalesByYear | .NET Core 2.1 |     PostgreSQL | 410.0 μs | 408.7 μs |  0.94 |     - |     - |     - | 109.45 KB |
|         VwSalesByYear | .NET Core 3.1 |     PostgreSQL | 489.8 μs | 489.6 μs |  1.06 |     - |     - |     - | 110.06 KB |
|                       |               |                |          |          |       |       |       |       |           |
| VwSalesByYearMutation |    .NET 4.7.2 |     PostgreSQL | 433.2 μs | 433.0 μs |  1.00 |     - |     - |     - |    144 KB |
| VwSalesByYearMutation | .NET Core 2.1 |     PostgreSQL | 432.2 μs | 436.1 μs |  1.00 |     - |     - |     - | 118.05 KB |
| VwSalesByYearMutation | .NET Core 3.1 |     PostgreSQL | 508.7 μs | 510.0 μs |  1.17 |     - |     - |     - | 115.48 KB |
|                       |               |                |          |          |       |       |       |       |           |
|         **VwSalesByYear** |    **.NET 4.7.2** | **SQLite.Classic** | **414.7 μs** | **416.3 μs** |  **1.00** |     **-** |     **-** |     **-** |    **136 KB** |
|         VwSalesByYear | .NET Core 2.1 | SQLite.Classic | 399.5 μs | 401.4 μs |  0.96 |     - |     - |     - | 109.05 KB |
|         VwSalesByYear | .NET Core 3.1 | SQLite.Classic | 484.2 μs | 486.3 μs |  1.16 |     - |     - |     - | 109.82 KB |
|                       |               |                |          |          |       |       |       |       |           |
| VwSalesByYearMutation |    .NET 4.7.2 | SQLite.Classic | 434.7 μs | 434.4 μs |  1.00 |     - |     - |     - |    144 KB |
| VwSalesByYearMutation | .NET Core 2.1 | SQLite.Classic | 427.4 μs | 430.8 μs |  0.99 |     - |     - |     - | 118.72 KB |
| VwSalesByYearMutation | .NET Core 3.1 | SQLite.Classic | 499.4 μs | 499.7 μs |  1.15 |     - |     - |     - | 117.19 KB |
|                       |               |                |          |          |       |       |       |       |           |
|         **VwSalesByYear** |    **.NET 4.7.2** |      **SQLite.MS** | **415.1 μs** | **413.9 μs** |  **1.00** |     **-** |     **-** |     **-** |    **136 KB** |
|         VwSalesByYear | .NET Core 2.1 |      SQLite.MS | 402.9 μs | 404.6 μs |  0.96 |     - |     - |     - | 108.98 KB |
|         VwSalesByYear | .NET Core 3.1 |      SQLite.MS | 483.1 μs | 481.1 μs |  1.15 |     - |     - |     - | 109.74 KB |
|                       |               |                |          |          |       |       |       |       |           |
| VwSalesByYearMutation |    .NET 4.7.2 |      SQLite.MS | 444.5 μs | 445.3 μs |  1.00 |     - |     - |     - |    144 KB |
| VwSalesByYearMutation | .NET Core 2.1 |      SQLite.MS | 429.0 μs | 429.1 μs |  0.96 |     - |     - |     - | 118.64 KB |
| VwSalesByYearMutation | .NET Core 3.1 |      SQLite.MS | 506.4 μs | 504.5 μs |  1.15 |     - |     - |     - | 117.09 KB |
|                       |               |                |          |          |       |       |       |       |           |
|         **VwSalesByYear** |    **.NET 4.7.2** | **SqlServer.2000** | **411.5 μs** | **409.9 μs** |  **1.00** |     **-** |     **-** |     **-** |    **136 KB** |
|         VwSalesByYear | .NET Core 2.1 | SqlServer.2000 | 411.8 μs | 412.6 μs |  1.00 |     - |     - |     - | 109.19 KB |
|         VwSalesByYear | .NET Core 3.1 | SqlServer.2000 | 490.2 μs | 490.6 μs |  1.19 |     - |     - |     - | 108.21 KB |
|                       |               |                |          |          |       |       |       |       |           |
| VwSalesByYearMutation |    .NET 4.7.2 | SqlServer.2000 | 443.0 μs | 443.1 μs |  1.00 |     - |     - |     - |    144 KB |
| VwSalesByYearMutation | .NET Core 2.1 | SqlServer.2000 | 433.8 μs | 436.8 μs |  0.99 |     - |     - |     - | 117.85 KB |
| VwSalesByYearMutation | .NET Core 3.1 | SqlServer.2000 | 517.5 μs | 516.6 μs |  1.17 |     - |     - |     - | 115.48 KB |
|                       |               |                |          |          |       |       |       |       |           |
|         **VwSalesByYear** |    **.NET 4.7.2** | **SqlServer.2005** | **418.3 μs** | **418.3 μs** |  **1.00** |     **-** |     **-** |     **-** |    **136 KB** |
|         VwSalesByYear | .NET Core 2.1 | SqlServer.2005 | 408.6 μs | 408.9 μs |  0.98 |     - |     - |     - | 111.67 KB |
|         VwSalesByYear | .NET Core 3.1 | SqlServer.2005 | 483.8 μs | 485.2 μs |  1.15 |     - |     - |     - | 110.71 KB |
|                       |               |                |          |          |       |       |       |       |           |
| VwSalesByYearMutation |    .NET 4.7.2 | SqlServer.2005 | 454.1 μs | 445.0 μs |  1.00 |     - |     - |     - |    144 KB |
| VwSalesByYearMutation | .NET Core 2.1 | SqlServer.2005 | 432.7 μs | 434.6 μs |  0.91 |     - |     - |     - | 117.85 KB |
| VwSalesByYearMutation | .NET Core 3.1 | SqlServer.2005 | 523.8 μs | 521.5 μs |  1.07 |     - |     - |     - | 115.48 KB |
|                       |               |                |          |          |       |       |       |       |           |
|         **VwSalesByYear** |    **.NET 4.7.2** | **SqlServer.2008** | **419.0 μs** | **420.9 μs** |  **1.00** |     **-** |     **-** |     **-** |    **136 KB** |
|         VwSalesByYear | .NET Core 2.1 | SqlServer.2008 | 418.7 μs | 413.3 μs |  1.02 |     - |     - |     - | 109.19 KB |
|         VwSalesByYear | .NET Core 3.1 | SqlServer.2008 | 484.2 μs | 484.5 μs |  1.15 |     - |     - |     - | 108.21 KB |
|                       |               |                |          |          |       |       |       |       |           |
| VwSalesByYearMutation |    .NET 4.7.2 | SqlServer.2008 | 447.3 μs | 447.1 μs |  1.00 |     - |     - |     - |    144 KB |
| VwSalesByYearMutation | .NET Core 2.1 | SqlServer.2008 | 443.1 μs | 442.6 μs |  0.99 |     - |     - |     - | 117.85 KB |
| VwSalesByYearMutation | .NET Core 3.1 | SqlServer.2008 | 520.1 μs | 521.6 μs |  1.16 |     - |     - |     - | 115.48 KB |
|                       |               |                |          |          |       |       |       |       |           |
|         **VwSalesByYear** |    **.NET 4.7.2** | **SqlServer.2012** | **416.2 μs** | **414.9 μs** |  **1.00** |     **-** |     **-** |     **-** |    **136 KB** |
|         VwSalesByYear | .NET Core 2.1 | SqlServer.2012 | 453.2 μs | 437.1 μs |  1.11 |     - |     - |     - | 109.19 KB |
|         VwSalesByYear | .NET Core 3.1 | SqlServer.2012 | 640.9 μs | 621.2 μs |  1.59 |     - |     - |     - | 108.21 KB |
|                       |               |                |          |          |       |       |       |       |           |
| VwSalesByYearMutation |    .NET 4.7.2 | SqlServer.2012 | 441.2 μs | 443.0 μs |  1.00 |     - |     - |     - |    144 KB |
| VwSalesByYearMutation | .NET Core 2.1 | SqlServer.2012 | 475.3 μs | 455.3 μs |  1.08 |     - |     - |     - | 117.85 KB |
| VwSalesByYearMutation | .NET Core 3.1 | SqlServer.2012 | 533.0 μs | 527.2 μs |  1.22 |     - |     - |     - | 115.48 KB |
|                       |               |                |          |          |       |       |       |       |           |
|         **VwSalesByYear** |    **.NET 4.7.2** | **SqlServer.2017** | **417.7 μs** | **418.5 μs** |  **1.00** |     **-** |     **-** |     **-** |    **136 KB** |
|         VwSalesByYear | .NET Core 2.1 | SqlServer.2017 | 405.3 μs | 408.8 μs |  0.97 |     - |     - |     - | 109.19 KB |
|         VwSalesByYear | .NET Core 3.1 | SqlServer.2017 | 491.4 μs | 488.4 μs |  1.18 |     - |     - |     - | 108.21 KB |
|                       |               |                |          |          |       |       |       |       |           |
| VwSalesByYearMutation |    .NET 4.7.2 | SqlServer.2017 | 438.4 μs | 438.9 μs |  1.00 |     - |     - |     - |    144 KB |
| VwSalesByYearMutation | .NET Core 2.1 | SqlServer.2017 | 435.7 μs | 435.5 μs |  1.01 |     - |     - |     - | 117.85 KB |
| VwSalesByYearMutation | .NET Core 3.1 | SqlServer.2017 | 519.6 μs | 517.6 μs |  1.19 |     - |     - |     - | 115.48 KB |
