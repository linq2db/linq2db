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
|       Method |              Runtime |       Mean |     Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|------------- |--------------------- |-----------:|-----------:|------:|-------:|----------:|------------:|
|   TypeMapper |             .NET 6.0 | 40.1507 ns | 40.4128 ns |     ? | 0.0019 |      32 B |           ? |
| DirectAccess |             .NET 6.0 |  0.0000 ns |  0.0000 ns |     ? |      - |         - |           ? |
|   TypeMapper |             .NET 7.0 | 38.4540 ns | 38.1852 ns |     ? | 0.0019 |      32 B |           ? |
| DirectAccess |             .NET 7.0 |  1.3702 ns |  1.3703 ns |     ? |      - |         - |           ? |
|   TypeMapper |        .NET Core 3.1 | 46.4838 ns | 46.7027 ns |     ? | 0.0019 |      32 B |           ? |
| DirectAccess |        .NET Core 3.1 |  0.0000 ns |  0.0000 ns |     ? |      - |         - |           ? |
|   TypeMapper | .NET Framework 4.7.2 | 54.6188 ns | 54.7308 ns |     ? | 0.0050 |      32 B |           ? |
| DirectAccess | .NET Framework 4.7.2 |  0.0000 ns |  0.0000 ns |     ? |      - |         - |           ? |
