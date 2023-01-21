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
|       Method |              Runtime |      Mean |    Median | Ratio | Allocated | Alloc Ratio |
|------------- |--------------------- |----------:|----------:|------:|----------:|------------:|
|    BuildFunc |             .NET 6.0 |  3.864 ns |  4.113 ns |     ? |         - |           ? |
| DirectAccess |             .NET 6.0 |  2.540 ns |  2.880 ns |     ? |         - |           ? |
|    BuildFunc |             .NET 7.0 |  4.471 ns |  5.491 ns |     ? |         - |           ? |
| DirectAccess |             .NET 7.0 |  3.134 ns |  3.316 ns |     ? |         - |           ? |
|    BuildFunc |        .NET Core 3.1 |  5.146 ns |  6.015 ns |     ? |         - |           ? |
| DirectAccess |        .NET Core 3.1 |  1.937 ns |  2.223 ns |     ? |         - |           ? |
|    BuildFunc | .NET Framework 4.7.2 | 13.544 ns | 16.145 ns |     ? |         - |           ? |
| DirectAccess | .NET Framework 4.7.2 |  2.285 ns |  2.480 ns |     ? |         - |           ? |
