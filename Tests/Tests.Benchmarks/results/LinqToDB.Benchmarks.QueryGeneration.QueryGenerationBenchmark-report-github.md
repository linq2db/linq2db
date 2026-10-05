```

BenchmarkDotNet v0.15.2, Windows 10 (10.0.17763.7553/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X 3.39GHz, 2 CPU, 32 logical and 16 physical cores
.NET SDK 9.0.302
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-FTOCRB : .NET 8.0.18 (8.0.1825.31117), X64 RyuJIT AVX2
  Job-DHTNJT : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-QIENBV : .NET Framework 4.8 (4.8.4795.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                    | Runtime              | DataProvider | Mean      | Allocated |
|-------------------------- |--------------------- |------------- |----------:|----------:|
| **VwSalesByYear**             | **.NET 8.0**             | **Access**       |  **72.25 μs** |  **36.59 KB** |
| VwSalesByYear             | .NET 9.0             | Access       |  37.61 μs |  36.17 KB |
| VwSalesByYear             | .NET Framework 4.6.2 | Access       | 264.53 μs |  64.01 KB |
|                           |                      |              |           |           |
| VwSalesByYearMutation     | .NET 8.0             | Access       |  73.44 μs |  37.96 KB |
| VwSalesByYearMutation     | .NET 9.0             | Access       |  39.76 μs |  39.18 KB |
| VwSalesByYearMutation     | .NET Framework 4.6.2 | Access       | 202.24 μs |  68.17 KB |
|                           |                      |              |           |           |
| VwSalesByCategoryContains | .NET 8.0             | Access       | 145.58 μs |  58.88 KB |
| VwSalesByCategoryContains | .NET 9.0             | Access       | 131.68 μs |  59.67 KB |
| VwSalesByCategoryContains | .NET Framework 4.6.2 | Access       | 416.17 μs | 101.79 KB |
|                           |                      |              |           |           |
| **VwSalesByYear**             | **.NET 8.0**             | **Firebird**     |  **39.89 μs** |     **36 KB** |
| VwSalesByYear             | .NET 9.0             | Firebird     |  36.75 μs |   36.1 KB |
| VwSalesByYear             | .NET Framework 4.6.2 | Firebird     | 220.86 μs |  63.99 KB |
|                           |                      |              |           |           |
| VwSalesByYearMutation     | .NET 8.0             | Firebird     |  93.18 μs |  38.47 KB |
| VwSalesByYearMutation     | .NET 9.0             | Firebird     |  85.33 μs |  38.61 KB |
| VwSalesByYearMutation     | .NET Framework 4.6.2 | Firebird     | 205.81 μs |  68.17 KB |
|                           |                      |              |           |           |
| VwSalesByCategoryContains | .NET 8.0             | Firebird     | 113.66 μs |  58.28 KB |
| VwSalesByCategoryContains | .NET 9.0             | Firebird     | 133.99 μs |  59.19 KB |
| VwSalesByCategoryContains | .NET Framework 4.6.2 | Firebird     | 417.86 μs | 101.79 KB |
