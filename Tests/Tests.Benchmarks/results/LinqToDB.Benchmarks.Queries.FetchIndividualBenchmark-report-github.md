``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-XCPGVR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-RHOQGE : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WEVYVV : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-ORXRGX : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|    Method |              Runtime |       Mean |     Median | Ratio |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|---------- |--------------------- |-----------:|-----------:|------:|-------:|-------:|----------:|------------:|
|      Linq |             .NET 6.0 |  42.704 μs |  49.835 μs | 18.76 | 0.6714 | 0.1221 |  11.73 KB |        7.60 |
|  Compiled |             .NET 6.0 |   5.519 μs |   5.480 μs |  2.47 | 0.2365 | 0.1144 |   3.97 KB |        2.57 |
| RawAdoNet |             .NET 6.0 |   1.432 μs |   1.434 μs |  0.64 | 0.0896 | 0.0439 |   1.48 KB |        0.96 |
|      Linq |             .NET 7.0 |  16.087 μs |  16.093 μs |  7.29 | 0.5493 | 0.1831 |   9.32 KB |        6.04 |
|  Compiled |             .NET 7.0 |   5.555 μs |   5.589 μs |  2.52 | 0.2365 | 0.1144 |   3.96 KB |        2.57 |
| RawAdoNet |             .NET 7.0 |   1.239 μs |   1.233 μs |  0.54 | 0.0896 | 0.0877 |   1.48 KB |        0.96 |
|      Linq |        .NET Core 3.1 |  57.548 μs |  57.496 μs | 25.27 | 0.6714 | 0.1221 |  11.63 KB |        7.53 |
|  Compiled |        .NET Core 3.1 |   6.951 μs |   6.892 μs |  3.15 | 0.2365 | 0.1144 |   3.95 KB |        2.56 |
| RawAdoNet |        .NET Core 3.1 |   1.848 μs |   1.834 μs |  0.80 | 0.0877 | 0.0420 |   1.48 KB |        0.96 |
|      Linq | .NET Framework 4.7.2 | 105.437 μs | 105.111 μs | 47.79 | 2.1973 | 0.2441 |  14.17 KB |        9.18 |
|  Compiled | .NET Framework 4.7.2 |   8.969 μs |   8.808 μs |  4.26 | 0.6866 | 0.1678 |   4.24 KB |        2.75 |
| RawAdoNet | .NET Framework 4.7.2 |   2.428 μs |   1.923 μs |  1.00 | 0.2499 | 0.1240 |   1.54 KB |        1.00 |
