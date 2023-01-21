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
|       Method |              Runtime |       Mean |     Median | Ratio | Allocated | Alloc Ratio |
|------------- |--------------------- |-----------:|-----------:|------:|----------:|------------:|
|  BuildAction |             .NET 6.0 |  1.3014 ns |  1.5143 ns |     ? |         - |           ? |
| DirectAccess |             .NET 6.0 |  1.6462 ns |  1.8764 ns |     ? |         - |           ? |
|  BuildAction |             .NET 7.0 |  1.3104 ns |  1.5832 ns |     ? |         - |           ? |
| DirectAccess |             .NET 7.0 |  0.7449 ns |  0.7485 ns |     ? |         - |           ? |
|  BuildAction |        .NET Core 3.1 |  2.0262 ns |  2.4795 ns |     ? |         - |           ? |
| DirectAccess |        .NET Core 3.1 |  1.0540 ns |  1.2132 ns |     ? |         - |           ? |
|  BuildAction | .NET Framework 4.7.2 | 12.1155 ns | 12.8646 ns |     ? |         - |           ? |
| DirectAccess | .NET Framework 4.7.2 |  0.6391 ns |  0.4510 ns |     ? |         - |           ? |
