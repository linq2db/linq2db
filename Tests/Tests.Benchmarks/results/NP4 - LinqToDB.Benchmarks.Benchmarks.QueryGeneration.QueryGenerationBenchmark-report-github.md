``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 11 (10.0.22621.1105)
AMD Ryzen 9 3950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.101
  [Host]     : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
  Job-JUUKFI : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-INTAJY : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
  Job-XHXLCE : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-GAJRPG : .NET Framework 4.8.1 (4.8.9105.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                    Method |              Runtime | DataProvider |       Mean | Ratio |    Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-------------------------- |--------------------- |------------- |-----------:|------:|--------:|-------:|----------:|------------:|
| **VwSalesByCategoryContains** |             **.NET 6.0** |       **Access** | **1,247.2 μs** |  **0.64** | **37.1094** | **1.9531** | **303.19 KB** |        **0.78** |
| VwSalesByCategoryContains |             .NET 7.0 |       Access | 1,041.6 μs |  0.54 | 33.2031 | 1.9531 | 280.74 KB |        0.72 |
| VwSalesByCategoryContains |        .NET Core 3.1 |       Access | 1,611.6 μs |  0.84 | 37.1094 | 1.9531 | 312.37 KB |        0.80 |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |       Access | 1,940.8 μs |  1.00 | 62.5000 | 3.9063 | 389.68 KB |        1.00 |
|                           |                      |              |            |       |         |        |           |             |
| **VwSalesByCategoryContains** |             **.NET 6.0** |     **Firebird** | **1,123.0 μs** |  **0.62** | **33.2031** | **1.9531** | **278.04 KB** |        **0.77** |
| VwSalesByCategoryContains |             .NET 7.0 |     Firebird |   963.7 μs |  0.53 | 29.2969 | 1.9531 | 254.07 KB |        0.70 |
| VwSalesByCategoryContains |        .NET Core 3.1 |     Firebird | 1,434.7 μs |  0.80 | 35.1563 | 1.9531 | 287.49 KB |        0.79 |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |     Firebird | 1,831.6 μs |  1.00 | 58.5938 | 3.9063 | 362.21 KB |        1.00 |
