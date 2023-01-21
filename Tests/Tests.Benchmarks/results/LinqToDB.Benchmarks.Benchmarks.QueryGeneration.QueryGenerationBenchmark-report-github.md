``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-RNZPMW : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XCCWXF : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WSMVMG : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-FMTKFQ : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                    Method |              Runtime |       CurrentOptions |       Mean |     Median | Ratio |    Gen0 | Allocated | Alloc Ratio |
|-------------------------- |--------------------- |--------------------- |-----------:|-----------:|------:|--------:|----------:|------------:|
|             VwSalesByYear |             .NET 6.0 | LinqToDB.DataOptions |   548.6 μs |   591.6 μs |  0.66 |  3.9063 |  71.82 KB |        0.79 |
|             VwSalesByYear |             .NET 6.0 | LinqToDB.DataOptions |   543.4 μs |   579.4 μs |  0.65 |  3.9063 |  72.07 KB |        0.79 |
|             VwSalesByYear |             .NET 7.0 | LinqToDB.DataOptions |   376.6 μs |   404.1 μs |  0.45 |  2.9297 |  48.43 KB |        0.53 |
|             VwSalesByYear |             .NET 7.0 | LinqToDB.DataOptions |   410.3 μs |   423.5 μs |  0.49 |  2.9297 |  49.73 KB |        0.55 |
|             VwSalesByYear |        .NET Core 3.1 | LinqToDB.DataOptions |   735.2 μs |   753.0 μs |  0.88 |  3.9063 |  72.26 KB |        0.79 |
|             VwSalesByYear |        .NET Core 3.1 | LinqToDB.DataOptions |   729.3 μs |   789.5 μs |  0.87 |  3.9063 |  72.01 KB |        0.79 |
|             VwSalesByYear | .NET Framework 4.7.2 | LinqToDB.DataOptions |   876.2 μs |   822.7 μs |  1.00 | 13.6719 |  91.03 KB |        1.00 |
|             VwSalesByYear | .NET Framework 4.7.2 | LinqToDB.DataOptions |   895.0 μs |   916.7 μs |  1.07 | 13.6719 |  90.75 KB |        1.00 |
|                           |                      |                      |            |            |       |         |           |             |
|     VwSalesByYearMutation |             .NET 6.0 | LinqToDB.DataOptions |   970.3 μs | 1,021.0 μs |  0.75 |  5.8594 | 112.67 KB |        0.83 |
|     VwSalesByYearMutation |             .NET 6.0 | LinqToDB.DataOptions |   862.7 μs |   919.9 μs |  0.66 |  5.8594 | 110.14 KB |        0.82 |
|     VwSalesByYearMutation |             .NET 7.0 | LinqToDB.DataOptions |   697.6 μs |   744.1 μs |  0.53 |  3.9063 |  85.41 KB |        0.63 |
|     VwSalesByYearMutation |             .NET 7.0 | LinqToDB.DataOptions |   756.8 μs |   809.6 μs |  0.58 |  4.8828 |  88.88 KB |        0.66 |
|     VwSalesByYearMutation |        .NET Core 3.1 | LinqToDB.DataOptions | 1,193.2 μs | 1,257.5 μs |  0.92 |  5.8594 | 111.29 KB |        0.82 |
|     VwSalesByYearMutation |        .NET Core 3.1 | LinqToDB.DataOptions | 1,242.8 μs | 1,331.3 μs |  0.96 |  5.8594 | 113.81 KB |        0.84 |
|     VwSalesByYearMutation | .NET Framework 4.7.2 | LinqToDB.DataOptions | 1,360.4 μs | 1,409.6 μs |  1.00 | 21.4844 | 135.02 KB |        1.00 |
|     VwSalesByYearMutation | .NET Framework 4.7.2 | LinqToDB.DataOptions | 1,268.8 μs | 1,345.8 μs |  0.97 | 21.4844 | 132.08 KB |        0.98 |
|                           |                      |                      |            |            |       |         |           |             |
| VwSalesByCategoryContains |             .NET 6.0 | LinqToDB.DataOptions | 1,291.7 μs | 1,303.4 μs |  0.60 |  7.8125 | 152.64 KB |        0.62 |
| VwSalesByCategoryContains |             .NET 6.0 | LinqToDB.DataOptions | 1,786.4 μs | 1,876.4 μs |  0.83 | 11.7188 | 214.54 KB |        0.87 |
| VwSalesByCategoryContains |             .NET 7.0 | LinqToDB.DataOptions | 1,628.3 μs | 1,767.7 μs |  0.76 |  7.8125 |    184 KB |        0.75 |
| VwSalesByCategoryContains |             .NET 7.0 | LinqToDB.DataOptions | 1,020.7 μs |   990.1 μs |  0.47 |  3.9063 |  121.8 KB |        0.49 |
| VwSalesByCategoryContains |        .NET Core 3.1 | LinqToDB.DataOptions | 1,793.3 μs | 1,931.0 μs |  0.84 |  7.8125 | 155.76 KB |        0.63 |
| VwSalesByCategoryContains |        .NET Core 3.1 | LinqToDB.DataOptions | 2,476.5 μs | 2,656.1 μs |  1.16 | 11.7188 | 216.65 KB |        0.88 |
| VwSalesByCategoryContains | .NET Framework 4.7.2 | LinqToDB.DataOptions | 2,289.7 μs | 2,410.2 μs |  1.00 | 39.0625 | 246.81 KB |        1.00 |
| VwSalesByCategoryContains | .NET Framework 4.7.2 | LinqToDB.DataOptions | 1,932.4 μs | 2,049.1 μs |  0.88 | 29.2969 | 184.97 KB |        0.75 |
