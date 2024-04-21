```

BenchmarkDotNet v0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a, Windows 10 (10.0.17763.5328/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.101
  [Host]     : .NET 7.0.15 (7.0.1523.57226), X64 RyuJIT AVX2
  Job-KJWIMT : .NET 6.0.26 (6.0.2623.60508), X64 RyuJIT AVX2
  Job-GULBRG : .NET 7.0.15 (7.0.1523.57226), X64 RyuJIT AVX2
  Job-LRGNRQ : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-SJROSW : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                    | Runtime              | DataProvider | Mean       | Allocated |
|-------------------------- |--------------------- |------------- |-----------:|----------:|
| **VwSalesByYear**             | **.NET 6.0**             | **Access**       |   **415.8 μs** |  **71.99 KB** |
| VwSalesByYear             | .NET 7.0             | Access       |   145.3 μs |  49.32 KB |
| VwSalesByYear             | .NET Core 3.1        | Access       |   497.3 μs |   74.6 KB |
| VwSalesByYear             | .NET Framework 4.7.2 | Access       |   674.4 μs |  97.14 KB |
|                           |                      |              |            |           |
| VwSalesByYearMutation     | .NET 6.0             | Access       |   604.7 μs | 108.94 KB |
| VwSalesByYearMutation     | .NET 7.0             | Access       |   583.6 μs |  85.81 KB |
| VwSalesByYearMutation     | .NET Core 3.1        | Access       |   363.6 μs | 112.51 KB |
| VwSalesByYearMutation     | .NET Framework 4.7.2 | Access       |   988.9 μs | 133.72 KB |
|                           |                      |              |            |           |
| VwSalesByCategoryContains | .NET 6.0             | Access       | 1,363.6 μs | 212.41 KB |
| VwSalesByCategoryContains | .NET 7.0             | Access       | 1,227.6 μs | 182.57 KB |
| VwSalesByCategoryContains | .NET Core 3.1        | Access       | 1,798.4 μs |  218.6 KB |
| VwSalesByCategoryContains | .NET Framework 4.7.2 | Access       | 1,818.4 μs | 246.91 KB |
|                           |                      |              |            |           |
| **VwSalesByYear**             | **.NET 6.0**             | **Firebird**     |   **412.5 μs** |  **72.25 KB** |
| VwSalesByYear             | .NET 7.0             | Firebird     |   327.8 μs |  49.05 KB |
| VwSalesByYear             | .NET Core 3.1        | Firebird     |   513.6 μs |  74.87 KB |
| VwSalesByYear             | .NET Framework 4.7.2 | Firebird     |   666.7 μs |   97.4 KB |
|                           |                      |              |            |           |
| VwSalesByYearMutation     | .NET 6.0             | Firebird     |   681.1 μs | 111.65 KB |
| VwSalesByYearMutation     | .NET 7.0             | Firebird     |   586.2 μs |  88.57 KB |
| VwSalesByYearMutation     | .NET Core 3.1        | Firebird     |   830.2 μs | 115.21 KB |
| VwSalesByYearMutation     | .NET Framework 4.7.2 | Firebird     |   929.8 μs | 136.49 KB |
|                           |                      |              |            |           |
| VwSalesByCategoryContains | .NET 6.0             | Firebird     |   995.9 μs | 151.15 KB |
| VwSalesByCategoryContains | .NET 7.0             | Firebird     |   811.4 μs | 121.94 KB |
| VwSalesByCategoryContains | .NET Core 3.1        | Firebird     | 1,209.2 μs | 156.94 KB |
| VwSalesByCategoryContains | .NET Framework 4.7.2 | Firebird     | 1,451.6 μs | 185.75 KB |
