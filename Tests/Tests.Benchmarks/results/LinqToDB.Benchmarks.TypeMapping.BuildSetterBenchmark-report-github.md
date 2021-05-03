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
|                  Method |              Runtime |       Mean | Ratio | Allocated |
|------------------------ |--------------------- |-----------:|------:|----------:|
|        TypeMapperAsEnum |             .NET 5.0 | 12.0616 ns | 12.70 |         - |
|      DirectAccessAsEnum |             .NET 5.0 |  1.0683 ns |  1.16 |         - |
|   TypeMapperAsKnownEnum |             .NET 5.0 |  2.4085 ns |  2.54 |         - |
| DirectAccessAsKnownEnum |             .NET 5.0 |  1.1129 ns |  1.17 |         - |
|      TypeMapperAsString |             .NET 5.0 |  4.3306 ns |  4.56 |         - |
|    DirectAccessAsString |             .NET 5.0 |  2.9144 ns |  3.07 |         - |
|        TypeMapperAsEnum |        .NET Core 3.1 | 14.4009 ns | 15.51 |         - |
|      DirectAccessAsEnum |        .NET Core 3.1 |  1.3209 ns |  1.39 |         - |
|   TypeMapperAsKnownEnum |        .NET Core 3.1 |  2.9753 ns |  3.14 |         - |
| DirectAccessAsKnownEnum |        .NET Core 3.1 |  1.3230 ns |  1.39 |         - |
|      TypeMapperAsString |        .NET Core 3.1 |  4.8659 ns |  5.13 |         - |
|    DirectAccessAsString |        .NET Core 3.1 |  2.6704 ns |  2.82 |         - |
|        TypeMapperAsEnum | .NET Framework 4.7.2 | 28.4500 ns | 29.96 |         - |
|      DirectAccessAsEnum | .NET Framework 4.7.2 |  0.9494 ns |  1.00 |         - |
|   TypeMapperAsKnownEnum | .NET Framework 4.7.2 |  8.7910 ns |  9.28 |         - |
| DirectAccessAsKnownEnum | .NET Framework 4.7.2 |  1.0671 ns |  1.12 |         - |
|      TypeMapperAsString | .NET Framework 4.7.2 | 10.7520 ns | 11.34 |         - |
|    DirectAccessAsString | .NET Framework 4.7.2 |  3.2953 ns |  3.45 |         - |
