``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-RNZPMW : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XCCWXF : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WSMVMG : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-FMTKFQ : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method |              Runtime |     Mean |   Median | Ratio |     Gen0 |    Gen1 | Allocated | Alloc Ratio |
|------- |--------------------- |---------:|---------:|------:|---------:|--------:|----------:|------------:|
|   Test |             .NET 6.0 | 2.379 ms | 2.500 ms |  0.75 |  35.1563 |  5.8594 | 592.59 KB |        0.64 |
|   Test |             .NET 7.0 | 2.237 ms | 2.296 ms |  0.71 |  35.1563 |  3.9063 | 592.08 KB |        0.64 |
|   Test |        .NET Core 3.1 | 2.983 ms | 3.220 ms |  0.94 |  35.1563 |  3.9063 | 592.58 KB |        0.64 |
|   Test | .NET Framework 4.7.2 | 3.374 ms | 3.511 ms |  1.00 | 148.4375 | 27.3438 | 931.29 KB |        1.00 |
