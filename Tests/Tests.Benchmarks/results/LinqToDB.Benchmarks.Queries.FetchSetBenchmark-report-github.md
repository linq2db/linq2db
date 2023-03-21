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
|    Method |              Runtime |     Mean | Allocated |
|---------- |--------------------- |---------:|----------:|
|      Linq |             .NET 6.0 | 16.42 ms |   7.95 MB |
|  Compiled |             .NET 6.0 | 17.33 ms |   7.94 MB |
| RawAdoNet |             .NET 6.0 | 14.76 ms |   7.94 MB |
|      Linq |             .NET 7.0 | 17.10 ms |   7.95 MB |
|  Compiled |             .NET 7.0 | 16.42 ms |   7.94 MB |
| RawAdoNet |             .NET 7.0 | 16.28 ms |   7.94 MB |
|      Linq |        .NET Core 3.1 | 17.41 ms |   7.95 MB |
|  Compiled |        .NET Core 3.1 | 16.92 ms |   7.94 MB |
| RawAdoNet |        .NET Core 3.1 | 16.20 ms |   7.94 MB |
|      Linq | .NET Framework 4.7.2 | 30.45 ms |   7.97 MB |
|  Compiled | .NET Framework 4.7.2 | 34.26 ms |   7.97 MB |
| RawAdoNet | .NET Framework 4.7.2 | 20.42 ms |   7.96 MB |
