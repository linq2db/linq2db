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
|        Method |              Runtime |     Mean | Ratio | Allocated |
|-------------- |--------------------- |---------:|------:|----------:|
|          Linq |             .NET 5.0 | 3.220 ms |  0.72 |      1 MB |
|     LinqAsync |             .NET 5.0 | 3.928 ms |  0.88 |      1 MB |
|      Compiled |             .NET 5.0 | 3.220 ms |  0.73 |      1 MB |
| CompiledAsync |             .NET 5.0 | 3.673 ms |  0.83 |      1 MB |
|          Linq |        .NET Core 3.1 | 3.738 ms |  0.85 |      1 MB |
|     LinqAsync |        .NET Core 3.1 | 4.758 ms |  1.08 |      1 MB |
|      Compiled |        .NET Core 3.1 | 3.580 ms |  0.82 |      1 MB |
| CompiledAsync |        .NET Core 3.1 | 4.399 ms |  1.00 |      1 MB |
|          Linq | .NET Framework 4.7.2 | 4.432 ms |  0.99 |      1 MB |
|     LinqAsync | .NET Framework 4.7.2 | 6.958 ms |  1.58 |      1 MB |
|      Compiled | .NET Framework 4.7.2 | 4.419 ms |  1.00 |      1 MB |
| CompiledAsync | .NET Framework 4.7.2 | 5.519 ms |  1.24 |      1 MB |
