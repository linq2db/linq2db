``` ini

BenchmarkDotNet=v0.12.1.1533-nightly, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-GUCTZK : .NET 5.0.5 (5.0.521.16609), X64 RyuJIT
  Job-IOHEYN : .NET Core 3.1.14 (CoreCLR 4.700.21.16201, CoreFX 4.700.21.16208), X64 RyuJIT
  Job-FWTWYQ : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|       Method |              Runtime |       Mean |     Median | Ratio | Allocated |
|------------- |--------------------- |-----------:|-----------:|------:|----------:|
|   TypeMapper |             .NET 5.0 | 39.5846 ns | 39.0235 ns |     ? |      32 B |
| DirectAccess |             .NET 5.0 |  0.0000 ns |  0.0000 ns |     ? |         - |
|   TypeMapper |        .NET Core 3.1 | 42.2185 ns | 42.2929 ns |     ? |      32 B |
| DirectAccess |        .NET Core 3.1 |  0.0052 ns |  0.0000 ns |     ? |         - |
|   TypeMapper | .NET Framework 4.7.2 | 50.0293 ns | 50.5452 ns |     ? |      32 B |
| DirectAccess | .NET Framework 4.7.2 |  0.0000 ns |  0.0000 ns |     ? |         - |
