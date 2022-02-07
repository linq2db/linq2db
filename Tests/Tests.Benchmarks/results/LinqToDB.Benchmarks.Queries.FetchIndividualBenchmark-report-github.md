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
|    Method |              Runtime |       Mean |     Median |  Ratio | Allocated |
|---------- |--------------------- |-----------:|-----------:|-------:|----------:|
|      Linq |             .NET 5.0 |  48.113 μs |  47.680 μs |  32.51 |  11,412 B |
|  Compiled |             .NET 5.0 |   5.292 μs |   5.289 μs |   3.58 |   2,480 B |
| RawAdoNet |             .NET 5.0 |   1.059 μs |   1.044 μs |   0.70 |   1,520 B |
|      Linq |        .NET Core 3.1 |  51.841 μs |  51.640 μs |  35.11 |  11,380 B |
|  Compiled |        .NET Core 3.1 |   6.229 μs |   6.218 μs |   4.21 |   2,464 B |
| RawAdoNet |        .NET Core 3.1 |   1.160 μs |   1.156 μs |   0.79 |   1,520 B |
|      Linq | .NET Framework 4.7.2 | 188.303 μs | 165.887 μs | 122.37 |  16,384 B |
|  Compiled | .NET Framework 4.7.2 |  41.005 μs |  31.890 μs |  28.72 |         - |
| RawAdoNet | .NET Framework 4.7.2 |   1.479 μs |   1.478 μs |   1.00 |   1,581 B |
