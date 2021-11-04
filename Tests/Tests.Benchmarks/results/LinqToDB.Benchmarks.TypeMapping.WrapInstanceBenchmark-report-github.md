``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417994 Hz, Resolution=292.5693 ns, Timer=TSC
.NET SDK=5.0.402
  [Host]     : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-ODZCDL : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-PCJJBI : .NET Core 3.1.20 (CoreCLR 4.700.21.47003, CoreFX 4.700.21.47101), X64 RyuJIT
  Job-HHEMGO : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|       Method |              Runtime |       Mean |     Median | Ratio | Allocated |
|------------- |--------------------- |-----------:|-----------:|------:|----------:|
|   TypeMapper |             .NET 5.0 | 36.0796 ns | 35.6939 ns |     ? |      32 B |
| DirectAccess |             .NET 5.0 |  0.0164 ns |  0.0169 ns |     ? |         - |
|   TypeMapper |        .NET Core 3.1 | 37.7359 ns | 37.6813 ns |     ? |      32 B |
| DirectAccess |        .NET Core 3.1 |  0.0052 ns |  0.0000 ns |     ? |         - |
|   TypeMapper | .NET Framework 4.7.2 | 46.2336 ns | 46.2350 ns |     ? |      32 B |
| DirectAccess | .NET Framework 4.7.2 |  0.0029 ns |  0.0000 ns |     ? |         - |
