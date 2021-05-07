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
|                          Method |              Runtime |       Mean | Ratio | Allocated |
|-------------------------------- |--------------------- |-----------:|------:|----------:|
|                TypeMapperAction |             .NET 5.0 |  5.5203 ns |  4.20 |         - |
|              DirectAccessAction |             .NET 5.0 |  1.0461 ns |  0.80 |         - |
|        TypeMapperActionWithCast |             .NET 5.0 |  4.5804 ns |  3.49 |         - |
|      DirectAccessActionWithCast |             .NET 5.0 |  0.5207 ns |  0.40 |         - |
|   TypeMapperActionWithParameter |             .NET 5.0 |  6.1771 ns |  4.70 |         - |
| DirectAccessActionWithParameter |             .NET 5.0 |  1.3937 ns |  1.06 |         - |
|                TypeMapperAction |        .NET Core 3.1 |  5.0313 ns |  3.83 |         - |
|              DirectAccessAction |        .NET Core 3.1 |  1.1062 ns |  0.84 |         - |
|        TypeMapperActionWithCast |        .NET Core 3.1 |  3.7461 ns |  2.85 |         - |
|      DirectAccessActionWithCast |        .NET Core 3.1 |  0.5416 ns |  0.41 |         - |
|   TypeMapperActionWithParameter |        .NET Core 3.1 |  6.0427 ns |  4.68 |         - |
| DirectAccessActionWithParameter |        .NET Core 3.1 |  1.0936 ns |  0.83 |         - |
|                TypeMapperAction | .NET Framework 4.7.2 | 19.6164 ns | 14.93 |         - |
|              DirectAccessAction | .NET Framework 4.7.2 |  1.3136 ns |  1.00 |         - |
|        TypeMapperActionWithCast | .NET Framework 4.7.2 | 13.5760 ns | 10.34 |         - |
|      DirectAccessActionWithCast | .NET Framework 4.7.2 |  1.3167 ns |  1.00 |         - |
|   TypeMapperActionWithParameter | .NET Framework 4.7.2 | 19.4356 ns | 14.80 |         - |
| DirectAccessActionWithParameter | .NET Framework 4.7.2 |  1.3507 ns |  1.03 |         - |
