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
| Method |              Runtime |     Mean | Ratio | Allocated |
|------- |--------------------- |---------:|------:|----------:|
|   Test |             .NET 5.0 | 1.479 ms |  0.56 |    262 KB |
|   Test |        .NET Core 3.1 | 1.630 ms |  0.61 |    262 KB |
|   Test | .NET Framework 4.7.2 | 2.796 ms |  1.00 |    835 KB |
