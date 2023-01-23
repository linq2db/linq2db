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
|    Method |              Runtime |     Mean | Allocated |
|---------- |--------------------- |---------:|----------:|
|      Linq |             .NET 6.0 | 19.58 ms |   7.95 MB |
|  Compiled |             .NET 6.0 | 19.78 ms |   7.94 MB |
| RawAdoNet |             .NET 6.0 | 18.42 ms |   7.94 MB |
|      Linq |             .NET 7.0 | 19.58 ms |   7.95 MB |
|  Compiled |             .NET 7.0 | 17.84 ms |   7.94 MB |
| RawAdoNet |             .NET 7.0 | 16.31 ms |   7.94 MB |
|      Linq |        .NET Core 3.1 | 18.51 ms |   7.95 MB |
|  Compiled |        .NET Core 3.1 | 20.14 ms |   7.94 MB |
| RawAdoNet |        .NET Core 3.1 | 18.22 ms |   7.94 MB |
|      Linq | .NET Framework 4.7.2 | 26.53 ms |   7.97 MB |
|  Compiled | .NET Framework 4.7.2 | 35.62 ms |   7.97 MB |
| RawAdoNet | .NET Framework 4.7.2 | 21.77 ms |   7.96 MB |
