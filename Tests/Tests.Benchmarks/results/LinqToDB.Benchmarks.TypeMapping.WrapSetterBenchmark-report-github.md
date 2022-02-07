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
|              Method |              Runtime |      Mean | Ratio | Allocated |
|-------------------- |--------------------- |----------:|------:|----------:|
|    TypeMapperString |             .NET 5.0 |  8.032 ns |  2.73 |         - |
|  DirectAccessString |             .NET 5.0 |  3.195 ns |  1.09 |         - |
|       TypeMapperInt |             .NET 5.0 |  5.773 ns |  1.96 |         - |
|     DirectAccessInt |             .NET 5.0 |  1.020 ns |  0.35 |         - |
|   TypeMapperBoolean |             .NET 5.0 |  6.185 ns |  2.10 |         - |
| DirectAccessBoolean |             .NET 5.0 |  1.080 ns |  0.37 |         - |
|   TypeMapperWrapper |             .NET 5.0 |  8.763 ns |  2.97 |         - |
| DirectAccessWrapper |             .NET 5.0 |  2.984 ns |  1.01 |         - |
|    TypeMapperString |        .NET Core 3.1 |  7.970 ns |  2.70 |         - |
|  DirectAccessString |        .NET Core 3.1 |  2.916 ns |  0.99 |         - |
|       TypeMapperInt |        .NET Core 3.1 |  5.873 ns |  1.99 |         - |
|     DirectAccessInt |        .NET Core 3.1 |  1.074 ns |  0.36 |         - |
|   TypeMapperBoolean |        .NET Core 3.1 |  5.890 ns |  2.00 |         - |
| DirectAccessBoolean |        .NET Core 3.1 |  1.337 ns |  0.45 |         - |
|   TypeMapperWrapper |        .NET Core 3.1 |  8.442 ns |  2.87 |         - |
| DirectAccessWrapper |        .NET Core 3.1 |  2.650 ns |  0.90 |         - |
|    TypeMapperString | .NET Framework 4.7.2 | 20.967 ns |  7.16 |         - |
|  DirectAccessString | .NET Framework 4.7.2 |  2.948 ns |  1.00 |         - |
|       TypeMapperInt | .NET Framework 4.7.2 | 19.235 ns |  6.53 |         - |
|     DirectAccessInt | .NET Framework 4.7.2 |  1.077 ns |  0.37 |         - |
|   TypeMapperBoolean | .NET Framework 4.7.2 | 19.205 ns |  6.52 |         - |
| DirectAccessBoolean | .NET Framework 4.7.2 |  1.084 ns |  0.37 |         - |
|   TypeMapperWrapper | .NET Framework 4.7.2 | 28.006 ns |  9.52 |         - |
| DirectAccessWrapper | .NET Framework 4.7.2 |  3.230 ns |  1.10 |         - |
