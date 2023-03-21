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
|        Method |              Runtime |     Mean | Allocated |
|-------------- |--------------------- |---------:|----------:|
|          Linq |             .NET 6.0 | 1.536 ms |   1.26 MB |
|     LinqAsync |             .NET 6.0 | 2.111 ms |   1.27 MB |
|      Compiled |             .NET 6.0 | 1.397 ms |   1.25 MB |
| CompiledAsync |             .NET 6.0 | 1.674 ms |   1.26 MB |
|          Linq |             .NET 7.0 | 1.564 ms |   1.26 MB |
|     LinqAsync |             .NET 7.0 | 1.876 ms |   1.26 MB |
|      Compiled |             .NET 7.0 | 1.619 ms |   1.25 MB |
| CompiledAsync |             .NET 7.0 | 2.380 ms |   1.26 MB |
|          Linq |        .NET Core 3.1 | 1.407 ms |   1.26 MB |
|     LinqAsync |        .NET Core 3.1 | 2.911 ms |   1.27 MB |
|      Compiled |        .NET Core 3.1 | 1.376 ms |   1.25 MB |
| CompiledAsync |        .NET Core 3.1 | 2.093 ms |   1.26 MB |
|          Linq | .NET Framework 4.7.2 | 3.710 ms |   1.28 MB |
|     LinqAsync | .NET Framework 4.7.2 | 4.939 ms |   1.28 MB |
|      Compiled | .NET Framework 4.7.2 | 3.106 ms |   1.27 MB |
| CompiledAsync | .NET Framework 4.7.2 | 4.763 ms |   1.27 MB |
