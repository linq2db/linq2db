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
|       Method |              Runtime |     Mean | Ratio | Allocated |
|------------- |--------------------- |---------:|------:|----------:|
|  BuildAction |             .NET 5.0 | 1.630 ns |  1.56 |         - |
| DirectAccess |             .NET 5.0 | 1.074 ns |  1.03 |         - |
|  BuildAction |        .NET Core 3.1 | 1.629 ns |  1.56 |         - |
| DirectAccess |        .NET Core 3.1 | 1.075 ns |  1.03 |         - |
|  BuildAction | .NET Framework 4.7.2 | 7.773 ns |  7.44 |         - |
| DirectAccess | .NET Framework 4.7.2 | 1.045 ns |  1.00 |         - |
