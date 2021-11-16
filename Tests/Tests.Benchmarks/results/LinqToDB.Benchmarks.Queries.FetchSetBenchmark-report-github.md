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
|    Method |              Runtime |     Mean | Ratio | Allocated |
|---------- |--------------------- |---------:|------:|----------:|
|      Linq |             .NET 5.0 | 15.12 ms |  0.96 |      8 MB |
|  Compiled |             .NET 5.0 | 14.38 ms |  0.91 |      8 MB |
| RawAdoNet |             .NET 5.0 | 13.15 ms |  0.83 |      8 MB |
|      Linq |        .NET Core 3.1 | 20.58 ms |  1.31 |      8 MB |
|  Compiled |        .NET Core 3.1 | 27.11 ms |  1.72 |      8 MB |
| RawAdoNet |        .NET Core 3.1 | 16.09 ms |  1.01 |      8 MB |
|      Linq | .NET Framework 4.7.2 | 27.83 ms |  1.78 |      8 MB |
|  Compiled | .NET Framework 4.7.2 | 28.39 ms |  1.79 |      8 MB |
| RawAdoNet | .NET Framework 4.7.2 | 15.79 ms |  1.00 |      8 MB |
