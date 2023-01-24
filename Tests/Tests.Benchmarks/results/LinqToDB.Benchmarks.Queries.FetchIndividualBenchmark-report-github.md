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
|    Method |              Runtime |      Mean | Allocated |
|---------- |--------------------- |----------:|----------:|
|      Linq |             .NET 6.0 | 37.375 μs |   9.41 KB |
|  Compiled |             .NET 6.0 |  5.411 μs |   2.98 KB |
| RawAdoNet |             .NET 6.0 |  1.666 μs |   1.48 KB |
|      Linq |             .NET 7.0 | 24.482 μs |   6.22 KB |
|  Compiled |             .NET 7.0 |  5.537 μs |   2.98 KB |
| RawAdoNet |             .NET 7.0 |  1.587 μs |   1.48 KB |
|      Linq |        .NET Core 3.1 | 47.212 μs |   9.38 KB |
|  Compiled |        .NET Core 3.1 |  7.202 μs |   2.97 KB |
| RawAdoNet |        .NET Core 3.1 |  1.917 μs |   1.48 KB |
|      Linq | .NET Framework 4.7.2 | 69.426 μs |  10.57 KB |
|  Compiled | .NET Framework 4.7.2 |  9.357 μs |   3.25 KB |
| RawAdoNet | .NET Framework 4.7.2 |  1.890 μs |   1.54 KB |
