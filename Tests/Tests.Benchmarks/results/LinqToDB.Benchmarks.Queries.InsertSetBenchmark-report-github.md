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
| Method |              Runtime |     Mean | Ratio |     Gen0 |    Gen1 | Allocated | Alloc Ratio |
|------- |--------------------- |---------:|------:|---------:|--------:|----------:|------------:|
|   Test |             .NET 6.0 | 1.630 ms |  0.58 |  35.1563 |  5.8594 | 590.61 KB |        0.63 |
|   Test |             .NET 7.0 | 1.792 ms |  0.61 |  35.1563 |  5.8594 | 589.93 KB |        0.63 |
|   Test |        .NET Core 3.1 | 2.295 ms |  0.81 |  35.1563 |  3.9063 | 590.63 KB |        0.63 |
|   Test | .NET Framework 4.7.2 | 2.822 ms |  1.00 | 148.4375 | 27.3438 | 931.62 KB |        1.00 |
