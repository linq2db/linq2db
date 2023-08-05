``` ini

BenchmarkDotNet=v0.13.5, OS=Windows 10 (10.0.17763.4010/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.201
  [Host]     : .NET 7.0.3 (7.0.323.6910), X64 RyuJIT AVX2
  Job-ZOLDKB : .NET 6.0.14 (6.0.1423.7309), X64 RyuJIT AVX2
  Job-EHWHZK : .NET 7.0.3 (7.0.323.6910), X64 RyuJIT AVX2
  Job-LWJRKG : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-AGOWOF : .NET Framework 4.8 (4.8.4614.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                    Method |              Runtime | DataProvider |       Mean | Allocated |
|-------------------------- |--------------------- |------------- |-----------:|----------:|
|             **VwSalesByYear** |             **.NET 6.0** |       **Access** |   **414.2 μs** |  **70.03 KB** |
|             VwSalesByYear |             .NET 7.0 |       Access |   301.7 μs |  49.59 KB |
|             VwSalesByYear |        .NET Core 3.1 |       Access |   590.0 μs |  72.34 KB |
|             VwSalesByYear | .NET Framework 4.7.2 |       Access |   662.2 μs |  92.94 KB |
|                           |                      |              |            |           |
|     VwSalesByYearMutation |             .NET 6.0 |       Access |   674.2 μs | 109.53 KB |
|     VwSalesByYearMutation |             .NET 7.0 |       Access |   586.2 μs |  86.65 KB |
|     VwSalesByYearMutation |        .NET Core 3.1 |       Access |   897.8 μs | 114.11 KB |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |       Access | 1,034.2 μs | 141.87 KB |
|                           |                      |              |            |           |
| VwSalesByCategoryContains |             .NET 6.0 |       Access | 1,383.8 μs | 213.76 KB |
| VwSalesByCategoryContains |             .NET 7.0 |       Access | 1,239.6 μs | 184.28 KB |
| VwSalesByCategoryContains |        .NET Core 3.1 |       Access | 1,876.4 μs | 218.12 KB |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |       Access | 1,997.8 μs |  247.8 KB |
|                           |                      |              |            |           |
|             **VwSalesByYear** |             **.NET 6.0** |     **Firebird** |   **193.9 μs** |  **70.28 KB** |
|             VwSalesByYear |             .NET 7.0 |     Firebird |   242.2 μs |  49.66 KB |
|             VwSalesByYear |        .NET Core 3.1 |     Firebird |   592.5 μs |  72.59 KB |
|             VwSalesByYear | .NET Framework 4.7.2 |     Firebird |   683.2 μs |  95.11 KB |
|                           |                      |              |            |           |
|     VwSalesByYearMutation |             .NET 6.0 |     Firebird |   604.1 μs | 112.06 KB |
|     VwSalesByYearMutation |             .NET 7.0 |     Firebird |   275.7 μs |  89.45 KB |
|     VwSalesByYearMutation |        .NET Core 3.1 |     Firebird |   944.3 μs | 116.63 KB |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |     Firebird | 1,052.3 μs | 144.81 KB |
|                           |                      |              |            |           |
| VwSalesByCategoryContains |             .NET 6.0 |     Firebird | 1,024.4 μs | 152.89 KB |
| VwSalesByCategoryContains |             .NET 7.0 |     Firebird |   901.1 μs | 123.35 KB |
| VwSalesByCategoryContains |        .NET Core 3.1 |     Firebird | 1,235.5 μs | 157.21 KB |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |     Firebird | 1,402.8 μs | 187.78 KB |
