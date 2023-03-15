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
| Method |              Runtime |     Mean | Allocated |
|------- |--------------------- |---------:|----------:|
|   Test |             .NET 6.0 | 1.836 ms | 590.64 KB |
|   Test |             .NET 7.0 | 1.736 ms | 587.78 KB |
|   Test |        .NET Core 3.1 | 2.028 ms | 590.46 KB |
|   Test | .NET Framework 4.7.2 | 2.778 ms | 931.07 KB |
