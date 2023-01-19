``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-UZBSVL : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-AYZXIO : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-NXXYQT : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-HMCTKM : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method |              Runtime |       Mean | Ratio |     Gen0 |    Gen1 | Allocated | Alloc Ratio |
|------- |--------------------- |-----------:|------:|---------:|--------:|----------:|------------:|
|   Test |             .NET 6.0 |   919.6 μs |  0.31 |  35.1563 |  3.9063 | 611.97 KB |        0.64 |
|   Test |             .NET 7.0 | 1,875.5 μs |  0.63 |  37.1094 |  7.8125 | 611.47 KB |        0.64 |
|   Test |        .NET Core 3.1 | 2,290.6 μs |  0.77 |  35.1563 |  3.9063 | 614.64 KB |        0.64 |
|   Test | .NET Framework 4.7.2 | 2,978.8 μs |  1.00 | 152.3438 | 27.3438 |    953 KB |        1.00 |
