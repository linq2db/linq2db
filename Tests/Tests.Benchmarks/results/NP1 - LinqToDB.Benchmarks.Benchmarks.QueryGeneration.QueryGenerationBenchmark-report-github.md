``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 11 (10.0.22621.1105)
AMD Ryzen 9 3950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.101
  [Host]     : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
  Job-NRUCIF : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-PCVVTY : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
  Job-XEHSNQ : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-YAVYLP : .NET Framework 4.8.1 (4.8.9105.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                    Method |              Runtime | DataProvider |       Mean | Ratio |    Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-------------------------- |--------------------- |------------- |-----------:|------:|--------:|-------:|----------:|------------:|
| **VwSalesByCategoryContains** |             **.NET 6.0** |       **Access** | **1,130.8 μs** |  **0.64** | **33.2031** | **1.9531** | **279.63 KB** |        **0.77** |
| VwSalesByCategoryContains |             .NET 7.0 |       Access |   938.6 μs |  0.53 | 31.2500 | 1.9531 | 256.09 KB |        0.71 |
| VwSalesByCategoryContains |        .NET Core 3.1 |       Access | 1,447.7 μs |  0.82 | 35.1563 | 1.9531 | 288.32 KB |        0.79 |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |       Access | 1,762.6 μs |  1.00 | 58.5938 | 3.9063 | 362.67 KB |        1.00 |
|                           |                      |              |            |       |         |        |           |             |
| **VwSalesByCategoryContains** |             **.NET 6.0** |     **Firebird** | **1,034.1 μs** |  **0.61** | **31.2500** | **1.9531** | **257.02 KB** |        **0.76** |
| VwSalesByCategoryContains |             .NET 7.0 |     Firebird |   889.8 μs |  0.53 | 27.3438 | 1.9531 | 233.49 KB |        0.69 |
| VwSalesByCategoryContains |        .NET Core 3.1 |     Firebird | 1,388.3 μs |  0.82 | 31.2500 | 1.9531 | 266.91 KB |        0.79 |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |     Firebird | 1,693.1 μs |  1.00 | 54.6875 | 1.9531 | 339.57 KB |        1.00 |
