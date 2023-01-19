``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-UZBSVL : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-AYZXIO : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-NXXYQT : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-HMCTKM : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|    Method |              Runtime |       Mean |     Median | Ratio |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|---------- |--------------------- |-----------:|-----------:|------:|-------:|-------:|----------:|------------:|
|      Linq |             .NET 6.0 |  76.847 μs |  76.862 μs | 31.43 | 2.5635 | 0.3662 |  42.51 KB |       27.53 |
|  Compiled |             .NET 6.0 |  11.371 μs |  11.416 μs |  4.63 | 1.3885 | 0.2747 |  22.73 KB |       14.72 |
| RawAdoNet |             .NET 6.0 |   1.532 μs |   1.536 μs |  0.62 | 0.0896 | 0.0439 |   1.48 KB |        0.96 |
|      Linq |             .NET 7.0 |  49.136 μs |  49.227 μs | 20.10 | 2.1362 | 0.3052 |   35.8 KB |       23.19 |
|  Compiled |             .NET 7.0 |   9.925 μs |   9.698 μs |  3.78 | 1.3885 | 0.2747 |  22.72 KB |       14.71 |
| RawAdoNet |             .NET 7.0 |   1.372 μs |   1.366 μs |  0.54 | 0.0877 | 0.0839 |   1.48 KB |        0.96 |
|      Linq |        .NET Core 3.1 | 100.216 μs |  99.552 μs | 40.83 | 2.6855 | 0.2441 |  45.14 KB |       29.24 |
|  Compiled |        .NET Core 3.1 |  12.396 μs |  15.626 μs |  4.71 | 1.5488 | 0.3052 |  25.35 KB |       16.42 |
| RawAdoNet |        .NET Core 3.1 |   1.754 μs |   1.747 μs |  0.71 | 0.0896 | 0.0439 |   1.48 KB |        0.96 |
|      Linq | .NET Framework 4.7.2 | 136.312 μs | 136.650 μs | 55.55 | 7.0801 | 0.7324 |  44.05 KB |       28.53 |
|  Compiled | .NET Framework 4.7.2 |  16.463 μs |  16.553 μs |  6.61 | 3.8147 | 0.4272 |  23.62 KB |       15.30 |
| RawAdoNet | .NET Framework 4.7.2 |   2.653 μs |   2.639 μs |  1.00 | 0.2480 | 0.1221 |   1.54 KB |        1.00 |
