```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.17763.5458/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.200
  [Host]     : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2
  Job-GXDOCB : .NET 6.0.27 (6.0.2724.6912), X64 RyuJIT AVX2
  Job-YDFVLV : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2
  Job-SBTNYY : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                    | Runtime              | DataProvider | Mean       | Allocated |
|-------------------------- |--------------------- |------------- |-----------:|----------:|
| **VwSalesByYear**             | **.NET 6.0**             | **Access**       |   **259.9 μs** |  **60.12 KB** |
| VwSalesByYear             | .NET 8.0             | Access       |   161.6 μs |  46.34 KB |
| VwSalesByYear             | .NET Framework 4.6.2 | Access       |   456.7 μs |  82.53 KB |
|                           |                      |              |            |           |
| VwSalesByYearMutation     | .NET 6.0             | Access       |   317.9 μs |  83.47 KB |
| VwSalesByYearMutation     | .NET 8.0             | Access       |   261.8 μs |  67.64 KB |
| VwSalesByYearMutation     | .NET Framework 4.6.2 | Access       |   613.6 μs | 107.88 KB |
|                           |                      |              |            |           |
| VwSalesByCategoryContains | .NET 6.0             | Access       |   796.6 μs | 133.66 KB |
| VwSalesByCategoryContains | .NET 8.0             | Access       |   568.8 μs |  113.6 KB |
| VwSalesByCategoryContains | .NET Framework 4.6.2 | Access       | 1,172.4 μs | 163.98 KB |
|                           |                      |              |            |           |
| **VwSalesByYear**             | **.NET 6.0**             | **Firebird**     |   **255.5 μs** |   **60.3 KB** |
| VwSalesByYear             | .NET 8.0             | Firebird     |   143.3 μs |  45.72 KB |
| VwSalesByYear             | .NET Framework 4.6.2 | Firebird     |   461.2 μs |  83.51 KB |
|                           |                      |              |            |           |
| VwSalesByYearMutation     | .NET 6.0             | Firebird     |   412.0 μs |  86.17 KB |
| VwSalesByYearMutation     | .NET 8.0             | Firebird     |   300.5 μs |  71.33 KB |
| VwSalesByYearMutation     | .NET Framework 4.6.2 | Firebird     |   686.8 μs | 110.61 KB |
|                           |                      |              |            |           |
| VwSalesByCategoryContains | .NET 6.0             | Firebird     |   669.7 μs | 130.28 KB |
| VwSalesByCategoryContains | .NET 8.0             | Firebird     |   472.3 μs | 109.53 KB |
| VwSalesByCategoryContains | .NET Framework 4.6.2 | Firebird     |   963.2 μs | 160.31 KB |
