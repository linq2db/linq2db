```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.17763.5696/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.204
  [Host]     : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX2
  Job-VZLGGZ : .NET 6.0.29 (6.0.2924.17105), X64 RyuJIT AVX2
  Job-AZKKUX : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX2
  Job-TQCFWV : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                    | Runtime              | DataProvider | Mean       | Allocated |
|-------------------------- |--------------------- |------------- |-----------:|----------:|
| **VwSalesByYear**             | **.NET 6.0**             | **Access**       |   **258.3 μs** |  **61.23 KB** |
| VwSalesByYear             | .NET 8.0             | Access       |   149.7 μs |  46.37 KB |
| VwSalesByYear             | .NET Framework 4.6.2 | Access       |   444.8 μs |  82.13 KB |
|                           |                      |              |            |           |
| VwSalesByYearMutation     | .NET 6.0             | Access       |   433.0 μs |   80.1 KB |
| VwSalesByYearMutation     | .NET 8.0             | Access       |   292.9 μs |  65.38 KB |
| VwSalesByYearMutation     | .NET Framework 4.6.2 | Access       |   670.8 μs | 106.03 KB |
|                           |                      |              |            |           |
| VwSalesByCategoryContains | .NET 6.0             | Access       | 1,411.0 μs | 147.14 KB |
| VwSalesByCategoryContains | .NET 8.0             | Access       |   984.1 μs | 125.89 KB |
| VwSalesByCategoryContains | .NET Framework 4.6.2 | Access       | 1,938.9 μs | 179.08 KB |
|                           |                      |              |            |           |
| **VwSalesByYear**             | **.NET 6.0**             | **Firebird**     |   **261.9 μs** |   **60.7 KB** |
| VwSalesByYear             | .NET 8.0             | Firebird     |   156.4 μs |  45.85 KB |
| VwSalesByYear             | .NET Framework 4.6.2 | Firebird     |   451.5 μs |  81.57 KB |
|                           |                      |              |            |           |
| VwSalesByYearMutation     | .NET 6.0             | Firebird     |   443.1 μs |  81.18 KB |
| VwSalesByYearMutation     | .NET 8.0             | Firebird     |   307.0 μs |  67.21 KB |
| VwSalesByYearMutation     | .NET Framework 4.6.2 | Firebird     |   688.9 μs | 107.04 KB |
|                           |                      |              |            |           |
| VwSalesByCategoryContains | .NET 6.0             | Firebird     |   726.8 μs | 134.01 KB |
| VwSalesByCategoryContains | .NET 8.0             | Firebird     |   487.4 μs | 112.37 KB |
| VwSalesByCategoryContains | .NET Framework 4.6.2 | Firebird     | 1,104.5 μs | 168.98 KB |
