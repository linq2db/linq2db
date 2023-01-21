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
|    Method |              Runtime |       Mean |     Median | Ratio |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|---------- |--------------------- |-----------:|-----------:|------:|-------:|-------:|----------:|------------:|
|      Linq |             .NET 6.0 |  59.693 μs |  62.262 μs | 17.67 | 0.4883 | 0.1221 |   9.41 KB |        6.09 |
|  Compiled |             .NET 6.0 |   9.358 μs |   9.681 μs |  2.78 | 0.1678 | 0.0763 |   2.98 KB |        1.93 |
| RawAdoNet |             .NET 6.0 |   3.260 μs |   3.267 μs |  0.97 | 0.0877 | 0.0420 |   1.48 KB |        0.96 |
|      Linq |             .NET 7.0 |  31.967 μs |  33.515 μs |  9.48 | 0.3662 | 0.1221 |   6.22 KB |        4.03 |
|  Compiled |             .NET 7.0 |   8.129 μs |   8.245 μs |  2.41 | 0.1755 | 0.0839 |   2.98 KB |        1.93 |
| RawAdoNet |             .NET 7.0 |   2.972 μs |   2.973 μs |  0.88 | 0.0839 | 0.0763 |   1.48 KB |        0.96 |
|      Linq |        .NET Core 3.1 |  66.466 μs |  69.101 μs | 19.76 | 0.5493 | 0.1831 |   9.38 KB |        6.07 |
|  Compiled |        .NET Core 3.1 |  10.897 μs |  11.029 μs |  3.23 | 0.1678 | 0.0763 |   2.97 KB |        1.92 |
| RawAdoNet |        .NET Core 3.1 |   3.855 μs |   3.886 μs |  1.15 | 0.0839 | 0.0381 |   1.48 KB |        0.96 |
|      Linq | .NET Framework 4.7.2 | 101.816 μs | 106.077 μs | 30.11 | 1.5869 | 0.2441 |  10.38 KB |        6.73 |
|  Compiled | .NET Framework 4.7.2 |  13.147 μs |  13.493 μs |  3.90 | 0.5188 | 0.1526 |   3.25 KB |        2.11 |
| RawAdoNet | .NET Framework 4.7.2 |   3.443 μs |   3.463 μs |  1.00 | 0.2441 | 0.1221 |   1.54 KB |        1.00 |
