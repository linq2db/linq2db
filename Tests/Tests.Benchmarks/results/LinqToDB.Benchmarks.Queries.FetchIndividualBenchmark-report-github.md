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
|    Method |              Runtime |      Mean | Allocated |
|---------- |--------------------- |----------:|----------:|
|      Linq |             .NET 6.0 | 17.665 μs |   9.48 KB |
|  Compiled |             .NET 6.0 |  5.490 μs |   2.89 KB |
| RawAdoNet |             .NET 6.0 |  2.348 μs |   1.48 KB |
|      Linq |             .NET 7.0 | 24.779 μs |   6.13 KB |
|  Compiled |             .NET 7.0 |  5.352 μs |   2.88 KB |
| RawAdoNet |             .NET 7.0 |  1.606 μs |   1.48 KB |
|      Linq |        .NET Core 3.1 | 33.606 μs |   8.95 KB |
|  Compiled |        .NET Core 3.1 |  7.107 μs |   2.88 KB |
| RawAdoNet |        .NET Core 3.1 |  2.507 μs |   1.48 KB |
|      Linq | .NET Framework 4.7.2 | 69.973 μs |  10.09 KB |
|  Compiled | .NET Framework 4.7.2 |  9.207 μs |   3.15 KB |
| RawAdoNet | .NET Framework 4.7.2 |  2.458 μs |   1.54 KB |
