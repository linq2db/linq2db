``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-TEPEZT : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-ISYUTK : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-SMHCKK : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-DHDWVI : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                    Method |              Runtime |       CurrentOptions |       Mean |     Median | Ratio |    Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-------------------------- |--------------------- |--------------------- |-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
|             VwSalesByYear |             .NET 6.0 | LinqToDB.DataOptions |   385.3 μs |   403.3 μs |  0.58 |  3.9063 |      - |  70.05 KB |        0.77 |
|             VwSalesByYear |             .NET 6.0 | LinqToDB.DataOptions |   400.9 μs |   404.6 μs |  0.62 |  3.9063 |      - |   70.3 KB |        0.77 |
|             VwSalesByYear |             .NET 7.0 | LinqToDB.DataOptions |   259.6 μs |   315.0 μs |  0.40 |  2.9297 |      - |  48.99 KB |        0.54 |
|             VwSalesByYear |             .NET 7.0 | LinqToDB.DataOptions |   318.8 μs |   318.2 μs |  0.49 |  2.9297 |      - |  48.46 KB |        0.53 |
|             VwSalesByYear |        .NET Core 3.1 | LinqToDB.DataOptions |   547.5 μs |   547.7 μs |  0.84 |  3.9063 |      - |   72.3 KB |        0.80 |
|             VwSalesByYear |        .NET Core 3.1 | LinqToDB.DataOptions |   549.0 μs |   554.1 μs |  0.85 |  3.9063 |      - |  72.05 KB |        0.79 |
|             VwSalesByYear | .NET Framework 4.7.2 | LinqToDB.DataOptions |   654.9 μs |   656.7 μs |  1.00 | 14.6484 |      - |  90.87 KB |        1.00 |
|             VwSalesByYear | .NET Framework 4.7.2 | LinqToDB.DataOptions |   660.2 μs |   661.2 μs |  1.01 | 14.6484 |      - |  90.96 KB |        1.00 |
|                           |                      |                      |            |            |       |         |        |           |             |
|     VwSalesByYearMutation |             .NET 6.0 | LinqToDB.DataOptions |   696.6 μs |   696.1 μs |  0.66 |  6.8359 |      - | 113.68 KB |        0.77 |
|     VwSalesByYearMutation |             .NET 6.0 | LinqToDB.DataOptions |   670.5 μs |   681.4 μs |  0.64 |  5.8594 |      - | 111.16 KB |        0.75 |
|     VwSalesByYearMutation |             .NET 7.0 | LinqToDB.DataOptions |   578.9 μs |   580.6 μs |  0.55 |  4.8828 |      - |  86.43 KB |        0.58 |
|     VwSalesByYearMutation |             .NET 7.0 | LinqToDB.DataOptions |   585.2 μs |   591.6 μs |  0.54 |  4.8828 |      - |  87.87 KB |        0.59 |
|     VwSalesByYearMutation |        .NET Core 3.1 | LinqToDB.DataOptions |   903.8 μs |   902.0 μs |  0.86 |  6.8359 |      - | 111.78 KB |        0.75 |
|     VwSalesByYearMutation |        .NET Core 3.1 | LinqToDB.DataOptions |   926.2 μs |   929.1 μs |  0.88 |  5.8594 |      - |  114.3 KB |        0.77 |
|     VwSalesByYearMutation | .NET Framework 4.7.2 | LinqToDB.DataOptions | 1,051.8 μs | 1,052.3 μs |  1.00 | 23.4375 |      - | 148.35 KB |        1.00 |
|     VwSalesByYearMutation | .NET Framework 4.7.2 | LinqToDB.DataOptions | 1,024.9 μs | 1,015.4 μs |  0.98 | 23.4375 |      - | 144.68 KB |        0.98 |
|                           |                      |                      |            |            |       |         |        |           |             |
| VwSalesByCategoryContains |             .NET 6.0 | LinqToDB.DataOptions | 1,024.3 μs | 1,024.5 μs |  0.60 |  7.8125 |      - | 151.39 KB |        0.58 |
| VwSalesByCategoryContains |             .NET 6.0 | LinqToDB.DataOptions | 1,363.3 μs | 1,368.0 μs |  0.80 | 11.7188 |      - | 212.26 KB |        0.82 |
| VwSalesByCategoryContains |             .NET 7.0 | LinqToDB.DataOptions | 1,246.8 μs | 1,248.0 μs |  0.73 |  9.7656 |      - | 183.98 KB |        0.71 |
| VwSalesByCategoryContains |             .NET 7.0 | LinqToDB.DataOptions |   901.4 μs |   903.6 μs |  0.51 |  5.8594 |      - | 123.06 KB |        0.47 |
| VwSalesByCategoryContains |        .NET Core 3.1 | LinqToDB.DataOptions | 1,297.8 μs | 1,365.0 μs |  0.82 |  7.8125 |      - | 158.39 KB |        0.61 |
| VwSalesByCategoryContains |        .NET Core 3.1 | LinqToDB.DataOptions | 1,835.4 μs | 1,834.4 μs |  1.07 | 11.7188 |      - | 219.29 KB |        0.84 |
| VwSalesByCategoryContains | .NET Framework 4.7.2 | LinqToDB.DataOptions | 1,755.3 μs | 2,006.2 μs |  1.00 | 41.0156 | 1.9531 |  259.9 KB |        1.00 |
| VwSalesByCategoryContains | .NET Framework 4.7.2 | LinqToDB.DataOptions | 1,512.8 μs | 1,512.7 μs |  0.89 | 31.2500 |      - | 193.82 KB |        0.75 |
