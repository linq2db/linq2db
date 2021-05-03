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
|                          Method |              Runtime |       Mean |     Median | Ratio | Allocated |
|-------------------------------- |--------------------- |-----------:|-----------:|------:|----------:|
|                TypeMapperAction |             .NET 5.0 |  5.6727 ns |  5.6421 ns |  5.26 |         - |
|              DirectAccessAction |             .NET 5.0 |  1.0896 ns |  1.0767 ns |  1.01 |         - |
|        TypeMapperActionWithCast |             .NET 5.0 |  4.8383 ns |  4.7625 ns |  4.60 |         - |
|      DirectAccessActionWithCast |             .NET 5.0 |  0.5716 ns |  0.5364 ns |  0.57 |         - |
|   TypeMapperActionWithParameter |             .NET 5.0 |  6.3273 ns |  6.2333 ns |  5.86 |         - |
| DirectAccessActionWithParameter |             .NET 5.0 |  1.3345 ns |  1.3315 ns |  1.24 |         - |
|                TypeMapperAction |        .NET Core 3.1 |  5.2531 ns |  5.3039 ns |  4.87 |         - |
|              DirectAccessAction |        .NET Core 3.1 |  1.3239 ns |  1.3279 ns |  1.23 |         - |
|        TypeMapperActionWithCast |        .NET Core 3.1 |  4.2435 ns |  4.2206 ns |  4.00 |         - |
|      DirectAccessActionWithCast |        .NET Core 3.1 |  0.5284 ns |  0.5245 ns |  0.49 |         - |
|   TypeMapperActionWithParameter |        .NET Core 3.1 |  6.3404 ns |  6.2169 ns |  6.07 |         - |
| DirectAccessActionWithParameter |        .NET Core 3.1 |  1.3266 ns |  1.3278 ns |  1.23 |         - |
|                TypeMapperAction | .NET Framework 4.7.2 | 19.5822 ns | 19.3910 ns | 18.27 |         - |
|              DirectAccessAction | .NET Framework 4.7.2 |  1.0780 ns |  1.0733 ns |  1.00 |         - |
|        TypeMapperActionWithCast | .NET Framework 4.7.2 | 13.6120 ns | 13.4798 ns | 12.76 |         - |
|      DirectAccessActionWithCast | .NET Framework 4.7.2 |  1.0849 ns |  1.0840 ns |  1.01 |         - |
|   TypeMapperActionWithParameter | .NET Framework 4.7.2 | 20.2726 ns | 20.2621 ns | 18.79 |         - |
| DirectAccessActionWithParameter | .NET Framework 4.7.2 |  1.0889 ns |  1.0927 ns |  1.01 |         - |
