``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 11 (10.0.22621.1105)
AMD Ryzen 9 3950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.101
  [Host]     : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
  Job-TGQZFS : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-TAOVSO : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
  Job-XAOGGQ : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-JOXFDN : .NET Framework 4.8.1 (4.8.9105.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                    Method |              Runtime | DataProvider |       Mean | Ratio |    Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-------------------------- |--------------------- |------------- |-----------:|------:|--------:|-------:|----------:|------------:|
| **VwSalesByCategoryContains** |             **.NET 6.0** |       **Access** | **1,173.1 μs** |  **0.64** | **33.2031** | **1.9531** | **280.66 KB** |        **0.77** |
| VwSalesByCategoryContains |             .NET 7.0 |       Access |   975.3 μs |  0.54 | 31.2500 | 1.9531 | 255.97 KB |        0.71 |
| VwSalesByCategoryContains |        .NET Core 3.1 |       Access | 1,438.1 μs |  0.80 | 35.1563 | 1.9531 | 288.33 KB |        0.80 |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |       Access | 1,833.2 μs |  1.00 | 58.5938 | 3.9063 | 362.37 KB |        1.00 |
|                           |                      |              |            |       |         |        |           |             |
| **VwSalesByCategoryContains** |             **.NET 6.0** |     **Firebird** | **1,078.9 μs** |  **0.63** | **31.2500** | **1.9531** | **257.02 KB** |        **0.76** |
| VwSalesByCategoryContains |             .NET 7.0 |     Firebird |   901.8 μs |  0.52 | 27.3438 | 1.9531 | 234.46 KB |        0.69 |
| VwSalesByCategoryContains |        .NET Core 3.1 |     Firebird | 1,390.9 μs |  0.82 | 31.2500 | 1.9531 | 266.93 KB |        0.79 |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |     Firebird | 1,724.2 μs |  1.00 | 54.6875 | 1.9531 | 339.55 KB |        1.00 |
