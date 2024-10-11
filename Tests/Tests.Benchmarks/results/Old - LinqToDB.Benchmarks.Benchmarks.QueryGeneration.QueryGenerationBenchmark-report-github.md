``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 11 (10.0.22621.1105)
AMD Ryzen 9 3950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.101
  [Host]     : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
  Job-GEWJLR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-JLZMGV : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
  Job-GKOERE : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-VIHTOX : .NET Framework 4.8.1 (4.8.9105.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                    Method |              Runtime | DataProvider |       Mean | Ratio |    Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-------------------------- |--------------------- |------------- |-----------:|------:|--------:|-------:|----------:|------------:|
| **VwSalesByCategoryContains** |             **.NET 6.0** |       **Access** | **1,014.4 μs** |  **0.64** | **33.2031** | **3.9063** | **272.45 KB** |        **0.79** |
| VwSalesByCategoryContains |             .NET 7.0 |       Access |   873.5 μs |  0.55 | 29.2969 | 3.9063 | 251.51 KB |        0.73 |
| VwSalesByCategoryContains |        .NET Core 3.1 |       Access | 1,323.1 μs |  0.84 | 33.2031 | 1.9531 | 278.15 KB |        0.80 |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |       Access | 1,581.7 μs |  1.00 | 54.6875 | 3.9063 |  346.8 KB |        1.00 |
|                           |                      |              |            |       |         |        |           |             |
| **VwSalesByCategoryContains** |             **.NET 6.0** |     **Firebird** |   **761.7 μs** |  **0.59** | **23.4375** |      **-** | **201.35 KB** |        **0.73** |
| VwSalesByCategoryContains |             .NET 7.0 |     Firebird |   618.3 μs |  0.48 | 21.4844 | 0.9766 | 180.25 KB |        0.66 |
| VwSalesByCategoryContains |        .NET Core 3.1 |     Firebird | 1,029.3 μs |  0.81 | 23.4375 |      - | 206.13 KB |        0.75 |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |     Firebird | 1,280.6 μs |  1.00 | 42.9688 | 1.9531 | 274.09 KB |        1.00 |
