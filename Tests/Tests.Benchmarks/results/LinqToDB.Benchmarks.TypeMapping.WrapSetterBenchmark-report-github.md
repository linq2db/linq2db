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
|              Method |              Runtime |      Mean |    Median | Ratio | Allocated |
|-------------------- |--------------------- |----------:|----------:|------:|----------:|
|    TypeMapperString |             .NET 5.0 |  7.928 ns |  7.935 ns |  2.50 |         - |
|  DirectAccessString |             .NET 5.0 |  3.194 ns |  3.200 ns |  1.01 |         - |
|       TypeMapperInt |             .NET 5.0 |  5.865 ns |  5.839 ns |  1.85 |         - |
|     DirectAccessInt |             .NET 5.0 |  1.074 ns |  1.050 ns |  0.34 |         - |
|   TypeMapperBoolean |             .NET 5.0 |  6.258 ns |  6.246 ns |  1.98 |         - |
| DirectAccessBoolean |             .NET 5.0 |  1.083 ns |  1.076 ns |  0.34 |         - |
|   TypeMapperWrapper |             .NET 5.0 |  8.870 ns |  8.863 ns |  2.79 |         - |
| DirectAccessWrapper |             .NET 5.0 |  2.876 ns |  2.869 ns |  0.91 |         - |
|    TypeMapperString |        .NET Core 3.1 |  7.664 ns |  7.592 ns |  2.41 |         - |
|  DirectAccessString |        .NET Core 3.1 |  2.718 ns |  2.653 ns |  0.88 |         - |
|       TypeMapperInt |        .NET Core 3.1 |  5.809 ns |  5.808 ns |  1.83 |         - |
|     DirectAccessInt |        .NET Core 3.1 |  1.074 ns |  1.078 ns |  0.34 |         - |
|   TypeMapperBoolean |        .NET Core 3.1 |  6.001 ns |  5.970 ns |  1.89 |         - |
| DirectAccessBoolean |        .NET Core 3.1 |  1.049 ns |  1.052 ns |  0.33 |         - |
|   TypeMapperWrapper |        .NET Core 3.1 |  8.524 ns |  8.541 ns |  2.68 |         - |
| DirectAccessWrapper |        .NET Core 3.1 |  2.909 ns |  2.911 ns |  0.92 |         - |
|    TypeMapperString | .NET Framework 4.7.2 | 21.277 ns | 21.283 ns |  6.70 |         - |
|  DirectAccessString | .NET Framework 4.7.2 |  3.176 ns |  3.172 ns |  1.00 |         - |
|       TypeMapperInt | .NET Framework 4.7.2 | 19.385 ns | 19.353 ns |  6.10 |         - |
|     DirectAccessInt | .NET Framework 4.7.2 |  1.316 ns |  1.314 ns |  0.41 |         - |
|   TypeMapperBoolean | .NET Framework 4.7.2 | 19.723 ns | 19.415 ns |  6.26 |         - |
| DirectAccessBoolean | .NET Framework 4.7.2 |  1.311 ns |  1.311 ns |  0.41 |         - |
|   TypeMapperWrapper | .NET Framework 4.7.2 | 28.366 ns | 27.949 ns |  9.01 |         - |
| DirectAccessWrapper | .NET Framework 4.7.2 |  3.166 ns |  3.164 ns |  1.00 |         - |
