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
|    Method |              Runtime |      Mean |    Median | Ratio |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|---------- |--------------------- |----------:|----------:|------:|-------:|-------:|----------:|------------:|
|      Linq |             .NET 6.0 | 36.816 μs | 36.937 μs | 18.56 | 0.5493 | 0.1831 |   9.41 KB |        6.09 |
|  Compiled |             .NET 6.0 |  5.593 μs |  5.590 μs |  2.83 | 0.1755 | 0.0839 |   2.98 KB |        1.93 |
| RawAdoNet |             .NET 6.0 |  1.600 μs |  1.591 μs |  0.80 | 0.0896 | 0.0439 |   1.48 KB |        0.96 |
|      Linq |             .NET 7.0 | 23.856 μs | 23.792 μs | 12.07 | 0.3662 | 0.1221 |   6.22 KB |        4.03 |
|  Compiled |             .NET 7.0 |  5.651 μs |  5.647 μs |  2.85 | 0.1755 | 0.0839 |   2.98 KB |        1.93 |
| RawAdoNet |             .NET 7.0 |  1.574 μs |  1.581 μs |  0.78 | 0.0896 | 0.0877 |   1.48 KB |        0.96 |
|      Linq |        .NET Core 3.1 | 47.533 μs | 47.734 μs | 23.49 | 0.5493 | 0.1831 |   9.38 KB |        6.07 |
|  Compiled |        .NET Core 3.1 |  6.835 μs |  6.898 μs |  3.41 | 0.1755 | 0.0839 |   2.97 KB |        1.92 |
| RawAdoNet |        .NET Core 3.1 |  1.908 μs |  1.912 μs |  0.95 | 0.0896 | 0.0439 |   1.48 KB |        0.96 |
|      Linq | .NET Framework 4.7.2 | 64.199 μs | 64.242 μs | 31.99 | 1.5869 | 0.2441 |  10.38 KB |        6.73 |
|  Compiled | .NET Framework 4.7.2 |  8.616 μs |  8.767 μs |  4.31 | 0.5188 | 0.1678 |   3.25 KB |        2.11 |
| RawAdoNet | .NET Framework 4.7.2 |  2.026 μs |  1.907 μs |  1.00 | 0.2499 | 0.1240 |   1.54 KB |        1.00 |
