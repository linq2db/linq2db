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
| Method                    | Runtime              | DataProvider | Mean       | Allocated |
|-------------------------- |--------------------- |------------- |-----------:|----------:|
| **VwSalesByYear**             | **.NET 8.0**             | **Access**       |   **258.7 μs** |  **49.47 KB** |
| VwSalesByYear             | .NET 9.0             | Access       |   105.8 μs |  50.39 KB |
| VwSalesByYear             | .NET Framework 4.6.2 | Access       |   658.6 μs |  95.35 KB |
|                           |                      |              |            |           |
| VwSalesByYearMutation     | .NET 8.0             | Access       |   445.9 μs |  88.27 KB |
| VwSalesByYearMutation     | .NET 9.0             | Access       |   272.0 μs |  87.13 KB |
| VwSalesByYearMutation     | .NET Framework 4.6.2 | Access       |   980.4 μs | 134.58 KB |
|                           |                      |              |            |           |
| VwSalesByCategoryContains | .NET 8.0             | Access       |   945.5 μs | 183.79 KB |
| VwSalesByCategoryContains | .NET 9.0             | Access       |   785.3 μs | 182.56 KB |
| VwSalesByCategoryContains | .NET Framework 4.6.2 | Access       | 1,736.2 μs | 249.24 KB |
|                           |                      |              |            |           |
| **VwSalesByYear**             | **.NET 8.0**             | **Firebird**     |   **173.3 μs** |  **50.27 KB** |
| VwSalesByYear             | .NET 9.0             | Firebird     |   189.9 μs |     51 KB |
| VwSalesByYear             | .NET Framework 4.6.2 | Firebird     |   392.0 μs |  95.62 KB |
|                           |                      |              |            |           |
| VwSalesByYearMutation     | .NET 8.0             | Firebird     |   472.9 μs |  90.06 KB |
| VwSalesByYearMutation     | .NET 9.0             | Firebird     |   209.9 μs |  89.88 KB |
| VwSalesByYearMutation     | .NET Framework 4.6.2 | Firebird     | 1,025.3 μs | 137.31 KB |
|                           |                      |              |            |           |
| VwSalesByCategoryContains | .NET 8.0             | Firebird     |   705.2 μs | 123.64 KB |
| VwSalesByCategoryContains | .NET 9.0             | Firebird     |   558.3 μs |  121.9 KB |
| VwSalesByCategoryContains | .NET Framework 4.6.2 | Firebird     |   656.5 μs | 187.74 KB |
