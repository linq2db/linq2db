``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 11 (10.0.22621.1105)
AMD Ryzen 9 3950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.101
  [Host]     : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
  Job-UGRNSS : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-WKCRQP : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
  Job-FUIKMJ : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-HSLRZG : .NET Framework 4.8.1 (4.8.9105.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                    Method |              Runtime | DataProvider |       Mean | Ratio |    Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-------------------------- |--------------------- |------------- |-----------:|------:|--------:|-------:|----------:|------------:|
| **VwSalesByCategoryContains** |             **.NET 6.0** |       **Access** | **1,150.3 μs** |  **0.64** | **33.2031** | **1.9531** | **279.63 KB** |        **0.77** |
| VwSalesByCategoryContains |             .NET 7.0 |       Access |   952.3 μs |  0.53 | 31.2500 | 1.9531 |    256 KB |        0.71 |
| VwSalesByCategoryContains |        .NET Core 3.1 |       Access | 1,452.5 μs |  0.81 | 35.1563 | 1.9531 | 288.32 KB |        0.80 |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |       Access | 1,789.1 μs |  1.00 | 58.5938 | 3.9063 | 362.36 KB |        1.00 |
|                           |                      |              |            |       |         |        |           |             |
| **VwSalesByCategoryContains** |             **.NET 6.0** |     **Firebird** | **1,066.4 μs** |  **0.62** | **31.2500** | **1.9531** | **257.02 KB** |        **0.76** |
| VwSalesByCategoryContains |             .NET 7.0 |     Firebird |   913.9 μs |  0.53 | 27.3438 | 1.9531 | 233.09 KB |        0.69 |
| VwSalesByCategoryContains |        .NET Core 3.1 |     Firebird | 1,397.5 μs |  0.81 | 31.2500 | 1.9531 | 266.94 KB |        0.79 |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |     Firebird | 1,723.0 μs |  1.00 | 54.6875 | 1.9531 | 339.55 KB |        1.00 |
