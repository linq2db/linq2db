```

BenchmarkDotNet v0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a, Windows 10 (10.0.17763.4644/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 7.0.401
  [Host]     : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
  Job-DAXXNM : .NET 6.0.22 (6.0.2223.42425), X64 RyuJIT AVX2
  Job-SLTPYD : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
  Job-YOWJJJ : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-OZLLFF : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                    | Runtime              | DataProvider | Mean       | Allocated |
|-------------------------- |--------------------- |------------- |-----------:|----------:|
| **VwSalesByYear**             | **.NET 6.0**             | **Access**       |   **402.5 μs** |  **70.64 KB** |
| VwSalesByYear             | .NET 7.0             | Access       |   322.8 μs |  49.01 KB |
| VwSalesByYear             | .NET Core 3.1        | Access       |   591.5 μs |  75.65 KB |
| VwSalesByYear             | .NET Framework 4.7.2 | Access       |   556.0 μs |  92.74 KB |
|                           |                      |              |            |           |
| VwSalesByYearMutation     | .NET 6.0             | Access       |   658.9 μs | 109.52 KB |
| VwSalesByYearMutation     | .NET 7.0             | Access       |   568.3 μs |  86.23 KB |
| VwSalesByYearMutation     | .NET Core 3.1        | Access       |   885.9 μs | 111.59 KB |
| VwSalesByYearMutation     | .NET Framework 4.7.2 | Access       | 1,020.7 μs |    139 KB |
|                           |                      |              |            |           |
| VwSalesByCategoryContains | .NET 6.0             | Access       | 1,355.1 μs | 214.83 KB |
| VwSalesByCategoryContains | .NET 7.0             | Access       |   545.4 μs | 183.66 KB |
| VwSalesByCategoryContains | .NET Core 3.1        | Access       | 1,817.8 μs | 216.76 KB |
| VwSalesByCategoryContains | .NET Framework 4.7.2 | Access       | 1,966.7 μs | 247.01 KB |
|                           |                      |              |            |           |
| **VwSalesByYear**             | **.NET 6.0**             | **Firebird**     |   **412.9 μs** |  **70.91 KB** |
| VwSalesByYear             | .NET 7.0             | Firebird     |   320.9 μs |  48.98 KB |
| VwSalesByYear             | .NET Core 3.1        | Firebird     |   573.6 μs |  75.91 KB |
| VwSalesByYear             | .NET Framework 4.7.2 | Firebird     |   293.8 μs |  92.99 KB |
|                           |                      |              |            |           |
| VwSalesByYearMutation     | .NET 6.0             | Firebird     |   685.9 μs | 112.23 KB |
| VwSalesByYearMutation     | .NET 7.0             | Firebird     |   594.7 μs |  89.74 KB |
| VwSalesByYearMutation     | .NET Core 3.1        | Firebird     |   916.3 μs | 114.29 KB |
| VwSalesByYearMutation     | .NET Framework 4.7.2 | Firebird     | 1,010.9 μs | 141.73 KB |
|                           |                      |              |            |           |
| VwSalesByCategoryContains | .NET 6.0             | Firebird     |   997.9 μs | 153.92 KB |
| VwSalesByCategoryContains | .NET 7.0             | Firebird     |   760.6 μs | 122.82 KB |
| VwSalesByCategoryContains | .NET Core 3.1        | Firebird     | 1,343.0 μs | 155.81 KB |
| VwSalesByCategoryContains | .NET Framework 4.7.2 | Firebird     | 1,466.5 μs | 183.26 KB |
