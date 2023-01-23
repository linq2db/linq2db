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
|        Method |              Runtime |     Mean | Allocated |
|-------------- |--------------------- |---------:|----------:|
|          Linq |             .NET 6.0 | 1.700 ms |   1.26 MB |
|     LinqAsync |             .NET 6.0 | 2.390 ms |   1.27 MB |
|      Compiled |             .NET 6.0 | 1.556 ms |   1.26 MB |
| CompiledAsync |             .NET 6.0 | 2.384 ms |   1.26 MB |
|          Linq |             .NET 7.0 | 1.738 ms |   1.26 MB |
|     LinqAsync |             .NET 7.0 | 2.435 ms |   1.26 MB |
|      Compiled |             .NET 7.0 | 1.863 ms |   1.26 MB |
| CompiledAsync |             .NET 7.0 | 2.224 ms |   1.26 MB |
|          Linq |        .NET Core 3.1 | 2.288 ms |   1.26 MB |
|     LinqAsync |        .NET Core 3.1 | 3.194 ms |   1.27 MB |
|      Compiled |        .NET Core 3.1 | 1.676 ms |   1.26 MB |
| CompiledAsync |        .NET Core 3.1 | 3.485 ms |   1.26 MB |
|          Linq | .NET Framework 4.7.2 | 4.346 ms |   1.28 MB |
|     LinqAsync | .NET Framework 4.7.2 | 5.379 ms |   1.28 MB |
|      Compiled | .NET Framework 4.7.2 | 3.261 ms |   1.27 MB |
| CompiledAsync | .NET Framework 4.7.2 | 4.881 ms |   1.27 MB |
