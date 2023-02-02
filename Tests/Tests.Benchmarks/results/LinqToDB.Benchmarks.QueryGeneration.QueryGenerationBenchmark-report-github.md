``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-HCNGBR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XBFFOD : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-INBZNN : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-THZJXI : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                    Method |              Runtime | DataProvider |       Mean | Allocated |
|-------------------------- |--------------------- |------------- |-----------:|----------:|
|             **VwSalesByYear** |             **.NET 6.0** |       **Access** |   **411.9 μs** |  **70.53 KB** |
|             VwSalesByYear |             .NET 7.0 |       Access |   302.9 μs |  49.23 KB |
|             VwSalesByYear |        .NET Core 3.1 |       Access |   593.6 μs |  73.34 KB |
|             VwSalesByYear | .NET Framework 4.7.2 |       Access |   657.4 μs |  92.66 KB |
|                           |                      |              |            |           |
|     VwSalesByYearMutation |             .NET 6.0 |       Access |   673.8 μs | 110.56 KB |
|     VwSalesByYearMutation |             .NET 7.0 |       Access |   584.7 μs |  86.54 KB |
|     VwSalesByYearMutation |        .NET Core 3.1 |       Access |   901.9 μs | 111.78 KB |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |       Access | 1,019.7 μs | 134.09 KB |
|                           |                      |              |            |           |
| VwSalesByCategoryContains |             .NET 6.0 |       Access | 1,022.4 μs | 214.62 KB |
| VwSalesByCategoryContains |             .NET 7.0 |       Access | 1,125.9 μs | 183.84 KB |
| VwSalesByCategoryContains |        .NET Core 3.1 |       Access | 1,849.7 μs | 218.86 KB |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |       Access | 1,629.3 μs | 247.74 KB |
|                           |                      |              |            |           |
|             **VwSalesByYear** |             **.NET 6.0** |     **Firebird** |   **357.5 μs** |  **70.78 KB** |
|             VwSalesByYear |             .NET 7.0 |     Firebird |   327.9 μs |  50.18 KB |
|             VwSalesByYear |        .NET Core 3.1 |     Firebird |   558.3 μs |  73.59 KB |
|             VwSalesByYear | .NET Framework 4.7.2 |     Firebird |   529.2 μs |   92.9 KB |
|                           |                      |              |            |           |
|     VwSalesByYearMutation |             .NET 6.0 |     Firebird |   699.3 μs | 113.09 KB |
|     VwSalesByYearMutation |             .NET 7.0 |     Firebird |   562.9 μs |  89.75 KB |
|     VwSalesByYearMutation |        .NET Core 3.1 |     Firebird |   946.5 μs |  114.3 KB |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |     Firebird | 1,053.5 μs | 136.63 KB |
|                           |                      |              |            |           |
| VwSalesByCategoryContains |             .NET 6.0 |     Firebird | 1,030.2 μs | 153.75 KB |
| VwSalesByCategoryContains |             .NET 7.0 |     Firebird |   805.5 μs | 122.03 KB |
| VwSalesByCategoryContains |        .NET Core 3.1 |     Firebird | 1,373.3 μs | 157.96 KB |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |     Firebird | 1,210.2 μs |  185.9 KB |
