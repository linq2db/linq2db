```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.17763.6766/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4775.0), X64 RyuJIT VectorSize=256
  Job-GEKMDY : .NET 6.0.36 (6.0.3624.51421), X64 RyuJIT AVX2
  Job-WEIMGV : .NET 8.0.12 (8.0.1224.60305), X64 RyuJIT AVX2
  Job-ARZZBJ : .NET 9.0.1 (9.0.124.61010), X64 RyuJIT AVX2
  Job-HBTJES : .NET Framework 4.8 (4.8.4775.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                    | Runtime              | DataProvider | Mean      | Allocated |
|-------------------------- |--------------------- |------------- |----------:|----------:|
| **VwSalesByYear**             | **.NET 6.0**             | **Access**       | **130.92 μs** |  **43.36 KB** |
| VwSalesByYear             | .NET 8.0             | Access       |  83.04 μs |  37.07 KB |
| VwSalesByYear             | .NET 9.0             | Access       |  81.57 μs |  36.43 KB |
| VwSalesByYear             | .NET Framework 4.6.2 | Access       | 262.57 μs |  66.14 KB |
|                           |                      |              |           |           |
| VwSalesByYearMutation     | .NET 6.0             | Access       | 142.61 μs |  49.62 KB |
| VwSalesByYearMutation     | .NET 8.0             | Access       |  96.43 μs |  38.17 KB |
| VwSalesByYearMutation     | .NET 9.0             | Access       |  85.42 μs |  38.41 KB |
| VwSalesByYearMutation     | .NET Framework 4.6.2 | Access       | 122.26 μs |  68.54 KB |
|                           |                      |              |           |           |
| VwSalesByCategoryContains | .NET 6.0             | Access       | 191.54 μs |  66.88 KB |
| VwSalesByCategoryContains | .NET 8.0             | Access       | 145.61 μs |  58.82 KB |
| VwSalesByCategoryContains | .NET 9.0             | Access       | 118.23 μs |  58.44 KB |
| VwSalesByCategoryContains | .NET Framework 4.6.2 | Access       | 422.86 μs |  102.5 KB |
|                           |                      |              |           |           |
| **VwSalesByYear**             | **.NET 6.0**             | **Firebird**     | **128.87 μs** |  **43.36 KB** |
| VwSalesByYear             | .NET 8.0             | Firebird     |  89.23 μs |  35.82 KB |
| VwSalesByYear             | .NET 9.0             | Firebird     |  53.18 μs |  36.21 KB |
| VwSalesByYear             | .NET Framework 4.6.2 | Firebird     | 260.35 μs |  66.14 KB |
|                           |                      |              |           |           |
| VwSalesByYearMutation     | .NET 6.0             | Firebird     | 107.41 μs |  47.17 KB |
| VwSalesByYearMutation     | .NET 8.0             | Firebird     |  44.73 μs |   39.3 KB |
| VwSalesByYearMutation     | .NET 9.0             | Firebird     |  83.79 μs |  38.55 KB |
| VwSalesByYearMutation     | .NET Framework 4.6.2 | Firebird     | 276.14 μs |  68.54 KB |
|                           |                      |              |           |           |
| VwSalesByCategoryContains | .NET 6.0             | Firebird     | 210.11 μs |  68.16 KB |
| VwSalesByCategoryContains | .NET 8.0             | Firebird     | 147.59 μs |  58.25 KB |
| VwSalesByCategoryContains | .NET 9.0             | Firebird     | 132.53 μs |  59.48 KB |
| VwSalesByCategoryContains | .NET Framework 4.6.2 | Firebird     | 423.06 μs |  102.5 KB |
