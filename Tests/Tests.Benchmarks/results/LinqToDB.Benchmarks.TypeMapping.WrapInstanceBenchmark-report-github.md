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
|       Method |              Runtime |       Mean |     Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|------------- |--------------------- |-----------:|-----------:|------:|-------:|----------:|------------:|
|   TypeMapper |             .NET 6.0 | 41.1788 ns | 41.4007 ns |     ? | 0.0019 |      32 B |           ? |
| DirectAccess |             .NET 6.0 |  0.0174 ns |  0.0000 ns |     ? |      - |         - |           ? |
|   TypeMapper |             .NET 7.0 | 39.3519 ns | 39.1986 ns |     ? | 0.0019 |      32 B |           ? |
| DirectAccess |             .NET 7.0 |  1.4511 ns |  1.4113 ns |     ? |      - |         - |           ? |
|   TypeMapper |        .NET Core 3.1 | 46.9589 ns | 48.3093 ns |     ? | 0.0019 |      32 B |           ? |
| DirectAccess |        .NET Core 3.1 |  0.0380 ns |  0.0506 ns |     ? |      - |         - |           ? |
|   TypeMapper | .NET Framework 4.7.2 | 53.3394 ns | 57.6210 ns |     ? | 0.0051 |      32 B |           ? |
| DirectAccess | .NET Framework 4.7.2 |  0.1400 ns |  0.0000 ns |     ? |      - |         - |           ? |
