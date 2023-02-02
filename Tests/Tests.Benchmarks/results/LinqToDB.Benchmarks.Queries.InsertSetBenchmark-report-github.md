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
| Method |              Runtime |     Mean | Allocated |
|------- |--------------------- |---------:|----------:|
|   Test |             .NET 6.0 | 1.684 ms | 592.76 KB |
|   Test |             .NET 7.0 | 1.650 ms | 592.08 KB |
|   Test |        .NET Core 3.1 | 2.325 ms | 592.75 KB |
|   Test | .NET Framework 4.7.2 | 2.531 ms | 933.57 KB |
