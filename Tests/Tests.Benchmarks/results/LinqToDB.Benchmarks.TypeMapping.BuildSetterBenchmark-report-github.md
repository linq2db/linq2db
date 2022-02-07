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
|                  Method |              Runtime |      Mean | Ratio | Allocated |
|------------------------ |--------------------- |----------:|------:|----------:|
|        TypeMapperAsEnum |             .NET 5.0 | 12.419 ns |  9.50 |         - |
|      DirectAccessAsEnum |             .NET 5.0 |  1.081 ns |  0.83 |         - |
|   TypeMapperAsKnownEnum |             .NET 5.0 |  2.417 ns |  1.84 |         - |
| DirectAccessAsKnownEnum |             .NET 5.0 |  1.055 ns |  0.80 |         - |
|      TypeMapperAsString |             .NET 5.0 |  4.231 ns |  3.22 |         - |
|    DirectAccessAsString |             .NET 5.0 |  3.005 ns |  2.30 |         - |
|        TypeMapperAsEnum |        .NET Core 3.1 | 13.318 ns | 10.14 |         - |
|      DirectAccessAsEnum |        .NET Core 3.1 |  1.052 ns |  0.80 |         - |
|   TypeMapperAsKnownEnum |        .NET Core 3.1 |  2.734 ns |  2.09 |         - |
| DirectAccessAsKnownEnum |        .NET Core 3.1 |  1.085 ns |  0.83 |         - |
|      TypeMapperAsString |        .NET Core 3.1 |  4.537 ns |  3.45 |         - |
|    DirectAccessAsString |        .NET Core 3.1 |  2.919 ns |  2.22 |         - |
|        TypeMapperAsEnum | .NET Framework 4.7.2 | 29.703 ns | 22.75 |         - |
|      DirectAccessAsEnum | .NET Framework 4.7.2 |  1.313 ns |  1.00 |         - |
|   TypeMapperAsKnownEnum | .NET Framework 4.7.2 |  8.823 ns |  6.71 |         - |
| DirectAccessAsKnownEnum | .NET Framework 4.7.2 |  1.311 ns |  1.00 |         - |
|      TypeMapperAsString | .NET Framework 4.7.2 | 10.416 ns |  7.93 |         - |
|    DirectAccessAsString | .NET Framework 4.7.2 |  3.205 ns |  2.45 |         - |
