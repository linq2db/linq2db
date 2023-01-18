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
| Method |              Runtime |     Mean |   Median | Ratio |     Gen0 |    Gen1 | Allocated | Alloc Ratio |
|------- |--------------------- |---------:|---------:|------:|---------:|--------:|----------:|------------:|
|   Test |             .NET 6.0 | 2.028 ms | 2.045 ms |  0.75 |  35.1563 |  3.9063 | 593.39 KB |        0.64 |
|   Test |             .NET 7.0 | 1.904 ms | 1.899 ms |  0.70 |  36.1328 |  5.8594 | 592.71 KB |        0.64 |
|   Test |        .NET Core 3.1 | 2.076 ms | 2.059 ms |  0.77 |  35.1563 |  3.9063 | 591.06 KB |        0.64 |
|   Test | .NET Framework 4.7.2 | 2.719 ms | 2.649 ms |  1.00 | 148.4375 | 27.3438 | 930.47 KB |        1.00 |
